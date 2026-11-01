// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_broken_application_shell : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(
        Path.Combine(_folder, "application.play"),
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shell", "model", "application.play"))
            .Replace("target Sales.Orders", "target Sales.Nowhere", StringComparison.Ordinal)
            .Replace("slice Sales.Orders.PlaceOrder", "slice Sales.Orders.OrderList", StringComparison.Ordinal)
            .Replace("active Demo", "active Missing", StringComparison.Ordinal));

    async Task Because() => _result = await Run(_folder);

    [Fact] void should_refuse_the_run() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_navigation_target_with_its_code_and_location() => _error.ToString().ShouldContain("PLAY0748");
    [Fact] void should_report_the_toolbar_button_with_its_code() => _error.ToString().ShouldContain("PLAY0762");
    [Fact] void should_report_the_sample_data_selection_with_its_code() => _error.ToString().ShouldContain("PLAY0714");
    [Fact] void should_locate_the_findings_in_the_file() => _error.ToString().ShouldContain("application.play(");
    [Fact] void should_say_the_shell_does_not_compile() => _error.ToString().ShouldContain("the application shell does not compile");
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
