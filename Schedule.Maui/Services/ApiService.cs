using System.Net.Http.Json;
using Schedule.Core.Models;

namespace Schedule.Maui.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://localhost:7085/";

    public ApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async Task<List<RealLesson>?> GetRealLessonsAsync(int groupId) =>
        await TryGetAsync<List<RealLesson>>($"api/RealLessons/group/{groupId}");

    public async Task<List<Group>?> GetGroupsAsync() => await TryGetAsync<List<Group>>("api/Groups");
    public async Task<List<Discipline>?> GetDisciplinesAsync() => await TryGetAsync<List<Discipline>>("api/Disciplines");
    public async Task<List<Teacher>?> GetTeachersAsync() => await TryGetAsync<List<Teacher>>("api/Teachers");
    public async Task<List<ClassRoom>?> GetClassRoomsAsync() => await TryGetAsync<List<ClassRoom>>("api/ClassRooms");
    public async Task<List<Semester>?> GetSemestersAsync() => await TryGetAsync<List<Semester>>("api/Semesters");

    private async Task<T?> TryGetAsync<T>(string url) where T : class
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Мережева помилка ({url}): {ex.Message}");
            return null;
        }
    }
}