// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_the_new_cli_satisfies_the_newer_set : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Publish("1.8.0", """{ "minimumCliVersion": "3.0.0", "packages": ["Cratis.Templates", "Cratis.Templates.Kotlin"] }""");
    }

    async Task Because() => _result = await _installer.Refresh("3.1.0");

    [Fact] void should_report_the_update() => _result.Updated.ShouldBeTrue();
    [Fact] void should_install_the_packages_the_catalog_lists() => Installed!.Packages.ShouldContainOnly("Cratis.Templates", "Cratis.Templates.Kotlin");
}
