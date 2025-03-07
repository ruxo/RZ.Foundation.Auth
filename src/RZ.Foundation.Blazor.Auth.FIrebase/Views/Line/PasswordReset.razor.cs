using System.Diagnostics;
using System.Reactive.Disposables;
using FluentValidation;
using LanguageExt;
using RZ.Foundation.Extensions;
using RZ.Foundation.Validation;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

[UsedImplicitly]
partial class PasswordReset
{
    public PasswordReset() {
        this.WhenActivated(_ => { });
    }
}

public class PasswordResetViewModel : AppViewModel
{
    readonly FirebaseJsInterop js;
    readonly FirebaseAuthService fbAuth;
    public PasswordResetViewModel(VmToolkit<PasswordResetViewModel> tool, FirebaseJsInterop js, FirebaseAuthService fbAuth, Option<string> email) : base(tool) {
        this.js = js;
        this.fbAuth = fbAuth;
        Email = email.ToNullable();

        this.WhenActivated(d => {
            this.WhenAnyValue(x => x.Status, status => status as ViewStatus.Failed)
                .WhereNotNull()
                .Subscribe(e => {
                     Shell.Notify(new(MessageSeverity.Error, e.Message));
                 })
                .DisposeWith(d);
        });
    }

    public ViewStatus Status
    {
        get;
        private set
        {
            this.RaisePropertyChanging(nameof(IsSending));
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsSending));
        }
    } = ViewStatus.Idle.Instance;

    public bool IsSending => Status is ViewStatus.Loading;

    public string? Email
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public async Task SendPasswordResetEmail() {
        if (EmailValidation(Email).Any())
            return;
        Debug.Assert(Email is not null);

        Status = ViewStatus.Loading.Instance;
        Status = await js.SendPasswordResetEmail(fbAuth.Config, Email) is { Error: { } error }
                     ? new ViewStatus.Failed(error.Message)
                     : ViewStatus.Ready.Instance;
        if (Status is ViewStatus.Ready){
            Shell.Notify(new(MessageSeverity.Success, "Password reset email sent"));
            Shell.CloseCurrentView();
        }
    }

    public readonly Func<string?, IEnumerable<string?>> EmailValidation
        = Validator.Of<string?>(b => b.Must(v => v is not null && Validation.CommonValidators.Email.IsValid(v))
                                      .WithMessage("Invalid email address"));

    public void Cancel() {
        Shell.CloseCurrentView();
    }
}