using System.Collections.Concurrent;
using Admins.Comms.Contract;
using Admins.Comms.Database.Models;
using Admins.Core.Contract;
using Dommel;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Admins.Comms.Manager;

public class ServerComms
{
    private ISwiftlyCore Core = null!;
    private IConfigurationManager _configurationManager = null!;
    private IServerManager _serverManager = null!;
    private ulong _lastSyncTimestamp = 0;

    public static ConcurrentDictionary<ulong, ISanction> AllSanctions { get; set; } = [];

    public ServerComms(ISwiftlyCore core)
    {
        Core = core;
    }

    public void SetServerManager(IServerManager serverManager)
    {
        _serverManager = serverManager;
    }

    public void SetConfigurationManager(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public void Load()
    {
        Task.Run(async () =>
        {
            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                var bans = await db.GetAllAsync<Sanction>();
                AllSanctions = new ConcurrentDictionary<ulong, ISanction>(bans.ToDictionary(b => b.Id, b => (ISanction)b));

                _lastSyncTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        });
    }

    public async Task SyncSanctionsFromDatabase()
    {
        if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == false)
            return;

        try
        {
            var db = Core.Database.GetConnection("admins");

            // Query sanctions updated since last sync
            var newSanctions = await db.SelectAsync<Sanction>(s => s.UpdatedAt > _lastSyncTimestamp);

            if (newSanctions.Any())
            {
                Core.Logger.LogInformation($"[Sanctions Sync] Found {newSanctions.Count()} new/updated sanctions from database");

                var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var sanction in newSanctions)
                {
                    // If sanction has expired, remove it from local cache
                    if (sanction.ExpiresAt != 0 && sanction.ExpiresAt <= currentTime)
                    {
                        AllSanctions.TryRemove(sanction.Id, out _);
                    }
                    else
                    {
                        // Add or update sanction in local cache
                        AllSanctions.AddOrUpdate(sanction.Id, (ISanction)sanction, (key, oldValue) => (ISanction)sanction);
                    }
                }

                // Update last sync timestamp to the latest UpdatedAt value
                var maxUpdatedAt = newSanctions.Max(s => s.UpdatedAt);
                _lastSyncTimestamp = Math.Max(_lastSyncTimestamp, maxUpdatedAt);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"[Sanctions Sync] Error syncing sanctions from database: {ex.Message}");
        }
    }

    public ISanction? FindActiveSanction(ulong steamId64, string playerIp, SanctionKind sanctionKind)
    {
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return AllSanctions.Values.FirstOrDefault(sanction =>
            ((sanction.SteamId64 == steamId64 && sanction.SanctionType == SanctionType.SteamID) || (!string.IsNullOrEmpty(playerIp) && sanction.PlayerIp == playerIp && sanction.SanctionType == SanctionType.IP)) &&
            (sanction.ExpiresAt == 0 || sanction.ExpiresAt > currentTime) && (sanction.SanctionKind == sanctionKind) &&
            (sanction.Server == _serverManager.GetServerGUID() || sanction.GlobalSanction)
        );
    }
}