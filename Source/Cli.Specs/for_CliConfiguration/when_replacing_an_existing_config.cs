// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_replacing_an_existing_config : given.a_temp_config_directory
{
    FileStream _original = null!;
    string _originalJson = null!;
    string _readFromOriginal = null!;

    void Establish()
    {
        new CliConfiguration { ActiveContext = "original" }.Save();
        _originalJson = File.ReadAllText(CliConfiguration.GetConfigPath());
        _original = new FileStream(CliConfiguration.GetConfigPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
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

    [Fact] void should_leave_the_original_file_intact_for_existing_readers() => _readFromOriginal.ShouldEqual(_originalJson);
    [Fact] void should_publish_the_replacement_config() => CliConfiguration.Load().ActiveContext.ShouldEqual("replacement");
    [Fact] void should_not_leave_temporary_files() => Directory.GetFiles(Path.GetDirectoryName(CliConfiguration.GetConfigPath())!, "*.tmp").ShouldBeEmpty();
}
