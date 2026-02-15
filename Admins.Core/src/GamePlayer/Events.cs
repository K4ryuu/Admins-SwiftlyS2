using Admins.Bans.Contract;
using Admins.Comms.Contract;
using Admins.Core.Contract;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace Admins.Core.GamePlayer;

public class GamePlayer : IGamePlayer
{
    private readonly ISwiftlyCore Core;
    private IBansManager? BansManager;
    private ICommsManager? CommsManager;
    private IConfigurationManager ConfigurationManager = null!;

    public GamePlayer(ISwiftlyCore core, IConfigurationManager configurationManager)
    {
        Core = core;
        ConfigurationManager = configurationManager;

        core.Registrator.Register(this);
    }

    public void SetBansManager(IBansManager? bansManager)
    {
        BansManager = bansManager;
    }

    public void SetCommsManager(ICommsManager? commsManager)
    {
        CommsManager = commsManager;
    }

    [EventListener<EventDelegates.OnClientSteamAuthorize>]
    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent e)
    {
        var player = Core.PlayerManager.GetPlayer(e.PlayerId);
        if (player == null) return;

        NotifyAdminsAboutPlayerRecord(player);
    }

    private void NotifyAdminsAboutPlayerRecord(IPlayer player)
    {
        Core.Scheduler.NextTick(() =>
        {
            var totalBans = 0;
            var totalGags = 0;
            var totalMutes = 0;

            if (BansManager != null)
            {
                var allBans = BansManager.GetBans();
                totalBans = allBans.Count(b => b.SteamId64 == (long)player.SteamID || b.PlayerIp == player.IPAddress);
            }

            if (CommsManager != null)
            {
                var allSanctions = CommsManager.GetSanctions();
                var playerSanctions = allSanctions.Where(s => s.SteamId64 == (long)player.SteamID || s.PlayerIp == player.IPAddress).ToList();
                totalGags = playerSanctions.Count(s => s.SanctionKind == SanctionKind.Gag);
                totalMutes = playerSanctions.Count(s => s.SanctionKind == SanctionKind.Mute);
            }

            if (totalBans == 0 && totalGags == 0 && totalMutes == 0) return;

            var admins = Core.PlayerManager.GetAllPlayers()
                .Where(p => Core.Permission.PlayerHasPermission(p.SteamID, "admins.notify"))
                .ToList();

            if (admins.Count == 0) return;

            var playerName = player.Controller.IsValid ? player.Controller.PlayerName : "Unknown";
            var message = $"{ConfigurationManager.GetCurrentConfiguration()!.Prefix} {playerName} connected - Bans: {totalBans}, Gags: {totalGags}, Mutes: {totalMutes}";

            foreach (var admin in admins)
            {
                admin.SendChat(message);
            }
        });
    }
}
