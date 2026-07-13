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
builder.Services.AddScoped(provider => new HttpClient(
    provider.GetRequiredService<CookieHttpMessageHandler>())
{
    BaseAddress = new Uri("https://localhost:7085")
});

await builder.Build().RunAsync();
