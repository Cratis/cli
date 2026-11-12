// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Environments;

namespace Cratis.Cli.Commands.Environments.for_ModelRequirements;

public class when_reading_a_model_with_an_extension : Extensions.given.a_restored_extension_project
{
    ModelRequirements _requirements;

    void Establish() => Restore();

    void Because() => _requirements = ModelRequirements.Read(Project.Model);

    [Fact] void should_need_the_secret_setting_of_the_extension_under_the_system() => _requirements.Requirements.Keys.Select(key => key.Key).ShouldContainOnly("systems.Payments.secretKey");
    [Fact] void should_mark_that_setting_as_a_secret() => _requirements.Requirements.Keys.Single().IsSecret.ShouldBeTrue();
    [Fact] void should_have_read_every_contract() => _requirements.Unresolved.ShouldBeEmpty();
}
