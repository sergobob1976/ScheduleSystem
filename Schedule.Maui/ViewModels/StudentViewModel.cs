using Schedule.Core.Models;
using Schedule.Core.Enums;
using Schedule.Maui.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace Schedule.Maui.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private User _user;
        private DateTime _selectedDate = DateTime.Today;
        private string? _selectedGroup;
        private string? _selectedTeacher;
        private string? _selectedClassRoom;
        private bool _isBusy;

        private const string SelectedGroupKey = "saved_group";
        private const string SelectedTeacherKey = "saved_teacher";

        private List<RealLesson> _allLessonsFromDb = new();
        public ObservableCollection<string> Groups { get; } = new();
        public ObservableCollection<string> Teachers { get; } = new();
        public ObservableCollection<string> ClassRooms { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string UserStatus => _user.IsLoggedIn
            ? $"Користувач: {_user.Username} ({_user.Role})"
            : "Вхід не виконано";

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisibleLessons)); }
        }

        public string? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    Preferences.Default.Set(SelectedGroupKey, value ?? string.Empty);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisibleLessons));
                }
            }
        }

        public string? SelectedTeacher
        {
            get => _selectedTeacher;
            set
            {
                if (_selectedTeacher != value)
                {
                    _selectedTeacher = value;
                    Preferences.Default.Set(SelectedTeacherKey, value ?? string.Empty);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisibleLessons));
                }
            }
        }

        public string? SelectedClassRoom
        {
            get => _selectedClassRoom;
            set
            {
                if (_selectedClassRoom != value)
                {
                    _selectedClassRoom = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisibleLessons));
                }
            }
        }

        public ObservableCollection<RealLesson> VisibleLessons
        {
            get
            {
                // Фільтруємо за датою і порівнюємо текстові назви з навігаційних властивостей об'єктів
                var filtered = _allLessonsFromDb.Where(l =>
                    l.LessonDate.Date == SelectedDate.Date &&
                    (string.IsNullOrEmpty(SelectedGroup) || l.Group?.Name == SelectedGroup) &&
                    (string.IsNullOrEmpty(SelectedTeacher) || l.Teacher?.Name == SelectedTeacher) &&
                    (string.IsNullOrEmpty(SelectedClassRoom) || l.ClassRoom?.Name == SelectedClassRoom)
                ).OrderBy(l => l.LessonPosition);

                return new ObservableCollection<RealLesson>(filtered);
            }
        }

        public ICommand LoginCommand { get; }

        public StudentViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _user = new User { Username = "Гість", IsLoggedIn = false, Role = UserRole.Guest };
            LoginCommand = new Command(OnLogin);
        }

        public async Task OnAppearingAsync()
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // 1. Завантаження груп
                var groupsFromDb = _databaseService.GetGroups();
                Groups.Clear();
                foreach (var g in groupsFromDb)
                {
                    if (!string.IsNullOrEmpty(g.Name)) Groups.Add(g.Name);
                }

                // 2. Завантаження вчителів
                var teachersFromDb = _databaseService.GetTeachers();
                Teachers.Clear();
                Teachers.Add(string.Empty);
                foreach (var t in teachersFromDb)
                {
                    if (!string.IsNullOrEmpty(t.Name)) Teachers.Add(t.Name);
                }

                // 3. Завантаження аудиторій
                var roomsFromDb = _databaseService.GetClassRooms();
                ClassRooms.Clear();
                ClassRooms.Add(string.Empty);
                foreach (var r in roomsFromDb)
                {
                    if (!string.IsNullOrEmpty(r.Name)) ClassRooms.Add(r.Name);
                }

                // 4. Завантаження занять
                _allLessonsFromDb = _databaseService.GetRealLessons();

                // 5. Відновлення налаштувань
                var savedGroup = Preferences.Default.Get(SelectedGroupKey, string.Empty);
                var savedTeacher = Preferences.Default.Get(SelectedTeacherKey, string.Empty);

                if (!string.IsNullOrEmpty(savedGroup) && Groups.Contains(savedGroup))
                    _selectedGroup = savedGroup;

                if (!string.IsNullOrEmpty(savedTeacher) && Teachers.Contains(savedTeacher))
                    _selectedTeacher = savedTeacher;

                OnPropertyChanged(nameof(SelectedGroup));
                OnPropertyChanged(nameof(SelectedTeacher));
                OnPropertyChanged(nameof(VisibleLessons));
            }
            catch (Exception ex)
            {
                if (Shell.Current != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.DisplayAlertAsync("Помилка БД", ex.Message, "OK");
                    });
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnLogin()
        {
            _user.Username = "Адміністратор";
            _user.IsLoggedIn = true;
            _user.Role = UserRole.Admin;
            OnPropertyChanged(nameof(UserStatus));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}