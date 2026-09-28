// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_installations_in_succession;

/// <summary>
/// A fresh comparison for one commit is not an answer for another, so moving between projects - or running
/// 'cratis ai update' - asks again, and only the latest comparison is kept.
/// </summary>
public class and_both_are_answered : given.two_installations
{
    AiCorpusUpdate? _first;
    AiCorpusUpdate? _second;

    async Task Because()
    {
        _first = await Check(First, $"{First}:3");
        _second = await Check(Second, $"{Second}:1");
    }

    [Fact] void should_compare_each_installation() => _compared.ShouldContainOnly(First, Second);
    [Fact] void should_report_the_first_update() => _first.ShouldEqual(new AiCorpusUpdate(First, 3));
    [Fact] void should_report_the_second_update() => _second.ShouldEqual(new AiCorpusUpdate(Second, 1));
    [Fact] void should_keep_the_latest_comparison() => _cache.Read(AiUpdateCheck.CacheKeyFor(Second))!.LatestVersion.ShouldEqual($"{Second}:1");
    [Fact] void should_drop_the_earlier_comparison() => _cache.Read(AiUpdateCheck.CacheKeyFor(First)).ShouldBeNull();
}
