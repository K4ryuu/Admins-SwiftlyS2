using SwiftlyS2.Shared.Players;

namespace Admins.Contract;

public interface IAdminSanctionsAPIv1
{
    /// <summary>
    /// Sets the sanctions list.
    /// </summary>
    /// <param name="sanctions">The list of sanctions to set.</param>
    public void SetSanctions(List<ISanction> sanctions);
    /// <summary>
    /// Gets the list of sanctions.
    /// </summary>
    /// <returns>The list of sanctions.</returns>
    public List<ISanction> GetSanctions();
    /// <summary>
    /// Adds a sanction.
    /// </summary>
    /// <param name="sanction">The sanction to add.</param>
    public void AddSanction(ISanction sanction);
    /// <summary>
    /// Updates a sanction.
    /// </summary>
    /// <param name="sanction">The sanction to update.</param>
    public void UpdateSanction(ISanction sanction);
    /// <summary>
    /// Removes a sanction.
    /// </summary>
    /// <param name="sanction">The sanction to remove.</param>
    public void RemoveSanction(ISanction sanction);
    /// <summary>
    /// Clears all sanctions.
    /// </summary>
    public void ClearSanctions();

    /// <summary>
    /// Finds active sanctions for a given SteamID64.
    /// </summary>
    /// <param name="steamId64">The SteamID64 of the player.</param>
    /// <returns>A list of active sanctions for the player.</returns>
    public List<ISanction> FindActiveSanctions(ulong steamId64);
    /// <summary>
    /// Checks if a player is muted or gagged.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="sanction">The sanction associated with the mute status, if any.</param>
    /// <returns>True if the player is muted; otherwise, false.</returns>
    public bool IsPlayerMuted(IPlayer player, out ISanction? sanction);
    /// <summary>
    /// Checks if a player is gagged.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="sanction">The sanction associated with the gag status, if any.</param>
    /// <returns>True if the player is gagged; otherwise, false.</returns>
    public bool IsPlayerGagged(IPlayer player, out ISanction? sanction);

    /// <summary>
    /// Event triggered when a sanction is added.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionAdded;
    /// <summary>
    /// Event triggered when a sanction is updated.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionUpdated;
    /// <summary>
    /// Event triggered when a sanction is removed.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionRemoved;
}