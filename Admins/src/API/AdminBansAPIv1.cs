using Admins.Bans;
using Admins.Contract;

namespace Admins.API;

/// <summary>
/// API v1 for managing player bans.
/// </summary>
public class AdminBansAPIv1 : IAdminBansAPIv1
{
    /// <summary>
    /// Event triggered when a ban is added.
    /// </summary>
    public event Action<IBan>? OnAdminBanAdded;

    /// <summary>
    /// Event triggered when a ban is updated.
    /// </summary>
    public event Action<IBan>? OnAdminBanUpdated;

    /// <summary>
    /// Event triggered when a ban is removed.
    /// </summary>
    public event Action<IBan>? OnAdminBanRemoved;

    /// <summary>
    /// Adds a new ban to the system.
    /// </summary>
    /// <param name="ban">The ban to add.</param>
    public void AddBan(IBan ban)
    {
        ServerBans.AddBan(ban);
    }

    /// <summary>
    /// Clears all bans from the system.
    /// </summary>
    public void ClearBans()
    {
        ServerBans.ClearBans();
    }

    /// <summary>
    /// Finds an active ban for the specified Steam ID and IP address.
    /// </summary>
    /// <param name="steamId64">The Steam ID to check.</param>
    /// <param name="playerIp">The IP address to check.</param>
    /// <returns>The active ban if found, null otherwise.</returns>
    public IBan? FindActiveBan(ulong steamId64, string playerIp)
    {
        return ServerBans.FindActiveBan(steamId64, playerIp);
    }

    /// <summary>
    /// Gets all bans in the system.
    /// </summary>
    /// <returns>List of all bans.</returns>
    public List<IBan> GetBans()
    {
        return ServerBans.GetBans();
    }

    /// <summary>
    /// Removes a ban from the system.
    /// </summary>
    /// <param name="ban">The ban to remove.</param>
    public void RemoveBan(IBan ban)
    {
        ServerBans.RemoveBan(ban);
    }

    /// <summary>
    /// Replaces all bans with the provided list.
    /// </summary>
    /// <param name="bans">The new list of bans.</param>
    public void SetBans(List<IBan> bans)
    {
        ServerBans.SetBans(bans);
    }

    /// <summary>
    /// Updates an existing ban.
    /// </summary>
    /// <param name="ban">The ban to update.</param>
    public void UpdateBan(IBan ban)
    {
        ServerBans.UpdateBan(ban);
    }

    /// <summary>
    /// Triggers the OnAdminBanAdded event.
    /// </summary>
    /// <param name="ban">The ban that was added.</param>
    public void TriggerBanAdded(IBan ban)
    {
        OnAdminBanAdded?.Invoke(ban);
    }

    /// <summary>
    /// Triggers the OnAdminBanUpdated event.
    /// </summary>
    /// <param name="ban">The ban that was updated.</param>
    public void TriggerBanUpdated(IBan ban)
    {
        OnAdminBanUpdated?.Invoke(ban);
    }

    /// <summary>
    /// Triggers the OnAdminBanRemoved event.
    /// </summary>
    /// <param name="ban">The ban that was removed.</param>
    public void TriggerBanRemoved(IBan ban)
    {
        OnAdminBanRemoved?.Invoke(ban);
    }
}