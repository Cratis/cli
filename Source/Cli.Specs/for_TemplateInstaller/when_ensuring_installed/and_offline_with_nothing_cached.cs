// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;

namespace Cratis.Cli.for_TemplateInstaller.when_ensuring_installed;

public class and_offline_with_nothing_cached : given.a_template_installer
{
    Exception _error;

    void Establish() => _source.Offline = true;

    async Task Because() => _error = await Catch.Exception(_installer.EnsureInstalled);

    [Fact] void should_fail_to_acquire_templates() => _error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
    [Fact] void should_tell_how_to_recover() => _error.Message.ShouldContain("--template-path");
}
