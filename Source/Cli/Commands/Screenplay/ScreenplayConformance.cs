// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Files;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles authored sources and delegates all structural comparison to Screenplay.
/// </summary>
public sealed class ScreenplayConformance : IScreenplayConformance
{
    /// <summary>
    /// Classifies typed changes without reimplementing structural comparison.
    /// </summary>
    /// <param name="difference">The Screenplay comparison result, with model before and code after.</param>
    /// <returns>The findings in typed section order.</returns>
    public static IReadOnlyList<ConformanceFinding> Classify(ModelDifference difference)
    {
        var findings = new List<ConformanceFinding>();
        foreach (var change in difference.Declarations)
        {
            var added = change.Change == DeclarationChangeKind.Added;
            var removed = change.Change == DeclarationChangeKind.Removed;
            var category = (Informational(change.Declaration), change.Change) switch
            {
                (true, _) => "Informational",
                (_, DeclarationChangeKind.Added) => "MissingFromModel",
                (_, DeclarationChangeKind.Removed) => "NotRealizedInCode",
                _ => "Informational"
            };
            var counterpart = difference.Declarations.Where(other =>
                other.Change == (added ? DeclarationChangeKind.Removed : DeclarationChangeKind.Added) &&
                other.Declaration.Kind == change.Declaration.Kind &&
                LastName(Address(other.Declaration)) == LastName(Address(change.Declaration)))
                .Select(other => Address(other.Declaration)).Order(StringComparer.Ordinal).ToArray();
            findings.Add(Finding(change.Declaration, category, change.Change.ToString(), blocking: category == "MissingFromModel") with
            {
                SameNameCounterpart = (added || removed) && counterpart.Length == 1 ? counterpart[0] : null
            });
        }
        findings.AddRange(difference.Events.Select(change => Finding(change.Declaration, "ShapeMismatch", change.Change.ToString(), change.Property, change.BeforeType, change.AfterType, true)));
        findings.AddRange(difference.Members.Select(change => Finding(
            change.Declaration,
            Informational(change.Declaration) ? "Informational" : "ShapeMismatch",
            change.Change.ToString(),
            change.Member,
            blocking: !Informational(change.Declaration) && change.AfterHash is not null)));
        findings.AddRange(difference.Specifications.Select(change => Finding(change.Declaration, "Informational", change.Change.ToString(), change.Member)));
        return findings;
    }

    /// <summary>
    /// Resolves the check's deliberately narrow exit contract: defects before incomplete coverage.
    /// </summary>
    /// <param name="difference">The typed comparison result.</param>
    /// <param name="findings">The classified findings.</param>
    /// <returns>0 for no blocking findings, 1 for defects, or 2 for a check that could not look.</returns>
    public static int ExitCode(ModelDifference difference, IReadOnlyList<ConformanceFinding> findings)
    {
        if (findings.Any(finding => finding.Blocking))
        {
            return 1;
        }
        return difference.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind is
            ComparisonGapKind.IncompleteSource or ComparisonGapKind.ExampleResolution or ComparisonGapKind.NotComparableAssigned or ComparisonGapKind.NotComparableIndexed) ? 2 : 0;
    }

    /// <inheritdoc/>
    public AuthoredScreenplay Read(string modelRoot)
    {
        var compiler = new PlayFileCompiler();
        var compilation = File.Exists(modelRoot) ? compiler.CompileApplication(modelRoot) : compiler.CompileFolder(modelRoot);
        var sources = compilation.Sources.ToDictionary(source => source.File.RelativePath.Replace('\\', '/'), source => source.Source, StringComparer.Ordinal);
        var name = compilation.Result.Value?.Domain?.Name ?? ScreenplayBinding.ApplicationName;
        return new(name, sources, [.. compilation.Result.Diagnostics.Select(diagnostic => new ScreenplayDiagnostic(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column})"))]);
    }

    /// <inheritdoc/>
    public ModelDifference Compare(AuthoredScreenplay model, string code) => ModelComparison.Compare(
        ComparedModel.FromSources(model.ApplicationName, model.Sources),
        ComparedModel.FromSources(model.ApplicationName, new Dictionary<string, string> { ["generated.play"] = code }));

    static bool Informational(ComparedDeclaration declaration) => declaration.Kind == "Application" || declaration.Kind == "Specification";
    static string Address(ComparedDeclaration declaration) => declaration.AfterAddress ?? declaration.BeforeAddress ?? string.Empty;
    static string LastName(string address) => address[(address.LastIndexOf('.') + 1)..];
    static ConformanceFinding Finding(ComparedDeclaration declaration, string category, string change, string? member = null, string? beforeType = null, string? afterType = null, bool blocking = false) =>
        new(category, declaration.Kind, Address(declaration), change, member, beforeType, afterType, blocking);
}
