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
[LlmDescription("Create new projects from templates. Interactive wizard when run without arguments (template, language, database — single-choice questions skipped); 'cratis new list' lists the concept templates with their languages and databases; explicit invocation takes the template, --language and dynamic parameters. No .NET SDK required.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("new", "Create new projects from templates (dotnet-new compatible; interactive wizard when run without arguments, 'cratis new list' lists them)")]
[CliExample("new list")]
[CliExample("new --language csharp")]
[CliExample("new list")]
[CliExample("new --language csharp")]
[CliExample("new cratis --language csharp -n MyApp -o MyApp")]
[CliExample("new cratis --language csharp -n MyApp --Framework net10.0 --dry-run")]
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
            if (string.Equals(settings.Template, "list", StringComparison.OrdinalIgnoreCase))
            {
                return await ListTemplates(settings);
            }

            return settings.Template is null && settings.Language is null
                ? await RunWizard(settings, cancellationToken)
                : await Instantiate(context.Remaining.Raw, settings, cancellationToken);
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

    static async Task<IReadOnlyList<DiscoveredTemplate>> ResolveTemplates(TemplatingEngine engine, NewSettings settings, string languagePackageId)
    {
        if (settings.TemplatePath is not null)
        {
            return engine.DiscoverLocal(settings.TemplatePath);
        }

        var packageId = settings.Package ?? languagePackageId;
        var version = settings.Version
            ?? (TemplateCatalogue.PinnedPackages.Contains(packageId, StringComparer.Ordinal)
                ? TemplateCatalogue.DefaultVersion
                : "*");
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
        IReadOnlyList<PostActionResult> actions,
        CreatedProjectAiUpdateResult aiUpdate)
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
                }),
                aiUpdate = new
                {
                    status = aiUpdate.Status,
                    detail = aiUpdate.Detail,
                    actions = aiUpdate.Actions
                }
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
        var aiColor = aiUpdate.Status switch
        {
            "updated" => success,
            "skipped" => muted,
            _ => warning
        };
        var aiSymbol = aiUpdate.Status == "updated" ? "✓" : "·";
        AnsiConsole.MarkupLine($"  [{aiColor}]{aiSymbol}[/] Cratis AI: {aiUpdate.Detail.EscapeMarkup()}");
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

    /// <summary>
    /// Acquires the catalogue's packages and builds the concept list from their metadata — the
    /// languages each concept supports and the databases each member offers.
    /// </summary>
    /// <param name="engine">The templating engine over the CLI's store.</param>
    /// <param name="settings">The command settings — package and version overrides apply.</param>
    /// <returns>The concept list the wizard and listing operate over.</returns>
    static async Task<IReadOnlyList<ConceptTemplate>> AcquireConcepts(TemplatingEngine engine, NewSettings settings)
    {
        var templates = new List<DiscoveredTemplate>();
        foreach (var packageId in settings.Package is not null ? [settings.Package] : TemplateCatalogue.PinnedPackages)
        {
            var isPinned = TemplateCatalogue.PinnedPackages.Contains(packageId, StringComparer.Ordinal);
            var version = settings.Version ?? (isPinned ? TemplateCatalogue.DefaultVersion : "*");
            templates.AddRange(await engine.Acquire(packageId, version, Environment.CurrentDirectory));
        }
        return ConceptTemplates.Build([.. templates]);
    }

    static async Task<int> ListTemplates(NewSettings settings)
    {
        var engine = TemplateCatalogue.CreateEngine();
        var concepts = await AcquireConcepts(engine, settings);

        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new
            {
                packages = TemplateCatalogue.PinnedPackages,
                version = TemplateCatalogue.DefaultVersion,
                templates = concepts.Select(concept => new
                {
                    shortName = concept.ShortName,
                    name = concept.Name,
                    description = concept.Description,
                    languages = concept.Languages,
                    databases = concept.DatabasesFor(concept.DefaultLanguage)
                })
            });
            return ExitCodes.Success;
        }

        if (settings.Format == "plain")
        {
            foreach (var concept in concepts)
            {
                Console.WriteLine($"{concept.ShortName}\t{concept.Name}\t{string.Join(',', concept.Languages)}\t{string.Join(',', concept.DatabasesFor(concept.DefaultLanguage))}");
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
            .AddColumn(new TableColumn($"[{accent}]Languages[/]"))
            .AddColumn(new TableColumn($"[{accent}]Databases[/]"));
        foreach (var concept in concepts)
        {
            table.AddRow(
                $"[bold]{concept.ShortName}[/]",
                concept.Name.EscapeMarkup(),
                string.Join(", ", concept.Languages),
                string.Join(", ", concept.DatabasesFor(concept.DefaultLanguage)));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(table)
            .Header($"[{accent}] Available Templates [{muted}]({string.Join(", ", TemplateCatalogue.PinnedPackages)} {TemplateCatalogue.DefaultVersion})[/] [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
        AnsiConsole.MarkupLine($"  [{muted}]Instantiate with[/] [bold]cratis new <template> --language csharp -n <Name>[/] — or just [bold]cratis new[/] for the wizard");
        AnsiConsole.WriteLine();
        return ExitCodes.Success;
    }

    static async Task<int> Instantiate(IReadOnlyList<string> rawTemplateArguments, NewSettings settings, CancellationToken cancellationToken)
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

        // Concept resolution: language derivatives share the concept's short name, so the member
        // is selected through the concept's language map rather than by name alone.
        var concept = ConceptTemplates.Find(ConceptTemplates.Build(templates), templateName);
        var memberLanguage = LanguageSelection.Normalize(settings.Language!) ?? concept?.DefaultLanguage ?? "csharp";
        var template = concept is not null
            ? concept.MemberFor(memberLanguage) ?? FindTemplate(templates, templateName)
            : FindTemplate(templates, templateName);
        if (template is null)
        {
            ReportError(settings, "template not found", $"no template matching '{templateName}' was found. Available: {string.Join(", ", ConceptTemplates.Build(templates).Select(entry => entry.ShortName))}.");
            return CouldNotRun;
        }

        if (settings.Parameters)
        {
            RenderParameters(settings, template);
            return ExitCodes.Success;
        }

        // Template parameters captured before CLI parsing (the framework drops unknown options)
        // plus anything passed after a '--' separator, which the framework does forward.
        var rawArguments = NewCommandArguments.Captured.Concat(rawTemplateArguments).ToArray();
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

        // The CLI runs the AI update itself right after creation, so templates that declare this
        // symbol must not also print their own "run cratis ai update" hint for the created project.
        if (!settings.DryRun && template.Manifest.Symbols.ContainsKey("SkipAiUpdateInstructions"))
        {
            bound["SkipAiUpdateInstructions"] = "true";
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

        // Finishing step: change into the created folder and synchronize its configured Cratis AI
        // content there — the same work 'cratis ai update' does, so the project needs no follow-up
        // command. A failure is reported and never discards the scaffold.
        var aiUpdate = settings.DryRun
            ? new CreatedProjectAiUpdateResult("skipped", [], "dry run — nothing was created.")
            : await Task.Run(() => Templates.CreatedProjectAiUpdate.Run(creation.OutputRoot));
        RenderCreation(settings, template, creation, actions, aiUpdate);

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
    async Task<int> RunWizard(NewSettings settings, CancellationToken cancellationToken)
    {
        var engine = TemplateCatalogue.CreateEngine();
        var concepts = await AcquireConcepts(engine, settings);
        var interactive = !settings.NoPrompts
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected
            && !GlobalSettings.IsAiAgentEnvironment();

        var choice = NewWizard.Ask(concepts, interactive);
        if (choice is null)
        {
            return CouldNotRun;
        }

        var (_, concept, language, database) = choice.Value;
        settings.Template = concept.ShortName;
        settings.Language = language;
        if (database is not null)
        {
            settings.Database = database;
        }

        return await Instantiate([], settings, cancellationToken);
    }
}
