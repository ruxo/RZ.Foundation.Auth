using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RZ.AspNet;
using RZ.Foundation.Blazor.Auth.Views;

namespace RZ.Foundation.Blazor.Auth;

public static class FirebaseAuthentication
{
    public const string Scheme = "Firebase";

    public static AppModule Module(Action<AuthorizationOptions>? authOptions = null)
        => new FirebaseAuthenticationModule(authOptions);

    public static IServiceCollection AddFirebaseAuthentication(this IServiceCollection services, Action<AuthorizationOptions>? authOptions = null) {
        services
           .AddCascadingAuthenticationState()
           .AddSingleton<FirebaseAuthService>()
           .AddRzAuth(authOptions)
           .AddAuthentication(defaultScheme: Scheme)
           .AddCookie(Scheme, opts => {
                opts.LoginPath = "/auth/login";
                opts.LogoutPath = "/auth/logout";
                opts.SlidingExpiration = false;
            });

        services.AddHttpClient("FirebaseAuth_GoogleIdentity", http => http.BaseAddress = new Uri("https://identitytoolkit.googleapis.com/"));

        services.AddTransient<FirebaseJsInterop>()
                .AddScoped<LoginViewModel>();

        return services;
    }
}

public class FirebaseAuthenticationModule(Action<AuthorizationOptions>? authOptions = null) : AppModule
{
    public override ValueTask<Unit> InstallServices(IHostApplicationBuilder builder) {
        builder.Services.AddFirebaseAuthentication(authOptions);
        return base.InstallServices(builder);
    }

    public override ValueTask<Unit> InstallMiddleware(IHostApplicationBuilder builder, WebApplication app) {
        app.UseAuthentication();
        app.UseAuthorization();
        return base.InstallMiddleware(builder, app);
    }
}