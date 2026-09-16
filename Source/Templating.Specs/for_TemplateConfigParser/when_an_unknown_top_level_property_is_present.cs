// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Specs.for_TemplateConfigParser.given;

namespace Cratis.Templating.Specs.for_TemplateConfigParser;

public class when_an_unknown_top_level_property_is_present : a_parser
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Parse("{ \"name\": \"X\", \"shortName\": \"x\", \"no-such-property\": true }"));

    [Fact] void should_fail_loudly_naming_the_property() =>
        _error!.Message.ShouldContain("no-such-property");
}
