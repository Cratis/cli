// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPackageProvenance.when_reading_packages;

public class and_the_selected_target_is_not_an_object : Specification
{
    string _assetsFile;
    IReadOnlyList<ResolvedScreenplayPackage> _result;

    void Establish()
    {
        _assetsFile = Path.GetTempFileName();
        File.WriteAllText(_assetsFile, "{ \"targets\": { \"net9.0\": \"not a target graph\" } }");
    }

    void Because() => _result = ScreenplayPackageProvenance.PackagesFrom(_assetsFile, "net9.0");

    [Fact] void should_return_no_provenance() => _result.ShouldBeEmpty();

    void Destroy() => File.Delete(_assetsFile);
}
