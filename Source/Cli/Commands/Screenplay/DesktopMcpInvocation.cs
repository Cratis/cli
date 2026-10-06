// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Routes desktop management separately from the protocol stream and project-local AI installation.</summary>
internal static class DesktopMcpInvocation
{
    const string Usage = "Usage: cratis screenplay mcp <install|status|update|uninstall> [--clients claude,chatgpt] [--version VERSION] [--model-root DIRECTORY] [--dry-run]";

    internal static bool IsMatch(string[] args) => args.Length >= 3 &&
        string.Equals(args[0], "screenplay", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "mcp", StringComparison.OrdinalIgnoreCase) &&
        new[] { "install", "status", "update", "uninstall" }.Contains(args[2], StringComparer.Ordinal);

    internal static async Task<int> Run(string[] args)
    {
        try
        {
            if (args[3..] is ["--help"] or ["-h"])
            {
                Console.WriteLine(Usage);
                return ExitCodes.Success;
            }
            string? requested = null;
            string? version = null;
            string? root = null;
            var dryRun = false;
            for (var index = 3; index < args.Length; index++)
            {
                if (args[index] == "--dry-run")
                {
                    if (dryRun) throw new AiMcpConfigurationInvalid("Duplicate --dry-run option.");
                    dryRun = true;
                    continue;
                }
                var option = args[index];
                if (index + 1 >= args.Length || args[index + 1].StartsWith('-')) throw new AiMcpConfigurationInvalid(Usage);
                var value = args[++index];
                switch (option)
                {
                    case "--clients" when requested is null: requested = value; break;
                    case "--version" when version is null: version = value; break;
                    case "--model-root" when root is null: root = value; break;
                    default: throw new AiMcpConfigurationInvalid($"Unknown/duplicate option '{option}'. {Usage}");
                }
            }
            if (version is not null) DesktopMcpArtifacts.ValidateVersion(version);
            if (root is not null && (args[2] == "status" || args[2] == "uninstall")) throw new AiMcpConfigurationInvalid("--model-root applies only to install/update.");
            var platform = DesktopMcpPlatform.Current;
            IDesktopMcpClient[] providers = [new ClaudeDesktopMcp(platform, DesktopMcpApplications.Open), new ChatGptDesktopMcp(platform)];
            var selected = Select(providers, requested, args[2]);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Cratis-CLI-Desktop-MCP/1.0");
            var artifacts = new DesktopMcpArtifacts(http, platform);
            if (args[2] == "status")
            {
                if (version is null && selected.Any(provider => provider.IsSupported))
                {
                    try { version = await artifacts.Latest(); }
                    catch (Exception) { await Console.Error.WriteLineAsync("Unable to check Screenplay releases. Showing local state only; check connectivity or use --version."); }
                }
                return await DesktopMcpOperations.Run(selected, client => Task.FromResult(client.Inspect(version)), Console.Out);
            }
            if (args[2] == "uninstall") return await DesktopMcpOperations.Run(selected, client => client.Uninstall(dryRun), Console.Out);
            return await DesktopMcpOperations.Run(
                selected,
                async client =>
            {
                if (!client.IsSupported) throw new AiMcpConfigurationInvalid("This desktop host is unsupported on the current platform; use Docker/.NET/manual MCP configuration.");
                version ??= await artifacts.Latest();
                var path = dryRun ? artifacts.Name(version, client.ArtifactSuffix) : await artifacts.Acquire(version, client.ArtifactSuffix);
                return args[2] == "update"
                    ? await client.Update(path, version, root, dryRun)
                    : await client.Install(path, version, root, dryRun);
                },
                Console.Out);
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Desktop MCP: {exception.Message} See 'cratis screenplay mcp install --help'.");
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
