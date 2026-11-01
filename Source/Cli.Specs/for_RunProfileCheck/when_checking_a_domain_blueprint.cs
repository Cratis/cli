// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RunProfileCheck;

/// <summary>
/// A domain blueprint is approved with the manifest its package publishes, which carries the design-time <c language="json">blueprint</c> and
/// <c language="json">sampleData</c> members. Its dependency on <c language="csharp">Cratis.Blueprint.Default</c> is satisfied by what the Stage ships (Cratis/Stage#457).
/// </summary>
public class when_checking_a_domain_blueprint : Specification
{
    const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Cratis.Blueprint.Crm\", \"version\": \"1.0.0\", \"kind\": \"Blueprint\"," +
        " \"dependencies\": [{ \"name\": \"Cratis.Blueprint.Default\", \"versionRange\": \"^1.0.0\" }]," +
        " \"layouts\": [], \"themes\": [], \"displayName\": \"CRM\", \"module\": \"@cratis/scene.blueprint.crm\"," +
        " \"screenTemplates\": [\"CrmLeadList\"], \"dialogTemplates\": [\"CrmQuickCreateLead\"]," +
        " \"sampleData\": [{ \"id\": \"crm-nordic-smb\", \"name\": \"Nordic SMB\" }]," +
        " \"blueprint\": { \"scenarios\": [] } }," +
        " \"module\": { \"npm\": \"@cratis/scene.blueprint.crm\", \"version\": \"4.53.1\", \"export\": \"crmBlueprint\", \"styles\": [] } }" +
        "] }";

    string _folder;
    ApprovedPackageSetReadResult _read;
    IReadOnlyList<ScreenplayDiagnostic> _unapproved;
    IReadOnlyList<ScreenplayDiagnostic> _approved;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cratis-run-blueprint-").FullName;
        File.WriteAllText(
            Path.Combine(_folder, "application.play"),
            "layout AppShell\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n" +
            "ui profile Desktop\n  target platform web\n  target size regular\n  layout AppShell\n  packages\n    Cratis.Components\n    Cratis.Blueprint.Crm\n");
    }

    void Because()
    {
        _read = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames);
        _unapproved = RunProfileCheck.Check(new DirectoryInfo(_folder), ApprovedPackageSet.Empty);
        _approved = RunProfileCheck.Check(new DirectoryInfo(_folder), _read.Set!);
    }

    void Destroy() => Directory.Delete(_folder, true);

    [Fact] void should_read_the_manifest_as_published() => _read.Diagnostics.ShouldBeEmpty();
    [Fact] void should_refuse_the_blueprint_until_it_is_approved() => _unapproved.Single().Code.ShouldEqual("CLI-RENDER-007");
    [Fact] void should_accept_the_approved_blueprint() => _approved.ShouldBeEmpty();
}
