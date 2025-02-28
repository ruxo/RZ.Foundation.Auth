namespace RZ.Foundation.Blazor.Auth;

public sealed record SignInInfo(string Type, string AccessToken, string? RefToken);

// ReSharper disable InconsistentNaming
public sealed record FirebaseSdkConfig(
    string apiKey,
    string authDomain,
    string projectId,
    string storageBucket,
    string messagingSenderId,
    string appId,
    string measurementId);
