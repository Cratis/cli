// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Render;

/// <summary>Resolves the bodies emitted by the exact document-set compilation for Stage's attachment envelope.</summary>
internal static class RenderImplementationBodies
{
    /// <summary>Returns only bodies whose file contents or unambiguous inline source are available.</summary>
    /// <param name="documents">The exact source set and its resolved attachment contents.</param>
    /// <param name="requirements">Requirements from this source set's compilation.</param>
    /// <returns>The resolved bodies keyed by requirement identity.</returns>
    // Mirrors Stage 4.17.0 Source/Contracts/Semantics/SemanticModelLoader.cs body resolution.
    // Its resolver is private; LoadFromPathAsync uses different document keys and throws on errors,
    // so the CLI must resolve against its own exact compiled document set and preserve diagnostics.
    public static ImmutableDictionary<string, string> Resolve(
        SemanticDocumentSet documents,
        ImmutableArray<SemanticImplementationRequirement> requirements)
    {
        var inlineBodies = new Dictionary<(DocumentId Document, int Line, int Column), List<string>>();
        var byPath = documents.Documents.ToDictionary(document => document.DisplayPath, StringComparer.Ordinal);

        // A document an import places in a module or feature is not a whole document on its own,
        // so the set is assembled the way a folder is and each body is found where it was written.
        var assembled = new PlayFileCompiler(new DocumentSetPlayFiles(documents), new ScreenplayCompiler()).CompileFolder(".").Result.Value;
        var collector = new InlineBodies();
        if (assembled is not null)
        {
            collector.VisitApplication(assembled);
        }
        else
        {
            foreach (var document in documents.Documents)
            {
                if (new ScreenplayCompiler().Parse(document.Text, document.DisplayPath).Value is { } syntax)
                {
                    collector.VisitApplication(syntax);
                }
            }
        }

        foreach (var block in collector.Bodies)
        {
            if (block.Location.Path is not { } path || !byPath.TryGetValue(path, out var document))
            {
                continue;
            }

            var key = (document.Id, block.Location.Line, block.Location.Column);
            if (!inlineBodies.TryGetValue(key, out var matches))
            {
                inlineBodies[key] = matches = [];
            }

            matches.Add(block.Code);
        }

        var bodies = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var requirement in requirements)
        {
            if (requirement.File is { } file)
            {
                if (AttachmentFiles.TryNormalize(file, out var key, out _) &&
                    documents.AttachmentContents.TryGetValue(key, out var content))
                {
                    bodies[requirement.RequirementId] = content;
                }

                continue;
            }

            if (inlineBodies.TryGetValue(
                (requirement.Source.Span.Document, requirement.Source.Span.StartLine, requirement.Source.Span.StartColumn),
                out var matches) && matches.Count == 1)
            {
                bodies[requirement.RequirementId] = matches[0];
            }
        }

        return bodies.ToImmutable();
    }

    sealed class InlineBodies : ScreenplaySyntaxWalker
    {
        public List<CodeBlockSyntax> Bodies { get; } = [];

        public override void VisitCodeBlock(CodeBlockSyntax syntax)
        {
            Bodies.Add(syntax);
            base.VisitCodeBlock(syntax);
        }
    }
}
