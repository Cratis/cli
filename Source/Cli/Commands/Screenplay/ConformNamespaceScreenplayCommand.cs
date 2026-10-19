// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;
using Cratis.Screenplay.Observations;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compares an authored model with the event registrations, appends and consumers a Chronicle namespace holds.
/// </summary>
[LlmDescription("Compares an authored .play model with what one Chronicle event store, namespace and event sequence holds: registered event types, appended event counts attributed to commands and reactors through the causation chain, and registered reactor subscriptions. Reads only event metadata, never payloads, up to a tail captured at the start; --max-events (default 100000) and --from/--to bound the scan and a reached bound is reported as partial coverage. Findings are Screenplay's: Drift, Informational, Unknown or Excluded. Exit codes: 0 when the comparison ran (drift is reported but does not fail unless --fail-on-drift), 7 drift_detected and 8 inconclusive with --fail-on-drift, 5 for model errors or a missing event store, namespace or sequence, 2 connection error, 3 server error, 4 authorization error. Never prints event content, event source ids, command values or raw causation.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("conform-namespace", "Compare an authored Screenplay model with what a Chronicle namespace observed", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "conform-namespace", "./plays", "-e", "Library", "-n", "Default")]
[CliExample("screenplay", "conform-namespace", "./MyApp.play", "-e", "Library", "-n", "Default", "--max-events", "20000", "--fail-on-drift", "-o", "json")]
[LlmOption("<MODEL_ROOT>", "string", "Authored .play root with imports, or a folder containing one application.")]
[LlmOption("-e, --event-store", "string", "Event store to observe; must be given or set in the context, and must exist.")]
[LlmOption("-n, --namespace", "string", "Namespace to observe; must be given or set in the context, and must exist.")]
[LlmOption("--sequence", "string", "Event sequence to observe (default: event-log).")]
[LlmOption("--from", "int", "First sequence number to examine; starting later gives partial coverage.")]
[LlmOption("--to", "int", "Last sequence number to examine; defaults to the captured tail.")]
[LlmOption("--max-events", "int", "Most events to examine (default 100000); reaching it records partial coverage.")]
[LlmOption("--fail-on-drift", "bool", "Exit 7 on known drift and 8 when the comparison is inconclusive.")]
[LlmOutputAdvice("json-compact", "Screenplay's observation report JSON: hasDrift (omitted when unknown), complete, coverage, findings with statements, slices and notEstablished limits.")]
public class ConformNamespaceScreenplayCommand : ChronicleCommand<ConformNamespaceScreenplaySettings>
{
    readonly Func<IServices, INamespaceObservations> _observations;
    readonly TimeProvider _clock;
    SemanticCompilation? _compilation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConformNamespaceScreenplayCommand"/> class.
    /// </summary>
    public ConformNamespaceScreenplayCommand()
        : this(services => new NamespaceObservations(services), TimeProvider.System)
    {
    }

    internal ConformNamespaceScreenplayCommand(Func<IServices, INamespaceObservations> observations, TimeProvider clock)
    {
        _observations = observations;
        _clock = clock;
    }

    /// <inheritdoc/>
    protected override bool UseStatusSpinner => false;

    /// <summary>
    /// Gets the exit code for a completed comparison.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="failOnDrift">Whether drift fails the command.</param>
    /// <returns>The exit code.</returns>
    internal static int ExitCode(ObservationReport report, bool failOnDrift) => (failOnDrift, report.HasDrift) switch
    {
        (false, _) or (true, false) => ExitCodes.Success,
        (true, true) => ExitCodes.DriftDetected,
        (true, null) => ExitCodes.Inconclusive
    };

    /// <summary>
    /// Validates the settings and compiles the model before any connection is made.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>An exit code when the command cannot run; otherwise <see langword="null"/>.</returns>
    internal int? Prepare(ConformNamespaceScreenplaySettings settings)
    {
        var format = settings.ResolveOutputFormat();
        if (InvalidSettings(settings) is { } invalid)
        {
            return Refuse(format, invalid.Message, invalid.Suggestion);
        }

        var modelRoot = PlayFileTargetResolver.Resolve(settings.ModelRoot, Directory.GetCurrentDirectory());
        if (!modelRoot.IsResolved)
        {
            return Refuse(format, modelRoot.Error!, "Point the command at an authored .play file or model folder");
        }

        var model = new ScreenplayConformance().Read(modelRoot.Path!);
        ScreenplayDiagnosticsWriter.Write(format, model.Diagnostics);
        var compilation = model.Sources.Count == 0 ? null : ScreenplayConformance.ComparisonInput(model.ApplicationName, model.Sources).Workspace.Compilation;
        if (compilation?.Success != true || model.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return Refuse(format, "The authored model is empty or has errors", "Run 'cratis screenplay validate' and fix the reported errors");
        }

        _compilation = compilation!.Value!;
        return null;
    }

    /// <summary>
    /// Observes the namespace, compares it with the prepared model and writes the report.
    /// </summary>
    /// <param name="observations">The Chronicle reads.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    internal async Task<int> Execute(INamespaceObservations observations, ConformNamespaceScreenplaySettings settings, string format, CancellationToken cancellationToken)
    {
        var target = new NamespaceTarget(settings.ResolveEventStore(), settings.ResolveNamespace(), settings.EventSequenceId);
        try
        {
            if (await NamespaceTargetCheck.Missing(observations, target, cancellationToken) is { } missing)
            {
                return Refuse(format, missing, "Check the --event-store, --namespace and --sequence values");
            }

            var scan = new NamespaceObservationScan(settings.From, settings.To, settings.MaxEvents);
            var snapshot = await NamespaceObservationSnapshot.Build(observations, target, scan, _clock.GetUtcNow(), cancellationToken);
            var map = ReactionIdentities.Map(ObservationIdentityMap.FromModel(_compilation!.Model), _compilation.Model, snapshot.Consumers);
            var report = ObservationComparison.Compare(_compilation, snapshot, map);
            ObservationReportOutput.Write(format, report, Console.Out);
            return ExitCode(report, settings.FailOnDrift);
        }
        catch (ChronicleReadFailed exception)
        {
            var exitCode = exception.Unauthorized ? ExitCodes.AuthenticationError : ExitCodes.ServerError;
            OutputFormatter.WriteError(format, exception.Message, "No snapshot was built; nothing was compared", ExitCodes.CodeFor(exitCode));
            return exitCode;
        }
    }

    /// <inheritdoc/>
    protected override int? ExecuteOffline(ConformNamespaceScreenplaySettings settings) => Prepare(settings);

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ConformNamespaceScreenplaySettings settings, string format)
    {
        using var interrupt = ShutdownSignal.LinkedTo(CancellationToken.None);
        return await Execute(_observations(services), settings, format, interrupt.Token);
    }

    static (string Message, string Suggestion)? InvalidSettings(ConformNamespaceScreenplaySettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ModelRoot))
        {
            return ("An authored MODEL_ROOT is required", "Pass a .play file or model folder");
        }

        if (settings.ResolveEventStoreWithSource().Source == SettingSource.Default)
        {
            return ("No event store is selected", "Pass --event-store or set one in the context; the built-in default is never assumed");
        }

        if (settings.ResolveNamespaceWithSource().Source == SettingSource.Default)
        {
            return ("No namespace is selected", "Pass --namespace or set one in the context; the built-in default is never assumed");
        }

        if (string.IsNullOrWhiteSpace(settings.EventSequenceId))
        {
            return ("No event sequence is selected", "Pass --sequence");
        }

        if (settings.MaxEvents < 1)
        {
            return ("--max-events must be at least 1", "Pass a positive number of events");
        }

        return settings.From > settings.To
            ? ("--from must not be after --to", "Pass a range whose start is not after its end")
            : null;
    }

    static int Refuse(string format, string message, string suggestion)
    {
        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
