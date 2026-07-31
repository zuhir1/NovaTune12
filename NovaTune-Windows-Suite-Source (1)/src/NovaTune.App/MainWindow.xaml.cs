using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaTune.App.ViewModels;

namespace NovaTune.App;

public sealed partial class MainWindow : Window
{
    public DashboardViewModel ViewModel { get; }

    public MainWindow(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt };
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));
        Navigation.SelectedItem = Navigation.MenuItems[0];
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag?.ToString() == "dashboard")
        {
            DashboardPage.Visibility = Visibility.Visible;
            ModulePage.Visibility = Visibility.Collapsed;
            return;
        }
        DashboardPage.Visibility = Visibility.Collapsed;
        ModulePage.Visibility = Visibility.Visible;
        ModuleTitle.Text = args.SelectedItemContainer?.Content?.ToString() ?? "Module";
    }

    private async void PreviewCleanup_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PreviewCleanupCommand.ExecuteAsync(null);
        if (ViewModel.CleanupCandidates.Count == 0) return;
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(new TextBlock { Text = $"{ViewModel.CleanupCandidates.Count} preview items are selected ({DashboardViewModel.FormatBytes(ViewModel.CleanupCandidates.Sum(x => x.SizeBytes))}).", TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = "NovaTune will first create a Windows restore point, then move the files to quarantine. It will not permanently delete them.", TextWrapping = TextWrapping.Wrap });
        var dialog = new ContentDialog
        {
            XamlRoot = Navigation.XamlRoot,
            Title = "Approve safe cleanup?",
            Content = content,
            PrimaryButtonText = "Create restore point and clean",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var result = await ViewModel.ExecuteCleanupAsync(ViewModel.CreateApprovedPreview());
        var resultDialog = new ContentDialog { XamlRoot = Navigation.XamlRoot, Title = result.Succeeded ? "Cleanup complete" : "Cleanup stopped", Content = result.Message, CloseButtonText = "OK" };
        await resultDialog.ShowAsync();
    }
}
