using Auth0.AspNetCore.Authentication;
using Blazor.Auth0.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using RZ.Foundation;
using RZ.Foundation.Helpers;
using RZ.Foundation.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
       .AddCascadingAuthenticationState()
       .AddMudServices()
       .AddRzMudBlazorSettings()
       .AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services
       .AddAuth0WebAppAuthentication(opts => {
            var dict = KeyValueString.Parse(builder.Configuration.GetConnectionString("Auth0") ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration));
            opts.Domain = dict["Domain"] ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration);
            opts.ClientId = dict["ClientId"] ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration);
        });

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
