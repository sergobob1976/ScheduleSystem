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
        _httpClient = httpClient;
        _databaseService = databaseService;
        _baseApiUrl = "http://10.0.2.2:5170/api";
    }

    public async Task<bool> SyncScheduleForGroupAsync(int groupId)
    {
        try
        {
            var groupsTask = _httpClient.GetFromJsonAsync<List<Group>>($"{_baseApiUrl}/Groups");
            var teachersTask = _httpClient.GetFromJsonAsync<List<Teacher>>($"{_baseApiUrl}/Teachers");

            // ВИПРАВЛЕНО: Змінено тип з ClassRooms на ClassRoom
            var classRoomsTask = _httpClient.GetFromJsonAsync<List<ClassRoom>>($"{_baseApiUrl}/ClassRooms");
            var disciplinesTask = _httpClient.GetFromJsonAsync<List<Discipline>>($"{_baseApiUrl}/Disciplines");
            var lessonsTask = _httpClient.GetFromJsonAsync<List<RealLesson>>($"{_baseApiUrl}/RealLessons/group/{groupId}");

            await Task.WhenAll(groupsTask, teachersTask, classRoomsTask, disciplinesTask, lessonsTask);

            var groups = await groupsTask ?? new();
            var teachers = await teachersTask ?? new();
            var classRooms = await classRoomsTask ?? new();
            var disciplines = await disciplinesTask ?? new();
            var lessons = await lessonsTask ?? new();

            _databaseService.SaveSyncedData(groups, teachers, classRooms, disciplines, lessons);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка синхронізації з API: {ex.Message}");
            return false;
        }
    }
}