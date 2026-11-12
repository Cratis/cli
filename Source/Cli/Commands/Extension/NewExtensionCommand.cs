// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Cli.Templates;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings for creating an extension package.
/// </summary>
public class NewExtensionSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    [CommandOption("-n|--name <NAME>")]
    [Description("The package name, a PascalCase identifier such as Stripe. Asked for by the wizard when omitted.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of package.
    /// </summary>
    [CommandOption("--kind <KIND>")]
    [Description("extension (a full extension, an OCI artifact; the default) or package (a frontend-only component package for npm).")]
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the backend language.
    /// </summary>
    [CommandOption("--backend <LANGUAGE>")]
    [Description("The backend of a full extension: csharp, kotlin, go, typescript or none (the default).")]
    public string? Backend { get; set; }

    /// <summary>
    /// Gets or sets the frontend kind.
    /// </summary>
    [CommandOption("--frontend <KIND>")]
    [Description("The frontend of a full extension: react, mount, screenplay (Screenplay-authored) or none (the default).")]
    public string? Frontend { get; set; }

    /// <summary>
    /// Gets or sets the component kind.
    /// </summary>
    [CommandOption("--component <KIND>")]
    [Description("The component kind of a frontend-only package: react or mount.")]
    public string? Component { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [CommandOption("--display-name <TEXT>")]
    [Description("The name a person sees in the manifest. Defaults to the package name spelled out.")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [CommandOption("--description <TEXT>")]
    [Description("What the package does, for the manifest.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the publisher.
    /// </summary>
    [CommandOption("--publisher <NAME>")]
    [Description("The publisher: the OCI namespace or the npm scope the package is published under. Defaults to example.")]
    public string? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the output folder.
    /// </summary>
    [CommandOption("-o|--output <PATH>")]
    [Description("The folder to create the package in. Defaults to a folder named after the package.")]
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets the template path.
    /// </summary>
    [CommandOption("--template-path <PATH>")]
    [Description("Use the templates in this local folder instead of the Cratis.Templates.Extensions package.")]
    public string? TemplatePath { get; set; }

    /// <summary>
    /// Gets or sets the template package.
    /// </summary>
    [CommandOption("--package <ID>")]
    [Description("The template package to use instead of Cratis.Templates.Extensions.")]
    public string? Package { get; set; }

    /// <summary>
    /// Gets or sets the template package version.
    /// </summary>
    [CommandOption("--version <VERSION>")]
    [Description("The template package version. Defaults to the latest release.")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only report what would be created.
    /// </summary>
    [CommandOption("--dry-run")]
    [Description("Report the files that would be created without writing anything.")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to write into a folder that is not empty.
    /// </summary>
    [CommandOption("--force")]
    [Description("Write into the output folder even when it is not empty.")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip the wizard.
    /// </summary>
    [CommandOption("--no-prompts")]
    [Description("Never ask questions: use the flags and the documented defaults.")]
    public bool NoPrompts { get; set; }

    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    [CommandOption("--format <FORMAT>")]
    [Description("Output format: table (default) or json.")]
    public string Format { get; set; } = "table";
}

/// <summary>
/// Creates an extension package: a full extension or a frontend-only component package that already builds and verifies.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("new", "Create an extension package: a full extension (OCI) or a frontend-only component package (npm)", Branch = typeof(ExtensionBranch))]
[CliExample("extension", "new")]
[CliExample("extension", "new", "-n", "Stripe", "--backend", "csharp", "--frontend", "react")]
[CliExample("extension", "new", "-n", "Stripe", "--backend", "go", "--no-prompts")]
[CliExample("extension", "new", "-n", "PaymentForm", "--kind", "package", "--component", "mount")]
[LlmDescription("Creates an extension package from the Cratis.Templates.Extensions templates: a contract.play with its manifest (display name, description, icon, marketplace images, requirements), a placeholder icon and image, a static HTML placeholder per frontend component, a sample ability or component with specs, a README and the build and publish scripts. --kind extension creates a full extension (OCI artifact) with an optional --backend (csharp, kotlin, go, typescript) and an optional --frontend (react, mount, screenplay); --kind package creates a frontend-only npm package and needs --component (react or mount). Without flags and in a terminal an interactive wizard asks only what the flags did not answer; --no-prompts never asks. Exit codes: 0 created, 1 creation failed, 2 invalid request or template could not be found.")]
[LlmOption("--name", "string", "The package name, a PascalCase identifier.")]
[LlmOption("--kind", "string", "extension (default) or package.")]
[LlmOption("--backend", "string", "csharp, kotlin, go, typescript or none (default). Full extensions only.")]
[LlmOption("--frontend", "string", "react, mount, screenplay or none (default). Full extensions only.")]
[LlmOption("--component", "string", "react or mount. Required for --kind package.")]
[LlmOutputAdvice("json", "JSON holds the kind, name, output folder and every created file; on failure error and detail.")]
public class NewExtensionCommand : AsyncCommand<NewExtensionSettings>
{
    /// <summary>
    /// Exit code: creation failed.
    /// </summary>
    public const int CreationFailed = ExitCodes.NotFound;

    /// <summary>
    /// Exit code: the request is invalid or the template could not be found or acquired.
    /// </summary>
    public const int CouldNotRun = ExitCodes.ConnectionError;

    /// <summary>
    /// Creates the package the settings describe.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="prompter">Asks the wizard's questions, or null to take the flags and defaults only.</param>
    /// <param name="scaffolder">Creates the package.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Run(NewExtensionSettings settings, IExtensionScaffoldPrompter? prompter, ExtensionScaffolder scaffolder, CancellationToken cancellationToken)
    {
        if (!TryParseKind(settings.Kind, out var kind))
        {
            return Report(settings, "invalid request", $"Unknown kind '{settings.Kind}'. Choose extension or package.");
        }

        var answers = new ExtensionScaffoldAnswers(settings.Name, kind, settings.Backend, settings.Frontend, settings.Component, settings.DisplayName, settings.Description, settings.Publisher);
        if (prompter is not null) answers = new ExtensionScaffoldWizard(prompter).Complete(answers);

        var (request, errors) = ExtensionScaffoldValidator.Validate(answers.ToRequest());
        if (errors.Count > 0) return Report(settings, "invalid request", string.Join('\n', errors));

        try
        {
            var target = new ExtensionScaffoldTarget(settings.Output, settings.TemplatePath, settings.Package, settings.Version, settings.DryRun, settings.Force);
            var result = await scaffolder.Create(request, target, cancellationToken);
            if (!result.Succeeded) return Report(settings, "template not usable", result.Error!);

            Render(settings, request, result);
            var failed = result.Actions.Any(action => action.Outcome is Cratis.Templating.PostActions.PostActionOutcome.Failed && !action.Action.ContinueOnError);
            return failed ? CreationFailed : ExitCodes.Success;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Report(settings, "creation failed", exception.Message, CreationFailed);
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, NewExtensionSettings settings, CancellationToken cancellationToken)
    {
        var interactive = !settings.NoPrompts && !Console.IsInputRedirected && !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment();
        return await Run(settings, interactive ? new SpectreExtensionScaffoldPrompter() : null, new ExtensionScaffolder(TemplateCatalogue.CreateEngine()), cancellationToken);
    }

    static bool TryParseKind(string? value, out ExtensionPackageKind? kind)
    {
        kind = null;
        if (value is null) return true;
        if (value.Equals("extension", StringComparison.OrdinalIgnoreCase)) kind = ExtensionPackageKind.Extension;
        else if (value.Equals("package", StringComparison.OrdinalIgnoreCase)) kind = ExtensionPackageKind.Package;
        return kind is not null;
    }

    static int Report(NewExtensionSettings settings, string headline, string detail, int exitCode = CouldNotRun)
    {
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new { error = headline, detail });
            return exitCode;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Danger.ToMarkup()}]✗ {headline.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"  {detail.EscapeMarkup()}");
        return exitCode;
    }

    static void Render(NewExtensionSettings settings, ExtensionScaffoldRequest request, ExtensionScaffoldResult result)
    {
        var creation = result.Creation!;
        var files = creation.CreatedFiles.Select(file => Path.GetRelativePath(creation.OutputRoot, file.Path).Replace('\\', '/')).ToArray();
        if (settings.Format == "json")
        {
            OutputFormatter.WriteObject(OutputFormats.Json, new { kind = request.Kind.ToString().ToLowerInvariant(), name = creation.Name, output = creation.OutputRoot, dryRun = settings.DryRun, files });
            return;
        }

        var verb = settings.DryRun ? "Would create" : "Created";
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Success.ToMarkup()}]✓[/] {verb} {request.Kind.ToString().ToLowerInvariant()} [bold]{creation.Name.EscapeMarkup()}[/] in {creation.OutputRoot.EscapeMarkup()} ({files.Length} files)");
        foreach (var file in files) AnsiConsole.MarkupLine($"    {file.EscapeMarkup()}");
        if (!settings.DryRun)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Next: cratis extension verify {creation.OutputRoot.EscapeMarkup()}[/]");
        }
    }
}
