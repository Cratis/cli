// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_manifest_is_wrong : given.a_valid_package
{
    string[] _noDescription = [];
    string[] _noRequirements = [];
    string[] _wrongFormat = [];
    string[] _tooLarge = [];
    string[] _missingFile = [];
    string[] _noCaption = [];
    string[] _blankCaption = [];
    VerifySeverity _noCaptionSeverity;
    bool _noCaptionPasses;

    async Task Because()
    {
        ChangeContract("  description \"A demo extension\"\n", string.Empty);
        Report = await Run();
        _noDescription = Codes("manifest");

        ChangeContract("  requires stage \"^5\"\n  requires platform web\n", string.Empty);
        Report = await Run();
        _noRequirements = Codes("manifest");

        Write("contract.play", Contract);
        Write("assets/icon.png", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        Report = await Run();
        _wrongFormat = Codes("manifest");

        Write("assets/icon.png", [.. Png, .. new byte[(int)ExtensionVerifyCodes.IconLimit]]);
        Report = await Run();
        _tooLarge = Codes("manifest");

        Write("assets/icon.png", Png);
        File.Delete(Path.Combine(Folder, "assets", "shot.png"));
        Report = await Run();
        _missingFile = Codes("manifest");

        Write("assets/shot.png", Png);
        ChangeContract("    caption \"The demo in use\"\n", string.Empty);
        Report = await Run();
        _noCaption = Codes("manifest");
        _noCaptionSeverity = Check("manifest").Diagnostics.Single().Severity;
        _noCaptionPasses = Report.Passed;

        ChangeContract("caption \"The demo in use\"", "caption \" \"");
        Report = await Run();
        _blankCaption = Codes("manifest");
    }

    [Fact] void should_report_a_missing_description() => _noDescription.ShouldContain("PLAY0999");
    [Fact] void should_report_missing_requirements() => _noRequirements.ShouldContain(ExtensionVerifyCodes.NoRequirements);
    [Fact] void should_report_an_icon_whose_content_is_not_its_format() => _wrongFormat.ShouldContain(ExtensionVerifyCodes.UnsupportedImageFormat);
    [Fact] void should_report_an_icon_over_its_limit() => _tooLarge.ShouldContain(ExtensionVerifyCodes.ImageTooLarge);
    [Fact] void should_report_an_image_that_is_not_in_the_package() => _missingFile.Count(code => code == "PLAY0998").ShouldEqual(1);
    [Fact] void should_warn_about_an_image_without_a_caption() => _noCaption.ShouldContain(ExtensionVerifyCodes.MissingImageCaption);
    [Fact] void should_make_a_missing_caption_a_warning() => _noCaptionSeverity.ShouldEqual(VerifySeverity.Warning);
    [Fact] void should_not_fail_for_a_warning() => _noCaptionPasses.ShouldBeTrue();
    [Fact] void should_report_a_blank_caption() => _blankCaption.ShouldContain(ExtensionVerifyCodes.BlankImageCaption);
}
