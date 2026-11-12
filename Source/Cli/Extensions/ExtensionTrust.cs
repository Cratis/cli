// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Asks a person whether to trust a publisher seen for the first time.
/// </summary>
public interface IPublisherPrompt
{
    /// <summary>
    /// Gets a value indicating whether a person can answer.
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Asks whether to trust a publisher.
    /// </summary>
    /// <param name="publisher">The publisher identity.</param>
    /// <param name="fingerprint">The fingerprint of its key, or <see langword="null"/> when the registry vouches for the key.</param>
    /// <param name="resolved">The full reference that needs the publisher.</param>
    /// <returns><see langword="true"/> to trust the publisher.</returns>
    bool Confirm(string publisher, string? fingerprint, string resolved);
}

/// <summary>
/// Decides whether a package may be used: it is signed, the signature is the publisher's and the project allows the publisher.
/// </summary>
/// <param name="allowList">The project's allow-list.</param>
/// <param name="projectRoot">The project root holding <c language="csharp">.cratis</c>, where a newly trusted publisher is recorded.</param>
/// <param name="prompt">Asks for a first-time publisher.</param>
public class ExtensionTrust(ExtensionAllowList allowList, string projectRoot, IPublisherPrompt prompt)
{
    /// <summary>
    /// Checks a package; returns when it may be used.
    /// </summary>
    /// <param name="reference">The reference as the model writes it, for messages.</param>
    /// <param name="package">The package.</param>
    /// <param name="lockedPublisher">The publisher the lock records, or <see langword="null"/> when the package is not locked yet.</param>
    /// <exception cref="ExtensionFailure">Thrown when the package may not be used.</exception>
    public void Check(string reference, RegistryPackage package, string? lockedPublisher)
    {
        var signature = package.Signature ?? throw new ExtensionFailure(ExtensionFailureKind.NotSigned, reference, $"'{reference}' {package.Version} is not signed, so it cannot be used.");
        if (!signature.Verifies() || !signature.BindsTo(package))
        {
            throw new ExtensionFailure(ExtensionFailureKind.SignatureInvalid, reference, $"The signature of '{reference}' {package.Version} is not valid for {package.Digest}.");
        }

        if (lockedPublisher is not null && lockedPublisher != signature.Publisher && lockedPublisher != VendoredExtensionStore.Publisher)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Mismatched, reference, $"'{reference}' {package.Version} is signed by '{signature.Publisher}' but the lock records '{lockedPublisher}'.");
        }

        var entry = allowList.Find(signature.Publisher);
        if (entry is null)
        {
            Introduce(reference, package, signature);
            return;
        }

        if (signature.Anchor == SignatureAnchor.Embedded && entry.Key != signature.Fingerprint)
        {
            throw new ExtensionFailure(ExtensionFailureKind.SignatureInvalid, reference, $"'{signature.Publisher}' signed '{reference}' {package.Version} with key {signature.Fingerprint}, not the key the allow-list records. Review the change and update {ExtensionAllowList.FileName} if it is expected.");
        }

        if (!entry.Covers(package.Resolved))
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unallowed, reference, $"'{signature.Publisher}' is not allowed to supply '{package.Resolved}'. Add the reference to its entry in .cratis/{ExtensionAllowList.FileName}.");
        }
    }

    void Introduce(string reference, RegistryPackage package, SignatureEvidence signature)
    {
        var fingerprint = signature.Anchor == SignatureAnchor.Embedded ? signature.Fingerprint : null;
        if (!prompt.IsInteractive)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unallowed, reference, $"'{signature.Publisher}' is not on the project's allow-list and this run cannot ask. Add it to .cratis/{ExtensionAllowList.FileName}, or run the command interactively.");
        }

        if (!prompt.Confirm(signature.Publisher, fingerprint, package.Resolved))
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unallowed, reference, $"'{signature.Publisher}' was not trusted for '{reference}'.");
        }

        allowList.Add(projectRoot, new(signature.Publisher, fingerprint, null));
    }
}
