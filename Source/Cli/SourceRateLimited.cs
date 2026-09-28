// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// The exception that is thrown when a version source refuses a request because its rate limit is spent.
/// </summary>
/// <param name="source">The source that refused the request.</param>
public class SourceRateLimited(string source) : Exception($"'{source}' refused the request because its rate limit is spent.");
