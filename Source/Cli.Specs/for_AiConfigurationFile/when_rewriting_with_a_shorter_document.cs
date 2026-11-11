// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_AiConfigurationFile;

public class when_rewriting_with_a_shorter_document : Specification
{
    const string Replacement = "{\"theme\":\"light\"}\n";
    string _folder;
    string _path;
    byte[] _original;

    void Establish()
    {
        _folder = Path.Combine(AiPhysicalRoot.Resolve(Path.GetTempPath()), $"cratis-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "config.json");
        _original = Encoding.UTF8.GetBytes("{\"theme\":\"dark\",\"description\":\"a longer existing document\"}\n");
        File.WriteAllBytes(_path, _original);
    }

    void Because() => AiConfigurationFile.Write(_path, Replacement, _original, null, null, null, () => { });

    [Fact] void should_replace_the_entire_document() => File.ReadAllText(_path).ShouldEqual(Replacement);

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
