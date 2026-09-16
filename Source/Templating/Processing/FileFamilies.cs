// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Processing;

/// <summary>
/// The conditional processing file families, matching the documented per-extension comment and directive syntax.
/// </summary>
public enum FileFamily
{
    /// <summary>C#, F#, C++, C and Cake files — raw preprocessor directives.</summary>
    Language,

    /// <summary>Visual Basic files — apostrophe-commented #If directives.</summary>
    VisualBasic,

    /// <summary>JavaScript and TypeScript files — C-style line comments.</summary>
    Script,

    /// <summary>JSON-family files — C-style line comments, actionable variants uncomment.</summary>
    Json,

    /// <summary>XML-family files — directives inside XML comments.</summary>
    Xml,

    /// <summary>MSBuild project files — XML comments plus Condition attributes on elements.</summary>
    MSBuild,

    /// <summary>Single-hash comment files — raw directives after a hash.</summary>
    SingleHash,

    /// <summary>CSS files — directives inside block comments.</summary>
    Css,

    /// <summary>Windows command files — directives after rem.</summary>
    Command,

    /// <summary>Razor views — directives inside Razor comments.</summary>
    Razor,

    /// <summary>Haml files — dash-hash line comments.</summary>
    Haml,

    /// <summary>JSX and TSX files — C-style line comments.</summary>
    Jsx,

    /// <summary>Everything else — C-style line comments per the documented default rules.</summary>
    Other
}

/// <summary>
/// The directive configuration for a file family: which tokens open and close conditional blocks,
/// which variants are actionable, and how directive lines are trimmed.
/// </summary>
/// <param name="IfTokens">Tokens opening a conditional block.</param>
/// <param name="ElseifTokens">Tokens starting an alternate branch.</param>
/// <param name="ElseTokens">Tokens starting the fallback branch.</param>
/// <param name="EndifTokens">Tokens closing the block.</param>
/// <param name="ActionableIfTokens">Actionable variants that uncomment their body when active.</param>
/// <param name="ActionableElseifTokens">Actionable elseif variants.</param>
/// <param name="ActionableElseTokens">Actionable else variants.</param>
/// <param name="UncommentActions">Actions applied to body lines of active actionable blocks: cStyleUncomment, cStyleReduceComment, xmlUncomment, markupUncomment.</param>
/// <param name="Trim">Whether directive lines are removed entirely from the output.</param>
/// <param name="WholeLine">Whether directives must start their line.</param>
/// <param name="OnOffPrefix">Comment prefix for the cnd:noEmit on/off markers.</param>
/// <param name="ProcessMsBuildConditions">Whether Condition attributes on elements are evaluated (MSBuild family only).</param>
public record FileFamilyConfig(
    IReadOnlyList<string> IfTokens,
    IReadOnlyList<string> ElseifTokens,
    IReadOnlyList<string> ElseTokens,
    IReadOnlyList<string> EndifTokens,
    IReadOnlyList<string> ActionableIfTokens,
    IReadOnlyList<string> ActionableElseifTokens,
    IReadOnlyList<string> ActionableElseTokens,
    IReadOnlyList<string> UncommentActions,
    bool Trim,
    bool WholeLine,
    string OnOffPrefix,
    bool ProcessMsBuildConditions = false)
{
    /// <summary>
    /// Gets a value indicating whether the family has any conditional directives at all.
    /// </summary>
    public bool HasDirectives => IfTokens.Count > 0;

    internal static FileFamilyConfig NoDirectives(string onOffPrefix) => new([], [], [], [], [], [], [], [], false, false, onOffPrefix);
}

/// <summary>
/// Detects the file family for a path from its extension and well-known file names, and provides each
/// family's directive configuration.
/// </summary>
public static class FileFamilies
{
    static readonly string[] _languageExtensions = [".cs", ".fs", ".cpp", ".h", ".hpp", ".cake"];
    static readonly string[] _jsonExtensions =
    [
        ".json", ".jsonld", ".hjson", ".json5", ".geojson", ".topojson", ".bowerrc", ".npmrc",
        ".job", ".postcssrc", ".babelrc", ".csslintrc", ".eslintrc", ".jade-lintrc", ".pug-lintrc",
        ".jshintrc", ".stylelintrc", ".yarnrc"
    ];
    static readonly string[] _xmlExtensions =
    [
        ".htm", ".html", ".jsp", ".asp", ".aspx", ".nuspec", ".xslt", ".xsd", ".vsixmanifest", ".vsct",
        ".storyboard", ".axml", ".plist", ".xib", ".strings", ".xml", ".xaml", ".axaml", ".md", ".appxmanifest"
    ];
    static readonly string[] _singleHashExtensions = [".sln", ".yml", ".yaml", ".sh", ".ps1"];
    static readonly string[] _singleHashNames = [".dockerignore", ".gitignore", ".gitattributes", ".editorconfig", "Dockerfile", "nginx.conf", "robots.txt"];
    static readonly string[] _xmlWellKnownNames = ["app.config", "web.config", "packages.config", "nuget.config"];

    /// <summary>
    /// Detects the file family for a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The detected family.</returns>
    public static FileFamily Detect(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (_xmlWellKnownNames.Any(name => fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
            || (fileName.StartsWith("web.", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase)))
        {
            return FileFamily.Xml;
        }

        if (_singleHashNames.Any(name => fileName.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return FileFamily.SingleHash;
        }

        if (fileName.EndsWith(".proj", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".proj.user", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".msbuild", StringComparison.Ordinal) || string.Equals(extension, ".targets", StringComparison.Ordinal) || string.Equals(extension, ".props", StringComparison.Ordinal))
        {
            return FileFamily.MSBuild;
        }

        return extension switch
        {
            var ext when _languageExtensions.Contains(ext) => FileFamily.Language,
            ".vb" => FileFamily.VisualBasic,
            ".js" or ".ts" => FileFamily.Script,
            var ext when _jsonExtensions.Contains(ext) => FileFamily.Json,
            var ext when _xmlExtensions.Contains(ext) => FileFamily.Xml,
            var ext when _singleHashExtensions.Contains(ext) => FileFamily.SingleHash,
            ".css" or ".css.min" => FileFamily.Css,
            ".bat" or ".cmd" => FileFamily.Command,
            ".cshtml" => FileFamily.Razor,
            ".haml" => FileFamily.Haml,
            ".jsx" or ".tsx" => FileFamily.Jsx,
            _ => FileFamily.Other
        };
    }

    /// <summary>
    /// Gets the directive configuration for a family.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <returns>The family configuration.</returns>
    public static FileFamilyConfig ConfigFor(FileFamily family) => family switch
    {
        FileFamily.Language => new(
            ["#if"], ["#elseif"], ["#else"], ["#endif"], [], [], [], [], false, true, "//"),
        FileFamily.VisualBasic => new(
            ["'#if"], ["'#elseif"], ["'#else"], ["'#end if", "'#endif"], [], [], [], [], false, true, "'"),
        FileFamily.Script or FileFamily.Json => new(
            ["//#if"],
            ["//#elseif"],
            ["//#else"],
            ["//#endif"],
            ["////#if"],
            ["////#elseif"],
            ["////#else"],
            ["cStyleUncomment", "cStyleReduceComment"],
            true,
            true,
            "//"),
        FileFamily.Xml => new(
            ["<!--#if"],
            ["<!--#elseif"],
            ["<!--#else"],
            ["<!--#endif", "#endif"],
            ["<!--/#if"],
            ["<!--/#elseif"],
            ["<!--/#else"],
            [],
            true,
            true,
            "<!--"),
        FileFamily.MSBuild => new(
            ["<!--#if"],
            ["<!--#elseif"],
            ["<!--#else"],
            ["<!--#endif", "#endif"],
            ["<!--/#if"],
            ["<!--/#elseif"],
            ["<!--/#else"],
            [],
            true,
            true,
            "<!--",
            ProcessMsBuildConditions: true),
        FileFamily.SingleHash => new(
            ["#if"],
            ["#elseif"],
            ["#else"],
            ["#endif"],
            [],
            [],
            [],
            [],
            false,
            true,
            "#"),
        FileFamily.Css => new(
            ["/*#if"],
            ["/*#elseif"],
            ["/*#else"],
            ["/*#endif", "#endif"],
            [],
            [],
            [],
            [],
            false,
            true,
            "/*"),
        FileFamily.Command => new(
            ["rem #if", "REM #if", "Rem #if"],
            ["rem #elseif", "REM #elseif"],
            ["rem #else", "REM #else"],
            ["rem #endif", "REM #endif"],
            [],
            [],
            [],
            [],
            false,
            true,
            "rem"),
        FileFamily.Razor => new(
            ["@*#if"],
            ["@*#elseif"],
            ["@*#else"],
            ["#endif*@", "#endif *@", "#endif"],
            [],
            [],
            [],
            [],
            false,
            true,
            "@*"),
        FileFamily.Haml => new(
            ["-#if"],
            ["-#elseif"],
            ["-#else"],
            ["-#endif", "#endif"],
            [],
            [],
            [],
            [],
            false,
            true,
            "-#"),
        FileFamily.Jsx => new(
            ["//#if"],
            ["//#elseif"],
            ["//#else"],
            ["//#endif"],
            ["////#if"],
            ["////#elseif"],
            ["////#else"],
            ["cStyleUncomment", "cStyleReduceComment"],
            true,
            true,
            "//"),
        _ => new(
            ["//#if"],
            ["//#elseif"],
            ["//#else"],
            ["//#endif"],
            ["////#if"],
            ["////#elseif"],
            ["////#else"],
            ["cStyleUncomment", "cStyleReduceComment"],
            true,
            true,
            "//")
    };
}
