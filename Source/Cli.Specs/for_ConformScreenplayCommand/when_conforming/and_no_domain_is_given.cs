// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_no_domain_is_given : given.a_conform_command
{
    async Task Because() => await Execute();
    [Fact] void should_use_the_authored_domain() => _options.Domain.ShouldEqual("Library");
    [Fact] void should_leave_settings_unchanged() => _settings.Domain.ShouldBeNull();
}
