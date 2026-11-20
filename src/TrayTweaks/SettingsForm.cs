namespace TrayTweaks;

public sealed class SettingsForm : Form
{
    public SettingsForm(TweakManager tweaks)
    {
        Text = "TrayTweaks Settings";
        Icon = AppIcon.Load(SystemInformation.IconSize);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 340);
        MinimumSize = new Size(360, 300);
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        Padding = new Padding(12);

        var startWithWindows = new CheckBox
        {
            Text = "Start TrayTweaks when I sign in to Windows",
            Checked = StartupRegistration.IsEnabled,
            AutoSize = true,
        };
        startWithWindows.CheckedChanged += (_, _) => StartupRegistration.SetEnabled(startWithWindows.Checked);

        var tweakList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            DisplayMember = nameof(Tweak.Name),
        };
        foreach (var tweak in TweakManager.AllTweaks)
            tweakList.Items.Add(tweak, tweaks.IsEnabled(tweak));

        var description = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        tweakList.SelectedIndexChanged += (_, _) =>
            description.Text = (tweakList.SelectedItem as Tweak)?.Description ?? "";
        tweakList.ItemCheck += (_, e) =>
        {
            var tweak = (Tweak)tweakList.Items[e.Index];
            // ItemCheck fires before the box changes; apply it, then show what really happened.
            BeginInvoke(() =>
            {
                tweaks.SetEnabled(tweak, e.NewValue == CheckState.Checked);
                if (tweakList.GetItemChecked(e.Index) != tweaks.IsEnabled(tweak))
                    tweakList.SetItemChecked(e.Index, tweaks.IsEnabled(tweak));
            });
        };

        var close = new Button { Text = "Close", AutoSize = true, Anchor = AnchorStyles.Right };
        close.Click += (_, _) => Close();
        AcceptButton = close;
        CancelButton = close;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(startWithWindows);
        layout.Controls.Add(new Label { Text = "Tweaks", AutoSize = true, Margin = new Padding(3, 12, 3, 3) });
        layout.Controls.Add(tweakList);
        layout.Controls.Add(description);
        layout.Controls.Add(close);
        Controls.Add(layout);

        if (tweakList.Items.Count > 0)
            tweakList.SelectedIndex = 0;
    }
}
