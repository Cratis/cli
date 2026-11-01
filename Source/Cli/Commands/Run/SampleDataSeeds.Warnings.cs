// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.Commands.Run;

internal static partial class SampleDataSeeds
{
    static void WarnUnsupported(ReadModelSyntax model, ProjectionSyntax projection, string target, List<SampleDataSeedWarning> warnings)
    {
        if (projection.Key is not null)
        {
            Warn(warnings, $"{target} uses a projection key: only default event source keys are seeded.", projection.Location);
        }

        foreach (var property in model.Properties.Where(property => property.Type.IsCollection))
        {
            Warn(warnings, $"{target}.{property.Name} stays empty: sample data cannot give rows to a collection property yet (Cratis/Screenplay#1000).", property.Location);
        }

        foreach (var block in projection.Blocks)
        {
            if (block is ChildrenSyntax children)
            {
                if (!model.Properties.Any(property => property.Type.IsCollection && property.Name == children.Property))
                {
                    Warn(warnings, $"{target}.{children.Property} stays empty: sample data cannot give rows to a collection property yet (Cratis/Screenplay#1000).", block.Location);
                }
            }
            else if (block is not FromSyntax from)
            {
                Warn(warnings, $"{target} uses {block.GetType().Name}: only scalar 'from' mappings are seeded.", block.Location);
            }
            else if (from.Key is not null || from.ParentKey is not null || from.Events.Any(spec => spec.Key is not null))
            {
                Warn(warnings, $"{target} uses a projection key: only default event source keys are seeded.", block.Location);
            }
            else
            {
                foreach (var mapping in from.Mappings)
                {
                    if (mapping is not SetMappingSyntax set || !Supported(set.Source))
                    {
                        Warn(warnings, $"{target}.{mapping.Property} has a non-invertible projection expression; only scalar property mappings, literals and $eventSourceId are seeded.", mapping.Location);
                    }
                }
            }
        }
    }

    static bool Supported(ExpressionSyntax expression) => expression is LiteralExpressionSyntax or EventSourceIdExpressionSyntax || (expression is PathExpressionSyntax path && !path.Path.Contains('.', StringComparison.Ordinal));
}
