// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.given;

/// <summary>An installer over a scratch templates root and a package source that never touches the network.</summary>
public class a_template_installer : Specification
{
    protected const string CliVersion = "2.0.0";
    protected string _templatesRoot;
    protected FakePackageSource _source;
    protected TemplateInstaller _installer;

    void Establish()
    {
        _templatesRoot = Path.Combine(Path.GetTempPath(), "cratis-specs-templateinstaller", Guid.NewGuid().ToString("N"));
        _source = new(Path.Combine(_templatesRoot, "packages"));
        _installer = new(_source, _templatesRoot, CliVersion);
    }

    void Destroy()
    {
        if (Directory.Exists(_templatesRoot)) Directory.Delete(_templatesRoot, true);
    }

    protected void Install(string version)
    {
        _source.Cache(version);
        new InstalledTemplates(version, TemplateCatalogue.PinnedPackages, null, DateTimeOffset.UtcNow).Write(_templatesRoot);
    }

    protected InstalledTemplates? Installed => InstalledTemplates.Read(_templatesRoot);
}
