// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>The project-owned selection used to resolve the Cratis AI corpus.</summary>
/// <param name="Harnesses">The harnesses to integrate.</param>
/// <param name="Profiles">The Cratis profiles to resolve.</param>
/// <param name="Languages">The languages used by the repository.</param>
public sealed record AiConfiguration(IReadOnlyList<string> Harnesses, IReadOnlyList<string> Profiles, IReadOnlyList<string> Languages)
{
    /// <summary>Gets the current configuration schema version.</summary>
    public const string SchemaVersion = "1.0";
}

/// <summary>A Cratis-owned file and the bytes installed for it.</summary>
/// <param name="Source">The stable source identity.</param>
/// <param name="Destination">The managed relative path.</param>
/// <param name="Hash">The hash of the installed bytes.</param>
public sealed record AiManagedFile(string Source, string Destination, string Hash);

/// <summary>A harness integration created by Cratis AI.</summary>
/// <param name="Path">The path relative to the consuming repository.</param>
/// <param name="Target">The relative symbolic-link target.</param>
/// <param name="IsDirectory">Whether the target is a directory.</param>
/// <param name="PreserveExisting">Whether an existing user-owned path satisfies the integration without becoming managed.</param>
public sealed record AiManagedIntegration(string Path, string Target, bool IsDirectory, bool PreserveExisting = false);

/// <summary>Metadata used to make synchronization deterministic without claiming user files.</summary>
/// <param name="SourceRevision">The corpus revision used during installation.</param>
/// <param name="Files">The Cratis-managed files.</param>
/// <param name="Integrations">The harness integrations created by Cratis.</param>
public sealed record AiInstallationManifest(string SourceRevision, IReadOnlyList<AiManagedFile> Files, IReadOnlyList<AiManagedIntegration>? Integrations = null);
