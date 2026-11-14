// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What <c>cratis build connect</c> was told about which Studio and which organization, with the Studio filled in
/// when it is the hosted one.
/// </summary>
/// <param name="Url">The Studio's address without a trailing slash, or <see langword="null"/> when none was given.</param>
/// <param name="Organization">The organization's name, or <see langword="null"/> when none was given.</param>
public sealed record StudioAddress(string? Url, string? Organization)
{
    /// <summary>The hosted Studio, used when <c>--studio</c> is given without an address or left out on a fresh machine.</summary>
    public const string Hosted = "https://app.cratis.studio";

    const string ConnectSegment = "builders/connect";

    /// <summary>
    /// Reads the <c>--studio</c> and <c>--organization</c> options. A <c>--studio</c> without an address means the hosted
    /// Studio. An address copied from Studio's builder address (<c>https://host/builders/connect/acme</c>) is reduced to
    /// the Studio and, unless <c>--organization</c> says otherwise, the organization it names.
    /// </summary>
    /// <param name="studio">The <c>--studio</c> option.</param>
    /// <param name="organization">The <c>--organization</c> option.</param>
    /// <returns>What was asked for.</returns>
    public static StudioAddress From(FlagValue<string> studio, string? organization)
    {
        if (!studio.IsSet) return new(null, organization);
        var value = studio.Value;
        if (string.IsNullOrWhiteSpace(value)) return new(Hosted, organization);
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)) return new(value.Trim().TrimEnd('/'), organization);
        var path = uri.AbsolutePath.Trim('/');
        var at = path.IndexOf(ConnectSegment, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return new(value.Trim().TrimEnd('/'), organization);
        var prefix = path[..at].Trim('/');
        var named = path[(at + ConnectSegment.Length)..].Trim('/').Split('/')[0];
        var origin = $"{uri.GetLeftPart(UriPartial.Authority)}{(prefix.Length > 0 ? $"/{prefix}" : string.Empty)}";
        return new(origin, organization ?? (named.Length > 0 ? Uri.UnescapeDataString(named) : null));
    }
}
