// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// Instantiates templates. <c language="csharp">cratis new</c> lists the available templates; <c language="csharp">cratis new &lt;template&gt;</c>
/// instantiates one with dotnet-new compatible <c language="csharp">-n</c>/<c language="csharp">-o</c> semantics, dynamic parameters,
/// dry runs and the explicit script policy. Rendering requires no .NET installation.
/// </summary>
[LlmDescription("Create new projects from templates. Lists available templates when run without arguments, or instantiates one with dynamic parameters. No .NET SDK required — acquisition, rendering and post actions run natively.")]
[CliCommand("new", "Create new projects from templates (dotnet-new compatible; lists templates when run without arguments)")]
[CliExample("new --language csharp")]
[CliExample("new --language csharp")]
[CliExample("new cratis --language csharp -n MyApp -o MyApp")]
[CliExample("new cratis --language csharp -n MyApp --Framework net8.0 --dry-run")]
[CliExample("new cratis-aspire --language csharp -n MyApp --allow-scripts yes")]
[CliExample("new cratis --language csharp -n MyApp --database postgresql")]
[LlmOutputAdvice("json", "JSON contains template, name, output, files, primaryOutputs and postActions with per-action outcomes — useful for scripted scaffolding.")]
public class NewCommand : AsyncCommand<NewSettings>
{
    /// <summary>
    /// Exit code: the command could not run — unknown template, acquisition failure, invalid manifest or arguments.
    /// </summary>
    public const int CouldNotRun = 2;

    /// <summary>
    /// Exit code: creation or a required post action failed.
    /// </summary>
    public const int CreationFailed = 1;

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, NewSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            return settings.Template is null && settings.Language is null
                ? ListTemplates(settings)
                : await Instantiate(context, settings, cancellationToken);
        }
        catch (TemplatePackageAcquisitionError error)
        {
            ReportError(settings, "could not acquire the template package", error.Message);
            return CouldNotRun;
        }
        catch (InvalidTemplateManifest error)
        {
            ReportError(settings, "invalid template manifest", error.Message);
            return CouldNotRun;
        }
        catch (UnsupportedTemplateConstruct error)
        {
            ReportError(settings, "unsupported template construct", error.Message);
            return CouldNotRun;
        }
        catch (SymbolResolutionError error)
        {
            ReportError(settings, "symbol resolution failed", error.Message);
            return CouldNotRun;
        }
        catch (ExpressionEvaluationError error)
        {
            ReportError(settings, "expression evaluation failed", error.Message);
            return CouldNotRun;
        }
    }

    static int ListTemplates(NewSettings settings)
    {
        var templates = TemplateCatalogue.List();
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new
            {
                package = TemplateCatalogue.DefaultPackageId,
                version = TemplateCatalogue.DefaultVersion,
                templates = templates.Select(template => new
                {
                    shortName = template.ShortName,
                    name = template.Name,
                    description = template.Description,
                    identity = template.Identity
                })
            });
            return ExitCodes.Success;
        }

        if (settings.Format == "plain")
        {
            foreach (var template in templates)
            {
                Console.WriteLine($"{template.ShortName}\t{template.Name}\t{template.Description}");
            }
            return ExitCodes.Success;
        }

        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(OutputFormatter.Muted))
            .AddColumn(new TableColumn($"[{accent}]Template[/]"))
            .AddColumn(new TableColumn($"[{accent}]Name[/]"))
            .AddColumn(new TableColumn($"[{accent}]Description[/]"));
        foreach (var template in templates)
        {
            table.AddRow($"[bold]{template.ShortName}[/]", template.Name.EscapeMarkup(), template.Description.EscapeMarkup());
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(table)
            .Header($"[{accent}] Available Templates [{muted}]({TemplateCatalogue.DefaultPackageId} {TemplateCatalogue.DefaultVersion})[/] [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
        AnsiConsole.MarkupLine($"  [{muted}]Instantiate with[/] [bold]cratis new <template> -n <Name>[/] [{muted}]— no .NET SDK required. List parameters with[/] [bold]cratis new <template> --parameters[/]");
        AnsiConsole.WriteLine();
        return ExitCodes.Success;
    }

    static async Task<IReadOnlyList<DiscoveredTemplate>> ResolveTemplates(TemplatingEngine engine, NewSettings settings, string languagePackageId)
    {
        if (settings.TemplatePath is not null)
        {
            return engine.DiscoverLocal(settings.TemplatePath);
        }

        var packageId = settings.Package ?? languagePackageId;
        var version = settings.Version
            ?? (packageId == TemplateCatalogue.DefaultPackageId ? TemplateCatalogue.DefaultVersion : "*");
        return await engine.Acquire(packageId, version, Environment.CurrentDirectory);
    }
    static DiscoveredTemplate? FindTemplate(IReadOnlyList<DiscoveredTemplate> templates, string name) =>
        templates.FirstOrDefault(template => template.Manifest.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? templates.FirstOrDefault(template => template.Manifest.Identity?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    static ScriptPolicy ParseScriptPolicy(NewSettings settings, bool interactive) => settings.AllowScripts switch
    {
        "yes" => ScriptPolicy.Allow,
        "no" => ScriptPolicy.Deny,
        "prompt" when !interactive => throw new TemplatePackageAcquisitionError(
            "--allow-scripts prompt requires an interactive terminal. Pass --allow-scripts yes or --allow-scripts no explicitly."),
        "prompt" => ScriptPolicy.Prompt,
        _ => ScriptPolicy.Deny
    };

    static void RenderParameters(NewSettings settings, DiscoveredTemplate template)
    {
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new
            {
                template = template.Manifest.ShortName,
                name = template.Manifest.Name,
                parameters = TemplateParameterHelp.DescribeAll(template.Manifest)
            });
            return;
        }
        TemplateParameterHelp.Render(template.Manifest, template.Manifest.Name, OutputFormatter.Accent.ToMarkup(), OutputFormatter.Muted.ToMarkup());
    }

    static void RenderCreation(
        NewSettings settings,
        DiscoveredTemplate template,
        InstantiationResult creation,
        IReadOnlyList<PostActionResult> actions)
    {
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new
            {
                template = template.Manifest.ShortName,
                name = creation.Name,
                output = creation.OutputRoot,
                dryRun = settings.DryRun,
                files = creation.CreatedFiles.Select(file => Path.GetRelativePath(creation.OutputRoot, file.Path).Replace('\\', '/')),
                primaryOutputs = creation.PrimaryOutputs.Select(path => Path.GetRelativePath(creation.OutputRoot, path).Replace('\\', '/')),
                postActions = actions.Select(action => new
                {
                    description = action.Action.Description ?? action.Action.ActionId,
                    outcome = action.Outcome.ToString().ToLowerInvariant(),
                    message = action.Message,
                    instructions = action.Instructions.Length > 0 ? action.Instructions : null,
                    continueOnError = action.Action.ContinueOnError
                })
            });
            return;
        }

        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();
        var success = OutputFormatter.Success.ToMarkup();
        var warning = OutputFormatter.Warning.ToMarkup();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(settings.DryRun
            ? $"  [{warning}]Dry run[/] [{muted}]— nothing was written. {creation.CreatedFiles.Count} file(s) would be created at[/] [bold]{creation.OutputRoot.EscapeMarkup()}[/]"
            : $"  [{success}]✓[/] Created [bold]{creation.Name.EscapeMarkup()}[/] [{muted}]at[/] [bold]{creation.OutputRoot.EscapeMarkup()}[/]");

        if (actions.Count > 0)
        {
            AnsiConsole.WriteLine();
            foreach (var action in actions)
            {
                var (symbol, color) = action.Outcome switch
                {
                    PostActionOutcome.Succeeded or PostActionOutcome.Displayed => ('✓', success),
                    PostActionOutcome.Skipped => ('·', muted),
                    PostActionOutcome.NotPerformed => ('·', warning),
                    _ => ('✗', warning)
                };
                AnsiConsole.MarkupLine(
                    $"  [{color}]{symbol}[/] {(action.Action.Description ?? action.Action.ActionId).EscapeMarkup()}");
                if (action.Message.Length > 0 && action.Outcome is not (PostActionOutcome.Succeeded or PostActionOutcome.Skipped))
                {
                    AnsiConsole.MarkupLine($"      [{muted}]{action.Message.EscapeMarkup()}[/]");
                }
                if (action.Instructions.Length > 0)
                {
                    AnsiConsole.MarkupLine($"      [{muted}]{action.Instructions.EscapeMarkup()}[/]");
                }
            }
        }
        AnsiConsole.WriteLine();
    }

    static void ReportError(NewSettings settings, string headline, string detail)
    {
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new { error = headline, detail });
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Danger.ToMarkup()}]✗ {headline.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  {detail.EscapeMarkup()}");
        AnsiConsole.WriteLine();
    }

    async Task<int> Instantiate(CommandContext context, NewSettings settings, CancellationToken cancellationToken)
    {
        // The language selects the template package and the template instantiated when none is
        // named; its template packages carry the per-language scaffolds.
        var language = LanguageSelection.Resolve(settings.Language!);
        if (language.Errors.Count > 0)
        {
            ReportError(settings, "invalid language selection", string.Join('\n', language.Errors));
            return CouldNotRun;
        }

        var engine = TemplateCatalogue.CreateEngine();
        var templates = await ResolveTemplates(engine, settings, language.PackageId!);
        var templateName = settings.Template ?? language.DefaultTemplate;
        var template = FindTemplate(templates, templateName);
        if (template is null)
        {
            ReportError(settings, "template not found", $"no template matching '{templateName}' was found. Available: {string.Join(", ", templates.Select(entry => entry.Manifest.ShortName))}.");
            return CouldNotRun;
        }

        if (settings.Parameters)
        {
            RenderParameters(settings, template);
            return ExitCodes.Success;
        }

        var rawArguments = context.Remaining.Raw;
        var binding = TemplateParameterBinder.Bind(template.Manifest, rawArguments);
        if (binding.Errors.Count > 0)
        {
            ReportError(settings, "invalid parameters", string.Join('\n', binding.Errors));
            return CouldNotRun;
        }

        // The --database selection becomes the template's Database parameter value, so templates
        // use it like any parameter — conditions, replacements and switch symbols.
        var selection = DatabaseSelection.Resolve(template.Manifest, settings.Database, binding.Values);
        if (selection.Errors.Count > 0)
        {
            ReportError(settings, "invalid database selection", string.Join('\n', selection.Errors));
            return CouldNotRun;
        }

        var bound = new Dictionary<string, string>(binding.Values, StringComparer.Ordinal);
        foreach (var (parameter, value) in selection.Merge)
        {
            bound[parameter] = value;
        }

        var interactive = !settings.NoPrompts
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected
            && !GlobalSettings.IsAiAgentEnvironment();
        var values = TemplateParameterPrompts.PromptForMissing(template.Manifest, bound, interactive);

        var scriptPolicy = ParseScriptPolicy(settings, interactive);
        var inputs = new InstantiationInputs(
            settings.Name,
            settings.Output,
            values,
            settings.Baseline,
            settings.DryRun,
            settings.Force);

        // Wire the engine's script-confirmation hook to the terminal — the engine never reads the console.
        PromptCallback.IsInteractive = interactive;
        PromptCallback.ConfirmAction = async (_, executable) => await AnsiConsole.ConfirmAsync(
            $"  Run post-action script '{executable}'?");

        var (creation, actions) = await engine.Instantiate(template, inputs, scriptPolicy, cancellationToken);
        RenderCreation(settings, template, creation, actions);

        // A declined script is a deliberate, reported outcome — not a failure of the run. Unknown
        // action ids and genuine failures of non-continueOnError actions fail the run.
        var requiredFailure = actions.FirstOrDefault(action =>
            action.Outcome is PostActionOutcome.Failed or PostActionOutcome.Unknown
            && !action.Action.ContinueOnError);
        if (requiredFailure is not null)
        {
            return CreationFailed;
        }

        return ExitCodes.Success;
    }
}
