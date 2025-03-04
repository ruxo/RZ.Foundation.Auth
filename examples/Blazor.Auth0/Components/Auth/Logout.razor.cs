using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;

namespace Blazor.Auth0.Components.Auth;

partial class Logout
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [CascadingParameter] public required HttpContext HttpContext { get; set; }

    protected override async Task OnInitializedAsync() {
        var authProps = new LogoutAuthenticationPropertiesBuilder()
                       .WithRedirectUri(ReturnUrl ?? "/")
                       .Build();
        await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authProps);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}