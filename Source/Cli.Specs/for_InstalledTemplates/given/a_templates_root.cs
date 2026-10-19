// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InstalledTemplates.given;

/// <summary>A scratch templates root, removed again after each spec.</summary>
public class a_templates_root : Specification
{
    protected string _root;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "cratis-specs-installedtemplates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    void Destroy()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
