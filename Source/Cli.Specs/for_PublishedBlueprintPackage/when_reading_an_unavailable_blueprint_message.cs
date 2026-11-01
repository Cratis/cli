// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PublishedBlueprintPackage;

public class when_reading_an_unavailable_blueprint_message : Specification
{
    string? _blueprint;
    string? _otherMessage;

    void Because()
    {
        _blueprint = PublishedBlueprintPackage.UnavailableBlueprintIn("Blueprint 'Cratis.Blueprint.Invoicing' referenced by template 'InvoicingDueDates' is not available");
        _otherMessage = PublishedBlueprintPackage.UnavailableBlueprintIn("Template 'InvoicePage' is not provided by blueprint 'Cratis.Blueprint.Invoicing'");
    }

    [Fact] void should_read_the_blueprint_name() => _blueprint.ShouldEqual("Cratis.Blueprint.Invoicing");
    [Fact] void should_ignore_other_messages() => _otherMessage.ShouldBeNull();
}
