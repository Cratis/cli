// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Remove unused private members - methods used by Specification framework via reflection
#pragma warning disable RCS1213 // Remove unused method declaration - methods used by Specification framework via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

public class when_rendering_renames_and_exclusions : a_conformance_spec
{
    InstantiationResult? _modern;
    InstantiationResult? _classic;

    void Because()
    {
        _modern = Instantiate("ConformanceRenames", "MyApp");
        _classic = Instantiate("ConformanceRenames", "MyApp", new Dictionary<string, string> { ["Style"] = "classic" });
    }

    [Fact]
    void should_apply_the_rename_pattern_to_paths() =>
        File.Exists(Path.Combine(_modern!.OutputRoot, "Renamed", "file.cs")).ShouldBeTrue();

    [Fact]
    void should_exclude_the_excluded_directory() =>
        File.Exists(Path.Combine(_modern!.OutputRoot, "excluded", "secret.txt")).ShouldBeFalse();

    [Fact]
    void should_keep_the_directory_for_the_active_modifier_condition() =>
        File.Exists(Path.Combine(_modern!.OutputRoot, "modern-only", "feature.cs")).ShouldBeTrue();

    [Fact]
    void should_exclude_the_directory_when_the_modifier_condition_is_false() =>
        File.Exists(Path.Combine(_classic!.OutputRoot, "modern-only", "feature.cs")).ShouldBeFalse();

    [Fact]
    void should_copy_copy_only_files_byte_exact()
    {
        var bytes = File.ReadAllBytes(Path.Combine(_modern!.OutputRoot, "Assets", "binary.asset"));
        (bytes.Length == 8 && bytes[0] == (byte)'B' && bytes[3] == 0 && bytes[4] == (byte)'D').ShouldBeTrue();
    }

    [Fact]
    void should_never_emit_the_template_config_directory() =>
        File.Exists(Path.Combine(_modern!.OutputRoot, ".template.config", "template.json")).ShouldBeFalse();
}
