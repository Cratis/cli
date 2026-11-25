// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Thrown when affected-test computation cannot proceed with the information it was given.
/// Callers treat it as a reason to run everything, never as a reason to skip work.
/// </summary>
/// <param name="message">What went wrong.</param>
/// <param name="innerException">The underlying failure, when there is one.</param>
public class AffectedException(string message, Exception? innerException = null) : Exception(message, innerException);
