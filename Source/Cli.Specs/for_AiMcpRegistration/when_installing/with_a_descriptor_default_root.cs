// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_descriptor_default_root : given.a_screenplay_corpus
{
    void Establish()
    {
        var path = Path.Combine(_corpus, ".cratis/ai/mcp-servers.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace(".cratis/screenplay", "models/default", StringComparison.Ordinal));
    }

    void Because() => _result = Install();

    [Fact] void should_use_the_descriptor_default_at_setup() => Directory.Exists(ProjectFile("models/default")).ShouldBeTrue();
    [Fact] void should_use_the_same_default_at_startup() => ScreenplayMcpRoot.Resolve(null, _project, null, _project, _ => null).ShouldEqual(Path.Combine(AiProjectPaths.PhysicalRoot(_project), "models/default"));
}
