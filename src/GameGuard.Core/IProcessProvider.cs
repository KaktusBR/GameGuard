namespace GameGuard.Core;

public interface IProcessProvider { IEnumerable<ProcessInfo> GetProcesses(); }

public interface IProcessKiller { void Kill(int pid); }
