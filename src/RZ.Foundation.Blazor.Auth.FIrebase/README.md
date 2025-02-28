# Firebase Authentication

## Dependencies

* Reactive UI
* (JS) Firebase SDK
* MudBlazor
* RZ.Foundation.Blazor

## Usage

### If not using, `RZ.AspNet.Bootstrapper`

1. Register Firebase authentication to the DI container:

    ```csharp
    builder.Services.AddFirebaseAuthentication();
    ```
2. Register the `FirebaseAuthentication` assembly to Razor pages

    ```csharp
    app.MapRazorComponents<App>()
       .AddAdditionalAssemblies(typeof(FirebaseAuthentication).Assembly)
       .AddInteractiveServerRenderMode();
    ```
   
### If using `RZ.AspNet.Bootstrapper`

Just registering via the host building pipline. For example:

```csharp
await builder.RunPipeline(
        CommonModules.ForwardHeaders(),
        CommonModules.Localization(supportedCultures: ["en", "th"]),
        RzBlazorModules.HandleBlazorErrors(),
        CommonModules.EnforceHsts(),
        CommonModules.AntiForgery(),
        RzBlazorModules.ExposeMappedAssets(),
        
        FirebaseAuthentication.Module(opts => {
            opts.AddPolicy(ServicePermissions.Authenticated, policy => policy.RequireAuthenticatedUser());
        }),
        RzBlazorModules.UseBlazorServer<App>(typeof(FirebaseAuthentication).Assembly)
    );
```