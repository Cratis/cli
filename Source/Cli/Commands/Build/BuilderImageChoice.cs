// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The builder image picked to run.
/// </summary>
/// <param name="Reference">The image reference, with its tag.</param>
/// <param name="NeedsPull">True when the image is not on this computer yet and has to be pulled first.</param>
public sealed record BuilderImageChoice(string Reference, bool NeedsPull);
