// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Provides the command tree for the Cratis CLI, derived at runtime from <see cref="CliCommandAttribute"/>
/// and <see cref="CliBranchAttribute"/> registrations. No manual maintenance required.
/// </summary>
public static class CliCommandTree
{
    /// <summary>
    /// Global options available on all commands.
    /// </summary>
    public static readonly IReadOnlyList<string> GlobalOptions =
    [
        "--server",
        "-o", "--output",
        "-q", "--quiet",
        "-y", "--yes",
        "--management-port",
        "--debug"
    ];

    const string CommandOptionAttributeFullName = "Spectre.Console.Cli.CommandOptionAttribute";

    static readonly Lazy<List<CommandNode>> _commands = new(Build);

    /// <summary>
    /// Gets the full command tree, built from CLI registration attributes.
    /// </summary>
    public static IReadOnlyList<CommandNode> Commands => _commands.Value;

    static List<CommandNode> Build()
    {
        var assembly = typeof(CliCommandTree).Assembly;
        var allTypes = assembly.GetTypes();

        var branchTypes = allTypes
            .Where(t => Attribute.IsDefined(t, typeof(CliBranchAttribute)))
            .ToHashSet();

        var commandMappings = allTypes
            .SelectMany(t => t.GetCustomAttributes<CliCommandAttribute>()
                .Where(a => !a.IsHidden)
                .Select(a => (Attr: a, CommandType: t)))
            .ToList();

        return BuildChildren(null, branchTypes, commandMappings);
    }

    static List<CommandNode> BuildChildren(
        Type? parentBranchType,
        HashSet<Type> branchTypes,
        List<(CliCommandAttribute Attr, Type CommandType)> commandMappings)
    {
        var nodes = new List<CommandNode>();

        foreach (var branchType in branchTypes.OrderBy(t => t.GetCustomAttribute<CliBranchAttribute>()!.Name))
        {
            var declaredParent = branchType.DeclaringType;
            var isRootBranch = declaredParent is null || !branchTypes.Contains(declaredParent);
            var belongsHere = parentBranchType is null ? isRootBranch : declaredParent == parentBranchType;
            if (!belongsHere)
            {
                continue;
            }

            var branchAttr = branchType.GetCustomAttribute<CliBranchAttribute>()!;
            var subChildren = BuildChildren(branchType, branchTypes, commandMappings);

            var branchCommands = commandMappings
                .Where(m => m.Attr.Branch == branchType)
                .OrderBy(m => m.Attr.Name)
                .Select(m => new CommandNode(m.Attr.Name, m.Attr.Description, CollectOptions(m.CommandType))
                {
                    DynamicCompletionContext = m.Attr.DynamicCompletion
                });

            var allChildren = subChildren.Concat(branchCommands).ToList();
            nodes.Add(new CommandNode(branchAttr.Name, branchAttr.Description, [], allChildren));
        }

        var leafCommands = commandMappings
            .Where(m => m.Attr.Branch == parentBranchType)
            .OrderBy(m => m.Attr.Name);

        foreach (var (attr, cmdType) in leafCommands)
        {
            nodes.Add(new CommandNode(attr.Name, attr.Description, CollectOptions(cmdType))
            {
                DynamicCompletionContext = attr.DynamicCompletion
            });
        }

        return nodes;
    }

    static List<string> CollectOptions(Type commandType)
    {
        var settingsType = GetSettingsType(commandType);
        if (settingsType is null)
        {
            return [];
        }

        var opts = new List<string>();
        var type = settingsType;

        while (type is not null && type.Name != "GlobalSettings" && type != typeof(object))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var template = GetCommandOptionTemplate(prop);
                if (template is null)
                {
                    continue;
                }

                foreach (var segment in template.Split('|'))
                {
                    var flag = segment.Trim();
                    var space = flag.IndexOf(' ');
                    if (space >= 0)
                    {
                        flag = flag[..space];
                    }

                    if (flag.StartsWith('-'))
                    {
                        opts.Add(flag);
                    }
                }
            }

            type = type.BaseType;
        }

        opts.Reverse();
        return opts;
    }

    static string? GetCommandOptionTemplate(PropertyInfo prop)
    {
        var attrData = prop.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == CommandOptionAttributeFullName);

        return attrData?.ConstructorArguments.Count > 0
            ? attrData.ConstructorArguments[0].Value as string
            : null;
    }

    static Type? GetSettingsType(Type commandType)
    {
        var type = commandType;

        while (type is not null)
        {
            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    return args[0];
                }
            }

            type = type.BaseType;
        }

        return null;
    }
}
