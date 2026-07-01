using System.Reflection;
using GameGuard.Agent;
using GameGuard.Core;

namespace GameGuard.App;

/// <summary>The user-session tray app: status, unlock menu, and the auto-popped block prompt.</summary>
static class TrayApp
{
    private const string PipeName = "GameGuardPipe";
    private static readonly TimeSpan PopCooldown = TimeSpan.FromSeconds(30);

    public static void Run()
    {
        var client = new PipeClient(PipeName);
        var fallbackDurations = new List<int> { 30, 60, 120, 180 };

        UnlockForm? openWindow = null;
        long lastBlockSeq = -1;                       // -1 = backlog not yet baselined
        var lastPop = DateTime.MinValue;

        // Opens the (single) unlock window. blockedGame != null => auto-pop "blocked" mode.
        void OpenUnlock(string? blockedGame)
        {
            if (openWindow is { IsDisposed: false })
            {
                openWindow.Activate();
                return;
            }

            var durations = fallbackDurations;
            try
            {
                var s = client.Send(new PipeRequest("status"));
                if (s.Durations is { Count: > 0 }) durations = new List<int>(s.Durations);
            }
            catch { /* service offline — use fallback */ }

            var form = new UnlockForm(client, durations, blockedGame);
            if (blockedGame is not null) form.TopMost = true; // surface over the game
            form.FormClosed += (_, _) => openWindow = null;
            openWindow = form;
            form.Show();
            form.Activate();
        }

        var tray = new NotifyIcon
        {
            Icon = TrayIcon.BuildController(),
            Visible = true,
            Text = "GameGuard",
        };

        // Native-rendered menu so it matches the OS theme.
        var menu = new ContextMenuStrip { RenderMode = ToolStripRenderMode.System };
        var header = new ToolStripMenuItem("GameGuard") { Enabled = false };
        var unlockItem = new ToolStripMenuItem("Unlock…", null, (_, _) => OpenUnlock(null));
        var exitItem = new ToolStripMenuItem("Exit", null,
            (_, _) => { tray.Visible = false; Application.Exit(); });
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(unlockItem);
        menu.Items.Add(exitItem);
        menu.Opening += (_, _) => header.Text = StatusLine(client);
        tray.ContextMenuStrip = menu;

        // Left-click also pops the menu. Reuse NotifyIcon's own private ShowContextMenu
        // so focus/dismiss behaviour matches a right-click.
        var showMenu = typeof(NotifyIcon).GetMethod(
            "ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) showMenu?.Invoke(tray, null);
        };

        var timer = new System.Windows.Forms.Timer { Interval = 5000 };
        timer.Tick += (_, _) =>
        {
            try
            {
                var s = client.Send(new PipeRequest("status"));
                tray.Text = Truncate(Describe(s));

                if (lastBlockSeq < 0)
                {
                    lastBlockSeq = s.BlockSeq;        // skip events that predate startup
                }
                else if (s.RecentBlocks is { Count: > 0 } && s.BlockSeq > lastBlockSeq)
                {
                    var game = s.RecentBlocks.Where(b => b.Seq > lastBlockSeq)
                                             .Select(b => b.AppName).FirstOrDefault();
                    lastBlockSeq = s.BlockSeq;
                    if (game is not null && DateTime.UtcNow - lastPop > PopCooldown)
                    {
                        lastPop = DateTime.UtcNow;
                        OpenUnlock(game);
                    }
                }
            }
            catch { tray.Text = "GameGuard: service offline"; }
        };
        timer.Start();

        Application.Run();
        tray.Visible = false;
        tray.Dispose();
    }

    private static string StatusLine(PipeClient client)
    {
        try { return Describe(client.Send(new PipeRequest("status"))); }
        catch { return "Service offline"; }
    }

    private static string Describe(PipeResponse s) =>
        s.IsLocked
            ? "GameGuard: games locked"
            : $"GameGuard: unlocked ({s.RemainingSeconds / 60} min left)";

    private static string Truncate(string text) =>
        text.Length <= 63 ? text : text[..63]; // NotifyIcon.Text hard limit
}
