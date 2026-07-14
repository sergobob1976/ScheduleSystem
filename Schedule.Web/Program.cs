using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Schedule.Web;
using Schedule.Web.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CookieAuthenticationStateProvider>());
builder.Services.AddScoped(_ => new CookieHttpMessageHandler
{
    InnerHandler = new HttpClientHandler()
});

var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredApiBaseUrl;
var apiBaseAddress = new Uri(
    apiBaseUrl.EndsWith('/') ? apiBaseUrl : $"{apiBaseUrl}/");

builder.Services.AddScoped(provider => new HttpClient(
    provider.GetRequiredService<CookieHttpMessageHandler>())
{
    BaseAddress = apiBaseAddress
});

await builder.Build().RunAsync();
