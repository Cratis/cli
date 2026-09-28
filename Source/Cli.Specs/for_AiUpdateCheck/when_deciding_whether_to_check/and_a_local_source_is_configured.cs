// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_deciding_whether_to_check;

/// <summary>
/// 'cratis ai update' installs from the local checkout then, so the published corpus says nothing about what it would bring in.
/// </summary>
public class and_a_local_source_is_configured : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.ShouldCheck("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "../AI");

    [Fact] void should_not_check() => _result.ShouldBeFalse();
}
