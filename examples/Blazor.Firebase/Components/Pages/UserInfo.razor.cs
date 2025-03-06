using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Authorization;
using ReactiveUI;
using RZ.Foundation;
using RZ.Foundation.Blazor.MVVM;

namespace Blazor.Firebase.Components.Pages;

[UsedImplicitly]
partial class UserInfo
{
    public UserInfo(IServiceProvider sp) {
        ViewModel = sp.Create<UserInfoViewModel>();
    }

    static string FixWidthForAccessToken(Claim claim)
        => claim.Type == "access_token" ? "width: 40em; word-break: break-all" : string.Empty;
}

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