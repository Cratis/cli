// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Sets up capture of a running system through an interactive wizard that writes a <c>cratis-prologue.json</c>
/// configuration for the Prologue extractor.
/// </summary>
[LlmDescription("Interactive wizard that produces a cratis-prologue.json capture configuration for the Prologue extractor: which sources to capture (SQL Server, PostgreSQL, API through a reverse proxy, OpenTelemetry) and where captured data goes (rolling JSON files or the Prologue Receiver API). Requires an interactive terminal — it cannot run in scripts or agent environments.")]
[CliCommand("start", "Set up capture of a running system with an interactive wizard", Branch = typeof(PrologueBranch))]
[CliExample("prologue", "start")]
[CliExample("prologue", "start", "--file", "./my-system")]
[LlmOption("--file", "string", "Where to write the configuration — a file path or a directory. Defaults to cratis-prologue.json in the current directory.")]
[LlmOutputAdvice("table", "The wizard is interactive and renders a summary table when done.")]
public class StartPrologueCommand : AsyncCommand<StartPrologueSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, StartPrologueSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        if (!AnsiConsole.Profile.Capabilities.Interactive || settings.Yes)
        {
            OutputFormatter.WriteError(
                format,
                "The Prologue setup wizard needs an interactive terminal",
                "Run it from a terminal without --yes, or write a cratis-prologue.json by hand — see https://github.com/Cratis/Prologue",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var input = await PrologueWizard.Collect(cancellationToken);
        var configuration = PrologueConfigurationBuilder.Build(input);
        var path = PrologueConfigurationFiles.ResolveOutputPath(settings.File, Directory.GetCurrentDirectory());

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await PrologueConfigurationFile.WriteToFile(configuration, path);

        PrintSummary(input, path);
        return ExitCodes.Success;
    }

    static void PrintSummary(PrologueWizardInput input, string path)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(OutputFormatter.Muted)
            .AddColumn(new TableColumn("[bold]Setting[/]").Padding(1, 0))
            .AddColumn(new TableColumn("[bold]Value[/]").Padding(1, 0));
        table.AddRow(new Markup("Prologue id"), new Markup(input.PrologueId.ToString().EscapeMarkup()));

        foreach (var source in input.SqlServer)
        {
            table.AddRow(new Markup("SQL Server"), new Markup(SqlServerSummary(source).EscapeMarkup()));
        }

        foreach (var source in input.Postgres)
        {
            table.AddRow(new Markup("PostgreSQL"), new Markup(source.Name.EscapeMarkup()));
        }

        if (input.Api is not null)
        {
            table.AddRow(new Markup("API"), new Markup($"{PrologueConfigurationBuilder.NormalizeBasePath(input.Api.BasePath)} → {input.Api.Destination}".EscapeMarkup()));
        }

        if (input.OpenTelemetry is not null)
        {
            table.AddRow(new Markup("OpenTelemetry"), new Markup(OpenTelemetrySummary(input.OpenTelemetry).EscapeMarkup()));
        }

        table.AddRow(new Markup("Output"), new Markup(OutputSummary(input.Output).EscapeMarkup()));
        table.AddRow(new Markup("File"), new Markup(path.EscapeMarkup()));

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        var configDirectory = Path.GetDirectoryName(path);
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]→ Run the extractor next to your system: docker run --rm -p 8080:8080 -v \"{configDirectory.EscapeMarkup()}:/config\" cratis/prologue-extractor[/]");
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]→ When captures have been collected, interpret them with: cratis prologue interpret[/]");
    }

    static string SqlServerSummary(SqlServerSourceInput source) =>
        source.Tables.Count == 0 ? $"{source.Name} (all tables)" : $"{source.Name} ({string.Join(", ", source.Tables)})";

    static string OpenTelemetrySummary(OpenTelemetrySourceInput source) =>
        source.ServiceNames.Count == 0 ? "all services" : string.Join(", ", source.ServiceNames);

    static string OutputSummary(CaptureOutputInput output) =>
        output.Kind == OutputKind.Json ? $"JSON capture files in {output.JsonDirectory}" : $"Prologue Receiver at {output.ApiEndpoint}";
}
