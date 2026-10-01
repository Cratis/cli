// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Diagnose;

namespace Cratis.Cli.for_DiagnoseCommand.when_checking_the_latest_server_version;

/// <summary>
/// The refresh the check starts behind a cached answer must reach the caller that waits for refreshes before
/// the process exits, so the check is given that caller's set.
/// </summary>
public class and_the_caller_waits_for_refreshes : Specification
{
    VersionRefreshes _callers = null!;
    VersionRefreshes? _given;
    string? _package;
    string? _version;
    string? _result;

    async Task Because()
    {
        _callers = VersionRefreshes.Begin();

        // The command runs further down the caller's asynchronous chain, after the caller began its set.
        await Task.Yield();
        _result = await DiagnoseCommand.CheckLatestServerVersion(
            "1.2.3",
            (package, version, refreshes, _) =>
            {
                _package = package;
                _version = version;
                _given = refreshes;
                return Task.FromResult<string?>("1.3.0");
            },
            CancellationToken.None);
    }

    [Fact] void should_give_the_check_the_callers_refreshes() => _given.ShouldEqual(_callers);
    [Fact] void should_check_the_server_package() => _package.ShouldEqual(UpdateChecker.ServerPackageId);
    [Fact] void should_check_from_the_server_version() => _version.ShouldEqual("1.2.3");
    [Fact] void should_return_the_latest_version() => _result.ShouldEqual("1.3.0");
}
