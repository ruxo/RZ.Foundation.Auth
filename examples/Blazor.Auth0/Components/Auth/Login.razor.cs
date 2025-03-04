using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;

namespace Blazor.Auth0.Components.Auth;

partial class Login
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    [CascadingParameter] public required HttpContext HttpContext { get; set; }

    protected override async Task OnInitializedAsync() {
        var authProps = new LoginAuthenticationPropertiesBuilder()
                       .WithRedirectUri(ReturnUrl ?? "/")
                       .Build();
        await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authProps);
    }
}