using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Schedule.Core.DTOs;

namespace Schedule.Web.Authentication;

public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly HttpClient _http;

    public CookieAuthenticationStateProvider(HttpClient http) => _http = http;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/authentication/me");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new AuthenticationState(Anonymous);

            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
            return new AuthenticationState(user is null ? Anonymous : CreatePrincipal(user));
        }
        catch (HttpRequestException)
        {
            return new AuthenticationState(Anonymous);
        }
    }

    public async Task<HttpResponseMessage> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/authentication/login", request);
        if (response.IsSuccessStatusCode)
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return response;
    }

    public async Task LogoutAsync()
    {
        await _http.PostAsync("api/authentication/logout", null);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal CreatePrincipal(AuthenticatedUserResponse user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie"));
    }
}
