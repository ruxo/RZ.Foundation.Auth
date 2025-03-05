using Blazor.Firebase.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using RZ.Foundation;
using RZ.Foundation.Blazor.Auth;
using RZ.Foundation.Blazor.Auth.Views;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
       .AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services
       .AddMudServices() // register MudBlazor
       .AddRzMudBlazorSettings() // register RZ MudBlazor settings
       .AddScoped<IAfterSignInHandler, Blazor.Firebase.Auth.AfterSignInHandler>();  // register App registration flow handler

var firebase = new FirebaseAuthenticationModule();
await firebase.InstallServices(builder);    // Install Firebase services

builder.Services
       .AddScoped<LoginViewModel>(sp => {
            var vm = sp.Create<LoginViewModel>();
            vm.Title = "Login to Firebase Example";
            return vm;
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
app.MapStaticAssets();
await firebase.InstallMiddleware(builder, app); // Install Firebase Auth middleware
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddAdditionalAssemblies(typeof(FirebaseAuthentication).Assembly);

app.Run();

partial class Program
{
    public static readonly InteractiveServerRenderMode ServerRender = new(prerender: false);
}