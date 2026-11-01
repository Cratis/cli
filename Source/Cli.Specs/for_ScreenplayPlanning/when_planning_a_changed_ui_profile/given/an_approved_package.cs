// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile.given;

/// <summary>
/// The corpus with its <c language="csharp">ui profile</c> naming a custom package, and the approved package set that supplies it.
/// </summary>
public class an_approved_package : the_screen_composition_corpus
{
    protected const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [\"@acme/scene.charts/styles.css\"] } }" +
        "] }";

    protected ApprovedPackageSet _approved = null!;

    void Establish()
    {
        ChangeApplication("^( +)Cratis\\.Components$", "$1Cratis.Components\n$1Acme.Charts");
        _approved = ApprovedPackageSetJson.Read(Document, Stage.Rendering.Cratis.CratisRendering.ShippedPackageNames).Set!;
    }
}
