// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The key pair AuthProxy signs onboarding assertions with and Ante verifies them with. It is generated for one run, kept in files only the user can read,
/// and deleted when the run ends, so nothing signed in one run is trusted in another.
/// </summary>
/// <param name="KeyId">The key identifier written on every assertion.</param>
/// <param name="PrivatePem">The private key. Given to AuthProxy only, as a file.</param>
/// <param name="PublicPem">The public key. Given to Ante as the one it trusts.</param>
public sealed record AttestationKey(string KeyId, string PrivatePem, string PublicPem)
{
    /// <summary>
    /// Generates a 2048-bit RSA key pair.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The key pair.</returns>
    public static AttestationKey Generate(string keyId)
    {
        using var rsa = RSA.Create(2048);
        return new(keyId, rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    /// <inheritdoc/>
    public override string ToString() => $"AttestationKey {{ KeyId = {KeyId} }}";
}
