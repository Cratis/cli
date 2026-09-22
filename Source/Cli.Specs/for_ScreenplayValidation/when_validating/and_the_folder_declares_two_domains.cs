// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_folder_declares_two_domains : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("first.play", "domain Sales\n");
        WriteDocument("second.play", "domain Shipping\n");
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_report_the_duplicate_domain() => _result.Diagnostics.Single().Code.ShouldEqual("PLAY0172");
    [Fact] void should_point_at_the_offending_document() => _result.Diagnostics.Single().Location.ShouldEqual("second.play(1,1)");
}
