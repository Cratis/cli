// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Cli.for_ShutdownSignal;

/// <summary>
/// A container this process started has to come down no matter how the request to stop arrived - a Ctrl+C at
/// an interactive terminal, a plain <c language="csharp">kill</c> from a process manager, or the terminal itself
/// being closed. Each of those is a different signal, and missing one of them is how a container gets left
/// behind for a reason nobody can see from the terminal that asked for it to stop.
/// </summary>
/// <remarks>
/// Registering a real <see cref="PosixSignalRegistration"/> and delivering an actual signal to assert on is not
/// something a specification can do without taking down the process running it, so what is asserted here is the
/// platform-dependent list a real registration is built from.
/// </remarks>
public class when_choosing_which_signals_to_watch : Specification
{
    PosixSignal[] _signals;

    void Because() => _signals = [.. ShutdownSignal.SignalsToWatch()];

    [Fact] void should_watch_for_ctrl_c() => _signals.ShouldContain(PosixSignal.SIGINT);
    [Fact] void should_watch_for_a_plain_kill() => _signals.ShouldContain(PosixSignal.SIGTERM);

    [Fact] void should_watch_for_a_closed_terminal_everywhere_one_can_close() =>
        _signals.Contains(PosixSignal.SIGHUP).ShouldEqual(!OperatingSystem.IsWindows());
}
