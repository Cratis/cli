// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ReleaseBaselineFile.given;

public class a_baseline_file : Specification
{
    protected string _folder = null!;
    protected string _path = null!;
    protected ApplicationIdentity _application;
    protected ReleaseBaselineRead _result = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-baseline-file-{Guid.NewGuid():N}")).FullName;
        _path = Path.Combine(_folder, ReleaseBaselineStore.FileName);
        _application = ApplicationIdentity.Create("App");
    }

    void Destroy() => Directory.Delete(_folder, true);
}
