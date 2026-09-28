// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Workbench;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_workbench_has_a_legacy_login : a_temp_config_directory
{
    WorkbenchSettings _settings = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        ChronicleSettings.ResetWarningsForSpecs();
        new CliConfiguration
        {
            ActiveContext = "legacy",
            Contexts = new Dictionary<string, CliContext>
            {
                ["legacy"] = new() { Server = "chronicle://legacy:35000", LoggedInUser = "admin" }
            }
        }.Save();
        _settings = new WorkbenchSettings();
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Because()
    {
        _settings.ResolveConnectionString();
        _settings.ResolveConnectionString();
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
            ChronicleSettings.ResetWarningsForSpecs();
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_not_write_over_the_tui() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_expose_a_notice_for_the_workbench() => _settings.LegacyLoginNeedsRefresh.ShouldBeTrue();
}
