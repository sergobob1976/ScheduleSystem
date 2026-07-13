using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Maui.Services;

namespace Schedule.Maui.ViewModels;

public enum ScheduleViewerMode
{
    Student,
    Teacher
}

public class ScheduleViewModel : INotifyPropertyChanged
{
    private const string ModePreferenceKey = "ScheduleViewerMode";
    private const string FilterIdPreferenceKey = "ScheduleFilterId";
    private const string FilterNamePreferenceKey = "ScheduleFilterName";

    private readonly ApiService _api;
    private readonly ScheduleCacheService _cache;
    private MobileScheduleOptionsResponse _options = new();
    private ScheduleViewerMode _mode = ScheduleViewerMode.Student;
    private ScheduleViewerMode _configurationMode = ScheduleViewerMode.Student;
    private MobileScheduleFilterOption? _selectedFilter;
    private MobileScheduleDay? _selectedDay;
    private bool _isInitialized;
    private DateTime _daysStartDate;
    private bool _isConfigured;
    private bool _isLoadingOptions;
    private bool _isRefreshing;
    private string _selectionName = string.Empty;
    private string _configurationError = string.Empty;

    public ObservableCollection<MobileScheduleFilterOption> AvailableFilters { get; } = [];
    public ObservableCollection<MobileScheduleDay> Days { get; } = [];

    public ScheduleViewModel(ApiService api, ScheduleCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public bool IsConfigured
    {
        get => _isConfigured;
        private set
        {
            if (_isConfigured == value) return;
            _isConfigured = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotConfigured));
        }
    }

    public bool IsNotConfigured => !IsConfigured;

    public bool IsLoadingOptions
    {
        get => _isLoadingOptions;
        private set
        {
            if (_isLoadingOptions == value) return;
            _isLoadingOptions = value;
            OnPropertyChanged();
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value) return;
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public bool IsStudentMode => _configurationMode == ScheduleViewerMode.Student;
    public bool IsTeacherMode => _configurationMode == ScheduleViewerMode.Teacher;
    public bool CanCancelConfiguration => Preferences.ContainsKey(FilterIdPreferenceKey);
    public string FilterPrompt => IsStudentMode ? "Оберіть свою групу" : "Оберіть себе у списку";
    public string CurrentSelection => _mode == ScheduleViewerMode.Student
        ? $"Група · {_selectionName}"
        : $"Викладач · {_selectionName}";

    public string ConfigurationError
    {
        get => _configurationError;
        private set
        {
            if (_configurationError == value) return;
            _configurationError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasConfigurationError));
        }
    }

    public bool HasConfigurationError => !string.IsNullOrWhiteSpace(ConfigurationError);

    public MobileScheduleFilterOption? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value) return;
            _selectedFilter = value;
            ConfigurationError = string.Empty;
            OnPropertyChanged();
        }
    }

    public MobileScheduleDay? SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (_selectedDay == value) return;
            _selectedDay = value;
            OnPropertyChanged();
            if (value is not null)
            {
                _ = LoadDayAsync(value);
            }
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            if (_daysStartDate != DateTime.Today)
            {
                CreateDays();
            }

            return;
        }

        _isInitialized = true;

        RestoreSelection();
        CreateDays();

        if (IsConfigured && SelectedDay is not null)
        {
            await LoadDayAsync(SelectedDay);
        }

        await LoadOptionsAsync();
    }

    public void SelectStudentMode() => SetConfigurationMode(ScheduleViewerMode.Student);
    public void SelectTeacherMode() => SetConfigurationMode(ScheduleViewerMode.Teacher);

    public async Task SaveSelectionAsync()
    {
        if (SelectedFilter is null)
        {
            ConfigurationError = FilterPrompt + ".";
            return;
        }

        _mode = _configurationMode;
        _selectionName = SelectedFilter.Name;
        Preferences.Set(ModePreferenceKey, _mode.ToString());
        Preferences.Set(FilterIdPreferenceKey, SelectedFilter.Id);
        Preferences.Set(FilterNamePreferenceKey, SelectedFilter.Name);

        OnPropertyChanged(nameof(CurrentSelection));
        OnPropertyChanged(nameof(CanCancelConfiguration));
        IsConfigured = true;
        CreateDays();
        if (SelectedDay is not null)
        {
            await LoadDayAsync(SelectedDay, true);
        }
    }

    public void BeginConfiguration()
    {
        _configurationMode = _mode;
        PopulateAvailableFilters();
        IsConfigured = false;
        ConfigurationError = string.Empty;
        OnPropertyChanged(nameof(CanCancelConfiguration));
        NotifyConfigurationModeChanged();
    }

    public void CancelConfiguration()
    {
        if (!CanCancelConfiguration) return;
        RestoreSelection();
        IsConfigured = true;
    }

    public async Task RefreshAsync()
    {
        if (SelectedDay is null) return;
        IsRefreshing = true;
        try
        {
            await LoadDayAsync(SelectedDay, true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void RestoreSelection()
    {
        if (!Preferences.ContainsKey(FilterIdPreferenceKey))
        {
            IsConfigured = false;
            return;
        }

        _mode = Enum.TryParse<ScheduleViewerMode>(
            Preferences.Get(ModePreferenceKey, ScheduleViewerMode.Student.ToString()),
            out var savedMode)
            ? savedMode
            : ScheduleViewerMode.Student;
        _configurationMode = _mode;
        _selectionName = Preferences.Get(FilterNamePreferenceKey, string.Empty);
        IsConfigured = !string.IsNullOrWhiteSpace(_selectionName);
        OnPropertyChanged(nameof(CurrentSelection));
    }

    private async Task LoadOptionsAsync()
    {
        IsLoadingOptions = true;
        ConfigurationError = string.Empty;
        try
        {
            _options = await _api.GetOptionsAsync();
            await _cache.SaveOptionsAsync(_options);
            PopulateAvailableFilters();
        }
        catch (Exception)
        {
            var cachedOptions = await _cache.GetOptionsAsync();
            if (cachedOptions is not null)
            {
                _options = cachedOptions.Value;
                PopulateAvailableFilters();
                if (!IsConfigured)
                {
                    ConfigurationError = $"Немає з’єднання. Показано список, збережений {cachedOptions.UpdatedAt:dd.MM.yyyy HH:mm}.";
                }
            }
            else if (!IsConfigured)
            {
                ConfigurationError = "Не вдалося завантажити список груп і викладачів. Перевірте з’єднання та повторіть спробу.";
            }
        }
        finally
        {
            IsLoadingOptions = false;
        }
    }

    private void SetConfigurationMode(ScheduleViewerMode mode)
    {
        if (_configurationMode == mode) return;
        _configurationMode = mode;
        PopulateAvailableFilters();
        ConfigurationError = string.Empty;
        NotifyConfigurationModeChanged();
    }

    private void NotifyConfigurationModeChanged()
    {
        OnPropertyChanged(nameof(IsStudentMode));
        OnPropertyChanged(nameof(IsTeacherMode));
        OnPropertyChanged(nameof(FilterPrompt));
    }

    private void PopulateAvailableFilters()
    {
        var savedId = Preferences.Get(FilterIdPreferenceKey, 0);
        var source = IsStudentMode ? _options.Groups : _options.Teachers;

        AvailableFilters.Clear();
        foreach (var item in source)
        {
            AvailableFilters.Add(item);
        }

        SelectedFilter = _configurationMode == _mode
            ? AvailableFilters.FirstOrDefault(item => item.Id == savedId)
            : null;
    }

    private void CreateDays()
    {
        _daysStartDate = DateTime.Today;
        Days.Clear();
        for (var offset = 0; offset < 14; offset++)
        {
            Days.Add(new MobileScheduleDay(DateTime.Today.AddDays(offset)));
        }

        SelectedDay = Days.FirstOrDefault();
    }

    private async Task LoadDayAsync(MobileScheduleDay day, bool force = false)
    {
        if (!IsConfigured || day.IsLoading || (day.IsLoaded && !force)) return;

        day.IsLoading = true;
        day.ErrorMessage = string.Empty;
        day.InformationMessage = string.Empty;
        try
        {
            var filterId = Preferences.Get(FilterIdPreferenceKey, 0);
            var forTeacher = _mode == ScheduleViewerMode.Teacher;
            var lessons = await _api.GetLessonsAsync(
                forTeacher,
                filterId,
                day.Date);

            SetLessons(day, lessons);
            await _cache.SaveDayAsync(forTeacher, filterId, day.Date, lessons);
            day.IsLoaded = true;
        }
        catch (Exception)
        {
            var filterId = Preferences.Get(FilterIdPreferenceKey, 0);
            var cachedDay = await _cache.GetDayAsync(
                _mode == ScheduleViewerMode.Teacher,
                filterId,
                day.Date);

            if (cachedDay is not null)
            {
                SetLessons(day, cachedDay.Value);
                day.InformationMessage = $"Офлайн · збережено {cachedDay.UpdatedAt:dd.MM.yyyy HH:mm}";
                day.IsLoaded = true;
            }
            else
            {
                day.ErrorMessage = "Немає з’єднання і збереженого розкладу на цей день. Потягніть екран униз, щоб повторити.";
            }
        }
        finally
        {
            day.IsLoading = false;
        }
    }

    private void SetLessons(
        MobileScheduleDay day,
        IEnumerable<MobileScheduleLessonResponse> lessons)
    {
        day.Lessons.Clear();
        foreach (var lesson in lessons)
        {
            day.Lessons.Add(new MobileScheduleLessonItem(lesson, _mode));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MobileScheduleDay : INotifyPropertyChanged
{
    private bool _isLoading;
    private bool _isLoaded;
    private string _errorMessage = string.Empty;
    private string _informationMessage = string.Empty;

    public MobileScheduleDay(DateTime date)
    {
        Date = date.Date;
    }

    public DateTime Date { get; }
    public ObservableCollection<MobileScheduleLessonItem> Lessons { get; } = [];
    public string HeaderTitle => Date == DateTime.Today
        ? "Сьогодні"
        : Date == DateTime.Today.AddDays(1)
            ? "Завтра"
            : Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("uk-UA"));
    public string HeaderSubtitle => Date <= DateTime.Today.AddDays(1)
        ? Date.ToString("dddd, dd.MM.yyyy", CultureInfo.GetCultureInfo("uk-UA"))
        : string.Empty;
    public string HeaderColor => Date == DateTime.Today
        ? "#2E7D32"
        : Date == DateTime.Today.AddDays(1)
            ? "#1565C0"
            : "#5B6472";

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (_isLoaded == value) return;
            _isLoaded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string InformationMessage
    {
        get => _informationMessage;
        set
        {
            if (_informationMessage == value) return;
            _informationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInformation));
        }
    }

    public bool HasInformation => !string.IsNullOrWhiteSpace(InformationMessage);
    public bool IsEmpty => IsLoaded && !IsLoading && !HasError && Lessons.Count == 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MobileScheduleLessonItem
{
    public MobileScheduleLessonItem(
        MobileScheduleLessonResponse lesson,
        ScheduleViewerMode mode)
    {
        LessonPosition = lesson.LessonPosition;
        DisciplineName = lesson.DisciplineName;
        LessonTypeName = lesson.LessonTypeName;
        AudienceLabel = mode == ScheduleViewerMode.Student ? "Викладач" : "Група";
        AudienceName = mode == ScheduleViewerMode.Student ? lesson.TeacherName : lesson.GroupName;
        ClassRoomName = lesson.ClassRoomName;
        IsCancelled = lesson.Status == RealLessonStatus.Cancelled;
        ConferenceLink = lesson.ConferenceLink;
        ResourceLink = lesson.ResourceLink;
    }

    public int LessonPosition { get; }
    public string DisciplineName { get; }
    public string LessonTypeName { get; }
    public string AudienceLabel { get; }
    public string AudienceName { get; }
    public string AudienceText => $"{AudienceLabel}: {AudienceName}";
    public string? ClassRoomName { get; }
    public bool HasClassRoom => !string.IsNullOrWhiteSpace(ClassRoomName);
    public bool IsCancelled { get; }
    public string StatusText => IsCancelled ? "Заняття скасовано" : string.Empty;
    public string? ConferenceLink { get; }
    public string? ResourceLink { get; }
    public bool HasConferenceLink => !string.IsNullOrWhiteSpace(ConferenceLink);
    public bool HasResourceLink => !string.IsNullOrWhiteSpace(ResourceLink);
}
