using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using RZ.Foundation.Blazor.MVVM;
using RZ.Foundation.Types;
using TiraxTech;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class Login(IJSRuntime js, NavigationManager nav)
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

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

public class LoginViewModel(ILogger<LoginViewModel> logger, IServiceProvider sp, NavigationManager navManager, FirebaseAuthService authService)
    : ViewModel, ISignInHandler
{
    Outcome<SignInInfo>? afterSignInInfo;

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

    [JSInvokable]
    public void AfterSignIn(bool success, SignInInfo? info, string? error) {
        afterSignInInfo = success ? info! : new ErrorInfo(StandardErrorCodes.Unhandled, error!);
    }

    public Task SignInWithEmail(string? returnUrl) {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password)){
            ErrorMessage = "Email and password are required";
            logger.LogInformation("Deny login with empty email or password");
            return Task.CompletedTask;
        }
        return SignInWith("email", returnUrl, js => js.SignInWithEmail(this, authService.Config, Email, Password));
    }

    public Task SignInWithGoogle(string? returnUrl)
        => SignInWith("Google", returnUrl, js => js.SignIn(this, authService.Config));

    async Task SignInWith(string method, string? returnUrl, Func<FirebaseJsInterop, ValueTask> signInTask) {
        logger.LogDebug("Login with {ReturnUrl}", returnUrl);
        IsAuthenticating = true;

        try{
            await using var js = sp.GetRequiredService<FirebaseJsInterop>();
            await signInTask(js);

            if (afterSignInInfo!.Value.IfSuccess(out var info, out var e)){
                var user = await authService.Validate(info);

                var result = user is null
                                 ? new AfterSignInCheck.Failed("Invalid token")
                                 : sp.GetService<IAfterSignInHandler>() is { } afterSignInHandler
                                     ? await afterSignInHandler.ProceedAfterSignInFlow(user)
                                     : new AfterSignInCheck.LoginSuccess(user);

                switch (result){
                    case AfterSignInCheck.Failed failed:
                        IsAuthenticating = false;
                        ErrorMessage = failed.Message;
                        break;

                    case AfterSignInCheck.CustomLoginFlow custom:
                        navManager.NavigateTo(custom.CustomFlowPath.UpdateQuery("returnUrl", returnUrl).ToString());
                        break;

                    case AfterSignInCheck.LoginSuccess login:
                        await authService.LoginSuccess(navManager, js, login.User, returnUrl);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown {nameof(AfterSignInCheck)} type: {result}");
                }
            }
            else{
                IsAuthenticating = false;
                if (e.Message.Contains("Firebase: Error (auth/")){
                    ErrorMessage = "Invalid user or password";
                }
                else{
                    logger.LogWarning("Authentication failed: {@Error}", e);
                    ErrorMessage = "Firebase authentication failed";
                }
            }
        }
        catch (Exception e){
            logger.LogError(e, "Failed to sign in {Method}", method);
            IsAuthenticating = false;
            // TODO Display error
            ErrorMessage = e.Message;
        }
    }
}