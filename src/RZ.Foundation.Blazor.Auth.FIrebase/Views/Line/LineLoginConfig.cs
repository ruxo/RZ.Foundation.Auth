using Microsoft.Extensions.Configuration;
using RZ.Foundation.Extensions;
using RZ.Foundation.Helpers;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

readonly record struct LineLoginConfig(Uri Authority, string ClientId, string ClientSecret)
{
    public static LineLoginConfig? From(IConfiguration configuration) {
        if (configuration.GetConnectionString("LineAuth") is not { } lineAuthConfig) return null;
        var kv = KeyValueString.Parse(lineAuthConfig);
        var result = from a in kv.Get("Authority")
                     from ci in kv.Get("ClientId")
                     from cs in kv.Get("ClientSecret")
                     select new LineLoginConfig(Uri.From(a), ci, cs);
        return result.ToNullable();
    }
}