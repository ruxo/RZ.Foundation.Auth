using LanguageExt.UnitsOfMeasure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class RedirectToLoginPage(ILogger<RedirectToLoginPage> logger, IServiceProvider sp, NavigationManager navManager, AuthenticationStateProvider authProvider)
{
    [Parameter] public string? RedirectUrl { get; set; }
    [Parameter] public TimeSpan? WarningThreshold { get; set; }

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    bool isAuthenticated;
    bool initialized;

    protected override async Task OnInitializedAsync() {
        if (HttpContext is not null) return;  // ignore server-side prerendering

        var auth = await authProvider.GetAuthenticationStateAsync();

        isAuthenticated = auth.User.IsAuthenticated();
        initialized = true;
        if (!isAuthenticated || auth.User.IsExpired(DateTimeOffset.UtcNow)){
            var returnUrl = RedirectUrl ?? navManager.Uri;
            var target = isAuthenticated? "logout" : "login";
            navManager.NavigateTo($"/{target}?redirectUrl={returnUrl}", forceLoad: true);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        logger.LogTrace("RedirectToLoginPage After render {FirstRender}", firstRender);

        var auth = await authProvider.GetAuthenticationStateAsync();
        isAuthenticated = auth.User.IsAuthenticated();
        if (firstRender && isAuthenticated){
            var expiration = auth.User.GetExpiration()!.Value;

            var threshold = (WarningThreshold ?? 15.Minutes()).TotalMilliseconds;

            await using var js = sp.GetRequiredService<FirebaseJsInterop>();
            await js.InstallExpirationTimer(expiration, (int)threshold);
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}