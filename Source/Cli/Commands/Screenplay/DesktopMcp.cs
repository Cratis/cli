// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Runs desktop management independently of the protocol stream and project-local AI installation.
/// </summary>
internal static class DesktopMcp
{
    internal static async Task<int> Execute(CommandContext context, DesktopMcpSettings settings, string operation, string? modelRoot, bool dryRun, DesktopMcpRun run, TextWriter output, TextWriter error)
    {
        if (context.Remaining.Parsed.Count > 0 || context.Remaining.Raw.Count > 0)
        {
            var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
            await error.WriteLineAsync($"Unknown option '{option}'. Run 'cratis screenplay desktop {operation} --help' for usage.");
            return ExitCodes.ValidationError;
        }

        return await run(operation, settings.Clients, settings.Version, modelRoot, dryRun, output, error);
    }

    internal static async Task<int> Run(string operation, string? clients, string? version, string? modelRoot, bool dryRun, TextWriter output, TextWriter error)
    {
        try
        {
            var platform = DesktopMcpPlatform.Current;
            IDesktopMcpClient[] providers = [new ClaudeDesktopMcp(platform, DesktopMcpApplications.Open), new ChatGptDesktopMcp(platform)];
            var selected = Select(providers, clients, operation);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Cratis-CLI-Desktop-MCP/1.0");
            var artifacts = new DesktopMcpArtifacts(http, platform);
            if (operation == "status")
            {
                if (version is null && selected.Any(provider => provider.IsSupported))
                {
                    try { version = await artifacts.Latest(); }
                    catch (Exception) { await error.WriteLineAsync("Unable to check Screenplay releases. Showing local state only; check connectivity or use --version."); }
                }
                return await DesktopMcpOperations.Run(selected, client => Task.FromResult(client.Inspect(version)), output);
            }
            if (operation == "uninstall") return await DesktopMcpOperations.Run(selected, client => client.Uninstall(dryRun), output);
            return await DesktopMcpOperations.Run(
                selected,
                async client =>
            {
                if (!client.IsSupported) throw new AiMcpConfigurationInvalid("This desktop host is unsupported on the current platform; use Docker/.NET/manual MCP configuration.");
                version ??= await artifacts.Latest();
                var path = dryRun ? artifacts.Name(version, client.ArtifactSuffix) : await artifacts.Acquire(version, client.ArtifactSuffix);
                return operation == "update"
                    ? await client.Update(path, version, modelRoot, dryRun)
                    : await client.Install(path, version, modelRoot, dryRun);
                },
                output);
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Desktop MCP: {exception.Message} See 'cratis screenplay desktop install --help'.");
            return ExitCodes.ValidationError;
        }
    }

    static IDesktopMcpClient[] Select(IDesktopMcpClient[] providers, string? requested, string operation)
    {
        if (requested is not null)
        {
            var ids = requested.Split(',').Select(value => value.Trim().ToLowerInvariant()).ToArray();
            if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || ids.Any(id => providers.All(provider => provider.Id != id)))
                throw new AiMcpConfigurationInvalid("Select --clients claude,chatgpt (one or both, without duplicates).");
            return [.. ids.Select(id => providers.Single(provider => provider.Id == id))];
        }
        if (operation == "status") return providers;
        if (Console.IsInputRedirected || Console.IsOutputRedirected || GlobalSettings.IsAiAgentEnvironment())
            throw new AiMcpConfigurationInvalid("Noninteractive install/update/uninstall requires --clients claude,chatgpt.");
        var detected = providers.Where(provider => provider.IsDetected && provider.IsSupported).ToArray();
        if (detected.Length == 0) throw new AiMcpConfigurationInvalid("No supported desktop applications detected. Install a supported host, or select --clients explicitly to stage a package.");
        var names = AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title("Select desktop applications").Required().AddChoices(detected.Select(provider => provider.DisplayName)));
        return [.. detected.Where(provider => names.Contains(provider.DisplayName, StringComparer.Ordinal))];
    }
}
