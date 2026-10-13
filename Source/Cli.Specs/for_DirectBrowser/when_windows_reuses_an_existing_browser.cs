// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_windows_reuses_an_existing_browser : Specification
{
    Exception? _error;
    bool _shellExecute;

    async Task Because() => _error = await Catch.Exception(() => new DirectBrowser().Open(
        "https://identity.example/authorize",
        CancellationToken.None,
        start =>
        {
            _shellExecute = start.UseShellExecute;
            return null;
        },
        windows: true));

    [Fact] void should_not_report_a_launcher_failure() => _error.ShouldBeNull();
    [Fact] void should_use_shell_execution() => _shellExecute.ShouldBeTrue();
}
