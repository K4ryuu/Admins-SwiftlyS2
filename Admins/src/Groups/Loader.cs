using Admins.API;
using Admins.Database.Groups;
using Dommel;
using SwiftlyS2.Shared;

namespace Admins.Groups;

public partial class Groups
{
    [SwiftlyInject]
    private static ISwiftlyCore Core = null!;

    public static List<Group> AllGroups { get; private set; } = new();

    public static void Load()
    {
        Task.Run(() =>
        {
            foreach (var (player, admin) in ServerAdmins.ServerAdmins.PlayerAdmins)
            {
                if (!admin.Servers.Contains(Admins.ServerGUID)) continue;

                foreach (var permission in admin.Permissions)
                {
                    Core.Permission.RemovePermission(player.SteamID, permission);
                }

                foreach (var group in admin.Groups)
                {
                    var obj = AllGroups.Find(p => p.Name == group && p.Servers.Contains(Admins.ServerGUID));
                    if (obj == null) continue;

                    foreach (var permission in obj.Permissions)
                    {
                        Core.Permission.RemovePermission(player.SteamID, permission);
                    }
                }
            }

            var database = Core.Database.GetConnection("admins");
            AllGroups = [.. database.GetAll<Group>()];

            ServerAdmins.ServerAdmins.Load();
        });
    }
}