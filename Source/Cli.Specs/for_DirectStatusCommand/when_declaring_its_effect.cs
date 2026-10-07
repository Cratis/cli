// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.Registration;

namespace Cratis.Cli.for_DirectStatusCommand;

public class when_declaring_its_effect : Specification
{
    CommandEffect _effect;

    void Because() => _effect = typeof(DirectStatusCommand).GetCustomAttribute<CommandEffectAttribute>()!.Effect;

    [Fact] void should_declare_that_refreshing_may_rotate_tokens() => _effect.ShouldEqual(CommandEffect.Mutating);
}
