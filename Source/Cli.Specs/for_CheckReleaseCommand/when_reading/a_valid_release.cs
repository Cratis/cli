// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_valid_release : given.a_release_file
{
    void Because() => Read(Valid);

    [Fact] void should_be_valid() => _valid.ShouldBeTrue();
    [Fact] void should_read_the_client() => _release!.ClientVersion.ShouldEqual("19.181.3");
    [Fact] void should_read_the_descriptor_set() => _release!.DescriptorSet.ToArray().ShouldEqual([1, 2, 3]);
    [Fact] void should_read_the_event_types() => _release!.EventTypes.Single().Type.Id.ShouldEqual("BookAdded");
    [Fact] void should_not_invent_provenance() => _release!.Environment.ShouldBeNull();
}
