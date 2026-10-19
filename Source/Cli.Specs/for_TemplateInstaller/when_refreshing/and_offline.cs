// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_offline : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Offline = true;
    }

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_keep_the_installed_version() => _result.CurrentVersion.ShouldEqual("1.7.0");
    [Fact] void should_say_why() => _result.Message!.ShouldContain("no such host");
    [Fact] void should_keep_the_installed_set() => Installed!.Version.ShouldEqual("1.7.0");
}
