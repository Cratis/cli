// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Plans, creates, removes, and checks the harness links Cratis owns in a project.</summary>
static class AiHarnessIntegrations
{
    static readonly (string Harness, Action<Planner> Plan)[] _harnessPlans =
    [
        ("claude", PlanClaude),
        ("codex", PlanCodex),
        ("copilot", PlanCopilot),
        ("pi", PlanPi),
        ("cursor", PlanCursor),
        ("opencode", PlanOpenCode)
    ];

    internal static Dictionary<string, AiManagedIntegration> Plan(IEnumerable<string> harnesses, IEnumerable<string> files, bool hasProjectInstructions)
    {
        var selected = harnesses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var planner = new Planner(files, hasProjectInstructions);
        foreach (var (harness, plan) in _harnessPlans)
        {
            if (selected.Contains(harness)) plan(planner);
        }
        return planner.Plans;
    }

    internal static void Create(string projectPath, AiManagedIntegration integration, AiFileOperations operations)
    {
        var path = Path.Combine(projectPath, integration.Path);
        operations.CreateDirectoryFor(path);
        operations.CreateSymbolicLink(path, integration.Target, integration.IsDirectory);
    }

    internal static void Delete(string projectPath, AiManagedIntegration integration, AiFileOperations operations)
    {
        var path = Path.Combine(projectPath, integration.Path);
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) operations.DeleteDirectory(path);
        else operations.DeleteFile(path);
    }

    internal static bool Matches(string projectPath, AiManagedIntegration integration)
    {
        var path = Path.Combine(projectPath, integration.Path);
        var target = integration.IsDirectory ? new DirectoryInfo(path).LinkTarget : new FileInfo(path).LinkTarget;
        return string.Equals(target, integration.Target, StringComparison.Ordinal);
    }

    static void PlanClaude(Planner planner)
    {
        planner.Add("CLAUDE.md", planner.RootInstructionsTarget, false, preserveExisting: true);
        planner.Add(".claude/CLAUDE.md", planner.NestedInstructionsTarget, false, preserveExisting: true);
        planner.Add(".claude/agents", "../.cratis/ai/agents", true);
        planner.Add(".claude/hooks", "../.cratis/ai/hooks", true);
        planner.Add(".claude/prompts", "../.cratis/ai/prompts", true);
        planner.Add(".claude/rules", "../.cratis/ai/rules", true);
        planner.Add(".claude/settings.json", "../.cratis/ai/hooks/settings.template.json", false);
        planner.Add(".claude/skills", "../.cratis/ai/skills", true);
        planner.AddCommands(".claude/commands");
    }

    static void PlanCodex(Planner planner)
    {
        planner.AddRootInstructions();
        planner.Add(".agents/skills", "../.cratis/ai/skills", true);
    }

    static void PlanCopilot(Planner planner)
    {
        planner.Add(".github/copilot-instructions.md", planner.NestedInstructionsTarget, false, preserveExisting: true);
        var hasCopilotInstructions = planner.Files.Any(file => file.StartsWith("harnesses/copilot/instructions/", StringComparison.Ordinal));
        planner.Add(".github/instructions", hasCopilotInstructions ? "../.cratis/ai/harnesses/copilot/instructions" : "../.cratis/ai/rules", true);
        planner.Add(".github/prompts", "../.cratis/ai/prompts", true);
        planner.Add(".github/skills", "../.cratis/ai/skills", true);
        planner.AddAgents(".github/agents", ".agent.md");
    }

    static void PlanPi(Planner planner)
    {
        planner.AddRootInstructions();
        planner.Add(".pi/agents", "../.cratis/ai/agents", true);
        planner.Add(".pi/extensions", "../.cratis/ai/harnesses/pi/extensions", true);
        planner.Add(".pi/skills", "../.cratis/ai/skills", true);
        planner.AddCommands(".pi/prompts");
    }

    static void PlanCursor(Planner planner)
    {
        planner.Add(".cursor/agents", "../.cratis/ai/agents", true);
        planner.Add(".cursor/rules", "../.cratis/ai/harnesses/cursor/rules", true);
        planner.Add(".cursor/skills", "../.cratis/ai/skills", true);
        planner.AddCommands(".cursor/commands");
    }

    static void PlanOpenCode(Planner planner)
    {
        planner.AddRootInstructions();

        // OpenCode cannot consume the canonical agent shape: it has no tools allowlist and restricts through a
        // permission map, so the corpus ships generated adapters under harnesses/opencode/agents. Link those when
        // the corpus carries them; an older corpus without them falls back to the canonical directory.
        var hasOpenCodeAgents = planner.Files.Any(file => file.StartsWith("harnesses/opencode/agents/", StringComparison.Ordinal));
        planner.Add(".opencode/agents", hasOpenCodeAgents ? "../.cratis/ai/harnesses/opencode/agents" : "../.cratis/ai/agents", true);
        planner.Add(".opencode/skills", "../.cratis/ai/skills", true);
        planner.AddCommands(".opencode/commands");
    }

    sealed class Planner(IEnumerable<string> files, bool hasProjectInstructions)
    {
        internal Dictionary<string, AiManagedIntegration> Plans { get; } = new(StringComparer.Ordinal);

        internal IEnumerable<string> Files => files;

        internal string RootInstructionsTarget { get; } = hasProjectInstructions ? AiProjectInstructions.RelativePath : ".cratis/ai/rules/general.md";

        internal string NestedInstructionsTarget { get; } = hasProjectInstructions ? "../.cratis/ai/rules/project.md" : "../.cratis/ai/rules/general.md";

        internal void Add(string path, string target, bool isDirectory, bool preserveExisting = false) => Plans.TryAdd(path, new(path, target, isDirectory, preserveExisting));

        internal void AddRootInstructions() => Add("AGENTS.md", RootInstructionsTarget, false, preserveExisting: true);

        internal void AddCommands(string directory)
        {
            foreach (var prompt in files.Where(file => file.StartsWith("prompts/", StringComparison.Ordinal) && file.EndsWith(".prompt.md", StringComparison.Ordinal)))
            {
                var name = Path.GetFileName(prompt)[..^".prompt.md".Length];
                Add($"{directory}/{name}.md", $"../../.cratis/ai/{prompt}", false);
            }
        }

        internal void AddAgents(string directory, string suffix)
        {
            foreach (var agent in files.Where(file => file.StartsWith("agents/", StringComparison.Ordinal) && file.EndsWith(".md", StringComparison.Ordinal)))
            {
                var name = Path.GetFileNameWithoutExtension(agent);
                Add($"{directory}/{name}{suffix}", $"../../.cratis/ai/{agent}", false);
            }
        }
    }
}
