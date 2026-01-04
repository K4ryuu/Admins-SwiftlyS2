namespace Admins.Comms.Contract;

public interface ICommsManager
{
    /// <summary>
    /// Sets the list of communication sanctions.
    /// </summary>
    /// <param name="sanctions">The list of admin communication sanctions to set.</param>
    public void SetSanctions(List<ISanction> sanctions);

    /// <summary>
    /// Gets all admin communication sanctions.
    /// </summary>
    /// <returns>A list of all admin communication sanctions.</returns>
    public List<ISanction> GetSanctions();

    /// <summary>
    /// Adds an admin communication sanction to the database.
    /// </summary>
    /// <param name="sanction">The admin communication sanction to add.</param>
    public void AddSanction(ISanction sanction);

    /// <summary>
    /// Updates an existing admin communication sanction in the database.
    /// </summary>
    /// <param name="sanction">The admin communication sanction to update.</param>
    public void UpdateSanction(ISanction sanction);

    /// <summary>
    /// Removes an admin communication sanction from the database.
    /// </summary>
    /// <param name="sanction">The admin communication sanction to remove.</param>
    public void RemoveSanction(ISanction sanction);

    /// <summary>
    /// Clears all admin communication sanctions from the database.
    /// </summary>
    public void ClearSanctions();

    /// <summary>
    /// Finds an active communication sanction by SteamID64.
    /// </summary>
    /// <param name="steamId64">The SteamID64 of the player.</param>
    /// <param name="playerIp">The IP address of the player.</param>
    /// <param name="sanctionKind">The kind of sanction to find.</param>
    /// <returns>The active admin communication sanction if found; otherwise, null.</returns>
    public ISanction? FindActiveSanction(ulong steamId64, string playerIp, SanctionKind sanctionKind);

    /// <summary>
    /// Gets all admin communication sanctions from the database.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionAdded;
    /// <summary>
    /// Event fired when an admin communication sanction is updated.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionUpdated;
    /// <summary>
    /// Event fired when an admin communication sanction is removed.
    /// </summary>
    event Action<ISanction>? OnAdminSanctionRemoved;
}