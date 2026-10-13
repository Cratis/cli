// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class and_a_sibling_is_not_admitted : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ScreenplayRenderPlan _full = null!;
    ScreenplayRenderPlan _scoped = null!;
    ArtifactPublicationResult _published = null!;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "unsupported.play"), """
        module Tasks
          feature Unsupported
            slice StateChange RegisterOtherProject
              command RegisterOtherProject
                projectId ProjectId generated identifier
                occurred ProjectId generated
                name String
                produces event OtherProjectRegistered
                  name String = name
                  timestamp DateTime = $context.occurred
        """);

    async Task Because()
    {
        _full = await Plan();
        _scoped = await Plan("Projects.Registration");
        _published = await Publish(_scoped);
    }

    [Fact] void should_keep_full_render_refused() => _full.Success.ShouldBeFalse();
    [Fact] void should_name_the_unrenderable_construct() => _full.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-038");
    [Fact] void should_render_the_admitted_feature() => _scoped.Success.ShouldBeTrue();
    [Fact] void should_publish_a_scaffolded_application() => File.Exists(Path.Combine(_destination, "Shop.csproj")).ShouldBeTrue();
    [Fact] void should_make_the_remainder_visible() => _published.Receipt.OutsideScope!.ShouldContain("Tasks.Unsupported.RegisterOtherProject");
}
