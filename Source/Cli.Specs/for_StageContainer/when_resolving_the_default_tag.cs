// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_StageContainer;

/// <summary>
/// The sandbox a run starts is the Stage the artifacts were planned for.
/// </summary>
/// <remarks>
/// The CLI plans artifacts with a specific version of the Stage rendering packages and the container reads
/// them. Pairing a known renderer with a moving tag means the two drift apart on somebody else's release, and
/// the symptom arrives at a user who changed nothing.
/// </remarks>
public class when_resolving_the_default_tag : Specification
{
    string _tag;
    string _renderer;

    void Establish() => _renderer = typeof(ArtifactRenderPlan).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    void Because() => _tag = StageContainer.DefaultTag;

    [Fact] void should_run_the_stage_the_renderer_came_from() => _tag.ShouldEqual(_renderer);

    [Fact] void should_not_run_a_moving_tag() => _tag.ShouldNotEqual(StageContainer.FallbackTag);

    /// <summary>
    /// Every Stage release publishes an exact tag, so a version is always a tag that exists. Build metadata is
    /// not part of one, and asking Docker for a tag that cannot exist fails at the worst moment.
    /// </summary>
    [Fact] void should_name_a_tag_that_can_exist() => System.Version.TryParse(_tag, out _).ShouldBeTrue();

    [Fact] void should_be_what_a_run_uses_when_no_tag_is_asked_for() => new Commands.Run.RunSettings().Tag.ShouldEqual(_tag);
}
