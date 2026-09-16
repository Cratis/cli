// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_nesting_is_unbalanced : a_processor
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Process("#if (A)\ncontent\n", LanguageConfig));

    [Fact]
    void should_report_the_missing_endif() => _error.ShouldNotBeNull();
}
