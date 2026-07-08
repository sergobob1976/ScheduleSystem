using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Schedule.Core.Models;
using Schedule.Maui.Services;

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

        // Запускаємо первинне завантаження даних при створенні ViewModel
        _ = InitializeAsync();
    }

    /// <summary>
    /// Повне зчитування актуальних даних (груп та занять) із локальної бази SQLite
    /// </summary>
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            // 1. Завантажуємо свіжі групи з SQLite сервісу
            var groupsFromDb = _databaseService.GetGroups();

            // Зберігаємо поточну обрану групу, щоб вибір не скидався при синхронізації
            var previousSelectedId = SelectedGroup?.Id;

            Groups.Clear();
            foreach (var g in groupsFromDb)
            {
                Groups.Add(g);
            }

            // Відновлюємо вибір групи або ставимо першу за замовчуванням
            if (Groups.Count > 0)
            {
                if (previousSelectedId.HasValue)
                {
                    SelectedGroup = Groups.FirstOrDefault(g => g.Id == previousSelectedId.Value) ?? Groups[0];
                }
                else if (SelectedGroup == null)
                {
                    SelectedGroup = Groups[0];
                }
            }

            // 2. Завантажуємо заняття
            var lessonsFromDb = _databaseService.GetRealLessons();

            AllLessons.Clear();
            foreach (var lesson in lessonsFromDb)
            {
                AllLessons.Add(lesson);
            }

            // 3. Оновлюємо відображення на екрані згідно з фільтрами
            FilterLessons();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка ініціалізації ViewModel: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
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