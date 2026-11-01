using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RtlTerminal;

public partial class DirectoryHistoryWindow : Window
{
    private readonly DirectoryHistoryStore _store;
    public DirectoryHistoryEntry? SelectedDirectory { get; private set; }
    public DirectoryHistoryWindow(DirectoryHistoryStore store)
    {
        _store = store; InitializeComponent(); SourceInitialized += (_, _) => ModernWindowChrome.Apply(this, false); Refresh();
    }
    private void Refresh()
    {
        if (HistoryList is null) return;
        var all = _store.Snapshot(); var query = SearchBox.Text.Trim();
        var matches = all.Where(e => e.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase) || e.Source.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        HistoryList.ItemsSource = matches;
        HistorySummary.Text = UiText.Format("{0} of {1} recent directories · {2}. Hover a row to see its first visit.", all.Count, DirectoryHistoryStore.Capacity, UiText.Get(_store.Enabled ? "History is on" : "History is off"));
        EmptyMessage.Text = UiText.Get(all.Count == 0 ? "No directories recorded yet." : "No matching directories.");
        EmptyMessage.Visibility = matches.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.IsEnabled = all.Count > 0;
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) => Refresh();
    private void History_SelectionChanged(object sender, SelectionChangedEventArgs e) => OpenButton.IsEnabled = HistoryList.SelectedItem is DirectoryHistoryEntry;
    private void History_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(HistoryList, e.OriginalSource as DependencyObject) is ListBoxItem) Open_Click(sender, e);
    }
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not DirectoryHistoryEntry entry) return;
        SelectedDirectory = entry; DialogResult = true;
    }
    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (UiText.Show(this, "Clear all recorded directories? New visits will still be recorded while history is enabled.", "Clear directory history", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _store.Clear(); Refresh(); }
        catch (Exception) { UiText.Show(this, "Unable to clear directory history. Please try again.", "Directory history"); }
    }
}
