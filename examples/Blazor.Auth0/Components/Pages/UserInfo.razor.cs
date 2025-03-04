using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ReactiveUI;
using RZ.Foundation.Blazor.MVVM;

namespace Blazor.Auth0.Components.Pages;

public sealed class UserInfoViewModel : ViewModel
{
    string? accessToken;

    public UserInfoViewModel(AuthenticationStateProvider authState) {
        Task.Run(async () => {
            User = (await authState.GetAuthenticationStateAsync()).User;
            accessToken = User.FindFirstValue("access_token")!;
        });
    }

    public string? AccessToken
    {
        get => accessToken;
        set => this.RaiseAndSetIfChanged(ref accessToken, value);
    }

    public ClaimsPrincipal User
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = new();
}