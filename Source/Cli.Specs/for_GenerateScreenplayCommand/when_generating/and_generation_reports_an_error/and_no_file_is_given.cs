// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating.and_generation_reports_an_error;

[Collection(CliSpecsCollection.Name)]
public class and_no_file_is_given : given.a_generation_reporting_an_error
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_keep_standard_output_clean() => _standardOutput.ToArray().ShouldBeEmpty();
}
