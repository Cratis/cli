// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The folder of one build, outside the project: the order, the outcome the agent writes, and the order's Screenplay
/// documents for the Screenplay tools.
/// </summary>
/// <param name="Root">The folder.</param>
public sealed record BuildWorkspace(string Root)
{
    /// <summary>Gets the path of the order.</summary>
    public string Order => Path.Combine(Root, "order.json");

    /// <summary>Gets the path the agent writes its outcome to.</summary>
    public string Outcome => Path.Combine(Root, "outcome.json");

    /// <summary>Gets the folder holding the order's Screenplay documents.</summary>
    public string Model => Path.Combine(Root, "model");

    /// <summary>
    /// Gets the workspace of an order without creating it.
    /// </summary>
    /// <param name="workspaceRoot">The folder workspaces are created in.</param>
    /// <param name="order">The order.</param>
    /// <returns>The workspace.</returns>
    public static BuildWorkspace At(string workspaceRoot, BuildOrder order) =>
        new(Path.Combine(workspaceRoot, string.Concat(order.OrderId.Where(character => char.IsAsciiLetterOrDigit(character) || character == '-'))));

    /// <summary>
    /// Creates the workspace of an order afresh, with the order and its Screenplay documents written to it.
    /// </summary>
    /// <param name="workspaceRoot">The folder workspaces are created in.</param>
    /// <param name="order">The order.</param>
    /// <returns>The workspace.</returns>
    public static BuildWorkspace Create(string workspaceRoot, BuildOrder order)
    {
        var workspace = At(workspaceRoot, order);
        if (Directory.Exists(workspace.Root)) Directory.Delete(workspace.Root, recursive: true);
        Directory.CreateDirectory(workspace.Model);
        File.WriteAllText(workspace.Order, order.Document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        foreach (var document in order.Documents.Where(document => document.Path.Length > 0))
        {
            BuildFiles.WriteDocument(workspace.Model, document);
        }

        return workspace;
    }
}
