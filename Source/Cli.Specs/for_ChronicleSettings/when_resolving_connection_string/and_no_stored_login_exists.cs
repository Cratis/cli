// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_no_stored_login_exists : given.a_temp_config_directory
{
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        ChronicleSettings.ResetWarningsForSpecs();
        new CliConfiguration
        {
            Contexts = new Dictionary<string, CliContext> { ["default"] = new() { TokenServer = "old:35000" } }
        }.Save();
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Because() => new ChronicleSettings { Server = "chronicle://other:35000" }.ResolveConnectionString();

    protected override void CleanUp()
    {
        Console.SetError(_previousError);
        _error.Dispose();
        ChronicleSettings.ResetWarningsForSpecs();
        base.CleanUp();
    }

    [Fact] void should_not_report_a_login_mismatch() => _error.ToString().ShouldBeEmpty();
}
