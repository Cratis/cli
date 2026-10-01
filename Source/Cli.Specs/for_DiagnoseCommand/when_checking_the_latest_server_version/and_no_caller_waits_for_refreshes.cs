// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Diagnose;

namespace Cratis.Cli.for_DiagnoseCommand.when_checking_the_latest_server_version;

public class and_no_caller_waits_for_refreshes : Specification
{
    bool _called;
    VersionRefreshes? _given;

    Task Because() => DiagnoseCommand.CheckLatestServerVersion(
        "1.2.3",
        (_, _, refreshes, _) =>
        {
            _called = true;
            _given = refreshes;
            return Task.FromResult<string?>(null);
        },
        CancellationToken.None);

    [Fact] void should_still_check() => _called.ShouldBeTrue();
    [Fact] void should_not_register_with_any_refreshes() => _given.ShouldBeNull();
}
