// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSource.when_downloading;

public class and_the_clone_fails : given.a_temporary_root
{
    string _clones;
    Exception? _exception;

    void Establish() => _clones = Path.Combine(_root, "clones");

    void Because()
    {
        try
        {
            AiCorpusSource.Download(new Uri(Path.Combine(_root, "missing-repository")).AbsoluteUri, _clones);
        }
        catch (InvalidOperationException exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_report_the_failure() => _exception.ShouldNotBeNull();
    [Fact] void should_leave_no_partial_checkout_behind() => Directory.EnumerateFileSystemEntries(_clones).ShouldBeEmpty();
}
