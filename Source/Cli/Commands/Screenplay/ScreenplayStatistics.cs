// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Counts of what a generated Screenplay document declares.
/// </summary>
/// <param name="Modules">The number of modules the document declares.</param>
/// <param name="Features">The number of features the document declares, including nested sub-features.</param>
/// <param name="Slices">The number of slices the document declares.</param>
/// <param name="Concepts">The number of concepts the document declares.</param>
/// <param name="Policies">The number of authorization policies the document declares.</param>
/// <param name="Commands">The number of commands declared across every slice.</param>
/// <param name="Events">The number of events declared across every slice.</param>
/// <param name="Queries">The number of queries declared across every slice.</param>
/// <param name="Reactors">The number of reactors declared across every slice.</param>
/// <param name="Constraints">The number of constraints declared across every slice.</param>
/// <remarks>
/// The generator's own model is flat — every slice, with no grouping into the modules and features the document
/// actually prints them under. Counting those means reading the structure back the same way a reader would: by
/// compiling the document that was written, with the same <c language="csharp">Cratis.Screenplay</c> compiler <c language="csharp">screenplay validate</c>
/// already uses.
/// </remarks>
public sealed record ScreenplayStatistics(
    int Modules,
    int Features,
    int Slices,
    int Concepts,
    int Policies,
    int Commands,
    int Events,
    int Queries,
    int Reactors,
    int Constraints)
{
    /// <summary>
    /// Gets statistics describing a document that declares nothing.
    /// </summary>
    public static readonly ScreenplayStatistics Zero = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Compiles a generated document and counts what it declares.
    /// </summary>
    /// <param name="source">The generated <c language="csharp">.play</c> source.</param>
    /// <returns>The <see cref="ScreenplayStatistics"/>, or <see cref="Zero"/> when the document could not be read back.</returns>
    public static ScreenplayStatistics For(string source)
    {
        var result = new ScreenplayCompiler().Compile(source);
        if (!result.Success || result.Value is not ApplicationSyntax application)
        {
            return Zero;
        }

        var features = AllFeatures(application.Modules.SelectMany(module => module.Features)).ToArray();
        var slices = features.SelectMany(feature => feature.Slices).ToArray();

        return new(
            application.Modules.Count(),
            features.Length,
            slices.Length,
            application.Concepts.Count(),
            application.Policies.Count(),
            slices.Sum(slice => slice.Commands.Count()),
            slices.Sum(slice => slice.Events.Count()),
            slices.Sum(slice => slice.Queries.Count()),
            slices.Sum(slice => slice.Reactions.Count()),
            slices.Sum(slice => slice.Constraints.Count()));
    }

    /// <summary>
    /// Flattens a feature tree, including every nested sub-feature.
    /// </summary>
    /// <param name="features">The top-level features to flatten.</param>
    /// <returns>Every feature, top-level and nested.</returns>
    static IEnumerable<FeatureSyntax> AllFeatures(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => new[] { feature }.Concat(AllFeatures(feature.Features)));
}
