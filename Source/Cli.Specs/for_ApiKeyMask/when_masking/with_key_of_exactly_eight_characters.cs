// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ApiKeyMask.when_masking;

public class with_key_of_exactly_eight_characters : Specification
{
    string _result;

    void Because() => _result = ApiKeyMask.Mask("12345678");

    [Fact] void should_fully_mask_the_key() => _result.ShouldEqual("********");
}
