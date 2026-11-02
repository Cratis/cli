// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>What a rule's applyTo patterns target.</summary>
/// <param name="Languages">The recognized languages at least one pattern targets.</param>
/// <param name="Documentation">Whether at least one pattern targets documentation files.</param>
/// <param name="Universal">Whether at least one pattern is not specific to a recognized language or documentation.</param>
record AiRuleApplicability(IReadOnlySet<string> Languages, bool Documentation, bool Universal);
