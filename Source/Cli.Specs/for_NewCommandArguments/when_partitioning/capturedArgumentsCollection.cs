// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Cli.for_NewCommandArguments.when_partitioning;

/// <summary>
/// The partitioner reports through a process-wide capture; these specs must not run in parallel.
/// </summary>
[CollectionDefinition(nameof(CapturedArgumentsCollection))]
public sealed class CapturedArgumentsCollection;
