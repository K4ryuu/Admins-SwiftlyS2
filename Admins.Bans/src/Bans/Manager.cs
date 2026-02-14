using Admins.Bans.Contract;
using Admins.Bans.Database.Models;
using Dommel;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Admins.Bans.Manager;

public class BansManager : IBansManager
{
    private ISwiftlyCore Core = null!;
    private ServerBans _serverBans = null!;
    private Core.Contract.IConfigurationManager _configurationManager = null!;

    public event Action<IBan>? OnAdminBanAdded;
    public event Action<IBan>? OnAdminBanUpdated;
    public event Action<IBan>? OnAdminBanRemoved;

    public BansManager(ISwiftlyCore core, ServerBans serverBans)
    {
        Core = core;
        _serverBans = serverBans;
    }

    public void SetConfigurationManager(Core.Contract.IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public void AddBan(IBan ban)
    {
        Task.Run(async () =>
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ban.CreatedAt = timestamp;
            ban.UpdatedAt = timestamp;

            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                ban.Id = Convert.ToInt64(await db.InsertAsync((Ban)ban));
            }

            OnAdminBanAdded?.Invoke(ban);

            var players = Core.PlayerManager.GetAllPlayers();
            foreach (var player in players)
            {
                if (player.IsFakeClient || !player.IsValid)
                    continue;

                if ((long)player.SteamID == ban.SteamId64 || (!string.IsNullOrEmpty(ban.PlayerIp) && player.IPAddress == ban.PlayerIp))
                {
                    _serverBans.CheckPlayer(player);
                }
            }
        });
    }

    public void ClearBans()
    {
        Task.Run(async () =>
        {
            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                await db.DeleteAllAsync<Ban>();
            }
        });
    }

    public IBan? FindActiveBan(long steamId64, string playerIp)
    {
        return _serverBans.FindActiveBan(steamId64, playerIp);
    }

    public List<IBan> GetBans()
    {
        try
        {
            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                return [.. db.GetAllAsync<Ban>().GetAwaiter().GetResult().Select(b => (IBan)b)];
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"[Bans] Error fetching bans from database: {ex.Message}");
        }

        return [];
    }

    public void RemoveBan(IBan ban)
    {
        Task.Run(async () =>
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ban.ExpiresAt = currentTime;
            ban.UpdatedAt = currentTime;

            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                await db.UpdateAsync((Ban)ban);
            }

            OnAdminBanRemoved?.Invoke(ban);
        });
    }

    public void SetBans(List<IBan> bans)
    {
        Task.Run(async () =>
        {
            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                await db.DeleteAllAsync<Ban>();
                await db.InsertAsync(bans.Select(b => (Ban)b).ToList());
            }
        });
    }

    public void UpdateBan(IBan ban)
    {
        Task.Run(async () =>
        {
            ban.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (_configurationManager.GetConfigurationMonitor()!.CurrentValue.UseDatabase == true)
            {
                var db = Core.Database.GetConnection("admins");
                await db.UpdateAsync((Ban)ban);
            }

            OnAdminBanUpdated?.Invoke(ban);
        });
    }
}
