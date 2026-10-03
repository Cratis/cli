// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Describes the machine without conflating package availability with host support.</summary>
/// <param name="Os">Operating system family.</param>
/// <param name="Architecture">Native architecture.</param>
/// <param name="Home">Personal configuration root.</param>
/// <param name="LocalApplications">Per-user applications root.</param>
/// <param name="ProgramFiles">Machine applications root.</param>
internal sealed record DesktopMcpPlatform(string Os, string Architecture, string Home, string LocalApplications, string ProgramFiles)
{
    internal static DesktopMcpPlatform Current => new(
        CurrentOs,
        RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

    internal string? Rid => (Os, Architecture) switch
    {
        ("osx", "arm64" or "x64") => $"{Os}-{Architecture}",
        ("win", "x64") => "win-x64",
        ("linux", "arm64" or "x64") => $"{Os}-{Architecture}",
        _ => null
    };

    internal bool SupportsDesktop => Rid is not null && (Os == "osx" || Os == "win");

    static string CurrentOs
    {
        get
        {
            if (OperatingSystem.IsMacOS()) return "osx";
            return OperatingSystem.IsWindows() ? "win" : "linux";
        }
    }
}
