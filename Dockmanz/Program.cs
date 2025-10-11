using Dockmanz.Components;
using Dockmanz.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<DockerService>()
    .AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

app.UseHsts();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
