// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.given;

public class a_planned_corpus_form : a_screenplay_planning
{
    private protected async Task<ScreenplayRenderPlan> PlanCorpusForm(string applicationName, CanonicalCorpusSourceForm form)
    {
        var path = Path.Combine(_folder, applicationName);
        foreach (var document in form.Documents)
        {
            var documentPath = Path.Combine(path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(documentPath)!);
            await File.WriteAllBytesAsync(documentPath, document.Bytes.AsMemory());
        }

        return await _planning.Plan(new(path, applicationName, CratisRendering.TargetId, null, null), CancellationToken.None);
    }
}
