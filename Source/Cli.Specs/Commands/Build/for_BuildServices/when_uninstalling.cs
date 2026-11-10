// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices;

public class when_uninstalling : given.a_service_manager
{
    bool _removed;

    async Task Establish()
    {
        await _services.Install(_definition, CancellationToken.None);
        _ran.Clear();
        _answers["systemctl --user disable --now svc.service"] = new(5, string.Empty, "not loaded");
    }

    async Task Because() => _removed = await _services.Uninstall("svc", CancellationToken.None);

    [Fact] void should_report_it_removed() => _removed.ShouldBeTrue();
    [Fact] void should_remove_the_unit() => File.Exists(_platform.UnitPath("svc")).ShouldBeFalse();
    [Fact] void should_stop_it_and_then_forget_it() =>
        _ran.ToArray().ShouldEqual(["systemctl --user disable --now svc.service", "systemctl --user daemon-reload"]);
}
