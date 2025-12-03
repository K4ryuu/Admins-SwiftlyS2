using Admins.Contract;
using Admins.Database.Models;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admins.API;

/// <summary>
/// API v1 for managing admins, groups, and permissions.
/// </summary>
public class AdminAPIv1 : IAdminAPIv1
{
    /// <summary>
    /// Gets the bans API instance.
    /// </summary>
    public IAdminBansAPIv1 AdminBansAPI => Admins.AdminBansAPI;

    /// <summary>
    /// Gets the sanctions API instance.
    /// </summary>
    public IAdminSanctionsAPIv1 AdminSanctionsAPI => Admins.AdminSanctionsAPI;

    /// <summary>
    /// Gets the menu API instance.
    /// </summary>
    public IAdminMenuAPIv1 AdminMenuAPI => Admins.AdminsMenuAPI;

    /// <summary>
    /// Event triggered when an admin is loaded for a player.
    /// </summary>
    public event Action<IPlayer, IAdmin>? OnAdminLoad;

    /// <summary>
    /// Adds a new admin to the system.
    /// </summary>
    /// <param name="steamId64">Steam ID of the admin.</param>
    /// <param name="adminName">Name of the admin.</param>
    /// <param name="groups">Groups the admin belongs to.</param>
    /// <param name="permissions">Permissions granted to the admin.</param>
    /// <returns>The created admin instance.</returns>
    public IAdmin? AddAdmin(ulong steamId64, string adminName, List<IGroup> groups, List<string> permissions)
    {
        var admin = new Admin
        {
            SteamId64 = (long)steamId64,
            Username = adminName,
            Groups = groups.Select(g => g.Name).ToList(),
            Permissions = permissions,
            Servers = [Admins.ServerGUID]
        };
        ServerAdmins.ServerAdmins.AddAdmin(admin);
        return admin;
    }

    /// <summary>
    /// Gets admin information for a player by player ID.
    /// </summary>
    /// <param name="playerid">The player ID.</param>
    /// <returns>Admin instance if found, null otherwise.</returns>
    public IAdmin? GetAdmin(int playerid)
    {
        var player = Admins.SwiftlyCore.PlayerManager.GetPlayer(playerid);
        if (player == null) return null;

        return GetAdmin(player);
    }

    /// <summary>
    /// Gets admin information for a player.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <returns>Admin instance if found, null otherwise.</returns>
    public IAdmin? GetAdmin(IPlayer player)
    {
        ServerAdmins.ServerAdmins.PlayerAdmins.TryGetValue(player, out var admin);
        return admin;
    }

    /// <summary>
    /// Gets admin information by Steam ID.
    /// </summary>
    /// <param name="steamId64">The Steam ID.</param>
    /// <returns>Admin instance if found, null otherwise.</returns>
    public IAdmin? GetAdmin(ulong steamId64)
    {
        var player = Admins.SwiftlyCore.PlayerManager.GetAllPlayers().ToList().Find(p => p.SteamID == steamId64);
        if (player == null) return null;

        return GetAdmin(player);
    }

    /// <summary>
    /// Gets all groups that an admin belongs to.
    /// </summary>
    /// <param name="admin">The admin instance.</param>
    /// <returns>List of groups.</returns>
    public List<IGroup> GetAdminGroups(IAdmin admin)
    {
        return Groups.Groups.AllGroups.Where(g => admin.Groups.Contains(g.Name)).Cast<IGroup>().ToList();
    }

    /// <summary>
    /// Gets all admins in the system.
    /// </summary>
    /// <returns>List of all admins.</returns>
    public List<IAdmin> GetAllAdmins()
    {
        return [.. ServerAdmins.ServerAdmins.AllAdmins.Cast<IAdmin>()];
    }

    /// <summary>
    /// Gets all groups in the system.
    /// </summary>
    /// <returns>List of all groups.</returns>
    public List<IGroup> GetAllGroups()
    {
        return [.. Groups.Groups.AllGroups.Cast<IGroup>()];
    }

    /// <summary>
    /// Gets a specific group by name.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <returns>Group instance if found, null otherwise.</returns>
    public IGroup? GetGroup(string groupName)
    {
        return Groups.Groups.AllGroups.Find(g => g.Name == groupName);
    }

    /// <summary>
    /// Gets all groups that a player belongs to through their admin status.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <returns>List of groups.</returns>
    public List<IGroup> GetPlayerGroups(IPlayer player)
    {
        var admin = GetAdmin(player);
        if (admin != null) return GetAdminGroups(admin);

        return [];
    }

    /// <summary>
    /// Reloads all admins from the database.
    /// </summary>
    public void RefreshAdmins()
    {
        ServerAdmins.ServerAdmins.Load();
    }

    /// <summary>
    /// Reloads all groups from the database.
    /// </summary>
    public void RefreshGroups()
    {
        Groups.Groups.Load();
    }

    /// <summary>
    /// Removes an admin from the system.
    /// </summary>
    /// <param name="admin">The admin to remove.</param>
    public void RemoveAdmin(IAdmin admin)
    {
        ServerAdmins.ServerAdmins.RemoveAdmin((Admin)admin);
    }

    /// <summary>
    /// Triggers the OnAdminLoad event.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <param name="admin">The admin instance.</param>
    public void TriggerLoadAdmin(IPlayer player, IAdmin admin)
    {
        OnAdminLoad?.Invoke(player, admin);
    }
}