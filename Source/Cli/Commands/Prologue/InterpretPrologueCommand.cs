// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Cratis.Prologue.Contracts;
using Cratis.Prologue.Interpretation;
using Cratis.Prologue.Interpreter.Contracts;
using Cratis.Prologue.Screenplay;
using Cratis.Screenplay.Printing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Interprets captured system behavior into a Cratis Screenplay — reads the capture files, drives an
/// interpreter session (optionally refined by the configured language model, asking questions along the way),
/// and writes the resulting <c>.play</c> file.
/// </summary>
[LlmDescription("Interprets Prologue capture (.jsonl) files into a Cratis Screenplay (.play) file. Uses deterministic heuristics, refined by the configured language model when one is set up (cratis-prologue.json Llm section or cratis llm use). In an interactive terminal the language model may ask clarifying questions; non-interactive runs never ask. Writes <SystemName>.play to the current directory unless --file is given.")]
[CliCommand("interpret", "Interpret captured system behavior into a Screenplay", Branch = typeof(PrologueBranch))]
[CliExample("prologue", "interpret")]
[CliExample("prologue", "interpret", "./captures")]
[CliExample("prologue", "interpret", "./captures", "--file", "MySystem.play")]
[LlmOption("[PATH]", "string", "Folder holding the capture (.jsonl) files. Defaults to the configured JSON output directory when a cratis-prologue.json is found, otherwise the current directory.")]
[LlmOption("--file", "string", "File to write the generated Screenplay to. Defaults to <SystemName>.play in the current directory.")]
[LlmOption("--prologue-id", "guid", "The Prologue the captures belong to. Defaults from cratis-prologue.json when present.")]
[LlmOutputAdvice("json", "JSON outputs the written path, system name, and module/feature/slice counts as one object.")]
public class InterpretPrologueCommand : AsyncCommand<InterpretPrologueSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, InterpretPrologueSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();

        var configurationPath = PrologueConfigurationFiles.Find(settings.Path, currentDirectory);
        var configuration = configurationPath is not null ? await PrologueConfigurationFile.ReadFromFile(configurationPath) : null;
        var folder = CaptureFolders.Resolve(settings.Path, configuration, configurationPath, currentDirectory);
        var prologueId = settings.PrologueId ?? configuration?.Prologue.PrologueId ?? Guid.Empty;

        var captures = Directory.Exists(folder) ? await CaptureFiles.ReadFromFolder(folder, prologueId) : [];
        if (captures.Count == 0)
        {
            OutputFormatter.WriteError(
                format,
                $"No captures found in '{folder}'",
                "Run the Prologue extractor with JSON output to produce capture (.jsonl) files, or point the command at the folder holding them",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var llmOptions = LlmOptionsResolver.Resolve(configuration, CliConfiguration.Load().Llm);
        var showStatus = string.Equals(format, OutputFormats.Table, StringComparison.Ordinal) ||
            string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal);
        if (!llmOptions.Enabled && showStatus)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]No language model configured — interpreting with heuristics only. Configure one with: cratis llm use <kind>[/]");
        }

        var callbacks = new ConsoleInterpreterCallbacks(llmOptions, showStatus, cancellationToken);
        var factory = new InterpreterSessionFactory(new HeuristicModelBuilder(), new ChatClientFactory(), NullLogger<InterpreterSession>.Instance);
        var interactive = AnsiConsole.Profile.Capabilities.Interactive && !settings.Yes;
        var session = factory.CreateNew(
            prologueId,
            captures,
            llmOptions,
            callbacks.OnStatusChanged,
            interactive ? IInterpreterSessionFactory.DefaultMaxQuestionRounds : 0);

        var state = await new InterpreterRunner().Run(session, callbacks, cancellationToken);
        if (state.Status == InterpreterStatus.Failed || state.Model is null)
        {
            OutputFormatter.WriteError(
                format,
                state.Error.Length > 0 ? state.Error : "Interpretation failed",
                errorCode: ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }

        callbacks.OnStatusChanged(InterpreterStatus.GeneratingScreenplay);
        var source = new ScreenplayGenerator(new ScreenplayPrinter()).Generate(state.Model);
        var outputPath = ScreenplayOutput.ResolvePath(settings.File, state.Model.SystemName, currentDirectory);
        await File.WriteAllTextAsync(outputPath, source, cancellationToken);

        WriteResult(format, state.Model, outputPath);
        return ExitCodes.Success;
    }

    static void WriteResult(string format, ExtractionResult model, string outputPath)
    {
        var features = model.Modules.Sum(module => module.Features.Sum(CountFeatures));
        var slices = model.Modules.Sum(module => module.Features.Sum(CountSlices));

        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            Console.WriteLine(outputPath);
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Path = outputPath,
                model.SystemName,
                Modules = model.Modules.Count,
                Features = features,
                Slices = slices
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"System:   {result.SystemName.EscapeMarkup()}\n" +
                    $"Modules:  {result.Modules}\n" +
                    $"Features: {result.Features}\n" +
                    $"Slices:   {result.Slices}");
                var panel = new Panel(content)
                    .Header(" Screenplay generated ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]→ Run it in a local Stage sandbox with: cratis run[/]");
            });
    }

    static int CountFeatures(ExtractedFeature feature) => 1 + feature.SubFeatures.Sum(CountFeatures);

    static int CountSlices(ExtractedFeature feature) => feature.Slices.Count + feature.SubFeatures.Sum(CountSlices);
}
