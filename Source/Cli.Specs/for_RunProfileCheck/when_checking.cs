// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RunProfileCheck;

public class when_checking : Specification
{
    const string UnknownPackageCode = "CLI-RENDER-007";

    const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [] } }" +
        "] }";

    string _root;
    IReadOnlyList<ScreenplayDiagnostic> _unapproved;
    IReadOnlyList<ScreenplayDiagnostic> _approved;
    IReadOnlyList<ScreenplayDiagnostic> _shipped;
    IReadOnlyList<ScreenplayDiagnostic> _notCompiling;
    IReadOnlyList<ScreenplayDiagnostic> _singleFile;
    IReadOnlyList<ScreenplayDiagnostic> _shell;

    void Establish() => _root = Directory.CreateTempSubdirectory("cratis-run-profile-").FullName;

    void Because()
    {
        var withPackage = Directory.CreateDirectory(Path.Combine(_root, "custom")).FullName;
        File.WriteAllText(Path.Combine(withPackage, "application.play"), Application("    Acme.Charts\n"));

        var shippedOnly = Directory.CreateDirectory(Path.Combine(_root, "shipped")).FullName;
        File.WriteAllText(Path.Combine(shippedOnly, "application.play"), Application(string.Empty));

        var broken = Directory.CreateDirectory(Path.Combine(_root, "broken")).FullName;
        File.WriteAllText(Path.Combine(broken, "application.play"), "this is not screenplay\n");

        var shell = Directory.CreateDirectory(Path.Combine(_root, "shell")).FullName;
        File.WriteAllText(Path.Combine(shell, "application.play"), BrokenShell);

        var approved = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames).Set!;
        _unapproved = RunProfileCheck.Check(new DirectoryInfo(withPackage), ApprovedPackageSet.Empty);
        _approved = RunProfileCheck.Check(new DirectoryInfo(withPackage), approved);
        _shipped = RunProfileCheck.Check(new DirectoryInfo(shippedOnly), ApprovedPackageSet.Empty);
        _notCompiling = RunProfileCheck.Check(new DirectoryInfo(broken), ApprovedPackageSet.Empty);
        _shell = RunProfileCheck.Check(new DirectoryInfo(shell), ApprovedPackageSet.Empty);
        _singleFile = RunProfileCheck.Check(new FileInfo(Path.Combine(withPackage, "application.play")), ApprovedPackageSet.Empty);
    }

    void Destroy() => Directory.Delete(_root, true);

    /// <summary>A navigation item that targets a feature nothing declares, a toolbar button that names a State View slice, a profile that selects a pack nothing declares.</summary>
    const string BrokenShell =
        "concept RowId : Uuid\n" +
        "layout AppShell\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n" +
        "ui profile Desktop\n  target platform web\n  target size expanded\n  layout AppShell\n  packages\n    core\n    Cratis.Components\n  sample data\n    active Missing\n" +
        "module Sales\n  navigation sidebar overrides\n    item orders title \"Orders\" target Sales.Nowhere\n" +
        "  feature Orders\n    slice StateView OrderList\n      readmodel OrderRow\n        rowId RowId\n      query AllOrders => observable OrderRow[]\n" +
        "      projection OrderList => OrderRow\n        from OrderPlaced\n          rowId = $eventSourceId\n" +
        "      screen OrderScreen\n        title \"Orders\"\n        toolbar actions\n          button new title \"New order\" slice Sales.Orders.OrderList\n" +
        "    slice StateChange PlaceOrder\n      command PlaceOrder\n        rowId RowId identifier\n        produces OrderPlaced\n          for rowId\n      event OrderPlaced\n";

    static string Application(string extraPackages) =>
        "layout AppShell\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n" +
        "ui profile Desktop\n  target platform web\n  target size expanded\n  layout AppShell\n  packages\n    core\n    Cratis.Components\n" + extraPackages;

    [Fact] void should_refuse_a_package_that_is_neither_shipped_nor_approved() => _unapproved.Single().Code.ShouldEqual(UnknownPackageCode);
    [Fact] void should_name_the_package() => _unapproved.Single().Message.ShouldContain("Acme.Charts");
    [Fact] void should_accept_the_package_once_it_is_approved() => _approved.ShouldBeEmpty();
    [Fact] void should_accept_the_shipped_packages() => _shipped.ShouldBeEmpty();
    [Fact] void should_leave_a_model_that_does_not_compile_to_the_stage() => _notCompiling.ShouldBeEmpty();
    [Fact] void should_report_the_sample_data_selection_with_its_code() => _shell.ShouldContain(_ => _.Code == "PLAY0714" && _.Location!.Contains("application.play(", StringComparison.Ordinal));
    [Fact] void should_report_the_navigation_target_with_its_code() => _shell.ShouldContain(_ => _.Code == "PLAY0748");
    [Fact] void should_report_the_toolbar_button_with_its_code() => _shell.ShouldContain(_ => _.Code == "PLAY0762");
    [Fact] void should_report_only_errors_of_the_shell() => _shell.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_check_a_single_file_the_same_way() => _singleFile.Count.ShouldEqual(1);
}
