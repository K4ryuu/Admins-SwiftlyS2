using Admins.Contract;
using Admins.Database.Models;
using Dommel;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Admins.Bans;

public partial class ServerBans
{
    public static List<IBan> Bans { get; set; } = [];

    public static void Load(Action? onLoaded)
    {
        if (!Admins.Config.CurrentValue.UseDatabase) return;

        Task.Run(() =>
        {
            var database = Admins.SwiftlyCore.Database.GetConnection("admins");
            SetBans([.. database.GetAll<Ban>()]);
            onLoaded?.Invoke();
        });
    }

    public static void SetBans(List<IBan> bans)
    {
        Bans = bans;
    }

    public static void DatabaseFetch()
    {
        Load(null);
    }

    public static List<IBan> GetBans()
    {
        return Bans;
    }

    public static void AddBan(IBan ban)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Admins.SwiftlyCore.Database.GetConnection("admins");
                var id = database.Insert((Ban)ban);
                ban.Id = (ulong)id;
            }
            Bans.Add(ban);
            Admins.AdminBansAPI.TriggerBanAdded(ban);
        });
    }

    public static void RemoveBan(IBan ban)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Admins.SwiftlyCore.Database.GetConnection("admins");
                database.Delete((Ban)ban);
            }
            Bans.Remove(ban);
            Admins.AdminBansAPI.TriggerBanRemoved(ban);
        });
    }

    public static void UpdateBan(IBan ban)
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Admins.SwiftlyCore.Database.GetConnection("admins");
                database.Update((Ban)ban);
            }

            Bans.RemoveAt(Bans.FindIndex(b => b.Id == ban.Id));
            Bans.Add(ban);
            Admins.AdminBansAPI.TriggerBanUpdated(ban);
        });
    }

    public static void ClearBans()
    {
        Task.Run(() =>
        {
            if (Admins.Config.CurrentValue.UseDatabase)
            {
                var database = Admins.SwiftlyCore.Database.GetConnection("admins");
                database.DeleteAll<Ban>();
            }
            Bans.Clear();
        });
    }

    public static IBan? FindActiveBan(ulong steamId64, string playerIp)
    {
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Bans.Find(ban =>
            ((ban.SteamId64 == steamId64 && ban.BanType == BanType.SteamID) || (!string.IsNullOrEmpty(playerIp) && ban.PlayerIp == playerIp && ban.BanType == BanType.IP)) &&
            (ban.ExpiresAt == 0 || ban.ExpiresAt > currentTime) &&
            (ban.Server == Admins.ServerGUID || ban.GlobalBan)
        );
    }

    public static bool CheckPlayer(IPlayer player)
    {
        var ban = FindActiveBan(player.SteamID, player.IPAddress);
        if (ban != null)
        {
            var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);
            string kickMessage = localizer[
                "ban.kick_message",
                ban.Reason,
                ban.ExpiresAt == 0 ? localizer["never"] : DateTimeOffset.FromUnixTimeMilliseconds((long)ban.ExpiresAt).ToString("yyyy-MM-dd HH:mm:ss"),
                ban.AdminName,
                ban.AdminSteamId64.ToString()
            ];
            player.SendMessage(MessageType.Console, kickMessage);

            Admins.SwiftlyCore.Scheduler.NextTick(() =>
            {
                player.Kick(kickMessage, SwiftlyS2.Shared.ProtobufDefinitions.ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
            });
            return false;
        }

        return true;
    }

    public static void RegisterAdminSubmenu()
    {
        Admins.AdminsMenuAPI.RegisterSubmenu(
            "adminmenu.bans.title",
            ["admins.menu.bans"],
            (player, key) => Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player)[key],
            CreateBansMenu
        );
    }

    #region Menu Structure

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateBansMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.manage_ban"], () => CreateManageBanMenu(player)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.manage_banip"], () => CreateManageBanIPMenu(player)))
            .Build();
    }

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateManageBanMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.ban"], () => CreatePlayerSelectionMenu(player, BanActionType.Ban)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.unban"], () => CreatePlayerSelectionMenu(player, BanActionType.Unban)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.globalban"], () => CreatePlayerSelectionMenu(player, BanActionType.GlobalBan)))
            .Build();
    }

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateManageBanIPMenu(IPlayer player)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(player);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.banip"], () => CreatePlayerSelectionMenu(player, BanActionType.BanIP)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.unbanip"], () => CreatePlayerSelectionMenu(player, BanActionType.UnbanIP)))
            .AddOption(new SubmenuMenuOption(localizer["adminmenu.bans.globalbanip"], () => CreatePlayerSelectionMenu(player, BanActionType.GlobalBanIP)))
            .Build();
    }

    #endregion

    #region Menu Action Handlers

    private enum BanActionType
    {
        Ban,
        Unban,
        GlobalBan,
        BanIP,
        UnbanIP,
        GlobalBanIP
    }

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreatePlayerSelectionMenu(IPlayer admin, BanActionType action)
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

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateRemoveConfirmMenu(IPlayer admin, IPlayer target, BanActionType action)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);

        ExecuteRemoveBan(admin, target, action);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new ButtonMenuOption($"✓ {localizer["adminmenu.done"]}"))
            .SetAutoCloseDelay(5.0f)
            .Build();
    }

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateDurationMenu(IPlayer admin, IPlayer target, BanActionType action)
    {
        var builder = Admins.SwiftlyCore.MenusAPI.CreateBuilder();
        var durations = Admins.Config.CurrentValue.BansDurationsInSeconds;

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

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateReasonMenu(IPlayer admin, IPlayer target, BanActionType action, TimeSpan duration)
    {
        var builder = Admins.SwiftlyCore.MenusAPI.CreateBuilder();
        var reasons = Admins.Config.CurrentValue.BansReasons;

        foreach (var reason in reasons)
        {
            builder.AddOption(new SubmenuMenuOption(reason, () => CreateExecutionConfirmMenu(admin, target, action, duration, reason)));
        }

        return builder.Build();
    }

    private static SwiftlyS2.Shared.Menus.IMenuAPI CreateExecutionConfirmMenu(IPlayer admin, IPlayer target, BanActionType action, TimeSpan duration, string reason)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);

        ExecuteBan(admin, target, action, duration, reason);

        return Admins.SwiftlyCore.MenusAPI.CreateBuilder()
            .AddOption(new ButtonMenuOption($"✓ {localizer["adminmenu.done"]}"))
            .SetAutoCloseDelay(5.0f)
            .Build();
    }

    private static void ExecuteBan(IPlayer admin, IPlayer target, BanActionType action, TimeSpan duration, string reason)
    {
        var adminName = admin.Controller.PlayerName;
        var expiresAt = duration == TimeSpan.Zero ? 0 : DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds();
        bool isGlobal = action.ToString().Contains("Global");

        BanType banType = action == BanActionType.BanIP || action == BanActionType.GlobalBanIP || action == BanActionType.UnbanIP
            ? BanType.IP
            : BanType.SteamID;

        var ban = new Ban
        {
            SteamId64 = target.SteamID,
            BanType = banType,
            Reason = reason,
            PlayerName = target.Controller.PlayerName,
            PlayerIp = target.IPAddress,
            ExpiresAt = (ulong)expiresAt,
            Length = (ulong)duration.TotalMilliseconds,
            AdminSteamId64 = admin.SteamID,
            AdminName = adminName,
            Server = Admins.ServerGUID,
            GlobalBan = isGlobal
        };

        ServerBans.AddBan(ban);

        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);
        Admins.SwiftlyCore.Scheduler.NextTick(() =>
        {
            admin.SendMessage(MessageType.Chat, localizer["adminmenu.ban_applied", Admins.Config.CurrentValue.Prefix, target.Controller.PlayerName]);

            target.Kick("Banned.", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
        });
    }

    private static void ExecuteRemoveBan(IPlayer admin, IPlayer target, BanActionType action)
    {
        var localizer = Admins.SwiftlyCore.Translation.GetPlayerLocalizer(admin);
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        BanType banType = action == BanActionType.UnbanIP
            ? BanType.IP
            : BanType.SteamID;

        List<IBan> bans;
        if (banType == BanType.IP)
        {
            bans = ServerBans.GetBans()
                .Where(b => b.PlayerIp == target.IPAddress &&
                           b.BanType == BanType.IP &&
                           (b.ExpiresAt == 0 || b.ExpiresAt > currentTime))
                .ToList();
        }
        else
        {
            bans = ServerBans.GetBans()
                .Where(b => b.SteamId64 == target.SteamID &&
                           b.BanType == BanType.SteamID &&
                           (b.ExpiresAt == 0 || b.ExpiresAt > currentTime))
                .ToList();
        }

        foreach (var ban in bans)
        {
            ServerBans.RemoveBan(ban);
        }

        var message = localizer["adminmenu.ban_removed", Admins.Config.CurrentValue.Prefix, target.Controller.PlayerName];
        Admins.SwiftlyCore.Scheduler.NextTick(() => admin.SendMessage(MessageType.Chat, message));
    }

    private static bool IsRemoveAction(BanActionType action)
    {
        return action == BanActionType.Unban || action == BanActionType.UnbanIP;
    }

    #endregion
}