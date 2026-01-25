namespace Admins.Comms.Contract;

public enum SanctionType
{
    SteamID = 1,
    IP = 2
}

public enum SanctionKind
{
    Gag = 1,
    Mute = 2
}

public interface ISanction
{
    ulong Id { get; set; }
    ulong SteamId64 { get; set; }
    string PlayerName { get; set; }
    string PlayerIp { get; set; }
    SanctionType SanctionType { get; set; }
    SanctionKind SanctionKind { get; set; }
    ulong ExpiresAt { get; set; }
    ulong Length { get; set; }
    string Reason { get; set; }
    ulong AdminSteamId64 { get; set; }
    string AdminName { get; set; }
    string Server { get; set; }
    bool GlobalSanction { get; set; }
    ulong CreatedAt { get; set; }
    ulong UpdatedAt { get; set; }
}