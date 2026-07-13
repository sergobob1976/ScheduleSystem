using System.Net.Http.Json;
using Schedule.Core.DTOs;

namespace Schedule.Maui.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        var handler = new HttpClientHandler();

#if DEBUG && !ANDROID
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

        _httpClient = new HttpClient(handler)
        {
#if ANDROID
            BaseAddress = new Uri("http://10.0.2.2:5271/"),
#else
            BaseAddress = new Uri("https://localhost:7085/"),
#endif
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<MobileScheduleOptionsResponse> GetOptionsAsync() =>
        await _httpClient.GetFromJsonAsync<MobileScheduleOptionsResponse>(
            "api/mobile-schedule/options")
        ?? new MobileScheduleOptionsResponse();

    public async Task<List<MobileScheduleLessonResponse>> GetLessonsAsync(
        bool forTeacher,
        int filterId,
        DateTime date)
    {
        var filterType = forTeacher ? "teacher" : "group";
        return await _httpClient.GetFromJsonAsync<List<MobileScheduleLessonResponse>>(
            $"api/mobile-schedule/{filterType}/{filterId}/date/{date:yyyy-MM-dd}")
            ?? [];
    }
}
