// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.for_ScreenplayConformance.given;

public class a_structural_comparison : Specification
{
    protected const string Source = "domain Library\nmodule Library\n  feature Registration\n    slice StateChange Register\n      command Register\n        id Uuid identifier\n        name String\n        produces Registered\n          for id\n          name = name\n      event Registered\n        name String\n";
    protected string _before = Source;
    protected string _after = Source;
    protected ModelDifference _difference = null!;
    protected IReadOnlyList<ConformanceFinding> _findings = [];
    protected int _exitCode;

    protected void Compare()
    {
        _difference = ModelComparison.Compare(ComparedModel.FromSources("Library", new Dictionary<string, string> { ["model.play"] = _before }), ComparedModel.FromSources("Library", new Dictionary<string, string> { ["code.play"] = _after }));
        _findings = ScreenplayConformance.Classify(_difference);
        _exitCode = ScreenplayConformance.ExitCode(_difference, _findings);
    }
}
