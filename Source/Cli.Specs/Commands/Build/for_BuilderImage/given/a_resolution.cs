// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.given;

public class a_resolution : Specification
{
    protected Dictionary<string, string> _local = [];
    protected Dictionary<string, string> _published = [];
    protected List<string> _askedPublished = [];

    protected Task<BuilderImageChoice> Resolve(string? image, string target) =>
        BuilderImage.Resolve(
            image,
            target,
            repository => Task.FromResult(_local.GetValueOrDefault(repository)),
            repository =>
            {
                _askedPublished.Add(repository);
                return Task.FromResult(_published.GetValueOrDefault(repository));
            });
}
