// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedResourceSources.given;

/// <summary>
/// Base context holding an intermediate output folder as a build leaves it — the assembly the project compiles
/// into, the strongly typed resource sources generated next to it, and a generated source that is not one of them.
/// </summary>
public class an_intermediate_output_folder : Specification
{
    protected string _folder;
    protected string _intermediate;
    protected string _assemblyPath;
    protected string _commonMessages;
    protected string _adminMessages;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _intermediate = Directory.CreateDirectory(Path.Combine(_folder, "obj", "Debug", "net10.0")).FullName;
        _assemblyPath = Path.Combine(_intermediate, "MyApp.dll");

        _adminMessages = Write("AdminMessages");
        _commonMessages = Write("CommonMessages");
        File.WriteAllText(Path.Combine(_intermediate, "MyApp.AssemblyInfo.cs"), "// Not a resource class");
    }

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    string Write(string name)
    {
        var file = Path.Combine(_intermediate, $"{name}.Designer.cs");
        File.WriteAllText(file, $"namespace MyApp.Resources;\n\npublic static class {name}\n{{\n}}\n");
        return file;
    }
}
