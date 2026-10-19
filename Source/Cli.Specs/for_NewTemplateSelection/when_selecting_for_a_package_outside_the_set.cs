// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_NewTemplateSelection;

public class when_selecting_for_a_package_outside_the_set : Specification
{
    TemplateSelection _result;

    async Task Because() => _result = await NewTemplateSelection.Select(new NewSettings { Package = "Acme.Templates" });

    [Fact] void should_use_only_that_package() => _result.Packages.ToArray().ShouldEqual(["Acme.Templates"]);
    [Fact] void should_take_its_latest_or_newest_cached_version() => NewTemplateSelection.VersionFor(_result, "Acme.Templates").ShouldEqual("*");
}
