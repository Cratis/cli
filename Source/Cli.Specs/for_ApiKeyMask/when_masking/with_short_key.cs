// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ApiKeyMask.when_masking;

public class with_short_key : Specification
{
    string _result;

    void Because() => _result = ApiKeyMask.Mask("abc");

    [Fact] void should_fully_mask_the_key() => _result.ShouldEqual("********");
    [Fact] void should_not_reveal_the_key_length() => _result.Length.ShouldEqual(8);
}
