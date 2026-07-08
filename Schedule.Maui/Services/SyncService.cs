using System.Net.Http.Json;
using Schedule.Core.Models;

namespace Schedule.Maui.Services;

public class SyncService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseService _databaseService;
    private readonly string _baseApiUrl;

    public SyncService(HttpClient httpClient, DatabaseService databaseService)
    {
        _databaseService = databaseService;

        // 1. Динамічно визначаємо адресу сервера залежно від платформи, де запущено додаток
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // 10.0.2.2 — це спеціальний міст в Android-емуляторі, який веде на localhost вашого ПК.
            // Мобільні ОС не люблять локальний SSL сертифікат, тому для Android безпечніше пустити трафік через HTTP порт.
            // Перевірте у launchSettings.json вашого API, який у вас HTTP-порт (найчастіше це 5085 або 5000)
            _baseApiUrl = "http://10.0.2.2:5085/api";
        }
        else
        {
            // Для Windows, iOS-симуляторів та macOS використовуємо стандартний прямий HTTPS порт розробника
            _baseApiUrl = "https://localhost:7085/api";
        }

        // 2. Створюємо кросплатформний обробник запитів, який ігнорує помилки самопідписаних SSL-сертифікатів розробника.
        // Це критично важливо для мобільних пристроїв під час тестів на локальному комп'ютері.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler);
    }

    /// <summary>
    /// Асинхронне завантаження даних із MySQL через Web API та збереження їх у локальну SQLite базу
    /// </summary>
    public async Task<bool> SyncScheduleForGroupAsync(int groupId)
    {
        try
        {
            // Паралельно запускаємо HTTP GET запити до всіх потрібних ендпоінтів контролерів
            var groupsTask = _httpClient.GetFromJsonAsync<List<Group>>($"{_baseApiUrl}/Groups");
            var teachersTask = _httpClient.GetFromJsonAsync<List<Teacher>>($"{_baseApiUrl}/Teachers");
            var classRoomsTask = _httpClient.GetFromJsonAsync<List<ClassRoom>>($"{_baseApiUrl}/ClassRooms");
            var disciplinesTask = _httpClient.GetFromJsonAsync<List<Discipline>>($"{_baseApiUrl}/Disciplines");

            // Отримуємо уроки конкретно для обраної студентом групи
            var lessonsTask = _httpClient.GetFromJsonAsync<List<RealLesson>>($"{_baseApiUrl}/RealLessons/group/{groupId}");

            // Очікуємо завершення всіх мережевих запитів
            await Task.WhenAll(groupsTask, teachersTask, classRoomsTask, disciplinesTask, lessonsTask);

            // Витягуємо результати (якщо повернувся null, створюємо пусті списки)
            var groups = await groupsTask ?? new();
            var teachers = await teachersTask ?? new();
            var classRooms = await classRoomsTask ?? new();
            var disciplines = await disciplinesTask ?? new();
            var lessons = await lessonsTask ?? new();

            // Передаємо всі завантажені списки об'єктів у сервіс бази даних для збереження в SQLite
            _databaseService.SaveSyncedData(groups, teachers, classRooms, disciplines, lessons);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка під час HTTP-синхронізації: {ex.Message}");
            return false;
        }
    }
}