// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// What a <c language="shell">docker run</c> of the Stage container is built from.
/// </summary>
/// <param name="Path">The absolute path to the folder of Screenplay files, or to the single Screenplay file, to mount.</param>
/// <param name="Tag">The image tag to run.</param>
/// <param name="HostPort">The host port to publish the Stage API on.</param>
/// <param name="WorkbenchHostPort">The host port to publish the Chronicle Workbench on.</param>
/// <param name="Name">The name to give the container.</param>
/// <param name="Packages">The approved custom packages to hand the Stage, or null for the packages it ships.</param>
/// <param name="Engine">The selected runtime engine. Defaults to semantic.</param>
/// <param name="SampleDataFile">An optional generated seed file mounted beside the original model.</param>
/// <param name="Runtime">How the Stage is joined to the other services of a run, or null to run it on its own.</param>
/// <param name="ExtensionArguments">The arguments that hand the Stage its extensions (<see cref="StageExtensions.BuildArguments"/>), or null for a model without any.</param>
/// <param name="Detached">True to start the container in the background (<c language="shell">-d</c>), so Docker returns once it is created.</param>
internal sealed record StageRunOptions(
    string Path,
    string Tag,
    int HostPort,
    int WorkbenchHostPort,
    string Name,
    StagePackageMounts? Packages = null,
    string Engine = "semantic",
    string? SampleDataFile = null,
    StageRuntimeOptions? Runtime = null,
    IReadOnlyList<string>? ExtensionArguments = null,
    bool Detached = false);
