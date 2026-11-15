// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The checks of <c language="bash">cratis extension verify</c> and the diagnostic codes of the CLI. The compiler's own codes
/// (<c language="text">PLAY0998</c> and <c language="text">PLAY1100</c> to <c language="text">PLAY1113</c>) are reported as they are.
/// </summary>
public static class ExtensionVerifyCodes
{
    /// <summary>The check that compiles <c language="text">contract.play</c>.</summary>
    public const string ContractCheck = "contract";

    /// <summary>The check of display name, description, icon, images and requirements.</summary>
    public const string ManifestCheck = "manifest";

    /// <summary>The check that the package ships what the contract declares and nothing else.</summary>
    public const string ContentsCheck = "contents";

    /// <summary>The check of the static HTML placeholders.</summary>
    public const string PlaceholdersCheck = "placeholders";

    /// <summary>The check of the frontend components against their declared kind and the Scene conformance kit.</summary>
    public const string FrontendCheck = "frontend";

    /// <summary>The check that the backend starts in its tier and completes the protocol handshake.</summary>
    public const string BackendCheck = "backend";

    /// <summary>The check of the package digest and contract hash against <c language="text">screenplay.lock</c>.</summary>
    public const string DigestsCheck = "digests";

    /// <summary>The check that the compiler can use the package as a provider.</summary>
    public const string CompatibilityCheck = "compatibility";

    /// <summary>The package holds no <c language="text">contract.play</c>.</summary>
    public const string ContractMissing = "EXTV001";

    /// <summary>The contract states no requirement.</summary>
    public const string NoRequirements = "EXTV010";

    /// <summary>An icon or image is not a supported format.</summary>
    public const string UnsupportedImageFormat = "EXTV011";

    /// <summary>An icon or image is larger than its limit.</summary>
    public const string ImageTooLarge = "EXTV012";

    /// <summary>An image declares a caption that is blank.</summary>
    public const string BlankImageCaption = "EXTV013";

    /// <summary>An image declares no caption.</summary>
    public const string MissingImageCaption = "EXTV014";

    /// <summary>The backend the contract declares is not in the package.</summary>
    public const string BackendNotShipped = "EXTV020";

    /// <summary>A component with a frontend is declared but the contract names no frontend entry.</summary>
    public const string ComponentWithoutFrontend = "EXTV021";

    /// <summary>The package ships a file the contract does not declare.</summary>
    public const string UndeclaredFile = "EXTV022";

    /// <summary>A placeholder is larger than its limit.</summary>
    public const string PlaceholderTooLarge = "EXTV030";

    /// <summary>The package is not the one <c language="text">screenplay.lock</c> pins.</summary>
    public const string DigestMismatch = "EXTV040";

    /// <summary>The contract is not the one <c language="text">screenplay.lock</c> pins.</summary>
    public const string ContractHashMismatch = "EXTV041";

    /// <summary>The Screenplay compiler is outside the range the contract requires.</summary>
    public const string ScreenplayRequirementNotMet = "EXTV050";

    /// <summary>The package is not a drop-in provider for the baseline contract.</summary>
    public const string IncompatibleWithBaseline = "EXTV051";

    /// <summary>The frontend check could not run.</summary>
    public const string FrontendNotRun = "EXTV060";

    /// <summary>The backend check could not run.</summary>
    public const string BackendNotRun = "EXTV070";

    /// <summary>The icon size limit in bytes.</summary>
    public const long IconLimit = 256 * 1024;

    /// <summary>The marketplace image size limit in bytes.</summary>
    public const long ImageLimit = 2 * 1024 * 1024;

    /// <summary>The placeholder size limit in bytes.</summary>
    public const long PlaceholderLimit = 256 * 1024;

    /// <summary>The checks in the order they run.</summary>
    public static readonly IReadOnlyList<string> Checks = [ContractCheck, ManifestCheck, ContentsCheck, PlaceholdersCheck, FrontendCheck, BackendCheck, DigestsCheck, CompatibilityCheck];
}
