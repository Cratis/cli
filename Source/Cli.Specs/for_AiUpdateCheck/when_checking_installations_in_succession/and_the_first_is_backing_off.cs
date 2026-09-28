// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_installations_in_succession;

/// <summary>
/// A failed comparison backs off only the commit it was made for.
/// </summary>
public class and_the_first_is_backing_off : given.two_installations
{
    AiCorpusUpdate? _second;

    async Task Because()
    {
        await Check(First, null);
        _second = await Check(Second, $"{Second}:2");
    }

    [Fact] void should_compare_the_second_installation() => _compared.ShouldContainOnly(First, Second);
    [Fact] void should_report_the_second_update() => _second.ShouldEqual(new AiCorpusUpdate(Second, 2));
}
