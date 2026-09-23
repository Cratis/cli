// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayMcpRoot
{
    internal static string Resolve(string? path, string? projectRoot, string? projectRootEnvironment, string workingDirectory, Func<string, string?> environment)
    {
        if (projectRootEnvironment is not null)
        {
            projectRoot = environment(projectRootEnvironment);
            if (string.IsNullOrWhiteSpace(projectRoot) || !Path.IsPathFullyQualified(projectRoot))
            {
                throw new AiMcpConfigurationInvalid($"Host did not supply an absolute {projectRootEnvironment}. Upgrade the host or pass --project-root explicitly.");
            }
        }
        if (path is not null) return AiProjectPaths.PhysicalRoot(Path.GetFullPath(path, workingDirectory));
        var project = AiProjectPaths.PhysicalRoot(Path.GetFullPath(projectRoot ?? workingDirectory, workingDirectory));
        var configurationPath = AiProjectPaths.Within(project, ".cratis/ai.json");
        if (!File.Exists(configurationPath))
        {
            if (projectRoot is not null) throw new AiMcpConfigurationInvalid($"No .cratis/ai.json in selected project '{project}'. Run 'cratis ai install' there; the server will not guess another project.");
            return project;
        }
        var configuration = AiCorpusSynchronizer.ReadConfiguration(project);
        var settings = configuration.McpServers?.GetValueOrDefault("screenplay");
        if (settings?.Enabled == false) throw new AiMcpConfigurationInvalid("Screenplay MCP is disabled in .cratis/ai.json.");
        var defaultRoot = AiMcpDescriptor.Read(project).FirstOrDefault(server => server.Id == "screenplay")?.DefaultRoot ?? AiMcpDescriptor.ScreenplayRoot;
        var relative = settings?.Root ?? defaultRoot;
        var root = AiProjectPaths.Within(project, relative);
        if (!Directory.Exists(root)) throw new AiMcpConfigurationInvalid($"Screenplay model directory '{relative}' does not exist. Run 'cratis ai update' or create it before starting MCP.");
        return root;
    }
}
