// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.given;

public class a_temp_project : Specification
{
    protected static readonly KdfParameters FastKdf = new(KdfParameters.Pbkdf2Sha256, 1000);

    protected string _root;
    protected ProjectEnvironments _environments;
    string? _previousEnvironment;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "cratis-env-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _environments = ProjectEnvironments.ForProject(_root);
        _previousEnvironment = Environment.GetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable);
        Environment.SetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable, null);
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable(ProjectEnvironments.EnvironmentVariable, _previousEnvironment);
        Directory.Delete(_root, recursive: true);
    }

    protected string FileText(string name) => File.ReadAllText(_environments.PathFor(name));
}
