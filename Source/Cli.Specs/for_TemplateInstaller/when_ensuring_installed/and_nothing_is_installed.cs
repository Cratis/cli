// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_ensuring_installed;

public class and_nothing_is_installed : given.a_template_installer
{
    TemplateSelection _result;

    void Establish()
    {
        _source.Publish("1.7.0");
        _source.Publish("1.8.0");
    }

    async Task Because() => _result = await _installer.EnsureInstalled();

    [Fact] void should_download_the_latest_version() => _result.Version.ShouldEqual("1.8.0");
    [Fact] void should_report_it_as_downloaded() => _result.Origin.ShouldEqual(TemplateSelectionOrigin.Downloaded);
    [Fact] void should_download_every_package_of_the_set() => _source.Acquired.ShouldContainOnly(TemplateCatalogue.PinnedPackages.Select(package => (PackageId: package, Version: "1.8.0")));
    [Fact] void should_record_the_set_as_installed() => Installed!.Version.ShouldEqual("1.8.0");
}
