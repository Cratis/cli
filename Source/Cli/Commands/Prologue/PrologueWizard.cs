// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// The interactive prompt flow of <c>cratis prologue start</c> — walks the user through sources and output and
/// collects a <see cref="PrologueWizardInput"/> for <see cref="PrologueConfigurationBuilder"/> to assemble.
/// </summary>
public static class PrologueWizard
{
    const string SqlServerChoice = "SQL Server database";
    const string PostgresChoice = "PostgreSQL database";
    const string ApiChoice = "API (HTTP commands through a reverse proxy)";
    const string OpenTelemetryChoice = "OpenTelemetry (spans, metrics, and logs)";

    /// <summary>
    /// Collects the wizard input by prompting in the terminal.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the prompts observe.</param>
    /// <returns>The collected <see cref="PrologueWizardInput"/>.</returns>
    public static async Task<PrologueWizardInput> Collect(CancellationToken cancellationToken)
    {
        var sources = await AnsiConsole.PromptAsync(
            new MultiSelectionPrompt<string>()
                .Title("Which sources should be captured?")
                .InstructionsText($"[{OutputFormatter.Muted.ToMarkup()}](press [blue]<space>[/] to toggle a source, [green]<enter>[/] to accept)[/]")
                .AddChoices(SqlServerChoice, PostgresChoice, ApiChoice, OpenTelemetryChoice),
            cancellationToken);

        var sqlServer = sources.Contains(SqlServerChoice) ? await CollectSqlServer(cancellationToken) : [];
        var postgres = sources.Contains(PostgresChoice) ? await CollectPostgres(cancellationToken) : [];
        var api = sources.Contains(ApiChoice) ? await CollectApi(cancellationToken) : null;
        var openTelemetry = sources.Contains(OpenTelemetryChoice) ? await CollectOpenTelemetry(cancellationToken) : null;
        var output = await CollectOutput(cancellationToken);

        return new(Guid.NewGuid(), sqlServer, postgres, api, openTelemetry, output);
    }

    static async Task<IReadOnlyList<SqlServerSourceInput>> CollectSqlServer(CancellationToken cancellationToken)
    {
        var instances = new List<SqlServerSourceInput>();
        do
        {
            OutputFormatter.WriteSection($"SQL Server database #{instances.Count + 1}");
            var name = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Name (identifies the source in captures):")
                    .DefaultValue(instances.Count == 0 ? "sqlserver" : $"sqlserver{instances.Count + 1}"),
                cancellationToken);
            var connectionString = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Connection string:"),
                cancellationToken);
            var tables = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Tables to capture (comma separated, empty for all):").AllowEmpty(),
                cancellationToken);
            instances.Add(new(name, connectionString, PrologueConfigurationBuilder.ParseList(tables)));
        }
        while (await AnsiConsole.ConfirmAsync("Add another SQL Server database?", defaultValue: false, cancellationToken));

        return instances;
    }

    static async Task<IReadOnlyList<PostgresSourceInput>> CollectPostgres(CancellationToken cancellationToken)
    {
        var instances = new List<PostgresSourceInput>();
        do
        {
            OutputFormatter.WriteSection($"PostgreSQL database #{instances.Count + 1}");
            var name = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Name (identifies the source in captures):")
                    .DefaultValue(instances.Count == 0 ? "postgres" : $"postgres{instances.Count + 1}"),
                cancellationToken);
            var connectionString = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Connection string:"),
                cancellationToken);
            instances.Add(new(name, connectionString));
        }
        while (await AnsiConsole.ConfirmAsync("Add another PostgreSQL database?", defaultValue: false, cancellationToken));

        return instances;
    }

    static async Task<ApiSourceInput> CollectApi(CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection("API capture");
        var basePath = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Base path the extractor proxies:")
                .DefaultValue(PrologueConfigurationBuilder.DefaultApiBasePath),
            cancellationToken);
        var destination = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Address of your system (where proxied requests are forwarded):")
                .DefaultValue("http://replace-with-your-system:8080/"),
            cancellationToken);

        return new(basePath, destination);
    }

    static async Task<OpenTelemetrySourceInput> CollectOpenTelemetry(CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection("OpenTelemetry capture");
        var serviceNames = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Service names to capture (comma separated, empty for all):").AllowEmpty(),
            cancellationToken);
        var attributeKeys = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Span attribute keys to capture values for (comma separated):").AllowEmpty(),
            cancellationToken);
        var upstreamHttp = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Upstream OTLP/HTTP collector (empty for a terminal capture):").AllowEmpty(),
            cancellationToken);
        var upstreamGrpc = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Upstream OTLP/gRPC collector (empty for a terminal capture):").AllowEmpty(),
            cancellationToken);

        return new(
            PrologueConfigurationBuilder.ParseList(serviceNames),
            PrologueConfigurationBuilder.ParseList(attributeKeys),
            upstreamHttp.Trim(),
            upstreamGrpc.Trim());
    }

    static async Task<CaptureOutputInput> CollectOutput(CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection("Output");
        var kind = await AnsiConsole.PromptAsync(
            new SelectionPrompt<OutputKind>()
                .Title("Where should captured data go?")
                .UseConverter(candidate => candidate == OutputKind.Json
                    ? "Rolling JSON capture files (interpret them later with: cratis prologue interpret)"
                    : "The Prologue Receiver API")
                .AddChoices(OutputKind.Json, OutputKind.Api),
            cancellationToken);

        var endpoint = new ApiOptions().Endpoint;
        var directory = new JsonFileOptions().Directory;
        if (kind == OutputKind.Api)
        {
            endpoint = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Prologue Receiver endpoint:").DefaultValue(endpoint),
                cancellationToken);
        }
        else
        {
            directory = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Directory for the capture files:").DefaultValue(directory),
                cancellationToken);
        }

        return new(kind, endpoint, directory);
    }
}
