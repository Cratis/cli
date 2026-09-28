// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.given;

public class a_project : Specification
{
    protected string _project = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
    }

    void Destroy() => Directory.Delete(_project, true);

    protected void Write(string relativePath, string content) =>
        File.WriteAllText(Path.Combine(_project, relativePath), content);
}
