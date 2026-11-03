// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Settings for the run command.
/// </summary>
public class RunSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the Screenplay file, or the folder of Screenplay files, to run. Defaults to the current directory.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Screenplay (.play) file, or folder of Screenplay files, to run. Folders are searched recursively. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the cratis/stage image tag to run.
    /// </summary>
    /// <remarks>
    /// Defaults to the Stage release this CLI renders with rather than to a moving tag, so the sandbox reading
    /// the artifacts is the one they were planned for. See <see cref="StageContainer.DefaultTag"/>.
    /// </remarks>
    [CommandOption("--tag <TAG>")]
    [Description("The cratis/stage image tag to run. Defaults to the Stage version this CLI renders with.")]
    public string Tag { get; set; } = StageContainer.DefaultTag;

    /// <summary>
    /// Gets or sets the Stage runtime engine, or null to use semantic.
    /// </summary>
    [CommandOption("--engine <ENGINE>")]
    [Description("Stage runtime engine: semantic (default) or eventmodel (explicit fallback), case-insensitive.")]
    public string? Engine { get; set; }

    /// <summary>
    /// Gets or sets the optional sample data pack to seed into the disposable sandbox.
    /// </summary>
    [CommandOption("--sample-data [PACK]")]
    [Description("Seed user-authored sample data into the disposable sandbox. Uses profile bindings and active pack when PACK is omitted.")]
    public FlagValue<string> SampleData { get; set; } = new();

    /// <summary>
    /// Gets or sets the host port to publish the Stage API on.
    /// </summary>
    [CommandOption("--port <PORT>")]
    [Description("Host port to publish the Stage API on.")]
    [DefaultValue(9090)]
    public int Port { get; set; } = 9090;

    /// <summary>
    /// Gets or sets the host port to publish the Chronicle Workbench on.
    /// </summary>
    [CommandOption("--workbench-port <PORT>")]
    [Description("Host port to publish the Chronicle Workbench on.")]
    [DefaultValue(35000)]
    public int WorkbenchPort { get; set; } = 35000;

    /// <summary>
    /// Gets or sets a value indicating whether standalone Stage ports are published beyond loopback.
    /// </summary>
    [CommandOption("--allow-remote")]
    [Description("Publish the unauthenticated sandbox's Stage API and Workbench on all IPv4 host interfaces instead of loopback. An explicit local sandbox choice, not added authentication. Does not change authenticated-stack bindings.")]
    [DefaultValue(false)]
    public bool AllowRemote { get; set; }

    /// <summary>
    /// Gets or sets the approved package set document for custom runtime packages.
    /// </summary>
    [CommandOption("--package-set <FILE>")]
    [Description("Approved package set (JSON) naming the custom runtime packages the ui profile may use, beyond core, scene.web and Cratis.Components. The Stage loads exactly these packages at runtime from their browser (ES module) builds.")]
    public string? PackageSet { get; set; }

    /// <summary>
    /// Gets or sets the folder holding the browser build of each approved package.
    /// </summary>
    [CommandOption("--packages <FOLDER>")]
    [Description("Folder holding a folder for each approved package, named by its npm name (for example ./node_modules). Defaults to the folder the package set is in. Only the approved packages' folders are mounted, read-only.")]
    public string? Packages { get; set; }

    /// <summary>
    /// Gets or sets the short form of <c language="csharp">--environment</c>.
    /// </summary>
    [CommandOption("-e <NAME>")]
    [Description("Environment to run with (short for --environment): the values for the model's identity providers and mail server. Defaults to 'development'.")]
    public string? EnvironmentShort { get; set; }

    /// <summary>
    /// Gets or sets the host port to publish AuthProxy on, which is the application's address when the model declares authentication.
    /// </summary>
    [CommandOption("--auth-port <PORT>")]
    [Description("Host port to publish AuthProxy on when the model declares authentication. This is the address to open instead of the Stage API.")]
    [DefaultValue(8080)]
    public int AuthPort { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the host port to publish the onboarding lobby on.
    /// </summary>
    [CommandOption("--lobby-port <PORT>")]
    [Description("Host port the onboarding lobby is reached on when the model declares onboarding. It is AuthProxy's second port: the same host name as the application, so the sign-in is shared.")]
    [DefaultValue(8081)]
    public int LobbyPort { get; set; } = 8081;

    /// <summary>
    /// Gets or sets the cratis/authproxy image tag to run.
    /// </summary>
    [CommandOption("--authproxy-tag <TAG>")]
    [Description("The cratis/authproxy image tag to run when the model declares authentication.")]
    public string AuthProxyTag { get; set; } = RunStackDefaults.AuthProxyTag;

    /// <summary>
    /// Gets or sets the Ante image tag to run.
    /// </summary>
    [CommandOption("--ante-tag <TAG>")]
    [Description("The ghcr.io/cratis/ante image tag to run when the model declares onboarding.")]
    public string AnteTag { get; set; } = RunStackDefaults.AnteTag;

    /// <summary>
    /// Gets or sets a value indicating whether the container's own output should be streamed.
    /// </summary>
    [CommandOption("--verbose")]
    [Description("Stream the container's output instead of showing startup progress.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }

    /// <summary>
    /// Builds a semantic-engine retry command preserving non-default runtime options.
    /// </summary>
    /// <returns>The command to run from the same working directory.</returns>
    public string SemanticCommand()
    {
        var arguments = new List<string> { "cratis run", Quote(Path ?? Directory.GetCurrentDirectory()), "--engine semantic" };
        if (Port != 9090) arguments.Add($"--port {Port.ToString(CultureInfo.InvariantCulture)}");
        if (WorkbenchPort != 35000) arguments.Add($"--workbench-port {WorkbenchPort.ToString(CultureInfo.InvariantCulture)}");
        if (!string.Equals(Tag, StageContainer.DefaultTag, StringComparison.Ordinal)) arguments.Add($"--tag {Quote(Tag)}");
        if (PackageSet is not null) arguments.Add($"--package-set {Quote(PackageSet)}");
        if (Packages is not null) arguments.Add($"--packages {Quote(Packages)}");
        if (EnvironmentShort is not null) arguments.Add($"-e {Quote(EnvironmentShort)}");
        if (AuthPort != 8080) arguments.Add($"--auth-port {AuthPort.ToString(CultureInfo.InvariantCulture)}");
        if (Verbose) arguments.Add("--verbose");
        arguments.AddRange(RemoteAccessArguments());

        return string.Join(' ', arguments);
    }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        Engine = Engine?.ToLowerInvariant();
        return Engine is null || string.Equals(Engine, "eventmodel", StringComparison.Ordinal) || string.Equals(Engine, "semantic", StringComparison.Ordinal)
            ? base.Validate()
            : ValidationResult.Error("--engine must be one of: eventmodel, semantic.");
    }

    static string Quote(string value)
    {
        if (value.Length > 0 && value.All(character => char.IsLetterOrDigit(character) || character is '/' or '.' or '-' or '_' or ':' or '@' || (OperatingSystem.IsWindows() && character == '\\'))) return value;
        var escaped = OperatingSystem.IsWindows()
            ? value.Replace("`", "``", StringComparison.Ordinal).Replace("\"", "`\"", StringComparison.Ordinal).Replace("$", "`$", StringComparison.Ordinal)
            : value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal);

        return $"\"{escaped}\"";
    }

    IEnumerable<string> RemoteAccessArguments() => AllowRemote ? ["--allow-remote"] : [];
}
