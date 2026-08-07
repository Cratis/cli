// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.given;

/// <summary>
/// A folder holding a project file, with nothing restored into it yet.
/// </summary>
public class a_project_folder : Specification
{
    protected string _folder;
    protected string _project;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _project = Path.Combine(_folder, "MyApp.csproj");
        File.WriteAllText(_project, "<Project />");
    }

    /// <summary>
    /// Writes an assets file into the given folder, as a restore does.
    /// </summary>
    /// <param name="folder">The intermediate output folder to restore into.</param>
    /// <returns>The full path of the assets file.</returns>
    protected static string Restore(string folder)
    {
        Directory.CreateDirectory(folder);
        var assets = Path.Combine(folder, ProjectRestoreState.AssetsFileName);
        File.WriteAllText(assets, "{}");
        return assets;
    }

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
