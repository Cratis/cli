// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The exception that is thrown when a resolved validation gate contains an unrecognized check.
/// </summary>
/// <param name="selection">The unrecognized check name.</param>
internal sealed class UnrecognizedValidationGateChecks(string selection) : Exception($"Resolved validation gate contains unrecognized check '{selection}'.");
