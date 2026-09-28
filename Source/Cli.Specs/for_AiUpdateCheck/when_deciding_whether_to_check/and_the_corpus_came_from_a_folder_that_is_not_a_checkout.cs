// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_deciding_whether_to_check;

/// <summary>
/// Without Git the revision is a timestamp, which cannot be compared against a published commit.
/// </summary>
public class and_the_corpus_came_from_a_folder_that_is_not_a_checkout : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.ShouldCheck("2026-07-30T20:00:00.0000000Z", null);

    [Fact] void should_not_check() => _result.ShouldBeFalse();
}
