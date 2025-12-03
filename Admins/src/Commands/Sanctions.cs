using Admins.Contract;
using Admins.Database.Models;
using Admins.Sanctions;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using TimeSpanParserUtil;

namespace Admins.Commands;

public partial class AdminCommands
{
    #region Gag Commands

    [Command("gag", permission: "admins.commands.gag")]
    public void Command_Gag(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "gag", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Gag, duration, reason, isGlobal: false);
    }

    [Command("globalgag", permission: "admins.commands.globalgag")]
    public void Command_GlobalGag(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalgag", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Gag, duration, reason, isGlobal: true);
    }

    #endregion

    #region Mute Commands

    [Command("mute", permission: "admins.commands.mute")]
    public void Command_Mute(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "mute", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Mute, duration, reason, isGlobal: false);
        ServerSanctions.ScheduleCheck();
    }

    [Command("globalmute", permission: "admins.commands.globalmute")]
    public void Command_GlobalMute(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalmute", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Mute, duration, reason, isGlobal: true);
        ServerSanctions.ScheduleCheck();
    }

    #endregion

    #region Silence Commands

    [Command("silence", permission: "admins.commands.silence")]
    public void Command_Silence(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "silence", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Gag, duration, reason, isGlobal: false);
        ApplySanction(players, context, SanctionKind.Mute, duration, reason, isGlobal: false);
        ServerSanctions.ScheduleCheck();
    }

    [Command("globalsilence", permission: "admins.commands.globalsilence")]
    public void Command_GlobalSilence(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalsilence", ["<player>", "<time>", "<reason>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplySanction(players, context, SanctionKind.Gag, duration, reason, isGlobal: true);
        ApplySanction(players, context, SanctionKind.Mute, duration, reason, isGlobal: true);
        ServerSanctions.ScheduleCheck();
    }

    #endregion

    #region Ungag/Unmute Commands

    [Command("ungag", permission: "admins.commands.ungag")]
    public void Command_Ungag(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "ungag", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        RemoveSanctions(players, context, SanctionKind.Gag);
    }

    [Command("unmute", permission: "admins.commands.unmute")]
    public void Command_Unmute(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "unmute", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        RemoveSanctions(players, context, SanctionKind.Mute);
        ServerSanctions.ScheduleCheck();
    }

    [Command("unsilence", permission: "admins.commands.unsilence")]
    public void Command_Unsilence(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "unsilence", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        var gagCount = RemoveSanctionsInternal(players, SanctionKind.Gag);
        var muteCount = RemoveSanctionsInternal(players, SanctionKind.Mute);
        ServerSanctions.ScheduleCheck();

        var localizer = GetPlayerLocalizer(context);
        var adminName = GetAdminName(context);
        var message = localizer[
            "command.unsilence_success",
            Admins.Config.CurrentValue.Prefix,
            adminName,
            gagCount,
            muteCount,
            players.Count
        ];
        context.Reply(message);
    }

    #endregion

    #region Offline Sanction Commands

    [Command("gago", permission: "admins.commands.gag")]
    public void Command_GagOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "gago", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Gag, duration, reason, isGlobal: false);
    }

    [Command("muteo", permission: "admins.commands.mute")]
    public void Command_MuteOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "muteo", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Mute, duration, reason, isGlobal: false);
        ServerSanctions.ScheduleCheck();
    }

    [Command("globalgago", permission: "admins.commands.globalgag")]
    public void Command_GlobalGagOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalgago", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Gag, duration, reason, isGlobal: true);
    }

    [Command("globalmuteo", permission: "admins.commands.globalmute")]
    public void Command_GlobalMuteOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalmuteo", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Mute, duration, reason, isGlobal: true);
        ServerSanctions.ScheduleCheck();
    }

    [Command("silenceo", permission: "admins.commands.silence")]
    public void Command_SilenceOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "silenceo", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Gag, duration, reason, isGlobal: false);
        ApplyOfflineSanction(context, steamId64, SanctionKind.Mute, duration, reason, isGlobal: false);
        ServerSanctions.ScheduleCheck();
    }

    [Command("globalsilenceo", permission: "admins.commands.globalsilence")]
    public void Command_GlobalSilenceOffline(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 3, "globalsilenceo", ["<steamid64>", "<time>", "<reason>"]))
        {
            return;
        }

        if (!TryParseSteamID(context, context.Args[0], out var steamId64))
        {
            return;
        }

        if (!TryParseDuration(context, context.Args[1], out var duration))
        {
            return;
        }

        var reason = string.Join(" ", context.Args.Skip(2));
        ApplyOfflineSanction(context, steamId64, SanctionKind.Gag, duration, reason, isGlobal: true);
        ApplyOfflineSanction(context, steamId64, SanctionKind.Mute, duration, reason, isGlobal: true);
        ServerSanctions.ScheduleCheck();
    }

    #endregion

    #region Sanction Helper Methods

    private void ApplySanction(
        List<IPlayer> players,
        ICommandContext context,
        SanctionKind sanctionType,
        TimeSpan duration,
        string reason,
        bool isGlobal)
    {
        var expiresAt = CalculateExpiresAt(duration);
        var adminName = GetAdminName(context);

        foreach (var player in players)
        {
            var sanction = new Sanction
            {
                SteamId64 = player.SteamID,
                SanctionType = sanctionType,
                Reason = reason,
                PlayerName = player.Controller.PlayerName,
                PlayerIp = player.IPAddress,
                ExpiresAt = (ulong)expiresAt,
                Length = (ulong)duration.TotalMilliseconds,
                AdminSteamId64 = context.IsSentByPlayer ? context.Sender!.SteamID : 0,
                AdminName = adminName,
                Server = Admins.ServerGUID,
                Global = isGlobal
            };

            ServerSanctions.AddSanction(sanction);
        }

        NotifySanctionApplied(players, context.Sender, sanctionType, expiresAt, adminName, reason);
    }

    private void NotifySanctionApplied(
        List<IPlayer> players,
        IPlayer? sender,
        SanctionKind sanctionType,
        long expiresAt,
        string adminName,
        string reason)
    {
        var messageKey = sanctionType == SanctionKind.Gag ? "gag.message" : "mute.message";

        SendMessageToPlayers(players, sender, (player, localizer) =>
        {
            var expiryText = expiresAt == 0
                ? localizer["never"]
                : DateTimeOffset.FromUnixTimeMilliseconds(expiresAt)
                    .ToString("yyyy-MM-dd HH:mm:ss");

            var message = localizer[
                messageKey,
                Admins.Config.CurrentValue.Prefix,
                adminName,
                expiryText,
                reason
            ];

            return (message, MessageType.Chat);
        });
    }

    private void RemoveSanctions(
        List<IPlayer> players,
        ICommandContext context,
        SanctionKind sanctionType)
    {
        var removedCount = RemoveSanctionsInternal(players, sanctionType);

        var localizer = GetPlayerLocalizer(context);
        var adminName = GetAdminName(context);
        var messageKey = sanctionType == SanctionKind.Gag ? "command.ungag_success" : "command.unmute_success";
        var message = localizer[
            messageKey,
            Admins.Config.CurrentValue.Prefix,
            adminName,
            removedCount,
            players.Count
        ];
        context.Reply(message);
    }

    private int RemoveSanctionsInternal(
        List<IPlayer> players,
        SanctionKind sanctionType)
    {
        var removedCount = 0;

        foreach (var player in players)
        {
            var sanctions = ServerSanctions.FindActiveSanctions(player.SteamID)
                .Where(s => s.SanctionType == sanctionType)
                .ToList();

            foreach (var sanction in sanctions)
            {
                ServerSanctions.RemoveSanction(sanction);
                removedCount++;
            }
        }

        return removedCount;
    }

    private void ApplyOfflineSanction(
        ICommandContext context,
        ulong steamId64,
        SanctionKind sanctionType,
        TimeSpan duration,
        string reason,
        bool isGlobal)
    {
        var expiresAt = CalculateExpiresAt(duration);
        var adminName = GetAdminName(context);

        var sanction = new Sanction
        {
            SteamId64 = steamId64,
            SanctionType = sanctionType,
            Reason = reason,
            PlayerName = "Unknown",
            PlayerIp = "",
            ExpiresAt = (ulong)expiresAt,
            Length = (ulong)duration.TotalMilliseconds,
            AdminSteamId64 = context.IsSentByPlayer ? context.Sender!.SteamID : 0,
            AdminName = adminName,
            Server = Admins.ServerGUID,
            Global = isGlobal
        };

        ServerSanctions.AddSanction(sanction);

        var localizer = GetPlayerLocalizer(context);
        var messageKey = sanctionType == SanctionKind.Gag ? "command.gago_success" : "command.muteo_success";
        var expiryText = expiresAt == 0
            ? localizer["never"]
            : DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).ToString("yyyy-MM-dd HH:mm:ss");

        var sanctionTypeKey = sanctionType == SanctionKind.Gag
            ? (isGlobal ? "global_gag" : "gag")
            : (isGlobal ? "global_mute" : "mute");
        var sanctionTypeText = localizer[sanctionTypeKey];

        var message = localizer[
            messageKey,
            Admins.Config.CurrentValue.Prefix,
            adminName,
            sanctionTypeText,
            steamId64,
            expiryText,
            reason
        ];
        context.Reply(message);
    }

    #endregion
}