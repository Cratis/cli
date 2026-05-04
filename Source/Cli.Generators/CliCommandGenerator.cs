// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Cli.Generators;

/// <summary>
/// Incremental source generator that discovers <c>[CliCommand]</c>-attributed classes
/// and generates Spectre.Console.Cli registration code and LLM context descriptors.
/// </summary>
[Generator]
public class CliCommandGenerator : IIncrementalGenerator
{
    // ── Internal data records ──────────────────────────────────────────────────

    sealed record BranchInfo(
        string FullName,
        string CliName,
        string Description,
        string? ParentFullName);

    sealed record ExampleData(
        ImmutableArray<string> Args,
        string? CommandName);

    sealed record LlmOptionData(
        string Name,
        string Type,
        string Description,
        string? CommandName);

    sealed record LlmAdviceData(
        string Format,
        string Reason,
        string? CommandName);

    sealed record CommandReg(
        string TypeFullName,
        string LeafName,
        string Description,
        string? LlmDescription,
        string? BranchFullName,
        bool IsHidden,
        bool ExcludeFromLlm,
        ImmutableArray<ExampleData> Examples,
        ImmutableArray<LlmOptionData> LlmOptions,
        ImmutableArray<LlmAdviceData> OutputAdvices,
        bool InheritsEventStoreSettings);

    // ── Initialize ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var branches = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Cratis.Cli.Registration.CliBranchAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetBranchInfo(ctx))
            .Collect();

        var commands = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Cratis.Cli.Registration.CliCommandAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetCommandRegs(ctx))
            .SelectMany(static (arr, _) => arr)
            .Collect();

        context.RegisterSourceOutput(
            branches.Combine(commands),
            static (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }

    // ── Extraction helpers ─────────────────────────────────────────────────────

    static BranchInfo GetBranchInfo(GeneratorAttributeSyntaxContext ctx)
    {
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];

        var cliName = (string)attr.ConstructorArguments[0].Value!;
        var description = (string)attr.ConstructorArguments[1].Value!;

        string? parentFullName = null;
        if (symbol.ContainingType != null)
        {
            var parentHasBranch = symbol.ContainingType.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == "Cratis.Cli.Registration.CliBranchAttribute");
            if (parentHasBranch)
                parentFullName = symbol.ContainingType.ToDisplayString();
        }

        return new BranchInfo(symbol.ToDisplayString(), cliName, description, parentFullName);
    }

    static ImmutableArray<CommandReg> GetCommandRegs(GeneratorAttributeSyntaxContext ctx)
    {
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var allAttrs = symbol.GetAttributes();

        // All [CliCommand] applications on this class (AllowMultiple = true)
        var cliCmdAttrs = ctx.Attributes;

        var examples = allAttrs
            .Where(a => a.AttributeClass?.ToDisplayString() == "Cratis.Cli.Registration.CliExampleAttribute")
            .Select(a =>
            {
                var args = a.ConstructorArguments[0].Values
                    .Select(v => (string)v.Value!)
                    .ToImmutableArray();
                var commandName = a.NamedArguments.FirstOrDefault(n => n.Key == "CommandName").Value.Value as string;
                return new ExampleData(args, commandName);
            })
            .ToImmutableArray();

        var llmOpts = allAttrs
            .Where(a => a.AttributeClass?.ToDisplayString() == "Cratis.Cli.Registration.LlmOptionAttribute")
            .Select(a => new LlmOptionData(
                (string)a.ConstructorArguments[0].Value!,
                (string)a.ConstructorArguments[1].Value!,
                (string)a.ConstructorArguments[2].Value!,
                a.NamedArguments.FirstOrDefault(n => n.Key == "CommandName").Value.Value as string))
            .ToImmutableArray();

        var advices = allAttrs
            .Where(a => a.AttributeClass?.ToDisplayString() == "Cratis.Cli.Registration.LlmOutputAdviceAttribute")
            .Select(a => new LlmAdviceData(
                (string)a.ConstructorArguments[0].Value!,
                (string)a.ConstructorArguments[1].Value!,
                a.NamedArguments.FirstOrDefault(n => n.Key == "CommandName").Value.Value as string))
            .ToImmutableArray();

        var llmDescription = allAttrs
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Cratis.Cli.Registration.LlmDescriptionAttribute")
            ?.ConstructorArguments[0].Value as string;

        var inheritsEventStore = InheritsEventStoreSettings(symbol);

        var result = ImmutableArray.CreateBuilder<CommandReg>(cliCmdAttrs.Length);
        foreach (var attr in cliCmdAttrs)
        {
            var leafName = (string)attr.ConstructorArguments[0].Value!;
            var description = (string)attr.ConstructorArguments[1].Value!;
            var isHidden = attr.NamedArguments.FirstOrDefault(n => n.Key == "IsHidden").Value.Value is true;
            var excludeFromLlm = attr.NamedArguments.FirstOrDefault(n => n.Key == "ExcludeFromLlm").Value.Value is true;

            string? branchFullName = null;
            var branchArg = attr.NamedArguments.FirstOrDefault(n => n.Key == "Branch");
            if (branchArg.Value.Kind == TypedConstantKind.Type)
                branchFullName = (branchArg.Value.Value as INamedTypeSymbol)?.ToDisplayString();

            result.Add(new CommandReg(
                symbol.ToDisplayString(),
                leafName,
                description,
                llmDescription,
                branchFullName,
                isHidden,
                excludeFromLlm,
                examples,
                llmOpts,
                advices,
                inheritsEventStore));
        }

        return result.MoveToImmutable();
    }

    static bool InheritsEventStoreSettings(INamedTypeSymbol symbol)
    {
        var baseType = symbol.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.TypeArguments.Length > 0)
            {
                if (baseType.TypeArguments[0] is INamedTypeSymbol settingsType)
                    return TypeInheritsFrom(settingsType, "EventStoreSettings");
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    static bool TypeInheritsFrom(INamedTypeSymbol type, string name)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.Name == name)
                return true;
            current = current.BaseType;
        }

        return false;
    }

    // ── Emit ───────────────────────────────────────────────────────────────────

    static void Emit(
        SourceProductionContext spc,
        ImmutableArray<BranchInfo> branches,
        ImmutableArray<CommandReg> commands)
    {
        var branchByFullName = new Dictionary<string, BranchInfo>();
        foreach (var b in branches)
            branchByFullName[b.FullName] = b;

        EmitCliApp(spc, branchByFullName, commands);
        EmitLlmContext(spc, branchByFullName, commands);
    }

    // ── Spectre registration ───────────────────────────────────────────────────

    static void EmitCliApp(
        SourceProductionContext spc,
        Dictionary<string, BranchInfo> branchByFullName,
        ImmutableArray<CommandReg> commands)
    {
        // Build branch → child-commands map
        var cmdsByBranch = new Dictionary<string, List<CommandReg>>();
        foreach (var cmd in commands)
        {
            var key = cmd.BranchFullName ?? "(root)";
            if (!cmdsByBranch.TryGetValue(key, out var list))
                cmdsByBranch[key] = list = new List<CommandReg>();
            list.Add(cmd);
        }

        // Build parent-branch → child-branches map
        var subBranches = new Dictionary<string, List<BranchInfo>>();
        foreach (var b in branchByFullName.Values)
        {
            var parentKey = b.ParentFullName ?? "(root)";
            if (!subBranches.TryGetValue(parentKey, out var list))
                subBranches[parentKey] = list = new List<BranchInfo>();
            list.Add(b);
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Copyright (c) Cratis. All rights reserved.");
        sb.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
        sb.AppendLine();
        sb.AppendLine("using Spectre.Console.Cli;");
        sb.AppendLine();
        sb.AppendLine("namespace Cratis.Cli;");
        sb.AppendLine();
        sb.AppendLine("public static partial class CliApp");
        sb.AppendLine("{");
        sb.AppendLine("    static void RegisterDiscoveredCommands(IConfigurator config)");
        sb.AppendLine("    {");

        var topBranches = GetOrEmpty(subBranches, "(root)");
        foreach (var branch in topBranches.OrderBy(b => b.CliName))
            EmitBranchNode(sb, branch, "config", cmdsByBranch, subBranches, 2);

        foreach (var cmd in GetOrEmpty(cmdsByBranch, "(root)").OrderBy(c => c.LeafName))
            EmitCommandLine(sb, cmd, "config", 2);

        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource("CliApp.RegisterDiscoveredCommands.g.cs", sb.ToString());
    }

    static void EmitBranchNode(
        StringBuilder sb,
        BranchInfo branch,
        string parentVar,
        Dictionary<string, List<CommandReg>> cmdsByBranch,
        Dictionary<string, List<BranchInfo>> subBranches,
        int depth)
    {
        var pad = Pad(depth);
        var varName = "_" + branch.CliName.Replace("-", "");
        sb.AppendLine($"{pad}{parentVar}.AddBranch(\"{branch.CliName}\", {varName} =>");
        sb.AppendLine($"{pad}{{");
        sb.AppendLine($"{pad}    {varName}.SetDescription(\"{Escape(branch.Description)}\");");

        var children = GetOrEmpty(subBranches, branch.FullName);
        foreach (var child in children.OrderBy(b => b.CliName))
            EmitBranchNode(sb, child, varName, cmdsByBranch, subBranches, depth + 1);

        var cmds = GetOrEmpty(cmdsByBranch, branch.FullName);
        foreach (var cmd in cmds.OrderBy(c => c.LeafName))
            EmitCommandLine(sb, cmd, varName, depth + 1);

        sb.AppendLine($"{pad}}});");
    }

    static void EmitCommandLine(StringBuilder sb, CommandReg cmd, string parentVar, int depth)
    {
        var pad = Pad(depth);
        var typeName = cmd.TypeFullName;

        var relevantExamples = cmd.Examples
            .Where(e => e.CommandName == null || e.CommandName == cmd.LeafName)
            .ToList();

        sb.Append($"{pad}{parentVar}.AddCommand<{typeName}>(\"{cmd.LeafName}\")");

        // Always chain description on next line for readability
        sb.AppendLine();
        sb.Append($"{pad}    .WithDescription(\"{Escape(cmd.Description)}\")");

        foreach (var ex in relevantExamples)
        {
            var argsJoined = string.Join(", ", ex.Args.Select(a => $"\"{Escape(a)}\""));
            sb.AppendLine();
            sb.Append($"{pad}    .WithExample({argsJoined})");
        }

        if (cmd.IsHidden)
        {
            sb.AppendLine();
            sb.Append($"{pad}    .IsHidden()");
        }

        sb.AppendLine(";");
    }

    // ── LLM context ────────────────────────────────────────────────────────────

    static void EmitLlmContext(
        SourceProductionContext spc,
        Dictionary<string, BranchInfo> branchByFullName,
        ImmutableArray<CommandReg> commands)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Copyright (c) Cratis. All rights reserved.");
        sb.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
        sb.AppendLine();
        sb.AppendLine("namespace Cratis.Cli.Commands.LlmContext;");
        sb.AppendLine();
        sb.AppendLine("public partial class LlmContextCommand");
        sb.AppendLine("{");

        EmitBuildCommandGroups(sb, branchByFullName, commands);
        sb.AppendLine();
        EmitBuildOutputAdvice(sb, branchByFullName, commands);

        sb.AppendLine("}");
        spc.AddSource("LlmContextCommand.Generated.g.cs", sb.ToString());
    }

    static void EmitBuildCommandGroups(
        StringBuilder sb,
        Dictionary<string, BranchInfo> branchByFullName,
        ImmutableArray<CommandReg> commands)
    {
        sb.AppendLine("    static System.Collections.Generic.IReadOnlyList<CommandGroupDescriptor> BuildDiscoveredCommandGroups() =>");
        sb.AppendLine("    [");

        // Build parent → child branch map
        var subBranchMap = new Dictionary<string, List<string>>();
        foreach (var b in branchByFullName.Values)
        {
            var parentKey = b.ParentFullName ?? "(root)";
            if (!subBranchMap.TryGetValue(parentKey, out var list))
                subBranchMap[parentKey] = list = new List<string>();
            list.Add(b.FullName);
        }

        // Build branch → direct commands map (only visible commands)
        var cmdsByBranch = new Dictionary<string, List<CommandReg>>();
        foreach (var cmd in commands.Where(c => !c.ExcludeFromLlm))
        {
            var key = cmd.BranchFullName ?? "(root)";
            if (!cmdsByBranch.TryGetValue(key, out var list))
                cmdsByBranch[key] = list = new List<CommandReg>();
            list.Add(cmd);
        }

        // Emit top-level branches (depth 1), then root commands
        var topBranches = subBranchMap.TryGetValue("(root)", out var tb) ? tb : new List<string>();
        foreach (var branchKey in topBranches.OrderBy(k => branchByFullName[k].CliName))
            EmitCommandGroupNode(sb, branchKey, branchByFullName, subBranchMap, cmdsByBranch, 2);

        // Root-level commands
        if (cmdsByBranch.TryGetValue("(root)", out var rootCmds))
        {
            sb.AppendLine("        new(");
            sb.AppendLine("            \"(root)\",");
            sb.AppendLine("            \"Root-level commands\",");
            sb.AppendLine("            null,");
            sb.AppendLine("            [");
            foreach (var cmd in rootCmds.OrderBy(c => c.LeafName))
                EmitCommandDescriptor(sb, cmd, suppressInherited: false, indent: 16);
            sb.AppendLine("            ],");
            sb.AppendLine("            null),");
        }

        sb.AppendLine("    ];");
    }

    /// <summary>
    /// Recursively emits a <c>CommandGroupDescriptor</c> for the given branch, including
    /// its sub-branches as <c>SubGroups</c>. Hoists inherited event-store options to the
    /// highest possible group level — either when ALL direct commands inherit them, or when
    /// any child group would hoist (consolidating repetition at the parent). Once hoisted at
    /// a level, all descendants have their inherited options suppressed.
    /// </summary>
    static void EmitCommandGroupNode(
        StringBuilder sb,
        string branchFullName,
        Dictionary<string, BranchInfo> branchByFullName,
        Dictionary<string, List<string>> subBranchMap,
        Dictionary<string, List<CommandReg>> cmdsByBranch,
        int depth,
        bool parentHoisted = false)
    {
        var branch = branchByFullName[branchFullName];
        var pad = Pad(depth);

        var directCmds = cmdsByBranch.TryGetValue(branchFullName, out var dc) ? dc : new List<CommandReg>();
        var childBranches = subBranchMap.TryGetValue(branchFullName, out var cb) ? cb : new List<string>();

        // Count child branches whose direct commands ALL inherit event-store settings
        var childrenThatWouldHoist = childBranches
            .Where(childKey =>
            {
                var childCmds = cmdsByBranch.TryGetValue(childKey, out var cc) ? cc : new List<CommandReg>();
                return childCmds.Count > 0 && childCmds.All(c => c.InheritsEventStoreSettings);
            })
            .ToList();

        // Hoist at this group level when parent hasn't already hoisted AND either:
        //   (a) all direct commands inherit (original rule), or
        //   (b) any child group would hoist — bubble up to avoid repeating at every sub-group
        var hoistInherited = !parentHoisted && (
            (directCmds.Count > 0 && directCmds.All(c => c.InheritsEventStoreSettings)) ||
            childrenThatWouldHoist.Count > 0);

        // Once hoisted here (or already hoisted by an ancestor), suppress in all descendants
        var suppressInDescendants = parentHoisted || hoistInherited;

        sb.AppendLine($"{pad}new(");
        sb.AppendLine($"{pad}    \"{Escape(branch.CliName)}\",");
        sb.AppendLine($"{pad}    \"{Escape(branch.Description)}\",");

        // InheritedOptions at group level
        if (hoistInherited)
            sb.AppendLine($"{pad}    EventStoreOptions(),");
        else
            sb.AppendLine($"{pad}    null,");

        // Commands (null if none)
        if (directCmds.Count > 0)
        {
            sb.AppendLine($"{pad}    [");
            foreach (var cmd in directCmds.OrderBy(c => c.LeafName))
                EmitCommandDescriptor(sb, cmd, suppressInherited: suppressInDescendants, indent: (depth + 2) * 4);
            sb.AppendLine($"{pad}    ],");
        }
        else
        {
            sb.AppendLine($"{pad}    null,");
        }

        // SubGroups (null if none)
        if (childBranches.Count > 0)
        {
            sb.AppendLine($"{pad}    [");
            foreach (var childKey in childBranches.OrderBy(k => branchByFullName[k].CliName))
                EmitCommandGroupNode(sb, childKey, branchByFullName, subBranchMap, cmdsByBranch, depth + 2, parentHoisted: suppressInDescendants);
            sb.AppendLine($"{pad}    ]),");
        }
        else
        {
            sb.AppendLine($"{pad}    null),");
        }
    }

    static void EmitCommandDescriptor(StringBuilder sb, CommandReg cmd, bool suppressInherited, int indent)
    {
        var allOpts = cmd.LlmOptions
            .Where(o => o.CommandName == null || o.CommandName == cmd.LeafName)
            .ToList();

        // Positional arguments have names wrapped in <>, named options start with - or --
        var args = allOpts.Where(o => o.Name.StartsWith("<")).ToList();
        var opts = allOpts.Where(o => !o.Name.StartsWith("<")).ToList();

        var pad = new string(' ', indent);
        var description = cmd.LlmDescription ?? cmd.Description;

        sb.AppendLine($"{pad}new CommandDescriptor(");
        sb.AppendLine($"{pad}    \"{Escape(cmd.LeafName)}\",");
        sb.AppendLine($"{pad}    \"{Escape(description)}\",");

        // InheritedOptions — suppress when this group or an ancestor already hoists them
        if (!suppressInherited && cmd.InheritsEventStoreSettings)
            sb.AppendLine($"{pad}    EventStoreOptions(),");
        else
            sb.AppendLine($"{pad}    null,");

        // Positional arguments
        if (args.Count > 0)
        {
            sb.AppendLine($"{pad}    [");
            foreach (var arg in args)
                sb.AppendLine($"{pad}        new OptionDescriptor(\"{Escape(arg.Name)}\", \"{Escape(arg.Type)}\", \"{Escape(arg.Description)}\"),");
            sb.AppendLine($"{pad}    ],");
        }
        else
        {
            sb.AppendLine($"{pad}    null,");
        }

        // Named options/flags
        if (opts.Count > 0)
        {
            sb.AppendLine($"{pad}    [");
            foreach (var opt in opts)
                sb.AppendLine($"{pad}        new OptionDescriptor(\"{Escape(opt.Name)}\", \"{Escape(opt.Type)}\", \"{Escape(opt.Description)}\"),");
            sb.AppendLine($"{pad}    ]),");
        }
        else
        {
            sb.AppendLine($"{pad}    null),");
        }
    }

    static void EmitBuildOutputAdvice(
        StringBuilder sb,
        Dictionary<string, BranchInfo> branchByFullName,
        ImmutableArray<CommandReg> commands)
    {
        sb.AppendLine("    static System.Collections.Generic.IReadOnlyList<CommandOutputAdvice> BuildDiscoveredOutputAdvice() =>");
        sb.AppendLine("    [");

        foreach (var cmd in commands)
        {
            foreach (var advice in cmd.OutputAdvices.Where(a => a.CommandName == null || a.CommandName == cmd.LeafName))
            {
                var path = BuildCommandPath(cmd, branchByFullName);
                sb.AppendLine($"        new CommandOutputAdvice(\"{Escape(path)}\", \"{Escape(advice.Format)}\", \"{Escape(advice.Reason)}\"),");
            }
        }

        sb.AppendLine("    ];");
    }

    static string BuildCommandPath(CommandReg cmd, Dictionary<string, BranchInfo> branchByFullName)
    {
        var segments = new List<string> { cmd.LeafName };
        var current = cmd.BranchFullName;
        while (current != null && branchByFullName.TryGetValue(current, out var branch))
        {
            segments.Insert(0, branch.CliName);
            current = branch.ParentFullName;
        }

        return string.Join(" ", segments);
    }

    static string Pad(int depth) => new(' ', depth * 4);

    static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    static List<T> GetOrEmpty<T>(Dictionary<string, List<T>> dict, string key) =>
        dict.TryGetValue(key, out var list) ? list : new List<T>();
}
