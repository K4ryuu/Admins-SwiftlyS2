using Admins.Comms.Configuration;
using Admins.Comms.Contract;
using Admins.Comms.Manager;
using Admins.Core.Contract;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Admins.Comms.Players;

public partial class GamePlayer
{
    private ISwiftlyCore Core = null!;
    private ServerComms Comms = null!;
    private IConfigurationManager ConfigurationManager = null!;
    private IOptionsMonitor<CommsConfiguration> Config = null!;

    public static Dictionary<ulong, VoiceFlagValue> OriginalVoiceFlags = [];

    public GamePlayer(ISwiftlyCore core, ServerComms comms, IOptionsMonitor<CommsConfiguration> configOptions)
    {
        Core = core;
        Comms = comms;
        Config = configOptions;

        core.Registrator.Register(this);

        core.Scheduler.RepeatBySeconds(1f, ScheduleCheck);
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        ConfigurationManager = configurationManager;
    }

    public TimeZoneInfo GetConfiguredTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.GetCurrentConfiguration()!.TimeZone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    public string FormatTimestampInTimeZone(long unixTimeMilliseconds)
    {
        var utcTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
        var timeZone = GetConfiguredTimeZone();
        var localTime = TimeZoneInfo.ConvertTime(utcTime, timeZone);
        return localTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void ScheduleCheck()
    {
        var players = Core.PlayerManager.GetAllPlayers();
        foreach (var player in players)
        {
            if (player.IsFakeClient) continue;

            if (IsPlayerMuted(player, out var sanction))
            {
                if (player.VoiceFlags != VoiceFlagValue.Muted)
                {
                    OriginalVoiceFlags[player.SteamID] = player.VoiceFlags;
                    player.VoiceFlags = VoiceFlagValue.Muted;
                    var localizer = Core.Translation.GetPlayerLocalizer(player);

                    var expiryText = sanction!.ExpiresAt == 0
                        ? localizer["never"]
                        : FormatTimestampInTimeZone((long)sanction!.ExpiresAt);

                    string muteMessage = localizer[
                        "mute.message",
                        ConfigurationManager.GetCurrentConfiguration()!.Prefix,
                        sanction!.AdminName,
                        expiryText,
                        sanction.Reason
                    ];
                    player.SendChat(muteMessage);
                }
            }
            else
            {
                if (OriginalVoiceFlags.TryGetValue(player.SteamID, out var originalFlags))
                {
                    player.VoiceFlags = originalFlags;
                    OriginalVoiceFlags.Remove(player.SteamID);
                }
            }
        }
    }

    public bool IsPlayerMuted(IPlayer player, out ISanction? activeSanction)
    {
        activeSanction = Comms.FindActiveSanction((long)player.SteamID, player.IPAddress, SanctionKind.Mute);
        return activeSanction != null;
    }

    public bool IsPlayerGagged(IPlayer player, out ISanction? activeSanction)
    {
        activeSanction = Comms.FindActiveSanction((long)player.SteamID, player.IPAddress, SanctionKind.Gag);
        return activeSanction != null;
    }

    private bool ShouldHandleAdminChat(string text, bool teamOnly)
    {
        return teamOnly
            && text.StartsWith(Config.CurrentValue.AdminChatStartCharacter)
            && Config.CurrentValue.EnableAdminChat;
    }

    private void HandleAdminChat(IPlayer sender, string text)
    {
        var hasAdminPermission = Core.Permission.PlayerHasPermission(
            sender.SteamID,
            "admins.chat"
        );

        var recipients = Core.PlayerManager.GetAllPlayers()
            .Where(p => Core.Permission.PlayerHasPermission(p.SteamID, "admins.chat"))
            .ToList();

        if (!recipients.Contains(sender))
        {
            recipients.Add(sender);
        }

        var messageContent = text.Substring(Config.CurrentValue.AdminChatStartCharacter.Length).Trim();
        var formatKey = hasAdminPermission
            ? "admins.admin_chat_format"
            : "admins.player_chat_format";

        foreach (var recipient in recipients)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(recipient);
            var message = localizer[formatKey, sender.Controller.PlayerName, messageContent];
            recipient.SendChat(message);
        }
    }

    [ClientChatHookHandler]
    public HookResult OnClientChat(int playerId, string text, bool teamonly)
    {
        var player = Core.PlayerManager.GetPlayer(playerId);
        if (player == null || player.IsFakeClient) return HookResult.Continue;

        if (ShouldHandleAdminChat(text, teamonly))
        {
            HandleAdminChat(player, text);
            return HookResult.Stop;
        }

        if (IsPlayerGagged(player, out var sanction))
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            var expiresAt = sanction!.ExpiresAt;

            var expiryText = expiresAt == 0
                            ? localizer["never"]
                            : FormatTimestampInTimeZone((long)expiresAt);

            var message = localizer[
                "gag.message",
                ConfigurationManager.GetCurrentConfiguration()!.Prefix,
                sanction.AdminName,
                expiryText,
                sanction.Reason
            ];

            player.SendChat(message);

            return HookResult.Stop;
        }

        return HookResult.Continue;
    }
}