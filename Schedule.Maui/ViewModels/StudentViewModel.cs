using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Schedule.Core.Models;
using Schedule.Maui.Services;
using Microsoft.Data.Sqlite;
using System.Data;
using Dapper;

namespace Schedule.Maui.ViewModels;

public class StudentViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private DateTime _selectedDate = DateTime.Today;
    private Group? _selectedGroup;
    private bool _isBusy;

    public ObservableCollection<RealLesson> AllLessons { get; set; } = new();
    public ObservableCollection<RealLesson> VisibleLessons { get; set; } = new();
    public ObservableCollection<Group> Groups { get; set; } = new();

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            _selectedDate = value;
            OnPropertyChanged();
            FilterLessons();
        }
    }

    public Group? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            _selectedGroup = value;
            OnPropertyChanged();
            FilterLessons();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    public StudentViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeAsync();
    }

    public void InitializeAsync()
    {
        try
        {
            IsBusy = true;

            // 1. Завантажуємо всі уроки з локальної SQLite бази даних
            var lessons = _databaseService.GetRealLessons();
            AllLessons.Clear();
            foreach (var lesson in lessons)
            {
                AllLessons.Add(lesson);
            }

            // 2. Витягуємо унікальний список груп з таблиці Groups
            LoadLocalGroups();

            // 3. Фільтруємо розклад для поточного відображення на екрані
            FilterLessons();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing ViewModel: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadLocalGroups()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db");
            using IDbConnection db = new SqliteConnection($"Data Source={dbPath}");

            // Запит через Dapper до локальної таблиці груп
            var groupsList = db.Query<Group>("SELECT * FROM Groups").ToList();

            Groups.Clear();
            foreach (var g in groupsList)
            {
                Groups.Add(g);
            }

            // Якщо групи існують і нічого ще не обрано, вибираємо першу за замовчуванням
            if (Groups.Count > 0 && SelectedGroup == null)
            {
                SelectedGroup = Groups[0];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Зараз локальна база порожня: {ex.Message}");
        }
    }

    private void FilterLessons()
    {
        VisibleLessons.Clear();

        // Фільтруємо за обраною датою та обраною у Picker групою
        var filtered = AllLessons.Where(l =>
            l.LessonDate.Date == SelectedDate.Date &&
            (SelectedGroup == null || l.Group?.Id == SelectedGroup.Id)
        ).OrderBy(l => l.LessonPosition);

        foreach (var lesson in filtered)
        {
            VisibleLessons.Add(lesson);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}