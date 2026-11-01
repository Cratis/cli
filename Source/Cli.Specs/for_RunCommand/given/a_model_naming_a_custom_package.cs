// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.given;

#pragma warning disable MA0136 // Fixture line endings do not affect the compiled model.

/// <summary>
/// A model whose ui profile activates the custom package <c language="csharp">Acme.Charts</c> and a screen that places one of its components,
/// next to the approved package set and the browser build of the package that make it renderable.
/// </summary>
public class a_model_naming_a_custom_package : a_run_command
{
    /// <summary>
    /// The approved package set document.
    /// </summary>
    protected const string PackageSetDocument =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [\"@acme/scene.charts/styles.css\"] } }" +
        "] }";

    /// <summary>
    /// Gets the folder that holds the approved package set and the folder of each package.
    /// </summary>
    protected string _approved;

    /// <summary>
    /// Gets the path of the approved package set document.
    /// </summary>
    protected string _packageSet;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_folder, "application.play"), Application);
        Directory.CreateDirectory(Path.Combine(_folder, "Workspaces"));
        File.WriteAllText(Path.Combine(_folder, "Workspaces", "Workspaces.play"), Workspaces);

        _approved = Directory.CreateTempSubdirectory("cratis-run-approved-").FullName;
        _packageSet = Path.Combine(_approved, "approved-packages.json");
        File.WriteAllText(_packageSet, PackageSetDocument);
    }

    void Destroy()
    {
        if (Directory.Exists(_approved))
        {
            Directory.Delete(_approved, true);
        }
    }

    /// <summary>
    /// Adds the browser build folder of the approved package.
    /// </summary>
    protected void AddPackageFolder() => Directory.CreateDirectory(Path.Combine(_approved, "@acme", "scene.charts"));

    const string Application = """
        concept WorkItemId : Uuid

        layout AppShell
          topbar
          content

          arrangement flow
            column
              topbar height 56
              content grow

        ui profile Desktop
          target platform web
          target size expanded
          layout AppShell
          packages
            core
            Cratis.Components
            Acme.Charts
        """;

    const string Workspaces = """
        module Workspaces
          feature Tracking
            slice StateView WorkItemList
              readmodel WorkItemSummary
                workItemId WorkItemId
                title String

              query AllWorkItems => observable WorkItemSummary[]

              projection WorkItemList => WorkItemSummary
                from WorkItemCreated
                  workItemId = $eventSourceId
                  title = title

              screen WorkItemList
                template MasterDetail
                  list
                    data WorkItemSummary[] via query AllWorkItems
                    table WorkItemSummary
                      column title label "Title"
                  details
                    component Acme.Charts.chart salesChart
                      property title = "Sales by week"
        """;
}
