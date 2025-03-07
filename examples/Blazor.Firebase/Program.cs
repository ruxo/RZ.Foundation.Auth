using Blazor.Firebase.Components;
using Blazor.Firebase.Components.Layout;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
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
       .AddCascadingValue("RzBlazor.Theme", _ => CreateExampleTheme())

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

    internal static MudTheme CreateExampleTheme() => new() {
        PaletteLight = new PaletteLight {
            Primary = "#2c85d2", // 208 64.9 50
            Secondary = "#ac5391", // 318 35 50
            Tertiary = "#d79d28", // 40 69 50

            // Background = "#F8F9FC", // 225 40 98
            Background = "#fcfbf7", // 39 50 98
            TextPrimary = "#191C1E", // 204 9.1 10.8

            //Surface = "#fcf7ee", // 39 70 96
            Surface = "#FAF6F0", // 39 50 96.1

            Error = "#ff0000", // 0 100 50
            Warning = "#ffe680", // 48 100 75
            WarningContrastText = "#333333",  // 48 0 20

            AppbarBackground = "#c9d2ed", // 225 50 86
            DrawerBackground = "#f4f4f5", // 240 5 96
            DrawerText = "#191C1E", // 204 9.1 10.8
            AppbarText = "#191C1E", // 204 9.1 10.8

            LinesDefault = "#70787E" // 206 5.9 46.7
        },
        LayoutProperties = new() {
            DefaultBorderRadius = "1rem"
        },
        Typography = new Typography {
            Default = new DefaultTypography {
                FontFamily = ["Noto Sans Thai", "Roboto", "Arial", "sans-serif"]
            },
            H6 = new H6Typography { FontSize = "0.875rem" },
            H5 = new H5Typography { FontSize = "1rem" },
            H4 = new H4Typography { FontSize = "1.125rem" },
            H3 = new H3Typography { FontSize = "1.25rem" },
            H2 = new H2Typography { FontSize = "1.5rem" },
            H1 = new H1Typography { FontSize = "1.875rem" },
        }
    };
}