using System.Diagnostics;
using LanguageExt.UnitsOfMeasure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using RZ.Foundation.Blazor.Auth.Helpers;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

[UsedImplicitly]
partial class LineAuth(IServiceProvider sp)
{
    [SupplyParameterFromQuery] public string? Code { get; set; }
    [SupplyParameterFromQuery] public string? Error { get; set; }
    [SupplyParameterFromQuery] public required string State { get; set; }

    protected override void OnParametersSet() {
        ViewModel = sp.Create<LineAuthViewModel>(new OidcResponse(Code, Error, State));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender)
            await ViewModel!.Validate();
    }
}

public readonly record struct OidcResponse(string? Code, string? Error, string State);

public sealed class LineAuthViewModel(VmToolkit<LineAuthViewModel> tool, IConfiguration configuration, TimeProvider clock, NavigationManager nav, FirebaseAuthService authService, HttpClient http, OidcResponse parameters)
    : LoginViewModelBase(tool, nav, authService)
{
    public ViewStatus Status
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = ViewStatus.Loading.Instance;

    public async Task Validate() {
        ReturnUrl = "/";

        var (code, error, encodedState) = parameters;

        if (LineLoginConfig.From(configuration) is not { } config) return;

        if (LineUtils.DecodeState(encodedState) is not { } state){
            Logger.LogWarning("LINE callback is called with an invalid state: {State}", encodedState);
            Status = new ViewStatus.Failed("Error");
            return;
        }
        ReturnUrl = state.ReturnUrl;
        if (state.ClientId != config.ClientId){
            Logger.LogWarning("LINE callback is called with an invalid client id: {State}", state);
            Status = new ViewStatus.Failed("Error");
            return;
        }

        Time diff = clock.GetUtcNow() - state.Timestamp;
        if (diff > 20.Seconds()){
            Logger.LogWarning("LINE callback is called with an expired state: {@State}", encodedState);
            Status = new ViewStatus.Failed("Error");
            return;
        }

        if (error is not null){
            Logger.LogDebug("LINE authentication failed {@State}: {Error}", state, error);
            Status = new ViewStatus.Failed("Error");
            return;
        }
        Debug.Assert(code is not null);

        var response = await http.GetLineToken(config.Authority, code, $"{Nav.BaseUri}auth/line", config.ClientId, config.ClientSecret);
        if (response.IfSuccess(out var oidcResponse, out var responseError)){
            var newToken = AuthService.CreateFirebaseLineToken(oidcResponse.IdToken);

            await SignInWith(js => js.SignInCustomJwt(this, AuthService.Config, "line", newToken));
            Status = ErrorMessage is null? ViewStatus.Ready.Instance : new ViewStatus.Failed(ErrorMessage);
        }
        else{
            Logger.LogWarning("LINE authentication failed: {@Error}", responseError);
            Status = new ViewStatus.Failed("Error");
        }
    }
}