using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RZ.Foundation.Blazor.Auth;

public static class RzAuthSettings
{
    public static IServiceCollection AddRzAuth(this IServiceCollection services, Action<AuthorizationOptions>? authOptions = null) {
        if (authOptions is null)
            services.AddAuthorizationCore();
        else
            services.AddAuthorizationCore(authOptions);
        return services
              .AddHttpContextAccessor()
              .AddScoped<AuthenticationStateProvider, RzAuthStateProvider>()
              .AddScoped<RzAuthStateProvider>(sp => (RzAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
    }
}