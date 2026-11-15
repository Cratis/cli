// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Environments;
using Cratis.Environments.Secrets;
using Spectre.Console;

namespace Cratis.Cli.Commands.Environments.for_EnvCommands;

[Collection(CliSpecsCollection.Name)]
public class when_reporting_a_retained_vault_entry : given.an_environment_project
{
    ProjectEnvironment _environment;
    IAnsiConsole _previousConsole;
    string _text;
    string _json;
    int _moved;

    async Task Establish()
    {
        _previousConsole = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_output), Ansi = AnsiSupport.No, Enrichment = new() { UseDefaultEnrichers = false } });
        AnsiConsole.Console.Profile.Width = 1000;
        Dictionary<string, string> values = [];
        var vault = Substitute.For<IKeyVaultSecrets>();
        vault.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            values[call.ArgAt<string>(0)] = call.ArgAt<string>(1);
            return Task.CompletedTask;
        });
        vault.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(values.GetValueOrDefault(call.ArgAt<string>(0))));
        vault.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            values.Remove(call.ArgAt<string>(0));
            return Task.CompletedTask;
        });
        _environment = ProjectEnvironment.Create(ProjectEnvironments.ForProject(_root), "production", "azurekeyvault://my-vault", PassphraseSources.Fixed("p"), keyVault: vault);
        await _environment.SetAsync("smtp:password", "fixture-value", secret: true);
        _moved = await _environment.ChangeSecretsProviderAsync("azurekeyvault://https://MY-VAULT.vault.azure.net:443/");
    }

    async Task Because()
    {
        await EnvChangeSecretsProviderCommand.WriteMoveResult(OutputFormats.Plain, _environment, _moved);
        _text = _output.ToString();
        _output.GetStringBuilder().Clear();
        await EnvChangeSecretsProviderCommand.WriteMoveResult(OutputFormats.JsonCompact, _environment, _moved);
        _json = _output.ToString();
    }

    void Destroy() => AnsiConsole.Console = _previousConsole;

    [Fact] void should_report_the_move_in_text() => _text.ShouldContain("Moved 1 secret(s)");
    [Fact] void should_not_claim_removal_in_text() => _text.Contains("Removed", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_warn_on_standard_error() => _error.ToString().ShouldContain("Warning: Kept previous AzureKeyVault entry");
    [Fact] void should_name_the_key_in_the_warning() => _error.ToString().ShouldContain("smtp:password");
    [Fact] void should_not_disclose_the_value() => AllOutput.ShouldNotContain("fixture-value");
    [Fact] void should_report_no_removals_in_json() => JsonElement.Parse(_json).GetProperty("secretsRemoved").GetInt32().ShouldEqual(0);
    [Fact] void should_include_the_warning_in_json() => JsonElement.Parse(_json).GetProperty("warnings")[0].GetString().ShouldContain("Kept previous");
    [Fact] async Task should_leave_the_new_secret_readable() => (await _environment.GetValueAsync("smtp:password", TestContext.Current.CancellationToken)).ShouldEqual("fixture-value");
}
