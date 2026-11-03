// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_TemplatePackageStore.when_acquiring_a_package;

public class with_competing_acquisitions : given_a_store
{
    [Fact]
    async Task should_reuse_the_committed_winner_without_opening_another_package()
    {
        var paths = new PackageStorePath(new DirectoryInfo(StoreRoot));
        var destination = paths.Child(Path.Combine("Test.Pkg", "1.0.0"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new PackageAcquisition(paths).Acquire(new DirectoryInfo(destination), async () =>
        {
            entered.SetResult();
            await release.Task;
            return File.OpenRead(Path.Combine(FeedRoot, "Test.Pkg.1.0.0.nupkg"));
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = new PackageAcquisition(paths).Acquire(new DirectoryInfo(destination), () => throw new IOException("second package must not be opened"));
        second.IsCompleted.ShouldBeFalse();
        release.SetResult();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        results[0].ShouldEqual(results[1]);
        File.Exists(Path.Combine(destination, ".cratis-complete")).ShouldBeTrue();
        Directory.GetDirectories(StoreRoot, ".cratis-stage-*").ShouldBeEmpty();
    }
}
