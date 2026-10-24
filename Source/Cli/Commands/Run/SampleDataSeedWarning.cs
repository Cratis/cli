// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Cli.Commands.Run;

internal record SampleDataSeedWarning(string Message, SourceLocation? Location);
