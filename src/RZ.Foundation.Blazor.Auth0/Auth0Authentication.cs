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
using RZ.Foundation.Blazor.Auth;
using RZ.Foundation.Helpers;
using RZ.Foundation.Types;
using TiraxTech;

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

                    opts.OpenIdConnectEvents = new OpenIdConnectEvents {
                        OnTicketReceived = async context => {
                            Console.WriteLine("OnTicketReceived");
                            if (context.HttpContext.RequestServices.GetService<IAfterSignInHandler>() is not {} afterSignInHandler)
                                return;

                            var user = context.Principal!;
                            var result = await afterSignInHandler.ProceedAfterSignInFlow(user);
                            switch (result){
                                case AfterSignInCheck.Failed failed:
                                    context.Fail(failed.Message);
                                    break;

                                case AfterSignInCheck.CustomLoginFlow custom:
                                    var path = context.Properties?.RedirectUri is {} url? custom.CustomFlowPath.UpdateQuery("returnUrl", url) : custom.CustomFlowPath;
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