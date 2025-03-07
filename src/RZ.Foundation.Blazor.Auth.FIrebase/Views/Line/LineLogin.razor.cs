using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RZ.Foundation.Blazor.Auth.Helpers;
using TiraxTech;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

[UsedImplicitly]
partial class LineLogin(ILogger<LineLogin> logger, TimeProvider clock, IConfiguration configuration, NavigationManager nav)
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [CascadingParameter] public required HttpContext HttpContext { get; set; }

    protected override async Task OnParametersSetAsync() {
        Console.WriteLine($"Context: {HttpContext}");

        var returnUrl = ReturnUrl ?? "/";
        if (LineLoginConfig.From(configuration) is not { } lineAuthConfig){
            logger.LogWarning("No LineAuth config found");
            HttpContext.Response.Redirect(returnUrl);
            return;
        }
        var oidc = await lineAuthConfig.Authority.GetOidcWellKnownConfig();

        var state = LineUtils.EncodeState(lineAuthConfig.ClientId, returnUrl, clock.GetUtcNow());
        Console.WriteLine($"Request state: {state}");
        var authorizeEndpoint = Uri.From(oidc.AuthorizationEndpoint)
                                   .UpdateQueries([
                                        ("response_type", "code"),
                                        ("client_id", lineAuthConfig.ClientId),
                                        ("redirect_uri", $"{nav.BaseUri}auth/line"),
                                        ("scope", "openid profile"),
                                        ("state", state)
                                    ]);
        HttpContext.Response.Redirect(authorizeEndpoint.ToString());
    }
}