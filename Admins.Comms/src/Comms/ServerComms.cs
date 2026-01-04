using System.Collections.Concurrent;
using Admins.Comms.Contract;
using Admins.Comms.Database.Models;
using Admins.Core.Contract;
using Dommel;
using SwiftlyS2.Shared;

namespace Admins.Comms.Manager;

public class ServerComms
{
    private ISwiftlyCore Core = null!;
    private IConfigurationManager _configurationManager = null!;
    private IServerManager _serverManager = null!;

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
            }
        });
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