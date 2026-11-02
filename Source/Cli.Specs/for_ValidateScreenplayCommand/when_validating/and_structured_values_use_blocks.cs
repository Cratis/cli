// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

public class and_structured_values_use_blocks : given.a_completeness_cli_process
{
    void Establish() => File.WriteAllText(_document,
        "type OrderLine\n  sku String\n  quantity Int\n\n" +
        "module Orders\n  feature Ordering\n    slice StateChange Place\n" +
        "      command PlaceOrder\n        lines OrderLine[]\n        tags String[]\n" +
        "      event OrderPlaced\n        lines OrderLine[]\n" +
        "      specification Placing\n        when PlaceOrder\n" +
        "          lines\n            item\n              sku = \"A-1\"\n              quantity = 2\n" +
        "          tags none\n        then OrderPlaced\n" +
        "          lines\n            item\n              sku = \"A-1\"\n              quantity = 2\n");

    Task Because() => Run();

    [Fact] void should_accept_the_model() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_valid_source() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("valid").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_report_inline_json() => _error.ShouldNotContain("PLAY0833");
}
