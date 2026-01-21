using Microsoft.Extensions.Configuration;
using RZ.Foundation.Helpers;
using LanguageExt;
using Uri = TiraxTech.Uri;

namespace RZ.Foundation.Blazor.Auth.Views.Line;

readonly record struct LineLoginConfig(Uri Authority, string ClientId, string ClientSecret)
{
    public static LineLoginConfig? From(IConfiguration configuration) {
        if (configuration.GetConnectionString("LineAuth") is not { } lineAuthConfig) return null;
        var kv = KeyValueString.Parse(lineAuthConfig);
        var result = from a in kv.TryGetValue("Authority")
                     from ci in kv.TryGetValue("ClientId")
                     from cs in kv.TryGetValue("ClientSecret")
                     select new LineLoginConfig(Uri.From(a), ci, cs);
        return result.ToNullable();
    }
}