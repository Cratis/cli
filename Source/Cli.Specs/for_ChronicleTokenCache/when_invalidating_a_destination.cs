// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_invalidating_a_destination : given.a_token_cache
{
    string _selected;
    string _sibling;

    void Establish()
    {
        _selected = CachePath(Connection(disableTls: true));
        _sibling = CachePath(Connection());
        Seed(_selected);
        Seed(_sibling);
    }

    void Because() => ChronicleTokenCache.Clear(Connection(disableTls: true), Context);

    [Fact] void should_delete_the_selected_destination() => File.Exists(_selected).ShouldBeFalse();
    [Fact] void should_preserve_the_other_destination() => File.Exists(_sibling).ShouldBeTrue();
}
