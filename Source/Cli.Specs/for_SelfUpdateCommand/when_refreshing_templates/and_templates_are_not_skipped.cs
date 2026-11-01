// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_SelfUpdateCommand.when_refreshing_templates;

public class and_templates_are_not_skipped : for_TemplateInstaller.given.a_template_installer
{
    TemplateRefreshResult? _result;

    void Establish() => _source.Publish("1.8.0");

    async Task Because() => _result = await SelfUpdateCommand.RefreshTemplatesUnlessSkipped(new SelfUpdateSettings { NoTemplates = false }, _installer, CliVersion);

    [Fact] void should_install_the_latest_templates() => _result!.CurrentVersion.ShouldEqual("1.8.0");
    [Fact] void should_report_the_installation() => _result!.Updated.ShouldBeTrue();
}
