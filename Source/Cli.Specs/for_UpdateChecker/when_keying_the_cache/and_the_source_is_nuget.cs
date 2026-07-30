// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_keying_the_cache;

public class and_the_source_is_nuget : Specification
{
    string _result = null!;

    void Because() => _result = UpdateChecker.CacheKeyFor(LatestVersionSource.NuGet, UpdateChecker.CliPackageId);

    [Fact] void should_key_by_the_package() => _result.ShouldEqual(UpdateChecker.CliPackageId);
}
