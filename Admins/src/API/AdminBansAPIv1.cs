using Admins.Bans;
using Admins.Contract;

namespace Admins.API;

public class AdminBansAPIv1 : IAdminBansAPIv1
{
    public event Action<IBan>? OnAdminBanAdded;
    public event Action<IBan>? OnAdminBanUpdated;
    public event Action<IBan>? OnAdminBanRemoved;

    public void AddBan(IBan ban)
    {
        ServerBans.AddBan(ban);
    }

    public void ClearBans()
    {
        ServerBans.ClearBans();
    }

    public IBan? FindActiveBan(ulong steamId64, string playerIp)
    {
        return ServerBans.FindActiveBan(steamId64, playerIp);
    }

    public List<IBan> GetBans()
    {
        return ServerBans.GetBans();
    }

    public void RemoveBan(IBan ban)
    {
        ServerBans.RemoveBan(ban);
    }

    public void SetBans(List<IBan> bans)
    {
        ServerBans.SetBans(bans);
    }

    public void UpdateBan(IBan ban)
    {
        ServerBans.UpdateBan(ban);
    }

    public void TriggerBanAdded(IBan ban)
    {
        OnAdminBanAdded?.Invoke(ban);
    }

    public void TriggerBanUpdated(IBan ban)
    {
        OnAdminBanUpdated?.Invoke(ban);
    }

    public void TriggerBanRemoved(IBan ban)
    {
        OnAdminBanRemoved?.Invoke(ban);
    }
}