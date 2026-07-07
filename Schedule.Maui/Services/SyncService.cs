using System.Net.Http.Json;
using Schedule.Core.Models;

namespace Schedule.Maui.Services;

public class SyncService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseService _databaseService;
    private readonly string _baseApiUrl;

    // Використовуємо ін'єктований httpClient, який налаштовується в MauiProgram
    public SyncService(HttpClient httpClient, DatabaseService databaseService)
    {
        _httpClient = httpClient;
        _databaseService = databaseService;

        // Для Windows використовуємо вашу перевірену HTTPS адресу
        _baseApiUrl = "https://localhost:7085/api";
    }

    public async Task<bool> SyncScheduleForGroupAsync(int groupId)
    {
        try
        {
            // Складаємо точні URL та робимо запити
            var groups = await _httpClient.GetFromJsonAsync<List<Group>>($"{_baseApiUrl}/Groups") ?? new();
            var teachers = await _httpClient.GetFromJsonAsync<List<Teacher>>($"{_baseApiUrl}/Teachers") ?? new();
            var classRooms = await _httpClient.GetFromJsonAsync<List<ClassRoom>>($"{_baseApiUrl}/ClassRooms") ?? new();
            var disciplines = await _httpClient.GetFromJsonAsync<List<Discipline>>($"{_baseApiUrl}/Disciplines") ?? new();
            var lessons = await _httpClient.GetFromJsonAsync<List<RealLesson>>($"{_baseApiUrl}/RealLessons/group/{groupId}") ?? new();

            _databaseService.SaveSyncedData(groups, teachers, classRooms, disciplines, lessons);

            return true;
        }
        catch (Exception ex)
        {
            // Виводимо точну помилку, якщо щось піде не так
            throw new Exception($"Помилка запиту до {_baseApiUrl}: {ex.Message}. {ex.InnerException?.Message}");
        }
    }
}