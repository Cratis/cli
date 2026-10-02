// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Shows an application as an event model, in the explorer Arc ships, served on a local web server.
/// </summary>
[LlmDescription("Serves the event model viewer for a .NET project on a local web server and opens it in the browser. Run it where the .csproj is, or pass the project or its folder. Reads the Screenplay documents embedded in the project's built output assembly (Debug by default, and assemblies it references in the same folder); when there are none - or the project has not been built - generates them from the project's source in memory. Listens on the loopback interface only and keeps running until stopped.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("view", "View an application as an event model in the browser")]
[CliExample("view")]
[CliExample("view", "./Source/Library/Library.csproj")]
[CliExample("view", "--port", "5050", "--no-browser")]
[LlmOption("--configuration", "string", "Build configuration whose output assembly is read (default: Debug).")]
[LlmOption("--framework", "string", "Target framework to read for a multi-targeted project (default: the first listed).")]
[LlmOption("--port", "int", "Local port to serve the viewer on (default: 0, a free port).")]
[LlmOption("--from-source", "bool", "Generate the documents from source even when the output assembly embeds some.")]
[LlmOption("--no-browser", "bool", "Do not open a browser window.")]
public class ViewCommand : AsyncCommand<ViewSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, ViewSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var target = ViewTarget.Resolve(settings.Path ?? Directory.GetCurrentDirectory());
        if (target.ProjectFile is not { } projectFile)
        {
            OutputFormatter.WriteError(format, target.Error!, "Run 'cratis view' in the folder of a .csproj, or pass the project file", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        using var interrupt = ShutdownSignal.LinkedTo(cancellationToken);

        ProjectOutput output;
        try
        {
            output = await ProjectOutputResolver.Resolve(projectFile, settings.Configuration, settings.Framework, interrupt.Token);
        }
        catch (ProjectCouldNotBeEvaluated ex)
        {
            OutputFormatter.WriteError(format, ex.Message, "Ensure the .NET SDK is installed and the project evaluates with 'dotnet msbuild'", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        ViewOutput.WriteHeader(format, projectFile, output);

        using var documents = await Find(format, projectFile, output, settings, interrupt.Token);
        ViewOutput.WriteDiagnostics(format, documents.Diagnostics);
        if (documents.Resources.Count == 0)
        {
            OutputFormatter.WriteError(format, "No Screenplay documents could be produced for the project", "Check that the project builds and contains Arc commands, events or read models", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        EventModelCatalog catalog;
        try
        {
            catalog = EventModelCatalog.For(documents.Resources);
        }
        catch (MalformedEventModelCatalog ex)
        {
            OutputFormatter.WriteError(format, ex.Message, "Rebuild the project, or pass --from-source to generate the documents instead", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        await using var server = await EventModelViewerServer.Start(new EventModelExplorer(catalog), settings.Port, interrupt.Token);
        ViewOutput.WriteServing(format, server.Address, documents, catalog);

        if (!settings.NoBrowser && !Browser.Open(server.Address))
        {
            ViewOutput.WriteBrowserNotOpened(format, server.Address);
        }

        await server.WaitForShutdown(interrupt.Token);
        return ExitCodes.Success;
    }

    static Task<ViewDocuments> Find(string format, string projectFile, ProjectOutput output, ViewSettings settings, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            return ViewDocuments.For(projectFile, output, settings.FromSource, cancellationToken);
        }

        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(OutputFormatter.Accent))
            .StartAsync("Reading the event model...", _ => ViewDocuments.For(projectFile, output, settings.FromSource, cancellationToken));
    }
}
