// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Studio.for_StudioFetchSettings;

public class when_validating : Specification
{
    const string Id = "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f";

    [Fact] void should_accept_each_format_for_a_slice() => StudioFetchSettings.Formats.All(format => new StudioFetchSettings { Context = "slice", Id = Id, Format = format }.Validate().Successful).ShouldBeTrue();
    [Fact] void should_accept_a_domain() => new StudioFetchSettings { Context = "domain", Id = Id }.Validate().Successful.ShouldBeTrue();
    [Fact] void should_accept_an_event_model_with_a_hyphen() => new StudioFetchSettings { Context = "event-model", Id = Id }.Validate().Successful.ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_scope() => new StudioFetchSettings { Context = "module", Id = Id }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_refuse_an_unknown_format() => new StudioFetchSettings { Context = "slice", Id = Id, Format = "xml" }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_refuse_an_identifier_that_is_not_one() => new StudioFetchSettings { Context = "slice", Id = "PayInvoice" }.Validate().Successful.ShouldBeFalse();
}
