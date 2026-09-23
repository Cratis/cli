// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_is_a_well_known_config_name : Specification
{
    [Fact] void should_detect_nuget_config_as_xml_family() => FileFamilies.Detect("nuget.config").ShouldEqual(FileFamily.Xml);
    [Fact] void should_detect_gitignore_as_single_hash_family() => FileFamilies.Detect(".gitignore").ShouldEqual(FileFamily.SingleHash);
    [Fact] void should_detect_dockerfile_as_single_hash_family() => FileFamilies.Detect("Dockerfile").ShouldEqual(FileFamily.SingleHash);
}
