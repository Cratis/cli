// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The key derivation parameters recorded in the environment file, so they can change later.
/// </summary>
/// <param name="Algorithm">The algorithm name.</param>
/// <param name="Iterations">The iteration count.</param>
public record KdfParameters(string Algorithm, int Iterations)
{
    /// <summary>
    /// The PBKDF2 with SHA-256 algorithm name.
    /// </summary>
    public const string Pbkdf2Sha256 = "pbkdf2-sha256";

    /// <summary>
    /// Gets the parameters new environments are created with.
    /// </summary>
    public static KdfParameters Default { get; } = new(Pbkdf2Sha256, 600_000);
}
