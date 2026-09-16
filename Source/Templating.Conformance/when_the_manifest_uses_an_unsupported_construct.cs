// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Remove unused private members - methods used by Specification framework via reflection
#pragma warning disable RCS1213 // Remove unused method declaration - methods used by Specification framework via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

public class when_the_manifest_uses_an_unsupported_construct : a_conformance_spec
{
    Exception? _error;

    void Because()
    {
        var root = Path.Combine(Path.GetTempPath(), "cratis-conformance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".template.config"));
        File.WriteAllText(
            Path.Combine(root, ".template.config", "template.json"),
            "{ \"name\": \"Bad\", \"shortName\": \"bad\", \"inventedProperty\": true }");
        _error = Catch.Exception(() => TemplateConfigParser.ParseFile(Path.Combine(root, ".template.config", "template.json")));
    }

    [Fact]
    void should_fail_loudly_with_the_property_named() =>
        _error!.Message.ShouldContain("inventedProperty");
}
