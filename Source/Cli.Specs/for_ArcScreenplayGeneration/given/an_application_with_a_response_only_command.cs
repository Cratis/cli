// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ArcScreenplayGeneration.given;

public class an_application_with_a_response_only_command : an_application_built_from_source
{
    void Establish()
    {
        var source = CSharpSyntaxTree.ParseText(
            string.Join('\n',
                "namespace Bookshop.Lending.Checking;",
                string.Empty,
                "[Cratis.Arc.Commands.ModelBound.Command]",
                "public record CheckAvailability(string BookId)",
                "{",
                "    public string Handle() => BookId;",
                "}"),
            path: "Checking/CheckAvailability.cs");
        var compilation = Loaded.Compilations.Single().AddSyntaxTrees(source);
        Loaded = new([compilation], [ProjectName], []);
    }
}
