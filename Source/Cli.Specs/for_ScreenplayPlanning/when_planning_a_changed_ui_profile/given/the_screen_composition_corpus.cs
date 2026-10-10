// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile.given;

/// <summary>
/// Writes the canonical screen-composition corpus to a folder so a spec can change its <c language="csharp">ui profile</c>.
/// </summary>
public class the_screen_composition_corpus : Specification
{
    protected string _folder = null!;
    private protected ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"cli-profile-{Guid.NewGuid():N}");
        foreach (var document in ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder").Documents)
        {
            var path = Path.Combine(_folder, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }
    }

    /// <summary>
    /// Changes the application document, failing the spec when the change matched nothing.
    /// </summary>
    /// <param name="pattern">The multi-line pattern to replace.</param>
    /// <param name="replacement">The replacement.</param>
    protected void ChangeApplication(string pattern, string replacement)
    {
        var path = Path.Combine(_folder, "application.play");
        var before = File.ReadAllText(path);
        var after = Regex.Replace(before, pattern, replacement, RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        after.ShouldNotEqual(before);
        File.WriteAllText(path, after);
    }

    /// <summary>
    /// Plans the corpus for the bundled Cratis target.
    /// </summary>
    /// <returns>A task that completes when planning finishes.</returns>
    protected async Task Plan() =>
        _result = await new ScreenplayPlanning().Plan(
            new(_folder, ScreenCompositionCorpus.V1.ApplicationName, CratisRendering.TargetId, null, null),
            CancellationToken.None);

    /// <summary>
    /// Gets the error diagnostics with a given code.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The matching errors.</returns>
    private protected IEnumerable<ScreenplayDiagnostic> ErrorsWith(string code) =>
        _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error && _.Code == code);

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
