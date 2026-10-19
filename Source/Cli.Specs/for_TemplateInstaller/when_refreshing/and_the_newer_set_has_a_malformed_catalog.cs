// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_the_newer_set_has_a_malformed_catalog : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Publish("1.8.0", """{ "packages": [null, "../../outside"] }""");
    }

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_install_the_newer_set() => _result.CurrentVersion.ShouldEqual("1.8.0");
    [Fact] void should_fall_back_to_the_built_in_packages() => Installed!.Packages.ToArray().ShouldEqual([.. TemplateCatalogue.PinnedPackages]);
    [Fact] void should_not_reach_outside_the_store() => _source.Acquired.Exists(entry => entry.PackageId.Contains("..")).ShouldBeFalse();
}
