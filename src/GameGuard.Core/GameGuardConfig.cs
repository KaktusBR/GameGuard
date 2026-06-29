namespace GameGuard.Core;

public class GameGuardConfig
{
    public HashedCode? Code { get; set; }
    public List<string> BlockedExecutables { get; set; } = new();
    public List<string> BlockedFolders { get; set; } = new();
    public List<string> BlockedDomains { get; set; } = new();
    public List<int> DurationsMinutes { get; set; } = new();

    public static GameGuardConfig Default() => new()
    {
        BlockedExecutables = new()
        {
            "steam.exe", "EpicGamesLauncher.exe", "Battle.net.exe",
            "GalaxyClient.exe", "Origin.exe", "EADesktop.exe",
            "RiotClientServices.exe", "Ubisoft Connect.exe", "upc.exe"
        },
        BlockedFolders = new()
        {
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Epic Games"
        },
        BlockedDomains = new()
        {
            "poki.com", "crazygames.com", "miniclip.com", "roblox.com", "now.gg",
            "coolmathgames.com", "addictinggames.com", "kongregate.com"
        },
        DurationsMinutes = new() { 30, 60, 120, 180 }
    };
}
