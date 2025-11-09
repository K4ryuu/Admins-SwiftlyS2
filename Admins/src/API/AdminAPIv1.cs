using Admins.Contract;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admins.API;

public class AdminAPIv1 : IAdminAPIv1
{
    [SwiftlyInject]
    private static ISwiftlyCore Core = null!;

    public event Action<IPlayer, IAdmin>? OnAdminLoad;

    public IAdmin? GetAdmin(int playerid)
    {
        var player = Core.PlayerManager.GetPlayer(playerid);
        if (player == null) return null;

        return GetAdmin(player);
    }

    public IAdmin? GetAdmin(IPlayer player)
    {
        ServerAdmins.ServerAdmins.PlayerAdmins.TryGetValue(player, out var admin);
        return admin;
    }

    public IAdmin? GetAdmin(ulong steamId64)
    {
        var player = Core.PlayerManager.GetAllPlayers().ToList().Find(p => p.SteamID == steamId64);
        if (player == null) return null;

        return GetAdmin(player);
    }

    public List<IGroup> GetAdminGroups(IAdmin admin)
    {
        return Groups.Groups.AllGroups.Where(g => admin.Groups.Contains(g.Name)).Cast<IGroup>().ToList();
    }

    public List<IAdmin> GetAllAdmins()
    {
        return [.. ServerAdmins.ServerAdmins.AllAdmins.Cast<IAdmin>()];
    }

    public List<IGroup> GetAllGroups()
    {
        return [.. Groups.Groups.AllGroups.Cast<IGroup>()];
    }

    public IGroup? GetGroup(string groupName)
    {
        return Groups.Groups.AllGroups.Find(g => g.Name == groupName);
    }

    public List<IGroup> GetPlayerGroups(IPlayer player)
    {
        var admin = GetAdmin(player);
        if (admin != null) return GetAdminGroups(admin);

        return [];
    }

    public void RefreshAdmins()
    {
        ServerAdmins.ServerAdmins.Load();
    }

    public void RefreshGroups()
    {
        Groups.Groups.Load();
    }

    public void TriggerLoadAdmin(IPlayer player, IAdmin admin)
    {
        OnAdminLoad?.Invoke(player, admin);
    }
}