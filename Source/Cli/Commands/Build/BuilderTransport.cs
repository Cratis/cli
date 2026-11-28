// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Build;

/// <summary>The transport for requests carrying a builder credential.</summary>
internal static class BuilderTransport
{
    static readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true }) { Timeout = TimeSpan.FromMinutes(5) };

    internal static StudioBuilderClient Connect(string address, string organization) => new(_client, Validate(address), organization);

    internal static Uri Validate(string address)
    {
        if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && Suitable(uri)) return uri;
        throw new StudioNotConnected("Use the final HTTPS Studio address without user information, query or fragment. HTTP is allowed only on loopback.");
    }

    static bool Suitable(Uri uri) => Decorations(uri).Length == 0 && Secure(uri);

    static string Decorations(Uri uri) => string.Concat(
        uri.GetComponents(UriComponents.UserInfo | UriComponents.KeepDelimiter, UriFormat.UriEscaped), uri.Query, uri.Fragment);

    static bool Secure(Uri uri) => uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && Local(uri));

    static bool Local(Uri uri) => uri.Host == "localhost" || (IPAddress.TryParse(uri.Host, out var ip) && !ip.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(ip));
}
