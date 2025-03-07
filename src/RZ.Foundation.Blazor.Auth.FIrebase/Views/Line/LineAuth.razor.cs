using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LanguageExt.UnitsOfMeasure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RZ.Foundation.Blazor.Auth.Helpers;
using RZ.Foundation.Helpers;

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
    public async Task Validate() {
        ReturnUrl = "/";

        var (code, error, encodedState) = parameters;

        if (LineLoginConfig.From(configuration) is not { } config) return;

        if (LineUtils.DecodeState(encodedState) is not { } state){
            Logger.LogWarning("LINE callback is called with an invalid state: {State}", encodedState);
            return;
        }
        ReturnUrl = state.ReturnUrl;
        if (state.ClientId != config.ClientId){
            Logger.LogWarning("LINE callback is called with an invalid client id: {State}", state);
            return;
        }

        Time diff = clock.GetUtcNow() - state.Timestamp;
        if (diff > 20.Seconds()){
            Logger.LogWarning("LINE callback is called with an expired state: {@State}", encodedState);
            return;
        }

        if (error is not null){
            Logger.LogDebug("LINE authentication failed {@State}: {Error}", state, error);
            return;
        }

        var oidc = await config.Authority.GetOidcWellKnownConfig();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            ["code"] = code!,
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["redirect_uri"] = $"{Nav.BaseUri}auth/line",
            ["grant_type"] = "authorization_code"
        });
        var response = await http.PostAsync(oidc.TokenEndpoint, content);
        if (response.IsSuccessStatusCode){
            var json = await response.Content.ReadAsStringAsync();
            var oidcResponse = new OpenIdConnectMessage(json);

            var jwt = new JwtSecurityTokenHandler();
            var token = (JwtSecurityToken) jwt.ReadToken(oidcResponse.IdToken);
            var claims = token.Claims.ToList();
            claims.Add(new("uid", claims.FindFirstValue(JwtRegisteredClaimNames.Sub)!));

            var rsa = RSA.Create();
            rsa.ImportFromPem(AuthService.ServiceAccount.PrivateKey);
            var signingKey = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var newJwt = new JwtSecurityToken(
                issuer: AuthService.ServiceAccount.Email,
                audience: "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit",
                claims: claims.Where(c => c.Type is not "uid"
                                                and not JwtRegisteredClaimNames.Sub
                                                and not JwtRegisteredClaimNames.Iat
                                                and not JwtRegisteredClaimNames.Exp
                                                and not JwtRegisteredClaimNames.Aud).Concat([
                    new(JwtRegisteredClaimNames.Sub, AuthService.ServiceAccount.Email),
                    new("uid", claims.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
                    claims.First(c => c.Type == JwtRegisteredClaimNames.Iat),
                    claims.First(c => c.Type == JwtRegisteredClaimNames.Exp),
                ]),
                signingCredentials: signingKey);

            var newToken = jwt.WriteToken(newJwt);

            await SignInWith(js => js.SignInCustomJwt(this, AuthService.Config, newToken));
        }
        else{
            var responseText = await response.Content.ReadAsStringAsync();
            Logger.LogWarning("LINE authentication failed: {Error} [{Response}]", response.StatusCode, responseText);
        }
    }
}