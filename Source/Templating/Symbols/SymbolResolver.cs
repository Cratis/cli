// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Text.Json;
using Cratis.Templating.Configuration;
using Cratis.Templating.Expressions;
using Cratis.Templating.Generators;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Symbols;

/// <summary>
/// The outcome of symbol resolution: the resolved values keyed by name, and the set of symbols that
/// were disabled by their <c language="csharp">isEnabled</c> conditions.
/// </summary>
/// <param name="Values">Resolved symbol values.</param>
/// <param name="DisabledSymbols">Names of symbols disabled by condition.</param>
public record ResolvedSymbols(IReadOnlyDictionary<string, string> Values, IReadOnlyList<string> DisabledSymbols);

/// <summary>
/// Resolves all symbols of a manifest in dependency order, with cycle detection. Parameters take
/// user-provided values, then baseline values, then defaults; generated symbols may reference other
/// symbols through their generator parameters; computed symbols reference symbols through their
/// expressions. The implicit <c language="csharp">name</c> symbol is created when <c language="csharp">sourceName</c> is set.
/// </summary>
/// <param name="forms">The registry that applies user-defined value forms to derived symbols.</param>
public class SymbolResolver(ValueFormRegistry forms)
{
    /// <summary>
    /// Extracts the identifiers referenced by an expression, excluding boolean literals.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <returns>The referenced identifiers.</returns>
    public static IReadOnlyCollection<string> ExpressionIdentifiers(string expression)
    {
        var result = new List<string>();
        foreach (var dialect in new[] { ExpressionDialect.Cpp2, ExpressionDialect.MSBuild })
        {
            try
            {
                foreach (var token in Tokenizer.Tokenize(expression, dialect))
                {
                    if (token.Kind is TokenKind.Identifier
                        && !token.Text.Equals("true", StringComparison.OrdinalIgnoreCase)
                        && !token.Text.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(token.Text);
                    }
                }
                return result;
            }
            catch (ExpressionEvaluationError)
            {
                // Try the next dialect.
            }
        }
        return result;
    }

    /// <summary>
    /// Resolves all symbols.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="parameterValues">User-provided parameter values, already validated as known symbols.</param>
    /// <param name="name">The instantiation name, used for the implicit name symbol.</param>
    /// <param name="hostData">Host-provided data for bind symbols.</param>
    /// <returns>The resolved symbols.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    /// <exception cref="SymbolResolutionError">Thrown when a symbol cannot be resolved.</exception>
    public ResolvedSymbols Resolve(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> parameterValues,
        string name,
        IReadOnlyDictionary<string, string> hostData)
    {
        if (manifest.SourceName is not null && manifest.Symbols.ContainsKey("name"))
        {
            throw new InvalidTemplateManifest(
                "template.json: symbol 'name' cannot be defined when 'sourceName' is set — sourceName provides the built-in 'name' symbol.");
        }

        var scope = new Dictionary<string, string>();
        var disabled = new List<string>();
        var symbols = manifest.Symbols.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (manifest.SourceName is not null)
        {
            // The implicit name symbol: the user-supplied name for the sourceName replacement.
            symbols["name"] = new SymbolConfig { Name = "name", Type = SymbolType.Parameter, DataType = "string" };
            scope["name"] = name;
        }

        var order = TopologicalOrder(symbols);
        var provided = new Dictionary<string, string>(parameterValues, StringComparer.Ordinal);
        var knownLiterals = manifest.QuotelessChoiceLiterals;

        // Pass 1: parameters and bind symbols — values come from outside, then conditions are checked.
        foreach (var symbolName in order.Where(symbolName => symbols[symbolName].Type is SymbolType.Parameter or SymbolType.Bind))
        {
            ResolveExternal(symbols[symbolName], provided, hostData, scope, disabled, knownLiterals);
        }

        // Pass 2: derived, computed and generated — dependency order guarantees their inputs are ready.
        foreach (var symbolName in order.Where(symbolName => symbols[symbolName].Type is SymbolType.Derived or SymbolType.Computed or SymbolType.Generated))
        {
            var symbol = symbols[symbolName];
            if (disabled.Contains(symbolName) || IsDisabled(symbol, scope, knownLiterals))
            {
                disabled.Add(symbolName);
                continue;
            }

            scope[symbolName] = symbol.Type switch
            {
                SymbolType.Derived => ResolveDerived(symbol, scope),
                SymbolType.Computed => ResolveComputed(symbol, scope, knownLiterals),
                SymbolType.Generated => GeneratorRunner.Run(
                    symbol.Generator ?? throw new InvalidTemplateManifest($"symbols.{symbolName}: generated symbol missing 'generator'."),
                    symbol.GeneratorParameters,
                    scope,
                    symbolName),
                _ => throw new SymbolResolutionError($"symbols.{symbolName}: unexpected symbol type {symbol.Type}.")
            };
        }

        if (manifest.SourceName is not null)
        {
            scope["name"] = name;
        }
        return new ResolvedSymbols(scope, disabled);
    }

    static bool IsDisabled(SymbolConfig symbol, Dictionary<string, string> scope, IReadOnlyCollection<string> knownLiterals) =>
        symbol.IsEnabled is not null && !ExpressionEvaluator.EvaluateBoolean(symbol.IsEnabled, ExpressionDialect.Cpp2, scope, knownLiterals);

    static string ResolveComputed(SymbolConfig symbol, Dictionary<string, string> scope, IReadOnlyCollection<string> knownLiterals)
    {
        var expression = symbol.Value
            ?? throw new SymbolResolutionError($"symbols.{symbol.Name}: computed symbol is missing 'value'.");
        var dialect = ExpressionEvaluator.DialectFromName(symbol.Evaluator);
        var result = ExpressionEvaluator.Evaluate(expression, dialect, scope, knownLiterals);

        // Computed symbols declared with a bool data type normalize to canonical true/false.
        return string.Equals(symbol.DataType, "bool", StringComparison.Ordinal)
            || string.Equals(symbol.DataType, "bool?", StringComparison.Ordinal)
            ? result.ToLowerInvariant()
            : result;
    }

    static List<string> TopologicalOrder(Dictionary<string, SymbolConfig> symbols)
    {
        var dependencies = symbols.ToDictionary(
            pair => pair.Key,
            pair => DependenciesOf(pair.Value).Where(symbols.ContainsKey).ToHashSet(StringComparer.Ordinal));

        var order = new List<string>(symbols.Count);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in symbols.Keys)
        {
            Visit(name, dependencies, visiting, visited, order);
        }
        return order;
    }

    static void Visit(
        string name,
        Dictionary<string, HashSet<string>> dependencies,
        HashSet<string> visiting,
        HashSet<string> visited,
        List<string> order)
    {
        if (visited.Contains(name))
        {
            return;
        }

        if (!visiting.Add(name))
        {
            throw new SymbolResolutionError(
                $"circular symbol dependency detected: {string.Join(" -> ", visiting)} -> {name}. " +
                "Symbols must form a directed acyclic graph.");
        }

        foreach (var dependency in dependencies[name])
        {
            Visit(dependency, dependencies, visiting, visited, order);
        }

        visiting.Remove(name);
        visited.Add(name);
        order.Add(name);
    }

    static List<string> DependenciesOf(SymbolConfig symbol) => symbol.Type switch
    {
        SymbolType.Derived => symbol.ValueSource is null ? [] : [symbol.ValueSource],
        SymbolType.Computed => [.. ExpressionIdentifiers(symbol.Value ?? string.Empty)],
        SymbolType.Generated => [.. GeneratorDependencies(symbol)],
        SymbolType.Parameter => [.. ExpressionIdentifiers(symbol.IsEnabled ?? string.Empty)
            .Concat(ExpressionIdentifiers(symbol.IsRequired ?? string.Empty))],
        _ => []
    };

    static IEnumerable<string> GeneratorDependencies(SymbolConfig symbol)
    {
        foreach (var (name, value) in symbol.GeneratorParameters)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String when string.Equals(name, "sourceVariable", StringComparison.Ordinal)
                    || string.Equals(name, "source", StringComparison.Ordinal)
                    || string.Equals(name, "parameter", StringComparison.Ordinal):
                    yield return value.GetString()!;
                    break;

                case JsonValueKind.Array when string.Equals(name, "symbols", StringComparison.Ordinal):
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            yield return item.GetString()!;
                        }
                    }
                    break;

                case JsonValueKind.Array when string.Equals(name, "cases", StringComparison.Ordinal):
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object
                            && item.TryGetProperty("condition", out var condition))
                        {
                            foreach (var identifier in ExpressionIdentifiers(condition.GetString() ?? string.Empty))
                            {
                                yield return identifier;
                            }
                        }
                    }
                    break;

                case JsonValueKind.String when string.Equals(name, "expression", StringComparison.Ordinal):
                    foreach (var identifier in ExpressionIdentifiers(value.GetString()!))
                    {
                        yield return identifier;
                    }
                    break;
            }
        }
    }

    static void ResolveExternal(
        SymbolConfig symbol,
        Dictionary<string, string> provided,
        IReadOnlyDictionary<string, string> hostData,
        Dictionary<string, string> scope,
        List<string> disabled,
        IReadOnlyCollection<string> knownLiterals)
    {
        if (symbol.Type is SymbolType.Parameter && IsDisabled(symbol, scope, knownLiterals))
        {
            disabled.Add(symbol.Name);
            return;
        }

        switch (symbol.Type)
        {
            case SymbolType.Parameter:
            {
                if (provided.TryGetValue(symbol.Name, out var value))
                {
                    scope[symbol.Name] = value;
                    return;
                }
                var defaultValue = symbol.DefaultValue ?? symbol.DefaultIfOptionWithoutValue;
                if (defaultValue is null && string.Equals(symbol.DataType, "bool", StringComparison.Ordinal))
                {
                    defaultValue = "false";
                }
                scope[symbol.Name] = defaultValue ?? string.Empty;
                break;
            }

            case SymbolType.Bind:
            {
                var binding = symbol.Binding
                    ?? throw new SymbolResolutionError($"symbols.{symbol.Name}: bind symbol is missing 'binding'.");
                if (!hostData.TryGetValue(binding, out var value))
                {
                    throw new SymbolResolutionError(
                        $"symbols.{symbol.Name}: bind symbol references host data '{binding}' which is not provided.");
                }
                scope[symbol.Name] = value;
                break;
            }

            default:
                return;
        }
    }

    string ResolveDerived(SymbolConfig symbol, Dictionary<string, string> scope)
    {
        var source = symbol.ValueSource
            ?? throw new SymbolResolutionError($"symbols.{symbol.Name}: derived symbol is missing 'valueSource'.");
        if (!scope.TryGetValue(source, out var value))
        {
            throw new SymbolResolutionError($"symbols.{symbol.Name}: valueSource '{source}' is not defined.");
        }

        foreach (var form in symbol.ValueTransform)
        {
            value = forms.Apply(form, value);
        }
        return value;
    }
}
