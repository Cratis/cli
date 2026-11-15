// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageOutput;

[Collection(CliSpecsCollection.Name)]
public class when_reporting_readiness_of_a_detached_stack : for_RunCommand.when_reporting_refusals.given.a_refusal_output
{
    static readonly RunStackPlan _stack = new(
        "network",
        new("network", "stage", "127.0.0.1", [], []),
        [],
        [],
        [],
        [new("Application", "http://localhost:8080"), new("Mail", "http://localhost:8025")],
        "http://localhost:8080",
        "/runs/mail");

    static void WriteReady(string format) =>
        RunOutput.WriteReady(format, new StageStartup("image"), "/models", StageEndpoints.For(9090, 35000), "semantic", stack: _stack, container: "cratis-run-1");

    string _plain = null!;
    string _json = null!;
    string _table = null!;

    string Written(string format)
    {
        _output.GetStringBuilder().Clear();
        WriteReady(format);
        return _output.ToString().ReplaceLineEndings("\n");
    }

    void Because()
    {
        _plain = Written(OutputFormats.Plain);
        _json = Written(OutputFormats.JsonCompact);
        _table = Written(OutputFormats.Table);
    }

    [Fact]
    void should_write_every_plain_line_in_order() =>
        _plain.ShouldEqual(
            "eventModel\t\n" +
            "eventStore\t\n" +
            "engine\tsemantic\n" +
            "service\tApplication\thttp://localhost:8080\n" +
            "service\tMail\thttp://localhost:8025\n" +
            "mailFolder\t/runs/mail\n" +
            "container\tcratis-run-1\n" +
            "status\tready\n");

    [Fact]
    void should_write_the_whole_ready_document() =>
        _json.ShouldEqual(
            "{\"status\":\"ready\",\"engine\":\"semantic\",\"notes\":[],\"path\":\"/models\"," +
            "\"stageApi\":\"http://localhost:9090\",\"apiReference\":\"http://localhost:9090/scalar/v1\",\"workbench\":\"https://localhost:35000\"," +
            "\"application\":\"http://localhost:8080\"," +
            "\"services\":[{\"name\":\"Application\",\"url\":\"http://localhost:8080\"},{\"name\":\"Mail\",\"url\":\"http://localhost:8025\"}]," +
            "\"mailFolder\":\"/runs/mail\",\"container\":\"cratis-run-1\"}\n");

    [Fact] void should_mark_it_ready_in_a_table() => _table.ShouldContain("✓ Ready");
    [Fact] void should_name_the_engine_in_a_table() => _table.ShouldContain("Runtime engine");
    [Fact] void should_list_the_services_in_a_table() => _table.ShouldContain("http://localhost:8025");
    [Fact] void should_name_the_mail_folder_in_a_table() => _table.ShouldContain("Onboarding emails");
    [Fact] void should_say_how_mail_is_written_in_a_table() => _table.ShouldContain("Written as .eml files, never sent — the environment names no mail server");
    [Fact] void should_say_how_to_stop_the_container_in_a_table() => _table.ShouldContain("Left running — stop it with: cratis stop cratis-run-1");
    [Fact] void should_not_offer_ctrl_c_in_a_table() => _table.ShouldNotContain("Press Ctrl+C to stop");
}
