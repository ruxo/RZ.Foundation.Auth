using Auth0.AspNetCore.Authentication;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace RZ.Foundation.Blazor.Auth.Auth0.Views;

[PublicAPI]
public abstract class LoginPage : ComponentBase
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [CascadingParameter] public required HttpContext HttpContext { get; set; }

    public static async Task InitializeAsync(HttpContext httpContext, string? returnUrl, string defaultReturnUrl = "/") {
        var authProps = new LoginAuthenticationPropertiesBuilder()
                       .WithRedirectUri(returnUrl ?? defaultReturnUrl)
                       .Build();
        await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authProps);
    }

    protected override Task OnInitializedAsync()
        => InitializeAsync(HttpContext, ReturnUrl);
}