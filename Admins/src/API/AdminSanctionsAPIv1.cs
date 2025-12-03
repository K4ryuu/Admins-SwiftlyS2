using Admins.Contract;
using Admins.Sanctions;
using SwiftlyS2.Shared.Players;

namespace Admins.API;

/// <summary>
/// API v1 for managing player sanctions (gag and mute).
/// </summary>
public class AdminSanctionsAPIv1 : IAdminSanctionsAPIv1
{
    /// <summary>
    /// Event triggered when a sanction is added.
    /// </summary>
    public event Action<ISanction>? OnAdminSanctionAdded;

    /// <summary>
    /// Event triggered when a sanction is updated.
    /// </summary>
    public event Action<ISanction>? OnAdminSanctionUpdated;

    /// <summary>
    /// Event triggered when a sanction is removed.
    /// </summary>
    public event Action<ISanction>? OnAdminSanctionRemoved;

    /// <summary>
    /// Adds a new sanction to the system.
    /// </summary>
    /// <param name="sanction">The sanction to add.</param>
    public void AddSanction(ISanction sanction)
    {
        ServerSanctions.AddSanction(sanction);
    }

    /// <summary>
    /// Clears all sanctions from the system.
    /// </summary>
    public void ClearSanctions()
    {
        ServerSanctions.ClearSanctions();
    }

    /// <summary>
    /// Finds all active sanctions for a player.
    /// </summary>
    /// <param name="steamId64">The Steam ID to check.</param>
    /// <returns>List of active sanctions.</returns>
    public List<ISanction> FindActiveSanctions(ulong steamId64)
    {
        return ServerSanctions.FindActiveSanctions(steamId64);
    }

    /// <summary>
    /// Gets all sanctions in the system.
    /// </summary>
    /// <returns>List of all sanctions.</returns>
    public List<ISanction> GetSanctions()
    {
        return ServerSanctions.GetSanctions();
    }

    /// <summary>
    /// Checks if a player is currently gagged.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="sanction">The active gag sanction if found.</param>
    /// <returns>True if player is gagged, false otherwise.</returns>
    public bool IsPlayerGagged(IPlayer player, out ISanction? sanction)
    {
        return ServerSanctions.IsPlayerGagged(player, out sanction);
    }

    /// <summary>
    /// Checks if a player is currently muted.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="sanction">The active mute sanction if found.</param>
    /// <returns>True if player is muted, false otherwise.</returns>
    public bool IsPlayerMuted(IPlayer player, out ISanction? sanction)
    {
        return ServerSanctions.IsPlayerMuted(player, out sanction);
    }

    /// <summary>
    /// Removes a sanction from the system.
    /// </summary>
    /// <param name="sanction">The sanction to remove.</param>
    public void RemoveSanction(ISanction sanction)
    {
        ServerSanctions.RemoveSanction(sanction);
    }

    /// <summary>
    /// Replaces all sanctions with the provided list.
    /// </summary>
    /// <param name="sanctions">The new list of sanctions.</param>
    public void SetSanctions(List<ISanction> sanctions)
    {
        ServerSanctions.SetSanctions(sanctions);
    }

    /// <summary>
    /// Updates an existing sanction.
    /// </summary>
    /// <param name="sanction">The sanction to update.</param>
    public void UpdateSanction(ISanction sanction)
    {
        ServerSanctions.UpdateSanction(sanction);
    }

    /// <summary>
    /// Triggers the OnAdminSanctionAdded event.
    /// </summary>
    /// <param name="sanction">The sanction that was added.</param>
    public void TriggerSanctionAdded(ISanction sanction)
    {
        OnAdminSanctionAdded?.Invoke(sanction);
    }

    /// <summary>
    /// Triggers the OnAdminSanctionUpdated event.
    /// </summary>
    /// <param name="sanction">The sanction that was updated.</param>
    public void TriggerSanctionUpdated(ISanction sanction)
    {
        OnAdminSanctionUpdated?.Invoke(sanction);
    }

    /// <summary>
    /// Triggers the OnAdminSanctionRemoved event.
    /// </summary>
    /// <param name="sanction">The sanction that was removed.</param>
    public void TriggerSanctionRemoved(ISanction sanction)
    {
        OnAdminSanctionRemoved?.Invoke(sanction);
    }
}