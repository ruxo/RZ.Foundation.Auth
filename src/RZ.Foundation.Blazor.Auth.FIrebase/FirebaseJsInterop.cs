using JetBrains.Annotations;
using Microsoft.JSInterop;

namespace RZ.Foundation.Blazor.Auth;

[PublicAPI]
public interface ISignInHandler
{
    void AfterSignIn(bool success, SignInInfo? info, string? error);
}

public class FirebaseJsInterop(IJSRuntime js) : IAsyncDisposable
{
    readonly Lazy<Task<IJSObjectReference>> importModule = new (() => js.InvokeAsync<IJSObjectReference>("import", "/_content/RZ.Foundation.Blazor.Auth.Firebase/firebase.js").AsTask());

    public async ValueTask SetFocusById(string id) {
        var module = await importModule.Value;
        await module.InvokeVoidAsync("setFocusById", id);
    }

    public async ValueTask SignInFacebook(ISignInHandler handler, FirebaseSdkConfig config) {
        var module = await importModule.Value;
        using var jsRef = DotNetObjectReference.Create(handler);
        await module.InvokeVoidAsync("signInFacebook", jsRef, config);
    }

    public async ValueTask SignInGoogle(ISignInHandler handler, FirebaseSdkConfig config) {
        var module = await importModule.Value;
        using var jsRef = DotNetObjectReference.Create(handler);
        await module.InvokeVoidAsync("signInGoogle", jsRef, config);
    }

    public async ValueTask SignInWithEmail(ISignInHandler handler, FirebaseSdkConfig config, string email, string password) {
        var module = await importModule.Value;
        using var jsRef = DotNetObjectReference.Create(handler);
        await module.InvokeVoidAsync("signInPassword", jsRef, config, email, password);
    }

    public async ValueTask SignUpWithEmail(ISignInHandler handler, FirebaseSdkConfig config, string email, string password) {
        var module = await importModule.Value;
        using var jsRef = DotNetObjectReference.Create(handler);
        await module.InvokeVoidAsync("signUpPassword", jsRef, config, email, password);
    }

    public async ValueTask StoreAfterSignIn(string encoded) {
        var module = await importModule.Value;
        await module.InvokeVoidAsync("storeAfterSignIn", encoded);
    }

    public async ValueTask InstallExpirationTimer(DateTimeOffset expiration, int timeToShowMilliseconds) {
        var module = await importModule.Value;
        await module.InvokeVoidAsync("installExpirationTimer", expiration, timeToShowMilliseconds);
    }

    public async ValueTask DisposeAsync() {
        if (importModule.IsValueCreated){
            using var import = importModule.Value;
            var module = await import;
            await Try(module, async m => await m.DisposeAsync());       // This can be called after the circuit has been disposed.
        }
        GC.SuppressFinalize(this);
    }
}