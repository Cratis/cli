// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSource.given;

/// <summary>A scratch folder, removed again after each spec.</summary>
public class a_temporary_root : Specification
{
    protected string _root;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), "cratis-specs-aicorpussource", Guid.NewGuid().ToString("N"));

    void Destroy()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
