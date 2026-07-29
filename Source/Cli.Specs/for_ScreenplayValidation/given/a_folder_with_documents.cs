// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.given;

/// <summary>
/// Base context that puts documents in a temporary folder and compiles them with the real Screenplay compiler.
/// </summary>
public class a_folder_with_documents : Specification
{
    protected const string ValidSource = "domain Library\n\nmodule Library\n";
    protected const string InvalidSource = "domain Library\n\nmodule Library\n  feature Lending\n    slice Reserving\n";

    protected string _folder;
    protected ScreenplayValidation _validation;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _validation = new ScreenplayValidation();
    }

    /// <summary>
    /// Writes a document into the folder.
    /// </summary>
    /// <param name="name">The file name to write, relative to the folder.</param>
    /// <param name="source">The source to write.</param>
    /// <returns>The full path of the written document.</returns>
    protected string WriteDocument(string name, string source)
    {
        var path = Path.Combine(_folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
        return path;
    }

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
