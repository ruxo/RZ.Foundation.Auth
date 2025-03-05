using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RZ.Foundation.Blazor.MVVM;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class SignUp
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    protected override void OnInitialized() {
        ViewModel!.ReturnUrl = ReturnUrl;
    }
}

public class SignUpViewModel(VmToolkit<SignUpViewModel> tool, NavigationManager navManager, FirebaseAuthService authService)
    : AppViewModel(tool)
{
    public string? Title { get; set; } = "Create an account";
    public Typo TitleTypo { get; set; } = Typo.h5;
    public string? TitleClass { get; set; } = "rz-text-center";
    public string? Subtitle { get; set; } = "Create an account to continue";
    public Typo SubtitleTypo { get; set; } = Typo.subtitle1;
    public string? SubtitleClass { get; set; } = "rz-text-center rz-muted";
    public string? MiddleText { get; set; } = "Or continue with";
    public string? AlreadyHaveAccountText { get; set; } = "Already have an account?";
    public string? LoginText { get; set; } = "Log in";

    public bool IsAuthenticating
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Email
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Password
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ErrorMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ReturnUrl { get; set; }

    public string LoginUrl => $"/auth/login?returnUrl={ReturnUrl ?? "/"}";
}
