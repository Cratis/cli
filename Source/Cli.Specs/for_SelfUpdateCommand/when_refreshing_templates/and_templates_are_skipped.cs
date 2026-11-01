// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_SelfUpdateCommand.when_refreshing_templates;

public class and_templates_are_skipped : for_TemplateInstaller.given.a_template_installer
{
    TemplateRefreshResult? _result;

    void Establish() => _source.Publish("1.8.0");

    async Task Because() => _result = await SelfUpdateCommand.RefreshTemplatesUnlessSkipped(new SelfUpdateSettings { NoTemplates = true }, _installer, CliVersion);

    [Fact] void should_report_nothing() => _result.ShouldBeNull();
    [Fact] void should_not_ask_the_feeds() => _source.LatestVersionRequests.ShouldEqual(0);
}
