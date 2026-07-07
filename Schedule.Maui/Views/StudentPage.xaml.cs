using Schedule.Maui.Services;
using Schedule.Maui.ViewModels;

namespace Schedule.Maui.Views;

public partial class StudentPage : ContentPage
{
    private readonly SyncService _syncService;
    private readonly StudentViewModel _viewModel;

    public StudentPage(StudentViewModel viewModel, SyncService syncService)
    {
        InitializeComponent();

        _syncService = syncService;
        _viewModel = viewModel;

        BindingContext = _viewModel;
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsBusy) return;

        try
        {
            _viewModel.IsBusy = true;

            int groupId = _viewModel.SelectedGroup?.Id ?? 1;

            bool isSuccess = await _syncService.SyncScheduleForGroupAsync(groupId);

            if (isSuccess)
            {
                _viewModel.InitializeAsync();
                await DisplayAlertAsync("Успіх", "Розклад успішно синхронізовано з сервером!", "OK");
            }
            else
            {
                await DisplayAlertAsync("Офлайн-режим", "Не вдалося з'єднатися з сервером. Відображаються раніше збережені дані.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Помилка синхронізації", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            _viewModel.IsBusy = false;
        }
    }
}