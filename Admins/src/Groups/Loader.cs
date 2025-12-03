using Admins.API;
using Admins.Database.Groups;
using Dommel;
using SwiftlyS2.Shared;

namespace Admins.Groups;

/// <summary>
/// Manages admin groups including loading from database and permission management.
/// </summary>
public partial class Groups
{
    /// <summary>
    /// Gets all loaded groups.
    /// </summary>
    public static List<Group> AllGroups { get; private set; } = new();

    /// <summary>
    /// Loads all groups from the database and reloads admin assignments.
    /// </summary>
    public static void Load()
    {
        Task.Run(() =>
        {
            foreach (var (player, admin) in ServerAdmins.ServerAdmins.PlayerAdmins)
            {
                if (!admin.Servers.Contains(Admins.ServerGUID)) continue;
                ServerAdmins.ServerAdmins.UnassignAdmin(player, admin);
            }

            var database = Admins.SwiftlyCore.Database.GetConnection("admins");
            AllGroups = [.. database.GetAll<Group>()];

            ServerAdmins.ServerAdmins.Load();
        });
    }
}