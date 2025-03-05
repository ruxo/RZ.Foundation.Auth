using Auth0.AspNetCore.Authentication;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace RZ.Foundation.Blazor.Auth0.Views;

[Authorize, PublicAPI]
public abstract class LogoutPage : ComponentBase
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [CascadingParameter] public required HttpContext HttpContext { get; set; }

    public static async Task InitializeAsync(HttpContext httpContext, string? returnUrl, string defaultReturnUrl = "/") {
        var authProps = new LogoutAuthenticationPropertiesBuilder()
                       .WithRedirectUri(returnUrl ?? defaultReturnUrl)
                       .Build();
        await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authProps);
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    protected override Task OnInitializedAsync()
        => InitializeAsync(HttpContext, ReturnUrl);
}