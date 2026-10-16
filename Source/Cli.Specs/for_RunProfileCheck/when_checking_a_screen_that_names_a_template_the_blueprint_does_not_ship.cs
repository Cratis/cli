// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RunProfileCheck;

/// <summary>
/// A screen that fills a template of an approved domain blueprint renders only when the blueprint's published manifest lists that template.
/// One it does not list (a sample naming <c language="csharp">CrmActivityCreateForm</c> before the package shipped it) stops the run before Docker
/// starts, with <c language="csharp">CLI-RENDER-020</c> naming the blueprint and the template, instead of showing a screen with no template (Cratis/Stage#457).
/// </summary>
public class when_checking_a_screen_that_names_a_template_the_blueprint_does_not_ship : Specification
{
    const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Cratis.Blueprint.Crm\", \"version\": \"1.0.0\", \"kind\": \"Blueprint\"," +
        " \"dependencies\": [{ \"name\": \"Cratis.Blueprint.Default\", \"versionRange\": \"^1.0.0\" }]," +
        " \"layouts\": [], \"themes\": [], \"displayName\": \"CRM\", \"module\": \"@cratis/scene.blueprint.crm\"," +
        " \"screenTemplates\": [\"CrmLeadList\", \"CrmLeadCreateForm\"], \"dialogTemplates\": [\"CrmQuickCreateLead\"]," +
        " \"sampleData\": [{ \"id\": \"crm-nordic-smb\", \"name\": \"Nordic SMB\" }]," +
        " \"blueprint\": { \"scenarios\": [] } }," +
        " \"module\": { \"npm\": \"@cratis/scene.blueprint.crm\", \"version\": \"4.53.1\", \"export\": \"crmBlueprint\", \"styles\": [] } }" +
        "] }";

    string _folder;
    ApprovedPackageSetReadResult _read;
    IReadOnlyList<ScreenplayDiagnostic> _findings;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cratis-run-template-").FullName;
        File.WriteAllText(Path.Combine(_folder, "application.play"), Source("CrmLeadCreateForm", "CrmActivityCreateForm"));
    }

    void Because()
    {
        _read = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames);
        _findings = RunProfileCheck.Check(new DirectoryInfo(_folder), _read.Set!);
    }

    void Destroy() => Directory.Delete(_folder, true);

    static string Source(params string[] templates) =>
        "domain Sales\n\nconcept LeadId : Uuid\n\n" +
        "layout AppShell\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n" +
        "ui profile Desktop\n  target platform web\n  target size regular\n  layout AppShell\n  packages\n    Cratis.Components\n    Cratis.Blueprint.Crm\n\n" +
        "module Crm\n  feature Leads\n    slice StateChange RegisterLead\n      command RegisterLead\n        leadId LeadId identifier\n\n        produces LeadRegistered\n          for leadId\n\n      event LeadRegistered\n" +
        string.Concat(templates.Select(template => $"\n      screen {template}\n        template blueprint Cratis.Blueprint.Crm {template} version \"^1.0\"\n"));

    [Fact] void should_read_the_manifest_as_published() => _read.Diagnostics.ShouldBeEmpty();
    [Fact] void should_refuse_only_the_template_the_blueprint_does_not_ship() => _findings.Count(finding => finding.Code == "CLI-RENDER-020").ShouldEqual(1);
    [Fact] void should_name_the_blueprint_and_the_template() => _findings.Single(finding => finding.Code == "CLI-RENDER-020").Message.ShouldContain("Blueprint 'Cratis.Blueprint.Crm' has no template 'CrmActivityCreateForm'");
    [Fact] void should_refuse_nothing_else() => _findings.Select(finding => finding.Code).Distinct().ShouldContainOnly(["CLI-RENDER-020"]);
}
