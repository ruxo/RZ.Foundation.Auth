using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class Login(IJSRuntime js, NavigationManager nav)
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    protected override void OnParametersSet() {
        ViewModel!.ReturnUrl = ReturnUrl;
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender){
            // มันมี gap ระหว่างการ redirect จาก server มา WASM ที่ทำให้ไม่สามารถ extract query string ได้ถูกต้อง เพราะ NavManager เองก็เพี้ยน มี URL ที่ไม่ตรง
            // กับ address bar (window.location)
            //
            // workaround ก็คือ ให้ NavigationManager ฝั่ง WASM redirect ซักรอบ ซึ่งมันไม่ได้ redirect จริง (แปลว่าจะไม่ได้ call firstRender = true อีกครั้ง)
            // แต่มันจะ binding parameter ใหม่ และเรียก skip firstRender ไปเลย ซึ่งทำให้ redirect ถูก!
            //
            var path = await js.InvokeAsync<string>("window.location.toString");
            if (path != nav.Uri){
                nav.NavigateTo(path, replace: true);
                return;
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}

public class LoginViewModel(VmToolkit<LoginViewModel> tool, NavigationManager nav, FirebaseAuthService authService)
    : LoginViewModelBase(tool, nav, authService)
{
    public string? Title { get; set; } = "Login to your account";
    public Typo TitleTypo { get; set; } = Typo.h5;
    public string? TitleClass { get; set; } = "rz-text-center";
    public string? Subtitle { get; set; } = "Choose your login method";
    public Typo SubtitleTypo { get; set; } = Typo.subtitle1;
    public string? SubtitleClass { get; set; } = "rz-text-center rz-muted";
    public string? ForgotPasswordText { get; set; } = "Forgot your password?";
    public string? MiddleText { get; set; } = "Or continue with";
    public string? NoAccountText { get; set; } = "Don't have an account?";
    public string? SignUpText { get; set; } = "Sign up";

    public string TermsAndConditionsLink { get; set; } = "/terms";
    public string PrivacyPolicyLink { get; set; } = "/privacy";

    public string SignUpLink => $"/auth/signup?returnUrl={ReturnUrl ?? "/"}";

    public async Task SignInWithEmail() {
        if (ValidateEmailAndPassword("login") is { } x)
            await SignInWith(js => js.SignInWithEmail(this, AuthService.Config, x.Email, x.Password));
    }
}