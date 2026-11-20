using System.ComponentModel;
using System.Windows;

namespace TrayTweaks;

public partial class SettingsWindow : Window
{
    public SettingsWindow(TweakManager tweaks)
    {
        InitializeComponent();

        StartWithWindows.IsChecked = StartupRegistration.IsEnabled;
        StartWithWindows.Click += (_, _) => StartupRegistration.SetEnabled(StartWithWindows.IsChecked == true);

        var rows = TweakManager.AllTweaks.Select(tweak => new TweakRow(tweak, tweaks)).ToList();
        TweakList.ItemsSource = rows;
        ThumbGrid.ItemsSource = Mouse.G604Map.ThumbGrid;
        OtherButtons.ItemsSource = Mouse.G604Map.OtherButtons;

        // Keeps the checkboxes right when a tweak is toggled from the tray menu.
        void Refresh() => rows.ForEach(row => row.Refresh());
        tweaks.Changed += Refresh;
        Closed += (_, _) => tweaks.Changed -= Refresh;
    }

    void RunAction_Click(object sender, RoutedEventArgs e) =>
        ((TweakRow)((FrameworkElement)sender).DataContext).Run();
}

public sealed class TweakRow(Tweak tweak, TweakManager tweaks) : INotifyPropertyChanged
{
    public string Name => tweak.Name;
    public string Description => tweak.Description;
    public string Glyph => tweak.Glyph;
    public Visibility ToggleVisibility => tweak is ToggleTweak ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ActionVisibility => tweak is ActionTweak ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEnabled
    {
        get => tweak is ToggleTweak toggle && tweaks.IsEnabled(toggle);
        set
        {
            if (tweak is ToggleTweak toggle)
                tweaks.SetEnabled(toggle, value);
            Refresh();
        }
    }

    public void Run()
    {
        if (tweak is ActionTweak action)
            tweaks.Run(action);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() => PropertyChanged?.Invoke(this, new(nameof(IsEnabled)));
}
