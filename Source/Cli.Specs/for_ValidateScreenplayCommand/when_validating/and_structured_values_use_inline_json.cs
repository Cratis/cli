// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

public class and_structured_values_use_inline_json : given.a_completeness_cli_process
{
    void Establish() => File.WriteAllText(_document,
        "type OrderLine\n  sku String\n  quantity Int\n\n" +
        "module Orders\n  feature Ordering\n    slice StateChange Place\n" +
        "      command PlaceOrder\n        lines OrderLine[]\n" +
        "      event OrderPlaced\n      specification Placing\n" +
        "        when PlaceOrder\n          lines = [{\"sku\":\"A-1\",\"quantity\":2}]\n" +
        "        then OrderPlaced\n");

    Task Because() => Run();

    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_leave_standard_output_empty() => _output.ShouldBeEmpty();
    [Fact] void should_report_the_removed_inline_json_form() => _error.ShouldContain("PLAY0833");
}
