// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.given;

public class a_temporary_folder : Specification
{
    protected string _folder;
    protected ViewTarget _result;

    void Establish() => _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    protected string Create(string name)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, "<Project />");
        return path;
    }
}
