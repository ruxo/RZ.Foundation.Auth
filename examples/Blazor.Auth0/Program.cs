using Blazor.Auth0.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using RZ.Foundation;
using RZ.Foundation.Blazor.Auth0;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddAuth0Authentication()
       .Services
       .AddMudServices()
       .AddRzMudBlazorSettings()
       .AddRazorComponents()
       .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication().UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

partial class Program
{
    public static readonly InteractiveServerRenderMode ServerRender = new(prerender: false);
}
