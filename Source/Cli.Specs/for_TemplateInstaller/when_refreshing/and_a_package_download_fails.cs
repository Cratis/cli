// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_a_package_download_fails : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Publish("1.8.0");
        _source.Failing.Add(("Cratis.Templates.Java", "1.8.0"));
    }

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_not_report_an_update() => _result.Updated.ShouldBeFalse();
    [Fact] void should_say_why() => _result.Message.ShouldContain("Cratis.Templates.Java");
    [Fact] void should_keep_the_installed_set() => Installed!.Version.ShouldEqual("1.7.0");
}
