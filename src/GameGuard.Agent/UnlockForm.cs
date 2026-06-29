using GameGuard.Core;

namespace GameGuard.Agent;

public class UnlockForm : Form
{
    private readonly PipeClient _client;
    private readonly TextBox _code = new() { PlaceholderText = "Parent code", Width = 220, UseSystemPasswordChar = true };
    private readonly ComboBox _duration = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public UnlockForm(PipeClient client, IEnumerable<int> durations)
    {
        _client = client;
        Text = "GameGuard — Unlock";
        Width = 280; Height = 220; FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false; MinimizeBox = false;

        foreach (var d in durations) _duration.Items.Add(d);
        if (_duration.Items.Count > 0) _duration.SelectedIndex = 0;

        var info = new Label { Text = "Games are blocked. Enter the parent code to unlock.", AutoSize = false, Width = 240, Height = 40, Left = 16, Top = 12 };
        _code.Left = 16; _code.Top = 56;
        _duration.Left = 16; _duration.Top = 88;
        var unlock = new Button { Text = "Unlock", Left = 16, Top = 120, Width = 220 };
        _status.Left = 16; _status.Top = 154;
        unlock.Click += (_, _) => DoUnlock();

        Controls.AddRange(new Control[] { info, _code, _duration, unlock, _status });
    }

    private void DoUnlock()
    {
        if (_duration.SelectedItem is not int minutes) return;
        try
        {
            var resp = _client.Send(new PipeRequest("unlock", _code.Text, minutes));
            if (resp.Success) { MessageBox.Show($"Unlocked for {minutes} minutes."); Close(); }
            else _status.Text = resp.Error ?? "Unlock failed.";
        }
        catch (Exception ex) { _status.Text = "Service unavailable: " + ex.Message; }
    }
}
