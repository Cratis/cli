// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;
using Cratis.Templating.Expressions;
using Cratis.Templating.FileSystem;
using Cratis.Templating.Processing;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Orchestration;

/// <summary>
/// Builds the token set for a render: every enabled symbol's <c language="csharp">replaces</c> (with its value-form
/// expansions), <c language="csharp">fileRename</c> tokens, the <c language="csharp">sourceName</c> default forms, and the replacement,
/// flag and expandVariables custom operations. Split into content and path replacers — fileRename
/// applies to paths only.
/// </summary>
public static class TokenAssembly
{
    /// <summary>
    /// Builds content and path replacers for a manifest.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="symbols">Resolved symbol values.</param>
    /// <param name="disabled">Names of disabled symbols, whose tokens are skipped.</param>
    /// <param name="forms">The value form registry.</param>
    /// <param name="relativePath">The file's relative path, used to match glob-scoped symbol forms.</param>
    /// <returns>Content and path token replacers.</returns>
    public static (TokenReplacer Content, TokenReplacer Paths) Build(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlyList<string> disabled,
        ValueFormRegistry forms,
        string relativePath)
    {
        var content = new TokenReplacer();
        var paths = new TokenReplacer();

        AddSymbolTokens(manifest, symbols, disabled, forms, relativePath, content, paths);
        AddSourceNameTokens(manifest, symbols, forms, content, paths);
        AddCustomOperationTokens(manifest, symbols, content);
        return (content, paths);
    }

    /// <summary>
    /// Resolves the custom conditional configuration applying to a file, if any — from special custom
    /// operations matching the file's path, or from global custom operations. A custom conditional
    /// replaces the family default. Returns null when neither a custom conditional nor a family
    /// default provides directives.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="relativePath">The file's path relative to the template root.</param>
    /// <param name="familyDefault">The family's default configuration.</param>
    /// <returns>The effective conditional configuration and its dialect.</returns>
    public static ConditionalContext? CustomConditionalFor(
        TemplateConfig manifest,
        string relativePath,
        FileFamilyConfig? familyDefault)
    {
        foreach (var (glob, operations) in manifest.SpecialCustomOperations)
        {
            var conditional = operations.FirstOrDefault(operation =>
                operation.Type == CustomOperationType.Conditional && GlobMatcher.Matches(relativePath, glob));
            if (conditional is not null)
            {
                return new ConditionalContext(
                    ToFileFamilyConfig(conditional),
                    ExpressionEvaluator.DialectFromName(conditional.Evaluator));
            }
        }

        var globalConditional = manifest.GlobalCustomOperations.FirstOrDefault(
            operation => operation.Type == CustomOperationType.Conditional);
        if (globalConditional is not null)
        {
            return new ConditionalContext(
                ToFileFamilyConfig(globalConditional),
                ExpressionEvaluator.DialectFromName(globalConditional.Evaluator));
        }

        return familyDefault?.HasDirectives != true
            ? null
            : new ConditionalContext(familyDefault, ExpressionDialect.Cpp2);
    }

    static void AddSymbolTokens(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> symbols,
        IReadOnlyList<string> disabled,
        ValueFormRegistry forms,
        string relativePath,
        TokenReplacer content,
        TokenReplacer paths)
    {
        foreach (var (name, symbol) in manifest.Symbols)
        {
            if (disabled.Contains(name) || !symbols.TryGetValue(name, out var value))
            {
                continue;
            }

            if (symbol.Replaces is not null)
            {
                AddTokenWithForms(content, symbol.Replaces, value, symbol, forms, relativePath);
            }

            if (symbol.FileRename is not null)
            {
                AddTokenWithForms(paths, symbol.FileRename, value, symbol, forms, relativePath);
            }
        }
    }

    static void AddTokenWithForms(
        TokenReplacer replacer,
        string replaces,
        string value,
        SymbolConfig symbol,
        ValueFormRegistry forms,
        string relativePath)
    {
        // A "{-VALUE-FORMS-}<form>" suffix declares an explicit form for the token; the token that
        // appears in the source is the config-time token transformed by that form.
        const string marker = "{-VALUE-FORMS-}";
        var markerIndex = replaces.IndexOf(marker, StringComparison.Ordinal);
        var baseToken = markerIndex >= 0 ? replaces[..markerIndex] : replaces;
        List<string> explicitForms = markerIndex >= 0
            ? [replaces[(markerIndex + marker.Length)..]]
            : [];

        if (explicitForms.Count > 0)
        {
            foreach (var form in explicitForms)
            {
                replacer.Add(forms.Apply(form, baseToken), forms.Apply(form, value));
            }
            return;
        }

        replacer.Add(baseToken, value);

        // The symbol's forms map adds additional transformed tokens: global forms plus any
        // glob-scoped forms matching this file.
        List<string> formNames = symbol.Forms.TryGetValue("global", out var globalForms)
            ? [.. globalForms]
            : [];
        foreach (var (glob, scopedForms) in symbol.Forms)
        {
            if (glob != "global" && GlobMatcher.Matches(relativePath, glob))
            {
                formNames.AddRange(scopedForms);
            }
        }

        foreach (var form in formNames)
        {
            replacer.Add(forms.Apply(form, baseToken), forms.Apply(form, value));
        }
    }

    static void AddSourceNameTokens(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> symbols,
        ValueFormRegistry forms,
        TokenReplacer content,
        TokenReplacer paths)
    {
        if (manifest.SourceName is null || !symbols.TryGetValue("name", out var name))
        {
            return;
        }

        foreach (var form in ValueFormRegistry.SourceNameDefaultForms)
        {
            var sourceToken = forms.Apply(form, manifest.SourceName);
            var replacement = forms.Apply(form, name);
            content.Add(sourceToken, replacement);
            paths.Add(sourceToken, replacement);
        }
    }

    static void AddCustomOperationTokens(
        TemplateConfig manifest,
        IReadOnlyDictionary<string, string> symbols,
        TokenReplacer content)
    {
        foreach (var operation in manifest.GlobalCustomOperations)
        {
            AddOperationToken(operation, symbols, content);
        }

        foreach (var operations in manifest.SpecialCustomOperations.Values)
        {
            foreach (var operation in operations)
            {
                AddOperationToken(operation, symbols, content);
            }
        }
    }

    static void AddOperationToken(
        CustomOperationConfig operation,
        IReadOnlyDictionary<string, string> symbols,
        TokenReplacer content)
    {
        switch (operation.Type)
        {
            case CustomOperationType.Replacement when operation.Token is not null:
                string replacement;
                if (operation.Variable is not null)
                {
                    replacement = symbols.TryGetValue(operation.Variable, out var variableValue) ? variableValue : string.Empty;
                }
                else
                {
                    replacement = operation.Replacement ?? string.Empty;
                }
                content.Add(operation.Token, replacement);
                break;

            case CustomOperationType.ExpandVariables:
                var prefix = operation.Prefix ?? "$(";
                var suffix = operation.Suffix ?? ")";
                foreach (var (name, value) in symbols)
                {
                    content.Add($"{prefix}{name}{suffix}", value);
                }
                break;

            case CustomOperationType.Flag when operation.Token is not null:
                // A flag's token is removed from the output when the flag fires.
                content.Add(operation.Token, string.Empty);
                break;

            // Conditional, region, include and balancedNesting operations are handled by the
            // conditional processor and the renderer, not by token replacement.
            case CustomOperationType.Conditional:
            case CustomOperationType.Region:
            case CustomOperationType.Include:
            case CustomOperationType.BalancedNesting:
                break;
        }
    }

    static FileFamilyConfig ToFileFamilyConfig(CustomOperationConfig operation) => new(
        TokensFor(operation, "if"),
        TokensFor(operation, "elseif"),
        TokensFor(operation, "else"),
        TokensFor(operation, "endif"),
        TokensFor(operation, "actionableIf"),
        TokensFor(operation, "actionableElseif"),
        TokensFor(operation, "actionableElse"),
        operation.Actions,
        operation.Trim ?? true,
        operation.WholeLine ?? true,
        "//");

    static IReadOnlyList<string> TokensFor(CustomOperationConfig operation, string key) =>
        operation.Tokens.TryGetValue(key, out var tokens) ? tokens : [];
}

/// <summary>
/// The effective conditional configuration for a file and the dialect its expressions evaluate with.
/// </summary>
/// <param name="Config">The directive configuration.</param>
/// <param name="Dialect">The expression dialect.</param>
public record ConditionalContext(FileFamilyConfig Config, ExpressionDialect Dialect);
