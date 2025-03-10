using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace RZ.Foundation.Blazor.Auth.Views;

[UsedImplicitly]
partial class Logout
{
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }
    [CascadingParameter] HttpContext? StaticContext { get; set; }

    [Inject] public required IHttpContextAccessor HttpContextAccessor { get; set; }
    [Inject] public required NavigationManager NavManager { get; set; }

    protected override async Task OnInitializedAsync() {
        if (StaticContext is not null){
            await HttpContextAccessor.HttpContext!.SignOutAsync(FirebaseAuthentication.Scheme);
        }
        else
            NavManager.NavigateTo(ReturnUrl ?? "/");
    }
}