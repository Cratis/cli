// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Represents the documents a view shows, where they came from, and what finding them reported.
/// </summary>
/// <param name="Origin">Where the documents came from.</param>
/// <param name="Resources">The resources holding the documents - one per project; empty when none were found.</param>
/// <param name="Diagnostics">Everything finding or generating the documents reported.</param>
/// <param name="Owner">What has to stay alive for as long as the resources are read, if anything.</param>
public sealed record ViewDocuments(
    ViewOrigin Origin,
    IReadOnlyList<IEventModelResources> Resources,
    IReadOnlyList<ViewDiagnostic> Diagnostics,
    IDisposable? Owner = null) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose() => Owner?.Dispose();

    /// <summary>
    /// Finds the documents of a project: the ones its output embeds when it has been built with them, otherwise
    /// ones generated from its source.
    /// </summary>
    /// <param name="projectFile">The full path of the project file.</param>
    /// <param name="output">What the project builds.</param>
    /// <param name="fromSource">Whether to generate from source even when the output embeds documents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="ViewDocuments"/>.</returns>
    /// <remarks>
    /// Embedded documents win, because they are what the application was built with and what it serves itself.
    /// Generating is the fallback for an application that does not embed them - or has not been built at all,
    /// since generating reads the source rather than the output.
    /// </remarks>
    public static async Task<ViewDocuments> For(string projectFile, ProjectOutput output, bool fromSource, CancellationToken cancellationToken)
    {
        if (!fromSource && output.IsBuilt)
        {
            var embedded = EmbeddedEventModels.From(output.TargetPath);
            if (embedded.Any)
            {
                return new(ViewOrigin.Embedded, embedded.Resources, [], embedded);
            }

            embedded.Dispose();
        }

        var (resources, diagnostics) = await GeneratedEventModels.Generate(projectFile, output, cancellationToken);
        return new(ViewOrigin.Generated, resources is null ? [] : [resources], diagnostics);
    }
}
