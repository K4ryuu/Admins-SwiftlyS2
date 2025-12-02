using Admins.Contract;
using Admins.Sanctions;
using SwiftlyS2.Shared.Players;

namespace Admins.API;

public class AdminSanctionsAPIv1 : IAdminSanctionsAPIv1
{
    public event Action<ISanction>? OnAdminSanctionAdded;
    public event Action<ISanction>? OnAdminSanctionUpdated;
    public event Action<ISanction>? OnAdminSanctionRemoved;

    public void AddSanction(ISanction sanction)
    {
        ServerSanctions.AddSanction(sanction);
    }

    public void ClearSanctions()
    {
        ServerSanctions.ClearSanctions();
    }

    public List<ISanction> FindActiveSanctions(ulong steamId64)
    {
        return ServerSanctions.FindActiveSanctions(steamId64);
    }

    public List<ISanction> GetSanctions()
    {
        return ServerSanctions.GetSanctions();
    }

    public bool IsPlayerGagged(IPlayer player, out ISanction? sanction)
    {
        return ServerSanctions.IsPlayerGagged(player, out sanction);
    }

    public bool IsPlayerMuted(IPlayer player, out ISanction? sanction)
    {
        return ServerSanctions.IsPlayerMuted(player, out sanction);
    }

    public void RemoveSanction(ISanction sanction)
    {
        ServerSanctions.RemoveSanction(sanction);
    }

    public void SetSanctions(List<ISanction> sanctions)
    {
        ServerSanctions.SetSanctions(sanctions);
    }

    public void UpdateSanction(ISanction sanction)
    {
        ServerSanctions.UpdateSanction(sanction);
    }

    public void TriggerSanctionAdded(ISanction sanction)
    {
        OnAdminSanctionAdded?.Invoke(sanction);
    }

    public void TriggerSanctionUpdated(ISanction sanction)
    {
        OnAdminSanctionUpdated?.Invoke(sanction);
    }

    public void TriggerSanctionRemoved(ISanction sanction)
    {
        OnAdminSanctionRemoved?.Invoke(sanction);
    }
}