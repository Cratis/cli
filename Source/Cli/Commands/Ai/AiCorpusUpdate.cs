// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Represents a newer Cratis AI corpus than the one installed in a project.
/// </summary>
/// <param name="InstalledRevision">The corpus commit recorded as installed.</param>
/// <param name="NewCommits">How many commits the default branch of Cratis/AI has gained since.</param>
public sealed record AiCorpusUpdate(string InstalledRevision, int NewCommits);
