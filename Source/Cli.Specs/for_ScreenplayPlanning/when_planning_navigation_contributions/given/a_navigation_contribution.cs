// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions.given;

public class a_navigation_contribution : when_planning_a_changed_ui_profile.given.the_screen_composition_corpus
{
    protected void AddProperties(params string[] properties)
    {
        var path = Path.Combine(_folder, "Workspaces", "Tracking", "Tracking.play");
        var before = File.ReadAllText(path);
        var after = before.Replace("contribute to Navigation", $"contribute to Navigation\n{string.Join('\n', properties.Select(property => $"      {property}"))}", StringComparison.Ordinal);
        after.ShouldNotEqual(before);
        File.WriteAllText(path, after);
    }

    private protected Task<ScreenplayRenderPlan> PlanContribution(params string[] scopes) => new ScreenplayPlanning().Plan(
        new(_folder, ScreenCompositionCorpus.V1.ApplicationName, "cratis") { Scope = scopes }, CancellationToken.None);
}
