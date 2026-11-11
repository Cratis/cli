// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_json_output : given.a_release_model
{
    Task Because() => Run("screenplay", "baseline", "check", _root, "-o", "json-compact");
    [Fact] void should_be_clean() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_machine_readable_exit_code() => JsonElement.Parse(_output).GetProperty("exitCode").GetInt32().ShouldEqual(0);
    [Fact] void should_report_no_violations() => JsonElement.Parse(_output).GetProperty("violations").GetArrayLength().ShouldEqual(0);
}
