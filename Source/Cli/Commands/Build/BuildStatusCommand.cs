// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What <c language="csharp">cratis build status</c> reports.
/// </summary>
/// <param name="Studio">The Studio address.</param>
/// <param name="Organization">The organization.</param>
/// <param name="Builder">The builder's identifier, or <see langword="null"/> when not registered.</param>
/// <param name="Name">What maintainers see the builder as.</param>
/// <param name="TokenStored">Whether the builder token is in the credential store.</param>
/// <param name="StudioTools">Whether the organization's MCP credential is configured.</param>
/// <param name="Kit">The kit installed in the current project, or <see langword="null"/>.</param>
/// <param name="Agent">The agent recorded for the current project, or <see langword="null"/>.</param>
public sealed record BuildStatus(string Studio, string Organization, string? Builder, string? Name, bool TokenStored, bool StudioTools, string? Kit, string? Agent);

/// <summary>
/// Shows the Studio connection of this machine and the build setup of the current project. Changes nothing.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "status",
    "Show which Studio and organization this machine builds for, whether its builder token and Studio's MCP credential are stored, and the kit and agent of the current project. Changes nothing.",
    Branch = typeof(BuildBranch))]
[CliExample("build", "status")]
[CliExample("build", "status", "--output", "json")]
public sealed class BuildStatusCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var studio = CliConfiguration.Load().Studio;
        if (studio is null)
        {
            OutputFormatter.WriteError(format, "This machine is not connected to Cratis Studio.", "Run 'cratis build connect --studio <url> --organization <name>'.", ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var tokenStored = false;
        var studioTools = false;
        try
        {
            var credentials = StudioCredentials.For(studio);
            tokenStored = await credentials.BuilderToken(studio, cancellationToken) is not null;
            studioTools = studio.McpClientId is not null && await credentials.McpSecret(studio, cancellationToken) is not null;
        }
        catch (DirectAuthError)
        {
        }

        var project = BuildProjectSettings.Read(Directory.GetCurrentDirectory());
        var status = new BuildStatus(studio.Url, studio.Organization, studio.BuilderId, studio.BuilderName, tokenStored, studioTools, project?.Kit, project?.Agent);
        OutputFormatter.WriteObject(format, status, Render);
        return ExitCodes.Success;
    }

    static void Render(BuildStatus status)
    {
        OutputFormatter.WriteLabel("Studio", status.Studio);
        OutputFormatter.WriteLabel("Organization", status.Organization);
        OutputFormatter.WriteLabel("Builder", status.Builder is null ? "not registered" : $"{status.Name} ({status.Builder})");
        OutputFormatter.WriteLabel("Builder token", status.TokenStored ? "stored" : "missing; run 'cratis build connect --force'");
        OutputFormatter.WriteLabel("Studio tools", status.StudioTools ? "configured" : "not configured; add --mcp-client-id to 'cratis build connect'");
        OutputFormatter.WriteLabel("Kit", status.Kit ?? "none in this folder; run 'cratis build init --kit <kit>'");
        OutputFormatter.WriteLabel("Agent", status.Agent ?? "none recorded");
    }
}
