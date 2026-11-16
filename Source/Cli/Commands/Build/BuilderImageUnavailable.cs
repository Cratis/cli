// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown when no version of a builder image is on this computer and Docker Hub could not
/// say which one is the newest.
/// </summary>
/// <param name="repository">The repository of the image.</param>
public sealed class BuilderImageUnavailable(string repository)
    : Exception($"No version of {repository} is on this computer and Docker Hub could not be reached to find the newest one. Check the network connection, or pass a tagged image with --image {repository}:<tag>.");
