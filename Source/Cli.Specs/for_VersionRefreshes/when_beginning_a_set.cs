// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_VersionRefreshes;

/// <summary>
/// The set a caller begins is the current one for everything it calls, and for nothing outside its chain.
/// </summary>
public class when_beginning_a_set : Specification
{
    VersionRefreshes _begun = null!;
    VersionRefreshes? _seenByCallee;
    VersionRefreshes? _seenByOtherChain;

    async Task Because()
    {
        // Another caller's chain starts before this one begins its set, and keeps running afterwards.
        var otherChain = Task.Run(async () =>
        {
            await Task.Delay(50);
            return VersionRefreshes.Current;
        });

        _begun = VersionRefreshes.Begin();
        _seenByCallee = await Callee();
        _seenByOtherChain = await otherChain;
    }

    static async Task<VersionRefreshes?> Callee()
    {
        await Task.Yield();
        return VersionRefreshes.Current;
    }

    [Fact] void should_make_the_set_current_for_what_the_caller_calls() => _seenByCallee.ShouldEqual(_begun);
    [Fact] void should_not_make_the_set_current_for_another_chain() => _seenByOtherChain.ShouldBeNull();
}
