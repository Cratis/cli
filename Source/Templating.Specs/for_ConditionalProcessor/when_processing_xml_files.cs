// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_processing_xml_files : a_processor
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["IndividualLocalAuth"] = "true" };
    string _result = null!;

    void Because() => _result = Process("        <root>\n        <!--#if (IndividualLocalAuth) -->\n          <SomeXmlHere>true</SomeXmlHere>\n        <!--#endif -->\n        </root>",
        XmlConfig,
        _scope);

    [Fact] void should_keep_the_element_when_the_condition_is_true() => _result.ShouldContain("<SomeXmlHere>true</SomeXmlHere>");
    [Fact] void should_remove_the_directive_lines() => _result.ShouldNotContain("#if");
}
