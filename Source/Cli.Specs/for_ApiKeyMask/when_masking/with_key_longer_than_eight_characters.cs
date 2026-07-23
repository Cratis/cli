// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ApiKeyMask.when_masking;

public class with_key_longer_than_eight_characters : Specification
{
    string _result;

    void Because() => _result = ApiKeyMask.Mask("sk-ant-api03-secret");

    [Fact] void should_reveal_only_the_first_and_last_four_characters() => _result.ShouldEqual("sk-a...cret");
}
