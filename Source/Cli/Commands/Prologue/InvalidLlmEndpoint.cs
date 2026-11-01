// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// The exception that is thrown when a language-model destination cannot be safely identified.
/// </summary>
public class InvalidLlmEndpoint() : Exception("The language-model endpoint must be an absolute HTTP or HTTPS URL with a host. No capture evidence was sent.");
