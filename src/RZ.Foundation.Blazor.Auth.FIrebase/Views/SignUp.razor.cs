using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class SignUp(NavigationManager nav, LoginViewModel loginVm)
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    protected override void OnParametersSet() {
        ViewModel!.ReturnUrl = ReturnUrl;
        base.OnParametersSet();
    }

    protected override void OnInitialized() {
        if (!loginVm.CanSignUp)
            nav.NavigateTo("/");

        if (!loginVm.UseEmailLogin)
            nav.NavigateTo($"/auth/login?returnUrl={Uri.EscapeDataString(ReturnUrl ?? "/")}");
    }
}

public class SignUpViewModel(VmToolkit<SignUpViewModel> tool, NavigationManager nav, FirebaseAuthService authService)
    : LoginViewModelBase(tool, nav, authService)
{
    public string? Title { get; set; } = "Create an account";
    public Typo TitleTypo { get; set; } = Typo.h5;
    public string? TitleClass { get; set; } = "rz-text-center";
    public string? Subtitle { get; set; } = "Create an account to continue";
    public Typo SubtitleTypo { get; set; } = Typo.subtitle1;
    public string? SubtitleClass { get; set; } = "rz-text-center rz-muted";
    public string? MiddleText { get; set; } = "Or";
    public string? AlreadyHaveAccountText { get; set; } = "Already have an account?";
    public string? LoginText { get; set; } = "Log in";

    public string LoginUrl => $"/auth/login?returnUrl={ReturnUrl ?? "/"}";

    public async Task SignUpWithEmail() {
        if (ValidateEmailAndPassword("login") is { } x)
            await SignInWith(js => js.SignUpWithEmail(this, AuthService.Config, x.Email, x.Password));
    }
}