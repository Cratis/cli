// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_a_feature_is_selected : given.a_scoped_cli_process
{
    async Task Because() => await Run("--scope", "M.F");

    [Fact] void should_fail_for_the_feature_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_selected_feature() => Summary.GetProperty("scope").GetString().ShouldEqual("M.F");
    [Fact] void should_include_descendant_errors() => Summary.GetProperty("errors").GetInt32().ShouldEqual(1);
    [Fact] void should_exclude_same_named_features_in_other_modules() => _error.ShouldNotContain("outsideBroken");
}
