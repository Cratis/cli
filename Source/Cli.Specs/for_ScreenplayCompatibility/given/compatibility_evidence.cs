// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.given;

public class compatibility_evidence : Specification
{
    protected sealed class a_provider(string name, string version) : IScreenplaySourceProvider
    {
        public string Name => name;
        public string Version => version;
        public IReadOnlyList<string> Supersedes => [];
        public bool RequiresSingleHost => true;
        public bool Matches(LoadedCompilation loaded) => false;
        public LoadedCompilation SelectFrom(LoadedCompilation loaded) => loaded;
        public GeneratedScreenplay GenerateFrom(LoadedCompilation loaded, string targetPath, ScreenplayGenerationOptions options) =>
            GeneratedScreenplay.Failed("SPEC", "Not used by compatibility specs");
    }

    protected static LoadedCompilation LoadedWith(params ResolvedScreenplayPackage[] packages) =>
        new([], [], [])
        {
            ProjectProvenance =
            [
                new ScreenplayProjectProvenance(
                    "Application",
                    "net9.0",
                    packages,
                    [new ScreenplayAssemblyIdentity("Marten", "9.0.0.0")],
                    ["marten.event-projection"])
            ]
        };
}
