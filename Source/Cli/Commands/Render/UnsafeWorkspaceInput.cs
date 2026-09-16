// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// The exception that is thrown when workspace input cannot be admitted as a regular file.
/// </summary>
/// <param name="message">The admission failure.</param>
/// <param name="innerException">The underlying platform failure, if any.</param>
internal sealed class UnsafeWorkspaceInput(string message, Exception? innerException = null) : IOException(message, innerException);
