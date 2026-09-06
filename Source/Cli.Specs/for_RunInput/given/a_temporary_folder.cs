// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.given;

public class a_temporary_folder : Specification
{
    protected string _folder;

    void Establish() => _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;

    void Destroy() => Directory.Delete(_folder, true);
}
