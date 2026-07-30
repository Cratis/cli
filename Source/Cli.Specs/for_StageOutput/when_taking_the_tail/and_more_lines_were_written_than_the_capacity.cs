// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageOutput.when_taking_the_tail;

public class and_more_lines_were_written_than_the_capacity : Specification
{
    StageOutput _output;
    IReadOnlyList<string> _result;

    void Establish()
    {
        _output = new(3);
        for (var line = 1; line <= 5; line++)
        {
            _output.Append($"line {line}");
        }
    }

    void Because() => _result = _output.Tail(10);

    [Fact] void should_only_keep_the_last_lines() => string.Join(',', _result).ShouldEqual("line 3,line 4,line 5");
}
