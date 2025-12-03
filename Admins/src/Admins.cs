using Admins.API;
using Admins.Bans;
using Admins.Commands;
using Admins.Configuration;
using Admins.Contract;
using Admins.Database;
using Admins.Database.Models;
using Admins.Sanctions;
using Dommel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace Admins;

[PluginMetadata(
    Id = "Admins",
    Version = "1.0.0",
    Name = "Admins",
    Author = "Swiftly Development Team"
)]
public sealed partial class Admins : BasePlugin
{
    public static Groups.Groups Groups { get; private set; } = new();
    public static ServerAdmins.ServerAdmins ServerAdmins { get; private set; } = new();
    public static AdminAPIv1 AdminAPI { get; private set; } = new();
    public static AdminBansAPIv1 AdminBansAPI { get; private set; } = new();
    public static AdminSanctionsAPIv1 AdminSanctionsAPI { get; private set; } = new();
    public static AdminMenuAPIv1 AdminsMenuAPI { get; private set; } = new();

    public static string ServerGUID { get; private set; } = string.Empty;

    public static IOptionsMonitor<AdminsConfig> Config { get; private set; } = null!;
    public static ISwiftlyCore SwiftlyCore { get; private set; } = null!;

    private readonly AdminCommands _adminCommands = new();

    private CancellationTokenSource? _syncBansTokenSource;
    private CancellationTokenSource? _syncSanctionsTokenSource;

    public Admins(ISwiftlyCore core) : base(core)
    {
        SwiftlyCore = core;

        var connection = core.Database.GetConnection("admins");
        MigrationRunner.RunMigrations(connection);
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IAdminAPIv1, AdminAPIv1>("Admins.API.v1", AdminAPI);
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
    }

    public override void Load(bool hotReload)
    {
        InitializeConfiguration();

        InitializeServerGuid();

        LoadServerData();

        StartBackgroundTasks();

        ServerSanctions.RegisterAdminSubmenu();
        ServerBans.RegisterAdminSubmenu();
        _adminCommands.Init();

        Core.Logger.LogInformation("Admins plugin loaded successfully");
    }

    private void InitializeConfiguration()
    {
        Core.Configuration
            .InitializeJsonWithModel<AdminsConfig>("config.jsonc", "Main")
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
            });

        ServiceCollection services = new();
        services.AddSwiftly(Core)
            .AddOptions<AdminsConfig>()
            .BindConfiguration("Main");

        var provider = services.BuildServiceProvider();
        Config = provider.GetRequiredService<IOptionsMonitor<AdminsConfig>>();
    }

    private void InitializeServerGuid()
    {
        var guidPath = Path.Combine(Core.PluginDataDirectory, "server_id.txt");

        if (!File.Exists(guidPath))
        {
            ServerGUID = Guid.NewGuid().ToString();
            File.WriteAllText(guidPath, ServerGUID);
            Core.Logger.LogWarning("Generated new Server GUID: {Guid}", ServerGUID);
            return;
        }

        ServerGUID = File.ReadAllText(guidPath).Trim();

        if (!Guid.TryParse(ServerGUID, out _))
        {
            ServerGUID = Guid.NewGuid().ToString();
            File.WriteAllText(guidPath, ServerGUID);
            Core.Logger.LogWarning("Invalid Server GUID detected. Generated new GUID: {Guid}", ServerGUID);
        }
    }

    private void LoadServerData()
    {
        global::Admins.Groups.Groups.Load();
        ServerBans.Load(null);
        ServerSanctions.Load(null);

        Core.Scheduler.RepeatBySeconds(10.0f, ServerSanctions.ScheduleCheck);
    }

    private void StartBackgroundTasks()
    {
        _syncBansTokenSource = Core.Scheduler.RepeatBySeconds(
            Config.CurrentValue.SyncIntervalInSeconds,
            ServerBans.DatabaseFetch
        );

        _syncSanctionsTokenSource = Core.Scheduler.RepeatBySeconds(
            Config.CurrentValue.SyncIntervalInSeconds,
            ServerSanctions.DatabaseFetch
        );

        Config.OnChange(config =>
        {
            RestartSyncTasks(config);
        });
    }

    private void RestartSyncTasks(AdminsConfig config)
    {
        _syncBansTokenSource?.Cancel();
        _syncBansTokenSource = Core.Scheduler.RepeatBySeconds(
            config.SyncIntervalInSeconds,
            ServerBans.DatabaseFetch
        );

        _syncSanctionsTokenSource?.Cancel();
        _syncSanctionsTokenSource = Core.Scheduler.RepeatBySeconds(
            config.SyncIntervalInSeconds,
            ServerSanctions.DatabaseFetch
        );

        Core.Logger.LogInformation(
            "Sync tasks restarted with interval: {Interval}s",
            config.SyncIntervalInSeconds
        );
    }

    public override void Unload()
    {
        _syncBansTokenSource?.Cancel();
        _syncSanctionsTokenSource?.Cancel();

        Core.Logger.LogInformation("Admins plugin unloaded successfully");
    }

    [EventListener<EventDelegates.OnSteamAPIActivated>]
    public void OnSteamAPIActivated()
    {
        if (!Config.CurrentValue.UseDatabase)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var serverIp = Core.Engine.ServerIP;
                var hostport = Core.ConVar.Find<int>("hostport");

                if (hostport == null || string.IsNullOrEmpty(serverIp))
                {
                    Core.Logger.LogWarning("Unable to register server: missing IP or port");
                    return;
                }

                using var database = Core.Database.GetConnection("admins");
                var existingServer = database.Count<Server>(s => s.GUID == ServerGUID);

                if (existingServer == 0)
                {
                    var server = new Server
                    {
                        IP = serverIp,
                        Port = hostport.Value,
                        GUID = ServerGUID
                    };
                    database.Insert(server);
                    Core.Logger.LogInformation(
                        "Server registered in database: {IP}:{Port}",
                        serverIp,
                        hostport.Value
                    );
                }
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(ex, "Failed to register server in database");
            }
        });
    }

    [EventListener<EventDelegates.OnClientSteamAuthorize>]
    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player == null)
        {
            return;
        }

        if (!ServerBans.CheckPlayer(player))
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var admin = AdminAPI.GetAdmin(@event.PlayerId);
                if (admin != null)
                {
                    global::Admins.ServerAdmins.ServerAdmins.AssignAdmin(player, (Admin)admin);
                    Core.Logger.LogInformation(
                        "Admin privileges assigned to player: {Name} ({SteamID})",
                        player.Controller.PlayerName,
                        player.SteamID
                    );
                }
            }
            catch (Exception ex)
            {
                Core.Logger.LogError(
                    ex,
                    "Failed to assign admin privileges to player: {PlayerId}",
                    @event.PlayerId
                );
            }
        });
    }

    [ClientChatHookHandler]
    public HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        var player = Core.PlayerManager.GetPlayer(playerId);
        if (player == null || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        if (ShouldHandleAdminChat(text, teamOnly))
        {
            HandleAdminChat(player, text);
            return HookResult.Stop;
        }

        if (ServerSanctions.IsPlayerGagged(player, out var sanction))
        {
            NotifyPlayerGagged(player, sanction!);
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private bool ShouldHandleAdminChat(string text, bool teamOnly)
    {
        return teamOnly
            && text.StartsWith('@')
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

        var messageContent = text[1..];
        var formatKey = hasAdminPermission
            ? "chat.admin_chat_format"
            : "chat.player_chat_format";

        foreach (var recipient in recipients)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(recipient);
            var message = localizer[formatKey, sender.Controller.PlayerName, messageContent];
            recipient.SendChat(message);
        }
    }

    private void NotifyPlayerGagged(IPlayer player, ISanction sanction)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);
        var expiryText = sanction.ExpiresAt == 0
            ? localizer["never"]
            : FormatTimestampInTimeZone((long)sanction.ExpiresAt);

        var message = localizer[
            "gag.message",
            Config.CurrentValue.Prefix,
            sanction.AdminName,
            expiryText,
            sanction.Reason
        ];

        player.SendChat(message);
    }

    public static TimeZoneInfo GetConfiguredTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(Config.CurrentValue.TimeZone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    public static DateTimeOffset GetCurrentTimeInTimeZone()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var timeZone = GetConfiguredTimeZone();
        return TimeZoneInfo.ConvertTime(utcNow, timeZone);
    }

    public static string FormatTimestampInTimeZone(long unixTimeMilliseconds)
    {
        var utcTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
        var timeZone = GetConfiguredTimeZone();
        var localTime = TimeZoneInfo.ConvertTime(utcTime, timeZone);
        return localTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}