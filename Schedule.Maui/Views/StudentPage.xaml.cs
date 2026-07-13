using Schedule.Maui.ViewModels;

namespace Schedule.Maui.Views;

public partial class StudentPage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    public StudentPage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private void OnStudentModeClicked(object? sender, EventArgs e) =>
        _viewModel.SelectStudentMode();

    private void OnTeacherModeClicked(object? sender, EventArgs e) =>
        _viewModel.SelectTeacherMode();

    private async void OnSaveSelectionClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveSelectionAsync();

    private void OnChangeSelectionClicked(object? sender, EventArgs e) =>
        _viewModel.BeginConfiguration();

    private void OnCancelSelectionClicked(object? sender, EventArgs e) =>
        _viewModel.CancelConfiguration();

    private async void OnRefreshing(object? sender, EventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void OnOpenLinkClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string url } ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        await Launcher.Default.OpenAsync(uri);
    }
}
