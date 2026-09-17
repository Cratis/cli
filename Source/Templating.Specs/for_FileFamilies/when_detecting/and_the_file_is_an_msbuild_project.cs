// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_is_an_msbuild_project : Specification
{
    [Fact] void should_detect_the_msbuild_family_for_csproj() => FileFamilies.Detect("MyApp.csproj").ShouldEqual(FileFamily.MSBuild);
    [Fact] void should_detect_the_msbuild_family_for_props() => FileFamilies.Detect("Directory.Build.props").ShouldEqual(FileFamily.MSBuild);
    [Fact] void should_process_condition_attributes() => FileFamilies.ConfigFor(FileFamily.MSBuild).ProcessMsBuildConditions.ShouldBeTrue();
}
