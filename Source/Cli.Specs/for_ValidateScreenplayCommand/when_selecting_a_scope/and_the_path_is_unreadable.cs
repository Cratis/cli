// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

[Collection(CliSpecsCollection.Name)]
public class and_the_path_is_unreadable : given.a_failed_scope_selection
{
    void Establish() => _kind = ScopeSelectionErrorKind.UnreadablePath;
    async Task Because() => await RunFailedSelection();

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_suggest_checking_read_access() => _error.GetProperty("suggestion").GetString().ShouldEqual("Check that the application files and folders are readable");
}
