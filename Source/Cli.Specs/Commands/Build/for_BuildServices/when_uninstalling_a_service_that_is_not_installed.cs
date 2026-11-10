// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices;

public class when_uninstalling_a_service_that_is_not_installed : given.a_service_manager
{
    bool _removed;

    async Task Because() => _removed = await _services.Uninstall("svc", CancellationToken.None);

    [Fact] void should_report_nothing_removed() => _removed.ShouldBeFalse();
    [Fact] void should_not_touch_the_service_manager() => _ran.ShouldBeEmpty();
}
