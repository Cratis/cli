// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_mixed_case_file_in_a_folder : given.a_temporary_folder
{
    string _selectedFolder;
    string _file;
    string _invalidSibling;
    string _emptyFolder;
    RunInput _fileResult;
    RunInput _folderResult;
    RunInput _invalidSiblingResult;
    RunInput _emptyFolderResult;
    RunInput _missingFileResult;
    bool _folderContainsPlayFiles;

    void Establish()
    {
        _selectedFolder = Directory.CreateDirectory(Path.Combine(_folder, "selected")).FullName;
        _file = Path.Combine(_selectedFolder, "input.PLAY");
        _invalidSibling = Path.Combine(_selectedFolder, "sibling.play.txt");
        _emptyFolder = Directory.CreateDirectory(Path.Combine(_selectedFolder, "empty")).FullName;
        File.WriteAllText(_file, "domain Selected\n");
        File.WriteAllText(_invalidSibling, "not a screenplay");
        File.WriteAllText(Path.Combine(_folder, "parent.play"), "domain Parent\n");
    }

    void Because()
    {
        _fileResult = RunInput.Resolve(_file);
        _folderResult = RunInput.Resolve(_selectedFolder);
        _folderContainsPlayFiles = PlayFiles.ExistIn(_selectedFolder);
        _invalidSiblingResult = RunInput.Resolve(_invalidSibling);
        _emptyFolderResult = RunInput.Resolve(_emptyFolder);
        _missingFileResult = RunInput.Resolve(Path.Combine(_selectedFolder, "missing.play"));
    }

    [Fact] void should_admit_the_selected_file() => _fileResult.Error.ShouldBeNull();
    [Fact] void should_classify_the_selected_file_as_a_file() => _fileResult.Target.ShouldBeOfExactType<FileInfo>();
    [Fact] void should_select_only_the_exact_file_not_its_parent_or_sibling() => _fileResult.Target!.FullName.ShouldEqual(_file);
    [Fact] void should_admit_the_selected_folder() => _folderResult.Error.ShouldBeNull();
    [Fact] void should_classify_the_selected_folder_as_a_directory() => _folderResult.Target.ShouldBeOfExactType<DirectoryInfo>();
    [Fact] void should_select_only_the_exact_folder_not_its_parent() => _folderResult.Target!.FullName.ShouldEqual(_selectedFolder);
    [Fact] void should_discover_the_uppercase_play_file_in_the_folder() => _folderContainsPlayFiles.ShouldBeTrue();
    [Fact] void should_not_admit_the_invalid_sibling_using_the_valid_file() => _invalidSiblingResult.Target.ShouldBeNull();
    [Fact] void should_not_admit_the_empty_folder_using_its_parent_files() => _emptyFolderResult.Target.ShouldBeNull();
    [Fact] void should_not_admit_a_missing_file_using_its_siblings() => _missingFileResult.Target.ShouldBeNull();
}
