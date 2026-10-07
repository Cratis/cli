// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Binds the documents a validation compiled into an executable semantic model, without rendering or writing anything.
/// </summary>
/// <remarks>
/// Binding readiness is a Screenplay verdict, independent of any renderer: a model that binds can still be
/// refused by a target that does not admit its constructs. The documents are bound exactly as
/// <c language="shell">cratis render</c> binds them, including their implementation attachments.
/// </remarks>
/// <param name="compiler">Compiles the documents through to the executable semantic model.</param>
public sealed class ScreenplayBinding(ISemanticModelCompiler compiler)
{
    /// <summary>
    /// The stable key the application is identified by while binding; validation has no application name of its own.
    /// </summary>
    public const string ApplicationName = "Application";

    /// <summary>
    /// The diagnostic code reported for a document imported from above the model root, which cannot be bound.
    /// </summary>
    public const string OutsideRootCode = "CLI-VALIDATE-001";

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayBinding"/> class with the bundled compiler.
    /// </summary>
    public ScreenplayBinding()
        : this(new SemanticModelCompiler())
    {
    }

    /// <summary>
    /// Binds compiled documents.
    /// </summary>
    /// <param name="root">The folder the documents' relative paths are relative to.</param>
    /// <param name="sources">The documents the validation compiled.</param>
    /// <returns>Whether the model binds, and every diagnostic binding and attachment loading reported.</returns>
    public (bool Executable, IReadOnlyList<ScreenplayDiagnostic> Diagnostics) Bind(string root, IEnumerable<PlayFileSource> sources)
    {
        var compiled = sources.ToArray();

        // Binding, like rendering, reads documents and their attachments only from beneath the model root. An import that
        // climbs above it is valid source, but binding it would widen the folder attachments may be read from.
        var outside = compiled.Where(source => source.File.RelativePath.Replace('\\', '/').Split('/')[0] == "..").ToArray();
        if (outside.Length > 0)
        {
            return (false, [.. outside.Select(source => new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Error,
                OutsideRootCode,
                $"'{source.File.RelativePath}' is imported from above the model root '{root}', so the model cannot be bound from here. Validate the folder that holds every document.",
                null))]);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName));
        var documents = compiled.Select(source => Document(catalog, source)).ToImmutableArray();
        var attachments = AttachmentFiles.Load(root, documents);
        var compilation = compiler.Compile(ApplicationName, SemanticDocumentSet.Create(documents, catalog, attachments.Contents));
        var diagnostics = compilation.Diagnostics.Concat(attachments.Diagnostics).Select(Map).ToArray();
        return (compilation.Success, diagnostics);
    }

    static SemanticSourceDocument Document(SemanticIdentityCatalog catalog, PlayFileSource source)
    {
        var relativePath = source.File.RelativePath.Replace('\\', '/');

        // A file without a persisted document identity gets an opaque key from its portable relative path, as rendering does.
        var key = $"file-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(relativePath.Normalize(NormalizationForm.FormC))))}";
        return SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, relativePath, source.Source);
    }

    static ScreenplayDiagnostic Map(Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Location.Path is null ? null : $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column})");
}
