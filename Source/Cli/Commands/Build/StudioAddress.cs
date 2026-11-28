// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What `cratis build connect` was told about which Studio and which organization, with the Studio filled in
/// when it is the hosted one.
/// </summary>
/// <param name="Url">The Studio's address without a trailing slash, or <see langword="null"/> when none was given.</param>
/// <param name="Organization">The organization's name, or <see langword="null"/> when none was given.</param>
public sealed record StudioAddress(string? Url, string? Organization)
{
    /// <summary>The hosted Studio, used when `--studio` is given without an address or left out on a fresh machine.</summary>
    public const string Hosted = "https://app.cratis.studio";

    const string ConnectSegment = "builders/connect";

    /// <summary>
    /// Reads the `--studio` and `--organization` options. A `--studio` without an address means the hosted
    /// Studio. An address copied from Studio's builder address (`https://host/builders/connect/acme`) is reduced to
    /// the Studio and, unless `--organization` says otherwise, the organization it names.
    /// </summary>
    /// <param name="studio">The `--studio` option.</param>
    /// <param name="organization">The `--organization` option.</param>
    /// <returns>What was asked for.</returns>
    public static StudioAddress From(FlagValue<string> studio, string? organization)
    {
        if (!studio.IsSet) return new(null, organization);
        var value = studio.Value;
        if (string.IsNullOrWhiteSpace(value)) return new(Hosted, organization);
        var uri = BuilderTransport.Validate(value.Trim());
        var path = uri.AbsolutePath.Trim('/');
        var at = path.IndexOf(ConnectSegment, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return new(value.Trim().TrimEnd('/'), organization);
        var prefix = path[..at].Trim('/');
        var named = path[(at + ConnectSegment.Length)..].Trim('/').Split('/')[0];
        var origin = $"{uri.GetLeftPart(UriPartial.Authority)}{(prefix.Length > 0 ? $"/{prefix}" : string.Empty)}";
        return new(origin, organization ?? (named.Length > 0 ? Uri.UnescapeDataString(named) : null));
    }
}
