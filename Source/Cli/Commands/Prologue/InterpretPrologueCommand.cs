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
/// and writes the resulting <c language="csharp">.play</c> file.
/// </summary>
[LlmDescription("Interprets Prologue capture (.jsonl) files into a Cratis Screenplay (.play) file. Uses deterministic heuristics, optionally refined by a language model. --no-llm or local llm.enabled: false prevents all model requests; an unset local enabled setting falls back to cratis llm use. Announces the provider, model, endpoint host and source to stderr before sending evidence. In an interactive terminal the language model may ask clarifying questions; non-interactive runs never ask. Writes <SystemName>.play to the current directory unless --file is given. Captures of several named services are all interpreted, each into its own module, unless --service narrows the run to one.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("interpret", "Interpret captured system behavior into a Screenplay", Branch = typeof(PrologueBranch))]
[CliExample("prologue", "interpret")]
[CliExample("prologue", "interpret", "./captures")]
[CliExample("prologue", "interpret", "./captures", "--file", "MySystem.play")]
[CliExample("prologue", "interpret", "./captures", "--service", "orders")]
[LlmOption("[PATH]", "string", "Folder holding the capture (.jsonl) files. Defaults to the configured JSON output directory when a cratis-prologue.json is found, otherwise the current directory.")]
[LlmOption("--file", "string", "File to write the generated Screenplay to. Defaults to <SystemName>.play in the current directory.")]
[LlmOption("--prologue-id", "guid", "The Prologue the captures belong to. Defaults from cratis-prologue.json when present.")]
[LlmOption("--service", "string", "Interpret only the captures of one named service. Defaults to every captured service, each becoming its own module.")]
[LlmOption("--no-llm", "bool", "Force heuristics-only interpretation. Never creates a chat client or sends capture evidence to a model, regardless of local or global configuration.")]
[LlmOutputAdvice("json", "JSON outputs the written path, system name, the captured services interpreted (empty string for the default service), module/feature/slice counts, and llm block (used, kind, model, endpointHost, source) as one object. The provider notice is written to stderr before interpretation.")]
public class InterpretPrologueCommand : AsyncCommand<InterpretPrologueSettings>
{
    readonly IChatClientFactory _chatClients;
    readonly Func<LlmConfiguration?> _loadLlmConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterpretPrologueCommand"/> class.
    /// </summary>
    public InterpretPrologueCommand() : this(new ChatClientFactory(), () => CliConfiguration.Load().Llm)
    {
    }

    internal InterpretPrologueCommand(IChatClientFactory chatClients, Func<LlmConfiguration?> loadLlmConfiguration)
    {
        _chatClients = chatClients;
        _loadLlmConfiguration = loadLlmConfiguration;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, InterpretPrologueSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();

        var configurationPath = PrologueConfigurationFiles.Find(settings.Path, currentDirectory);
        var configurationJson = configurationPath is not null ? await File.ReadAllTextAsync(configurationPath, cancellationToken) : null;
        var configuration = configurationJson is not null ? PrologueConfigurationFile.Read(configurationJson) : null;
        var folder = CaptureFolders.Resolve(settings.Path, configuration, configurationPath, currentDirectory);
        var prologueId = settings.PrologueId ?? configuration?.Prologue.PrologueId ?? Guid.Empty;

        var allCaptures = Directory.Exists(folder) ? await CaptureFiles.ReadFromFolder(folder, prologueId) : [];
        if (allCaptures.Count == 0)
        {
            OutputFormatter.WriteError(
                format,
                $"No captures found in '{folder}'",
                "Run the Prologue extractor with JSON output to produce capture (.jsonl) files, or point the command at the folder holding them",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var captures = allCaptures.OfService(settings.Service is { Length: > 0 } service ? new ServiceName(service) : null);
        if (captures.Count == 0)
        {
            OutputFormatter.WriteError(
                format,
                $"No captures of the service '{settings.Service}' found in '{folder}'",
                $"Captured services: {string.Join(", ", allCaptures.Services().Select(DisplayName))}",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var (llmOptions, llmSource) = settings.NoLlm
            ? LlmOptionsResolver.ResolveWithSource(null, null, noLlm: true)
            : LlmOptionsResolver.ResolveWithSource(configurationJson, _loadLlmConfiguration());
        LlmUsage llm;
        try
        {
            llm = LlmUsage.From(llmOptions, llmSource);
        }
        catch (InvalidLlmEndpoint error)
        {
            OutputFormatter.WriteError(format, error.Message, "Configure an endpoint such as http://127.0.0.1:11434, or use --no-llm for heuristics only.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        // Always announce before creating a session, including JSON, quiet and unattended runs.
        await Console.Error.WriteLineAsync(llm.Notice);
        var showStatus = string.Equals(format, OutputFormats.Table, StringComparison.Ordinal) ||
            string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal);

        var callbacks = new ConsoleInterpreterCallbacks(llmOptions, showStatus, cancellationToken);
        var factory = new InterpreterSessionFactory(new HeuristicModelBuilder(), _chatClients, NullLogger<InterpreterSession>.Instance);
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

        WriteResult(format, state.Model, outputPath, captures.Services(), llm);
        return ExitCodes.Success;
    }

    static string DisplayName(ServiceName service) => service.IsDefault ? "(default)" : service.Value;

    static void WriteResult(string format, ExtractionResult model, string outputPath, IReadOnlyList<ServiceName> services, LlmUsage llm)
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
                Services = services.Select(service => service.Value).ToList(),
                Modules = model.Modules.Count,
                Features = features,
                Slices = slices,
                Llm = llm
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"System:   {result.SystemName.EscapeMarkup()}\n" +
                    $"Services: {string.Join(", ", services.Select(DisplayName)).EscapeMarkup()}\n" +
                    $"Modules:  {result.Modules}\n" +
                    $"Features: {result.Features}\n" +
                    $"Slices:   {result.Slices}\n" +
                    result.Llm.Notice.EscapeMarkup());
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
