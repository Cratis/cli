// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_saving_a_symlinked_config : given.a_temp_config_directory
{
    string _target = null!;
    string _linkTarget = null!;
    string _intermediateLink = null!;
    string _originalJson = null!;
    string _readFromOriginal = null!;
    FileStream _original = null!;

    void Establish()
    {
        new CliConfiguration { ActiveContext = "original" }.Save();
        _originalJson = File.ReadAllText(CliConfiguration.GetConfigPath());
        var targetDirectory = Path.Combine(_tempConfigHome, "dotfiles");
        Directory.CreateDirectory(targetDirectory);
        _target = Path.Combine(targetDirectory, "config.json");
        File.Move(CliConfiguration.GetConfigPath(), _target);
        _intermediateLink = Path.Combine(targetDirectory, "current.json");
        File.CreateSymbolicLink(_intermediateLink, "config.json");
        _linkTarget = Path.Combine("..", "dotfiles", "current.json");
        File.CreateSymbolicLink(CliConfiguration.GetConfigPath(), _linkTarget);
        _original = new FileStream(_target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    void Because()
    {
        new CliConfiguration { ActiveContext = "replacement" }.Save();
        using var reader = new StreamReader(_original, leaveOpen: true);
        _readFromOriginal = reader.ReadToEnd();
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            _original.Dispose();
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Unix.Fact] void should_preserve_the_config_symlink() => new FileInfo(CliConfiguration.GetConfigPath()).LinkTarget.ShouldEqual(_linkTarget);
    [Unix.Fact] void should_preserve_intermediate_symlinks() => new FileInfo(_intermediateLink).LinkTarget.ShouldEqual("config.json");
    [Unix.Fact] void should_update_the_target() => File.ReadAllText(_target).ShouldContain("replacement");
    [Unix.Fact] void should_publish_the_replacement_config() => CliConfiguration.Load().ActiveContext.ShouldEqual("replacement");
    [Unix.Fact] void should_replace_the_target_atomically() => _readFromOriginal.ShouldEqual(_originalJson);
    [Unix.Fact] void should_not_leave_temporary_files() => Directory.GetFiles(_tempConfigHome, "*.tmp", SearchOption.AllDirectories).ShouldBeEmpty();

    internal static class Unix
    {
        public sealed class FactAttribute : Xunit.FactAttribute
        {
            public FactAttribute()
            {
                if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                {
                    Skip = "Requires Linux or macOS symbolic link support.";
                }
            }
        }
    }
}
