using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using JetBrains.Annotations;
using LanguageExt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RZ.AspNet;
using RZ.Foundation.Extensions;
using RZ.Foundation.Helpers;
using RZ.Foundation.Types;
using TiraxTech;

namespace RZ.Foundation.Blazor.Auth.Auth0;

public static class AUth0Authentication
{
    public const string Scheme = "RzAuth0";

    public static IHostApplicationBuilder AddAuth0Authentication(this IHostApplicationBuilder builder, Action<AuthorizationOptions>? authOptions = null, string configName = "Auth0") {
        builder.Services
               .AddCascadingAuthenticationState()
               .AddRzAuth(authOptions);

        var dict = KeyValueString.Parse(builder.Configuration.GetConnectionString(configName) ?? throw new ErrorInfoException(StandardErrorCodes.MissingConfiguration));
        var auth0Registration =
            builder.Services
                   .AddAuth0WebAppAuthentication(opts => {
                        opts.ForwardSignIn = "/auth/login";
                        opts.Domain = dict["Domain"];
                        opts.ClientId = dict["ClientId"];
                        opts.ClientSecret = dict.Get("ClientSecret").ToNullable();

                        opts.OpenIdConnectEvents = new OpenIdConnectEvents {
                            OnTokenValidated = async context => {
                                Console.WriteLine($"token validated [{context.Properties?.RedirectUri}]");
                                var user = context.Principal!;
                                var registeredIdentity = user.Identities.FirstOrDefault(i => i.AuthenticationType == Scheme);

                                if (registeredIdentity is not null || context.HttpContext.RequestServices.GetService<IAfterSignInHandler>() is not { } afterSignInHandler)
                                    return;

                                user.AddIdentity(new ClaimsIdentity([], Scheme));
                                var result = await afterSignInHandler.ProceedAfterSignInFlow(user);
                                switch (result){
                                    case AfterSignInCheck.Failed failed:
                                        context.Fail(failed.Message);
                                        break;

                                    case AfterSignInCheck.CustomLoginFlow custom:
                                        var url = context.Properties?.RedirectUri ?? "/";
                                        var path = custom.CustomFlowPath.UpdateQuery("returnUrl", url);
                                        context.Response.Redirect(path.ToString());
                                        context.HandleResponse();
                                        break;

                                    case AfterSignInCheck.LoginSuccess login:
                                        context.Principal = login.User;
                                        break;

                                    default:
                                        context.Fail($"Unknown {nameof(AfterSignInCheck)} type: {result}");
                                        break;
                                }
                            }
                        };
                    });
        var accessConfig = from aud in dict.Get("Audience")
                           from scope in dict.Get("Scope")
                           select new { aud, scope };
        if (accessConfig.IfSome(out var x))
            auth0Registration.WithAccessToken(opts => {
                opts.Audience = x.aud;
                opts.Scope = x.scope;
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