using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using TiraxTech;

namespace RZ.Foundation.Blazor.Auth.Views;

public partial class LoginViewModelBase(VmToolkit tool, NavigationManager nav, FirebaseAuthService authService)
    : AppViewModel(tool), ISignInHandler
{
    protected Outcome<SignInInfo>? AfterSignInInfo;

    public string? Email
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            ErrorMessage = null;
        }
    }

    public string? Password
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            ErrorMessage = null;
        }
    }

    public bool IsAuthenticating
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ErrorMessage
    {
        get;
        protected set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            if (value is not null){
                Console.WriteLine($"Error: {value}");
                Shell.Notify(new(MessageSeverity.Error, value));
            }
        }
    }

    public string? ReturnUrl
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected FirebaseAuthService AuthService { get; } = authService;
    protected NavigationManager Nav => nav;

    [JSInvokable]
    public void AfterSignIn(bool success, SignInInfo? info, string? error) {
        if (error is not null)
            Logger.LogWarning("Firebase sign in failed: {Error}", error);

        AfterSignInInfo = success                                 ? info!
                          : IsFirebaseAuthError(error!) is { } fe ? new ErrorInfo(fe) : new ErrorInfo(StandardErrorCodes.Unhandled, error!);
    }

    static string? IsFirebaseAuthError(string error) {
        var match = FirebaseAuthError().Match(error);
        return match.Success ? match.Groups["error"].Value : null;
    }

    protected const string InvalidEmailError = "invalid-email";
    protected const string EmailUsedError = "email-already-in-use";
    protected const string WeakPasswordError = "weak-password";
    protected const string InvalidCredentialError = "invalid-credential";

    [GeneratedRegex(@"Firebase: [^(]+ \(auth/(?<error>[^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex FirebaseAuthError();

    protected (string Email, string Password)? ValidateEmailAndPassword(string method) {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password)){
            ErrorMessage = "Email and password are required";
            Logger.LogInformation("Deny {Method} with empty email or password", method);
            return null;
        }
        return (Email, Password);
    }

    public Task SignInWithFacebook()
        => SignInWith(js => js.SignInFacebook(this, AuthService.Config));

    public Task SignInWithGoogle()
        => SignInWith(js => js.SignInGoogle(this, AuthService.Config));

    public void RedirectToLineLogin() {
        // Prevent double refresh due to ... framework's bug??
        nav.NavigateTo($"/line/login?ReturnUrl={ReturnUrl ?? "/"}", forceLoad: true);
    }

    protected async Task SignInWith(Func<FirebaseJsInterop, ValueTask> signInTask, [CallerMemberName] string method = "") {
        Logger.LogDebug("Login with {ReturnUrl}", ReturnUrl);
        IsAuthenticating = true;

        try{
            await using var js = Services.GetRequiredService<FirebaseJsInterop>();
            await signInTask(js);

            if (AfterSignInInfo!.Value.IfSuccess(out var info, out var e)){
                var user = await AuthService.Validate(info);

                var result = user is null
                                 ? new AfterSignInCheck.Failed("Invalid token")
                                 : Services.GetService<IAfterSignInHandler>() is { } afterSignInHandler
                                     ? await afterSignInHandler.ProceedAfterSignInFlow(user)
                                     : new AfterSignInCheck.LoginSuccess(user);

                switch (result){
                    case AfterSignInCheck.Failed failed:
                        IsAuthenticating = false;
                        ErrorMessage = failed.Message;
                        break;

                    case AfterSignInCheck.CustomLoginFlow custom:
                        nav.NavigateTo(custom.CustomFlowPath.UpdateQuery("returnUrl", ReturnUrl).ToString());
                        break;

                    case AfterSignInCheck.LoginSuccess login:
                        await FirebaseAuthService.LoginSuccess(nav, js, login.User, ReturnUrl);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown {nameof(AfterSignInCheck)} type: {result}");
                }
            }
            else{
                IsAuthenticating = false;
                Logger.LogError("Sign up failed: {@Error}", AfterSignInInfo!.Value.Error);
                Console.WriteLine($"Code [{e.Code}]");
                switch (e.Code){
                    case InvalidEmailError:
                        ErrorMessage = "Invalid email address";
                        break;
                    case EmailUsedError:
                        ErrorMessage = "Email address already in use";
                        break;
                    case WeakPasswordError:
                        ErrorMessage = "Password is too weak";
                        break;
                    case InvalidCredentialError:
                        ErrorMessage = "Invalid user or password";
                        break;

                    default:
                        Logger.LogWarning("Authentication failed: {@Error}", e);
                        ErrorMessage = "Firebase authentication failed";
                        break;
                }
            }
        }
        catch (Exception e){
            Logger.LogError(e, "Failed to sign in {Method}", method);
            IsAuthenticating = false;
            // TODO Display error
            ErrorMessage = e.Message;
        }
    }
}