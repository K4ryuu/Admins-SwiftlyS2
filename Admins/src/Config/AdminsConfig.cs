using SwiftlyS2.Shared.Natives;

namespace Admins.Configuration;

public class AdminsConfig
{
    public bool EnableAdminChat { get; set; } = true;
    public string Prefix { get; set; } = "[[blue]SwiftlyS2[default]]";
    public bool UseDatabase { get; set; } = true;
    public Color AdminMenuColor { get; set; } = Color.FromHex("#00FEED");

    public float SyncIntervalInSeconds { get; set; } = 30.0f;

    public List<string> MuteReasons { get; set; } = [
        "Obscene language",
        "Insult players",
        "Admin disrespect",
        "Inappropriate language",
        "Spam",
        "Trading",
        "Other",
        "Advertisement",
        "Music in voice"
    ];

    public List<int> MuteDurationsInSeconds { get; set; } = [
        0,
        300,
        600,
        900,
        1800,
        3600,
        7200,
        21600,
        43200,
        86400,
        172800,
        604800,
        1209600
    ];

    public List<string> BansReasons { get; set; } = [
        "Hacking",
        "Aimbot",
        "Wallhack",
        "SpeedHack",
        "Exploit",
        "Team Killing",
        "Team Flashing",
        "Spamming Mic/Chat",
        "Inappropriate Spray",
        "Inappropriate Language",
        "Inappropriate Name",
        "Ignoring Staff",
        "Team Stacking",
        "Other"
    ];

    public List<int> BansDurationsInSeconds { get; set; } = [
        0,
        300,
        600,
        900,
        1800,
        3600,
        7200,
        21600,
        43200,
        86400,
        172800,
        604800,
        1209600
    ];
}