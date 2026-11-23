using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TrayTweaks.Secrets;

namespace TrayTweaks;

public partial class SettingsWindow : Window
{
    public SettingsWindow(TweakManager tweaks)
    {
        InitializeComponent();
        WindowBackdrop.UseGlass(this);

        StartWithWindows.IsChecked = StartupRegistration.IsEnabled;
        StartWithWindows.Click += (_, _) =>
        {
            try
            {
                StartupRegistration.SetEnabled(StartWithWindows.IsChecked == true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Start at sign-in could not be changed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            StartWithWindows.IsChecked = StartupRegistration.IsEnabled;
        };

        var rows = TweakManager.AllTweaks.Select(tweak => new TweakRow(tweak, tweaks)).ToList();
        TweakList.ItemsSource = rows;
        ThumbGrid.ItemsSource = Mouse.G604Map.ThumbGrid;
        OtherButtons.ItemsSource = Mouse.G604Map.OtherButtons;
        SecretList.ItemsSource = SecretStore.Slots.Select(slot => new SecretRow(slot)).ToList();

        // Keeps the checkboxes right when a tweak is toggled from the tray menu.
        void Refresh() => rows.ForEach(row => row.Refresh());
        tweaks.Changed += Refresh;
        Closed += (_, _) => tweaks.Changed -= Refresh;
    }

    void RunAction_Click(object sender, RoutedEventArgs e) =>
        ((TweakRow)((FrameworkElement)sender).DataContext).Run();

    // The parts of one editor on the Passwords tab, found from any control inside it.
    sealed record SecretEditor(PasswordBox Dots, TextBox Plain, ToggleButton Show, SecretRow Row)
    {
        public static SecretEditor Of(object sender)
        {
            var element = (FrameworkElement)sender;
            while (element is not StackPanel { Orientation: Orientation.Horizontal })
                element = (FrameworkElement)element.Parent;
            var panel = (StackPanel)element;
            var boxes = panel.Children.OfType<Grid>().Single();
            return new(
                boxes.Children.OfType<PasswordBox>().Single(),
                boxes.Children.OfType<TextBox>().Single(),
                panel.Children.OfType<ToggleButton>().Single(),
                (SecretRow)panel.DataContext);
        }

        public string Text => Show.IsChecked == true ? Plain.Text : Dots.Password;
    }

    // Runs again each time the tab is shown, so it restores what was being typed, not just what is saved.
    void SecretEditor_Loaded(object sender, RoutedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        editor.Dots.Password = editor.Row.Draft;
    }

    void SecretDots_Changed(object sender, RoutedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        if (editor.Show.IsChecked != true)
            editor.Row.Draft = editor.Dots.Password;
    }

    void SecretPlain_Changed(object sender, TextChangedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        if (editor.Show.IsChecked == true)
            editor.Row.Draft = editor.Plain.Text;
    }

    void SecretShow_Click(object sender, RoutedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        var show = editor.Show.IsChecked == true;
        if (show)
            editor.Plain.Text = editor.Row.Draft;
        else
            editor.Dots.Password = editor.Row.Draft;
        editor.Plain.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        editor.Dots.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        editor.Show.Content = show ? "Hide" : "Show";
        if (!show)
            editor.Plain.Clear();
    }

    void SaveSecret_Click(object sender, RoutedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        if (editor.Text.Length == 0)
            editor.Row.Remove();
        else
            editor.Row.Save(editor.Text);
    }

    void RemoveSecret_Click(object sender, RoutedEventArgs e)
    {
        var editor = SecretEditor.Of(sender);
        editor.Row.Remove();
        editor.Dots.Clear();
        editor.Plain.Clear();
    }
}

public sealed class SecretRow(SecretSlot slot) : INotifyPropertyChanged
{
    string _saved = SecretStore.Default.Get(slot.Id) ?? "";
    string? _draft;

    public string Name => slot.Name;
    public string Description => slot.Description;

    /// <summary>What is in the box right now, saved or not.</summary>
    public string Draft
    {
        get => _draft ?? _saved;
        set
        {
            _draft = value;
            PropertyChanged?.Invoke(this, new(nameof(Status)));
        }
    }

    public string Status => Draft != _saved ? "Unsaved" : _saved.Length == 0 ? "Not set" : "Saved";

    public void Save(string value)
    {
        SecretStore.Default.Set(slot.Id, value);
        _saved = value;
        Draft = value;
    }

    public void Remove()
    {
        SecretStore.Default.Remove(slot.Id);
        _saved = "";
        Draft = "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
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
