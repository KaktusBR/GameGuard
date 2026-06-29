using GameGuard.Core;

namespace GameGuard.Agent;

static class Program
{
    private const string PipeName = "GameGuardPipe";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var client = new PipeClient(PipeName);
        var durations = new List<int> { 30, 60, 120, 180 };

        var tray = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Visible = true,
            Text = "GameGuard"
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Unlock…", null, (_, _) => OpenUnlock(client, durations));
        menu.Items.Add("Exit", null, (_, _) => { tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => OpenUnlock(client, durations);

        var timer = new System.Windows.Forms.Timer { Interval = 5000 };
        timer.Tick += (_, _) =>
        {
            try
            {
                var s = client.Send(new PipeRequest("status"));
                tray.Text = s.IsLocked ? "GameGuard: LOCKED"
                    : $"GameGuard: unlocked ({s.RemainingSeconds / 60} min left)";
            }
            catch { tray.Text = "GameGuard: service offline"; }
        };
        timer.Start();

        Application.Run();
        tray.Dispose();
    }

    private static void OpenUnlock(PipeClient client, List<int> fallbackDurations)
    {
        List<int> durations = fallbackDurations;
        try
        {
            var s = client.Send(new PipeRequest("status"));
            if (s.Durations is { Count: > 0 }) durations = new List<int>(s.Durations);
        }
        catch { /* service offline — use fallback */ }
        using var form = new UnlockForm(client, durations);
        form.ShowDialog();
    }
}
