// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Templating.Packages;

internal static partial class PackageVersion
{
    [GeneratedRegex(@"\A[0-9]+(?:\.[0-9]+){0,3}(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex VersionToken { get; }

    [GeneratedRegex(@"\A[A-Za-z0-9_][A-Za-z0-9_.-]*\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex PackageId { get; }

    internal static bool IsValid(string? value) => value is not null && VersionToken.IsMatch(value)
        && NumericText(value).Split('.').All(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out _));

    internal static void Validate(string value)
    {
        if (!IsValid(value)) throw new TemplatePackageAcquisitionError("invalid package version.");
    }

    internal static void ValidateId(string value)
    {
        if (!IsValidId(value)) throw new TemplatePackageAcquisitionError("invalid package id.");
    }

    internal static Version Numeric(string value)
    {
        var numeric = NumericText(value);
        return Version.Parse(numeric.Contains('.') ? numeric : $"{numeric}.0");
    }

    internal static bool IsPrerelease(string value) => value.Split('+')[0].Contains('-');

    internal static string SelectLatest(IEnumerable<string> versions, bool preserveSpelling = false)
    {
        var valid = versions.Where(IsValid).ToArray();
        if (valid.Length == 0) throw new TemplatePackageAcquisitionError("no valid package versions were available.");
        var stable = valid.Where(version => !IsPrerelease(version)).OrderByDescending(Numeric).ToArray();
        if (stable.Length > 0)
        {
            return preserveSpelling || stable[0].Contains('+') ? stable[0] : Numeric(stable[0]).ToString();
        }
        return valid.OrderByDescending(Numeric).ThenByDescending(version => version, StringComparer.Ordinal).First();
    }

    static bool IsValidId(string value) => !string.IsNullOrEmpty(value) && PackageId.IsMatch(value) && !value.EndsWith('.');

    static string NumericText(string value) => value.Split('-', '+')[0];
}
