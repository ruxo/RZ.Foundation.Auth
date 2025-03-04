using Auth0.AspNetCore.Authentication;
using JetBrains.Annotations;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RZ.AspNet;
using RZ.Foundation.Blazor.Auth;
using RZ.Foundation.Helpers;
using RZ.Foundation.Types;

namespace RZ.Foundation.Blazor.Auth0;

public static class AUth0Authentication
{
    public static IHostApplicationBuilder AddAuth0Authentication(this IHostApplicationBuilder builder, Action<AuthorizationOptions>? authOptions = null, string configName = "Auth0") {
        builder.Services
               .AddCascadingAuthenticationState()
               .AddRzAuth(authOptions)
               .AddAuth0WebAppAuthentication(opts => {
                    var dict = KeyValueString.Parse(builder.Configuration.GetConnectionString(configName) ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration));
                    opts.Domain = dict["Domain"] ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration);
                    opts.ClientId = dict["ClientId"] ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration);
                });
        return builder;
    }
}

[PublicAPI]
public class Auth0AuthenticationModule : AppModule
{
    public override ValueTask<Unit> InstallServices(IHostApplicationBuilder builder) {
        builder.AddAuth0Authentication();
        return base.InstallServices(builder);
    }

    public override ValueTask<Unit> InstallMiddleware(IHostApplicationBuilder builder, WebApplication app) {
        app.UseAuthentication();
        app.UseAuthorization();
        return base.InstallMiddleware(builder, app);
    }
}