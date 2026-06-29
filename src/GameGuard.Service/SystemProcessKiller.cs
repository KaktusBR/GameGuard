using System.Diagnostics;
using System.Runtime.Versioning;
using GameGuard.Core;

namespace GameGuard.Service;

[SupportedOSPlatform("windows")]
public class SystemProcessKiller : IProcessKiller
{
    public void Kill(int pid)
    {
        var p = Process.GetProcessById(pid);
        p.Kill(entireProcessTree: true);
    }
}
