namespace Admins.Contract;

public enum SanctionKind
{
    Mute = 1,
    Gag = 2,
}

public interface ISanction
{
    ulong Id { get; set; }
    ulong SteamId64 { get; set; }
    string PlayerName { get; set; }
    string PlayerIp { get; set; }
    SanctionKind SanctionType { get; set; }
    ulong ExpiresAt { get; set; }
    ulong Length { get; set; }
    string Reason { get; set; }
    ulong AdminSteamId64 { get; set; }
    string AdminName { get; set; }
    string Server { get; set; }
    bool Global { get; set; }
}