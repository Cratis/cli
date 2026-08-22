// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPackageProvenance.when_reading_packages;

public class and_the_assets_file_has_several_targets : Specification
{
    string _folder;
    IReadOnlyList<ResolvedScreenplayPackage> _result;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        File.WriteAllText(
            Path.Combine(_folder, ProjectRestoreState.AssetsFileName),
            string.Join('\n',
            [
                "{",
                "  \"targets\": {",
                "    \"net8.0\": {",
                "      \"Marten/8.0.0\": { \"type\": \"package\" }",
                "    },",
                "    \"net9.0\": {",
                "      \"WolverineFx.Marten/6.23.1\": { \"type\": \"package\" },",
                "      \"Unrelated/1.0.0\": { \"type\": \"package\" },",
                "      \"Marten/9.20.0\": { \"type\": \"package\" },",
                "      \"WolverineFx/6.23.1\": { \"type\": \"package\" }",
                "    }",
                "  }",
                "}"
            ]));
    }

    void Because() => _result = ScreenplayPackageProvenance.PackagesFrom(
        Path.Combine(_folder, ProjectRestoreState.AssetsFileName),
        "net9.0");

    [Fact] void should_read_only_the_selected_target() => _result.ShouldNotContain(new ResolvedScreenplayPackage("Marten", "8.0.0"));
    [Fact] void should_read_only_source_framework_packages() => _result.ShouldNotContain(new ResolvedScreenplayPackage("Unrelated", "1.0.0"));
    [Fact] void should_return_resolved_packages_in_stable_order() => _result.ShouldContainOnly(
        [
            new ResolvedScreenplayPackage("Marten", "9.20.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.23.1"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.23.1")
        ]);

    void Destroy() => Directory.Delete(_folder, true);
}
