using Admins.Bans;
using Admins.Contract;
using Admins.Database.Models;
using Dommel;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admins.Sanctions;

public partial class ServerSanctions
{
    [SwiftlyInject]
    private static ISwiftlyCore Core = null!;

    public static List<ISanction> Sanctions { get; set; } = [];
    public static Dictionary<ulong, VoiceFlagValue> OriginalVoiceFlags = new();

    public static void Load(Action? onLoaded)
    {
        if (!Admins.Config.CurrentValue.UseDatabase) return;

        Task.Run(() =>
        {
            var database = Core.Database.GetConnection("admins");
            SetSanctions([.. database.GetAll<Sanction>()]);
            onLoaded?.Invoke();
        });
    }

    public static void SetSanctions(List<ISanction> sanctions)
    {
        Sanctions = sanctions;
    }

    public static List<ISanction> GetSanctions()
    {
        return Sanctions;
    }

    public static void DatabaseFetch()
    {
        Load(ScheduleCheck);
    }

    public static void ScheduleCheck()
    {
        var players = Core.PlayerManager.GetAllPlayers();
        foreach (var player in players)
        {
            if (player.IsFakeClient) continue;
            if (!ServerBans.CheckPlayer(player)) continue;

            if (IsPlayerMuted(player, out var sanction))
            {
                if (player.VoiceFlags != VoiceFlagValue.Muted)
                {
                    OriginalVoiceFlags[player.SteamID] = player.VoiceFlags;
                    player.VoiceFlags = VoiceFlagValue.Muted;
                    var localizer = Core.Translation.GetPlayerLocalizer(player);
                    var expiryText = sanction!.ExpiresAt == 0
                        ? localizer["never"]
                        : Admins.FormatTimestampInTimeZone((long)sanction!.ExpiresAt);
                    string muteMessage = localizer[
                        "mute.message",
                        Admins.Config.CurrentValue.Prefix,
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

    public static void AddSanction(ISanction sanction)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Core.Database.GetConnection("admins");
                var id = database.Insert((Sanction)sanction);
                sanction.Id = (ulong)id;
            }
            Sanctions.Add(sanction);
            Admins.AdminSanctionsAPI.TriggerSanctionAdded(sanction);
        });
    }

    public static void RemoveSanction(ISanction sanction)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Core.Database.GetConnection("admins");
                database.Delete((Sanction)sanction);
            }
            Sanctions.Remove(sanction);
            Admins.AdminSanctionsAPI.TriggerSanctionRemoved(sanction);
        });
    }

    public static void UpdateSanction(ISanction sanction)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Core.Database.GetConnection("admins");
                database.Update((Sanction)sanction);
            }
            Sanctions.RemoveAt(Sanctions.FindIndex(s => s.Id == sanction.Id));
            Sanctions.Add(sanction);
            Admins.AdminSanctionsAPI.TriggerSanctionUpdated(sanction);
        });
    }

    public static void ClearSanctions()
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Core.Database.GetConnection("admins");
                database.DeleteAll<Sanction>();
            }
            Sanctions.Clear();
        });
    }

    public static List<ISanction> FindActiveSanctions(ulong steamId64)
    {
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Sanctions.FindAll(sanction =>
            sanction.SteamId64 == steamId64 &&
            (sanction.ExpiresAt == 0 || sanction.ExpiresAt > currentTime) &&
            (sanction.Server == Admins.ServerGUID || sanction.Global)
        );
    }

    public static bool IsPlayerMuted(IPlayer player, out ISanction? sanction)
    {
        var sanctions = FindActiveSanctions(player.SteamID);
        if (sanctions.Count == 0)
        {
            sanction = null;
            return false;
        }

        sanction = sanctions.Find(sanction => sanction.SanctionType == SanctionKind.Mute);
        return sanction != null;
    }

    public static bool IsPlayerGagged(IPlayer player, out ISanction? sanction)
    {
        var sanctions = FindActiveSanctions(player.SteamID);
        if (sanctions.Count == 0)
        {
            sanction = null;
            return false;
        }

        sanction = sanctions.Find(sanction => sanction.SanctionType == SanctionKind.Gag);
        return sanction != null;
    }

    public static void RegisterAdminSubmenu()
    {
        Admins.AdminsMenuAPI.RegisterSubmenu(
            "adminmenu.sanctions.title",
            ["admins.menu.sanctions"],
            (player, key) => Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player)[key],
            CreateSanctionsMenu
        );
    }

    #region Menu Structure

    private static IMenuAPI CreateSanctionsMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.manage_gag"], () => CreateManageGagMenu(player)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.manage_mute"], () => CreateManageMuteMenu(player)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.manage_silence"], () => CreateManageSilenceMenu(player)))
            .Build();
    }

    private static IMenuAPI CreateManageGagMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.gag"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Gag)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.ungag"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Ungag)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.globalgag"], () => CreatePlayerSelectionMenu(player, SanctionActionType.GlobalGag)))
            .Build();
    }

    private static IMenuAPI CreateManageMuteMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.mute"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Mute)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.unmute"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Unmute)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.globalmute"], () => CreatePlayerSelectionMenu(player, SanctionActionType.GlobalMute)))
            .Build();
    }

    private static IMenuAPI CreateManageSilenceMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.silence"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Silence)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.unsilence"], () => CreatePlayerSelectionMenu(player, SanctionActionType.Unsilence)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.sanctions.globalsilence"], () => CreatePlayerSelectionMenu(player, SanctionActionType.GlobalSilence)))
            .Build();
    }

    #endregion

    #region Menu Action Handlers

    private enum SanctionActionType
    {
        Gag,
        Ungag,
        GlobalGag,
        Mute,
        Unmute,
        GlobalMute,
        Silence,
        Unsilence,
        GlobalSilence
    }

    private static IMenuAPI CreatePlayerSelectionMenu(IPlayer admin, SanctionActionType action)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);
        var players = Admins.SwiftlyCore.PlayerManager.GetAllPlayers().Where(p => p.IsValid).ToList();

        var builder = Admins.SwiftlyCore.MenusAPI.CreateBuilder();

        if (players.Count == 0)
        {
            builder.AddOption(new ButtonMenuOption(localizer["adminmenu.no_players_available", Admins.Config.CurrentValue.Prefix]));
        }
        else
        {
            foreach (var target in players)
            {
                if (IsRemoveAction(action))
                {
                    builder.AddOption(new SubmenuMenuOption(target.Controller.PlayerName, () => CreateRemoveConfirmMenu(admin, target, action)));
                }
                else
                {
                    builder.AddOption(new SubmenuMenuOption(target.Controller.PlayerName, () => CreateDurationMenu(admin, target, action)));
                }
            }
        }

        return builder.Build();
    }

    private static IMenuAPI CreateRemoveConfirmMenu(IPlayer admin, IPlayer target, SanctionActionType action)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);

        ExecuteRemoveSanction(admin, target, action);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new ButtonMenuOption($"✓ {localizer["adminmenu.done"]}"))
            .SetAutoCloseDelay(5.0f)
            .Build();
    }

    private static IMenuAPI CreateDurationMenu(IPlayer admin, IPlayer target, SanctionActionType action)
    {
        var builder = Admins.SwiftlyCore.MenusAPI.CreateBuilder();
        var durations = Admins.Config.CurrentValue.MuteDurationsInSeconds;

        foreach (var durationInSeconds in durations)
        {
            var duration = TimeSpan.FromSeconds(durationInSeconds);
            var label = FormatDurationLabel(durationInSeconds);
            builder.AddOption(new SubmenuMenuOption(label, () => CreateReasonMenu(admin, target, action, duration)));
        }

        return builder.Build();
    }

    private static string FormatDurationLabel(int seconds)
    {
        if (seconds == 0)
            return "Permanent";

        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            return $"{days}d";
        }
        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            return $"{hours}h";
        }
        if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes}m";
        }
        return $"{seconds}s";
    }

    private static IMenuAPI CreateReasonMenu(IPlayer admin, IPlayer target, SanctionActionType action, TimeSpan duration)
    {
        var builder = Admins.SwiftlyCore.MenusAPI.CreateBuilder();
        var reasons = Admins.Config.CurrentValue.MuteReasons;

        foreach (var reason in reasons)
        {
            builder.AddOption(new SubmenuMenuOption(reason, () => CreateExecutionConfirmMenu(admin, target, action, duration, reason)));
        }

        return builder.Build();
    }

    private static IMenuAPI CreateExecutionConfirmMenu(IPlayer admin, IPlayer target, SanctionActionType action, TimeSpan duration, string reason)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);

        ExecuteSanction(admin, target, action, duration, reason);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new ButtonMenuOption($"✓ {localizer["adminmenu.done"]}"))
            .SetAutoCloseDelay(5.0f)
            .Build();
    }

    private static void ExecuteSanction(IPlayer admin, IPlayer target, SanctionActionType action, TimeSpan duration, string reason)
    {
        var adminName = admin.Controller.PlayerName;
        var expiresAt = duration == TimeSpan.Zero ? 0 : DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds();
        bool isGlobal = action.ToString().Contains("Global");

        switch (action)
        {
            case SanctionActionType.Gag:
            case SanctionActionType.GlobalGag:
                {
                    var sanction = new Sanction
                    {
                        SteamId64 = target.SteamID,
                        SanctionType = SanctionKind.Gag,
                        Reason = reason,
                        PlayerName = target.Controller.PlayerName,
                        PlayerIp = target.IPAddress,
                        ExpiresAt = (ulong)expiresAt,
                        Length = (ulong)duration.TotalMilliseconds,
                        AdminSteamId64 = admin.SteamID,
                        AdminName = adminName,
                        Server = Admins.ServerGUID,
                        Global = isGlobal
                    };
                    ServerSanctions.AddSanction(sanction);
                    break;
                }
            case SanctionActionType.Mute:
            case SanctionActionType.GlobalMute:
                {
                    var sanction = new Sanction
                    {
                        SteamId64 = target.SteamID,
                        SanctionType = SanctionKind.Mute,
                        Reason = reason,
                        PlayerName = target.Controller.PlayerName,
                        PlayerIp = target.IPAddress,
                        ExpiresAt = (ulong)expiresAt,
                        Length = (ulong)duration.TotalMilliseconds,
                        AdminSteamId64 = admin.SteamID,
                        AdminName = adminName,
                        Server = Admins.ServerGUID,
                        Global = isGlobal
                    };
                    ServerSanctions.AddSanction(sanction);
                    ServerSanctions.ScheduleCheck();
                    break;
                }
            case SanctionActionType.Silence:
            case SanctionActionType.GlobalSilence:
                {
                    var gagSanction = new Sanction
                    {
                        SteamId64 = target.SteamID,
                        SanctionType = SanctionKind.Gag,
                        Reason = reason,
                        PlayerName = target.Controller.PlayerName,
                        PlayerIp = target.IPAddress,
                        ExpiresAt = (ulong)expiresAt,
                        Length = (ulong)duration.TotalMilliseconds,
                        AdminSteamId64 = admin.SteamID,
                        AdminName = adminName,
                        Server = Admins.ServerGUID,
                        Global = isGlobal
                    };
                    var muteSanction = new Sanction
                    {
                        SteamId64 = target.SteamID,
                        SanctionType = SanctionKind.Mute,
                        Reason = reason,
                        PlayerName = target.Controller.PlayerName,
                        PlayerIp = target.IPAddress,
                        ExpiresAt = (ulong)expiresAt,
                        Length = (ulong)duration.TotalMilliseconds,
                        AdminSteamId64 = admin.SteamID,
                        AdminName = adminName,
                        Server = Admins.ServerGUID,
                        Global = isGlobal
                    };
                    ServerSanctions.AddSanction(gagSanction);
                    ServerSanctions.AddSanction(muteSanction);
                    ServerSanctions.ScheduleCheck();
                    break;
                }
        }

        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);
        Admins.SwiftlyCore.Scheduler.NextTick(() =>
        {
            admin.SendMessage(MessageType.Chat, localizer["adminmenu.sanction_applied", Admins.Config.CurrentValue.Prefix, target.Controller.PlayerName]);
        });
    }

    private static void ExecuteRemoveSanction(IPlayer admin, IPlayer target, SanctionActionType action)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);
        var adminName = admin.Controller.PlayerName;

        switch (action)
        {
            case SanctionActionType.Ungag:
                {
                    var sanctions = ServerSanctions.FindActiveSanctions(target.SteamID)
                        .Where(s => s.SanctionType == SanctionKind.Gag)
                        .ToList();

                    foreach (var sanction in sanctions)
                    {
                        ServerSanctions.RemoveSanction(sanction);
                    }

                    var message = localizer["command.ungag_success", Admins.Config.CurrentValue.Prefix, adminName, sanctions.Count, 1];
                    Admins.SwiftlyCore.Scheduler.NextTick(() => admin.SendMessage(MessageType.Chat, message));
                    break;
                }
            case SanctionActionType.Unmute:
                {
                    var sanctions = ServerSanctions.FindActiveSanctions(target.SteamID)
                        .Where(s => s.SanctionType == SanctionKind.Mute)
                        .ToList();

                    foreach (var sanction in sanctions)
                    {
                        ServerSanctions.RemoveSanction(sanction);
                    }

                    ServerSanctions.ScheduleCheck();
                    var message = localizer["command.unmute_success", Admins.Config.CurrentValue.Prefix, adminName, sanctions.Count, 1];
                    Admins.SwiftlyCore.Scheduler.NextTick(() => admin.SendMessage(MessageType.Chat, message));
                    break;
                }
            case SanctionActionType.Unsilence:
                {
                    var gagSanctions = ServerSanctions.FindActiveSanctions(target.SteamID)
                        .Where(s => s.SanctionType == SanctionKind.Gag)
                        .ToList();
                    var muteSanctions = ServerSanctions.FindActiveSanctions(target.SteamID)
                        .Where(s => s.SanctionType == SanctionKind.Mute)
                        .ToList();

                    foreach (var sanction in gagSanctions)
                    {
                        ServerSanctions.RemoveSanction(sanction);
                    }
                    foreach (var sanction in muteSanctions)
                    {
                        ServerSanctions.RemoveSanction(sanction);
                    }

                    ServerSanctions.ScheduleCheck();
                    var message = localizer["command.unsilence_success", Admins.Config.CurrentValue.Prefix, adminName, gagSanctions.Count, muteSanctions.Count, 1];
                    Admins.SwiftlyCore.Scheduler.NextTick(() => admin.SendMessage(MessageType.Chat, message));
                    break;
                }
        }
    }

    private static bool IsRemoveAction(SanctionActionType action)
    {
        return action == SanctionActionType.Ungag || action == SanctionActionType.Unmute || action == SanctionActionType.Unsilence;
    }

    #endregion
}