// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// The interactive prompt flow of <c language="csharp">cratis prologue start</c> — walks the user through sources and output and
/// collects a <see cref="PrologueWizardInput"/> for <see cref="PrologueConfigurationBuilder"/> to assemble.
/// </summary>
public static class PrologueWizard
{
    internal const string SqlServerChoice = "SQL Server database";
    internal const string PostgresChoice = "PostgreSQL database";
    internal const string ApiChoice = "API (HTTP commands through a reverse proxy)";
    internal const string OpenTelemetryChoice = "OpenTelemetry (spans, metrics, and logs)";

    /// <summary>
    /// Collects the wizard input by prompting in the terminal.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the prompts observe.</param>
    /// <returns>The collected <see cref="PrologueWizardInput"/>.</returns>
    public static async Task<PrologueWizardInput> Collect(CancellationToken cancellationToken)
    {
        var severalServices = await AnsiConsole.ConfirmAsync(
            "Is the system made of several services, each with its own databases, API or telemetry?",
            defaultValue: false,
            cancellationToken);
        if (severalServices)
        {
            return await PrologueServicesWizard.Collect(cancellationToken);
        }

        var sources = await CollectSources("Which sources should be captured?", cancellationToken);
        var sqlServer = sources.Contains(SqlServerChoice) ? await CollectSqlServer("sqlserver", cancellationToken) : [];
        var postgres = sources.Contains(PostgresChoice) ? await CollectPostgres("postgres", cancellationToken) : [];
        var api = sources.Contains(ApiChoice) ? await CollectApi(PrologueConfigurationBuilder.DefaultApiBasePath, _ => true, cancellationToken) : null;
        var openTelemetry = sources.Contains(OpenTelemetryChoice) ? await CollectOpenTelemetry(askServiceNames: true, cancellationToken) : null;
        var output = await CollectOutput(cancellationToken);

        return new(Guid.NewGuid(), sqlServer, postgres, api, openTelemetry, output);
    }

    internal static Task<List<string>> CollectSources(string title, CancellationToken cancellationToken) =>
        AnsiConsole.PromptAsync(
            new MultiSelectionPrompt<string>()
                .Title(title)
                .InstructionsText($"[{OutputFormatter.Muted.ToMarkup()}](press [blue]<space>[/] to toggle a source, [green]<enter>[/] to accept)[/]")
                .AddChoices(SqlServerChoice, PostgresChoice, ApiChoice, OpenTelemetryChoice),
            cancellationToken);

    internal static async Task<IReadOnlyList<SqlServerSourceInput>> CollectSqlServer(string defaultName, CancellationToken cancellationToken)
    {
        var instances = new List<SqlServerSourceInput>();
        do
        {
            OutputFormatter.WriteSection($"SQL Server database #{instances.Count + 1}");
            var name = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Name (identifies the source in captures):")
                    .DefaultValue(instances.Count == 0 ? defaultName : $"{defaultName}{instances.Count + 1}"),
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

    internal static async Task<IReadOnlyList<PostgresSourceInput>> CollectPostgres(string defaultName, CancellationToken cancellationToken)
    {
        var instances = new List<PostgresSourceInput>();
        do
        {
            OutputFormatter.WriteSection($"PostgreSQL database #{instances.Count + 1}");
            var name = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Name (identifies the source in captures):")
                    .DefaultValue(instances.Count == 0 ? defaultName : $"{defaultName}{instances.Count + 1}"),
                cancellationToken);
            var connectionString = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Connection string:"),
                cancellationToken);
            instances.Add(new(name, connectionString));
        }
        while (await AnsiConsole.ConfirmAsync("Add another PostgreSQL database?", defaultValue: false, cancellationToken));

        return instances;
    }

    internal static async Task<ApiSourceInput> CollectApi(string defaultBasePath, Func<string, bool> isAvailable, CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection("API capture");
        var basePath = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Base path the extractor proxies (forwarded unchanged):")
                .DefaultValue(defaultBasePath)
                .Validate(candidate => isAvailable(PrologueConfigurationBuilder.NormalizeBasePath(candidate))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Another service already uses this base path")),
            cancellationToken);
        var destination = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Address of your system (where proxied requests are forwarded):")
                .DefaultValue("http://replace-with-your-system:8080/"),
            cancellationToken);

        return new(basePath, destination);
    }

    internal static async Task<OpenTelemetrySourceInput> CollectOpenTelemetry(bool askServiceNames, CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection("OpenTelemetry capture");
        var serviceNames = askServiceNames
            ? await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Service names to capture (comma separated, empty for all):").AllowEmpty(),
                cancellationToken)
            : string.Empty;
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

    internal static async Task<CaptureOutputInput> CollectOutput(CancellationToken cancellationToken)
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
