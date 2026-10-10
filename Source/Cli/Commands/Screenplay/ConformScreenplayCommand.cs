// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Checks hand-written code against an authored model without writing a generated document.
/// </summary>
[LlmDescription("Checks hand-written application source against an authored .play model. Uses the same source-adapter pipeline as generate, in memory; no generated model is written and no application is started. Screenplay compares exact structural addresses, not behavioral equivalence. Code-only declarations and shape mismatches block; model-only declarations and members absent from extraction are informational. Exit codes are 0 no blocking findings, 1 defects found, 2 could not run or incomplete comparison. These differ from the general CLI exit codes. Generation options must reproduce the authored model's layout. MSBuild evaluates trusted source projects and may create intermediate build files.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("conform", "Check application source against an authored Screenplay model", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "conform", "./plays")]
[CliExample("screenplay", "conform", "./MyApp.play", "--project", "./MyApp.slnx", "-o", "json")]
[LlmOption("<MODEL_ROOT>", "string", "Authored .play root with imports, or a folder containing one application.")]
[LlmOption("--project", "string", "Solution, project or folder to extract. Defaults to current-directory discovery as generate does.")]
[LlmOption("--provider", "string", "Source provider: auto, arc, marten, or critter-stack.")]
[LlmOption("--framework", "string", "Explicit target framework for multi-targeted application projects.")]
[LlmOption("--domain", "string", "Generated domain; defaults to the authored domain name, or Application for an unnamed model.")]
[LlmOption("--feature-root", "string", "Project-relative feature placement root for Marten/Critter Stack.")]
[LlmOption("--module", "string", "Place all recovered features in this module.")]
[LlmOption("--skip-segments", "int", "Leading namespace segments to skip for placement.")]
[LlmOption("--modules-from-namespace-roots", "bool", "Recover module names from namespace roots (Arc).")]
[LlmOption("--authoring-only-constructs", "bool", "Include Arc authoring-only constructs; comparison then reports declaration-level coverage.")]
[LlmOutputAdvice("json", "Structured findings, coverage gaps and provenance go to stdout; generation diagnostics go to stderr. A clean result is structural only, never an execution verdict.")]
public class ConformScreenplayCommand : AsyncCommand<ConformScreenplaySettings>
{
    readonly IScreenplayGeneration _generation;
    readonly IScreenplayConformance _conformance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConformScreenplayCommand"/> class.
    /// </summary>
    public ConformScreenplayCommand()
        : this(ScreenplayGenerations.Create(), new ScreenplayConformance())
    {
    }

    internal ConformScreenplayCommand(IScreenplayGeneration generation, IScreenplayConformance conformance)
    {
        _generation = generation;
        _conformance = conformance;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ConformScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            if (string.IsNullOrWhiteSpace(settings.ModelRoot))
            {
                return CouldNotRun(format, "An authored MODEL_ROOT is required");
            }
            var currentDirectory = Directory.GetCurrentDirectory();
            var modelRoot = PlayFileTargetResolver.Resolve(settings.ModelRoot, currentDirectory);
            if (!modelRoot.IsResolved)
            {
                return CouldNotRun(format, modelRoot.Error!);
            }
            var project = ScreenplayTargetResolver.Resolve(settings.Project, currentDirectory);
            if (!project.IsResolved)
            {
                return CouldNotRun(format, project.Error!);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var model = _conformance.Read(modelRoot.Path!);
            ScreenplayDiagnosticsWriter.Write(format, model.Diagnostics);
            if (model.Sources.Count == 0 || model.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
            {
                return CouldNotRun(format, "The authored model is empty or has source errors");
            }
            var options = settings.ToGenerationOptions();
            options = options with { Domain = options.Domain ?? model.ApplicationName };
            var generated = await _generation.Generate(project.Path!, options, _ => { }, cancellationToken);
            ScreenplayDiagnosticsWriter.Write(format, generated.Diagnostics, generated.Provenance);
            if (string.IsNullOrWhiteSpace(generated.Source) || generated.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
            {
                return CouldNotRun(format, "Source generation failed; conformance could not be checked");
            }
            cancellationToken.ThrowIfCancellationRequested();
            var difference = _conformance.Compare(model, generated.Source);
            cancellationToken.ThrowIfCancellationRequested();
            var findings = ScreenplayConformance.Classify(difference);
            var exitCode = ScreenplayConformance.ExitCode(difference, findings);
            WriteResult(format, modelRoot.Path!, project.Path!, generated, difference, findings, exitCode);
            return exitCode;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CouldNotRun(format, $"Conformance could not run: {exception.Message}");
        }
    }

    static int CouldNotRun(string format, string message)
    {
        OutputFormatter.WriteError(format, message, "Resolve the model/project or reported diagnostics and run conform again", "CLI-CONFORM-001");
        return 2;
    }

    static void WriteResult(string format, string modelRoot, string project, GeneratedScreenplay generated, ModelDifference difference, IReadOnlyList<ConformanceFinding> findings, int exitCode)
    {
        var verdict = exitCode switch { 1 => "defects-found", 2 => "incomplete", _ => "clean" };
        var gaps = difference.Sections.SelectMany(section => section.Gaps.Select(gap => new { Section = section.Section.ToString(), Kind = gap.Kind.ToString(), gap.Statement })).ToArray();
        OutputFormatter.WriteObject(
            format,
            new
            {
                Verdict = verdict,
                ModelRoot = modelRoot,
                Project = project,
                generated.Projects,
                Matching = difference.Matching.ToString(),
                ComparedCounts = new { Model = difference.BeforeDeclarations, Code = difference.AfterDeclarations },
                BlockingCount = findings.Count(finding => finding.Blocking),
                InformationalCount = findings.Count(finding => !finding.Blocking),
                MissingFromModel = findings.Where(finding => finding.Category == "MissingFromModel").ToArray(),
                NotRealizedInCode = findings.Where(finding => finding.Category == "NotRealizedInCode").ToArray(),
                ShapeMismatches = findings.Where(finding => finding.Category == "ShapeMismatch").ToArray(),
                Informational = findings.Where(finding => finding.Category == "Informational").ToArray(),
                difference.Dependants,
                difference.NotCompared,
                Gaps = gaps,
                GenerationDiagnostics = generated.Diagnostics.Count,
                generated.Provenance
            },
            _ =>
            {
                AnsiConsole.MarkupLine($"[bold]Conformance: {verdict}[/] ({findings.Count(finding => finding.Blocking)} blocking, {findings.Count(finding => !finding.Blocking)} informational)");
                AnsiConsole.MarkupLine($"Model: {modelRoot.EscapeMarkup()}\nCode: {project.EscapeMarkup()}\nMatching: {difference.Matching}; declarations: model {difference.BeforeDeclarations}, code {difference.AfterDeclarations}");
                if (findings.Count > 0)
                {
                    OutputFormatter.Write(format, findings, ["Category", "Kind", "Address", "Change", "Member", "Model type", "Code type", "Blocking", "Same-name counterpart"], finding =>
                        [finding.Category, finding.Kind, finding.Address, finding.Change, finding.Member ?? string.Empty, finding.BeforeType ?? string.Empty, finding.AfterType ?? string.Empty, finding.Blocking ? "yes" : "no", finding.SameNameCounterpart ?? string.Empty]);
                }
                foreach (var gap in gaps)
                {
                    AnsiConsole.MarkupLine($"Coverage {gap.Section.EscapeMarkup()}/{gap.Kind.EscapeMarkup()}: {gap.Statement.EscapeMarkup()}");
                }
                foreach (var statement in difference.NotCompared)
                {
                    AnsiConsole.MarkupLine($"Not compared: {statement.EscapeMarkup()}");
                }
            });
        if (format == OutputFormats.Quiet)
        {
            Console.WriteLine(verdict);
        }
    }
}
