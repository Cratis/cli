// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

sealed record ScreenplayPackageEvidence(string Name, string[] Versions, int[] ReviewedMajors)
{
    public int Count => Versions.Length;

    public string Version => Versions[0];

    public string? DivergentReason => Count > 1 ? $"Projects resolve divergent {Name} versions: {string.Join(", ", Versions)}" : null;

    public string? UnclassifiedReason => Major is null ? $"{Name} version '{Version}' cannot be classified" : null;

    public string? UnknownReason => DivergentReason ?? UnclassifiedReason;

    public string? NewerMajorReason => Major!.Value > ReviewedMajors.Max()
        ? $"{Name} {Version} is newer than the highest source-reviewed major ({ReviewedMajors.Max()})"
        : null;

    public string? UnreviewedMajorReason => ReviewedMajors.Contains(Major!.Value)
        ? null
        : $"{Name} {Version} has no canonical or source-reviewed major-generation evidence";

    int? Major
    {
        get
        {
            var separator = Version.IndexOfAny(['-', '+']);
            if (separator == Version.Length - 1)
            {
                return null;
            }

            var core = separator < 0 ? Version : Version[..separator];
            return System.Version.TryParse(core, out var parsed) ? parsed.Major : null;
        }
    }

    public string? MismatchReason(ScreenplayPackageEvidence expected) =>
        string.Equals(Version, expected.Version, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"{Name} {Version} does not match {expected.Name} {expected.Version}";
}
