// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Projections;

/// <summary>
/// Shows the declaration of a specific projection.
/// </summary>
[CliCommand("show", "Show a projection declaration", Branch = typeof(ChronicleBranch.Projections))]
[CliExample("chronicle", "projections", "show", "MyProjection", "-o", "json")]
[LlmOutputAdvice("json", "JSON (612B) and plain (574B) are similar size. JSON is easier to parse for the declaration field.")]
[LlmOption("<IDENTIFIER>", "string", "Projection identifier (positional)")]
public class ShowProjectionCommand : ChronicleCommand<ShowProjectionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ShowProjectionSettings settings, string format)
    {
        var declarations = await services.Projections.GetAllDeclarations(new GetAllDeclarationsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var match = declarations.FirstOrDefault(d => string.Equals(d.Identifier, settings.Identifier, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            OutputFormatter.WriteError(format, $"Projection '{settings.Identifier}' not found", "Use 'cratis projections list' to see available projections", ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Identifier,
                match.ContainerName,
                match.Declaration
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]Projection:[/] {data.Identifier.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Container:[/]  {data.ContainerName.EscapeMarkup()}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine(data.Declaration);
            });

        return ExitCodes.Success;
    }
}
