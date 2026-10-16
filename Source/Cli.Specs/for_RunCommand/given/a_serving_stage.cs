// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cratis.Cli.for_RunCommand.given;

/// <summary>
/// A fake Docker session that remains running until stopped, and an HTTP host serving Stage admission responses.
/// </summary>
public class a_serving_stage : a_run_command
{
    protected int _port;
    protected string _statusBody = "{\"state\":\"ready\",\"engine\":\"semantic\"}";
    protected int _statusCode = 200;
    protected int _statusRequests;

    HttpListener _listener;
    CancellationTokenSource _shutdown;
    Task _serving;

    async Task Establish()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n");
        if (OperatingSystem.IsWindows()) return;

        var pipe = Path.Combine(_tools, "stop-pipe");
        using var createPipe = System.Diagnostics.Process.Start("/usr/bin/mkfifo", pipe);
        await createPipe.WaitForExitAsync();
        createPipe.ExitCode.ShouldEqual(0);
        await File.WriteAllTextAsync(Path.Combine(_tools, "docker"),
            $"#!/bin/sh\nprintf '%s\\n' \"$@\" >> '{_dockerArguments}'\n" +
            $"case \"$1\" in\nrun) printf '%s\\n' 'Application started'; read stopped < '{pipe}' ;;\n" +
            $"stop) printf '%s\\n' stopped > '{pipe}' ;;\nps) exit 0 ;;\nesac\n");

        using var reserve = new TcpListener(IPAddress.Loopback, 0);
        reserve.Start();
        _port = ((IPEndPoint)reserve.LocalEndpoint).Port;
        reserve.Stop();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        _shutdown = new CancellationTokenSource();
        _serving = Serve(_shutdown.Token);
    }

    async Task Serve(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                var status = context.Request.Url!.AbsolutePath == "/stage/status";
                var body = "not found";
                if (status)
                {
                    body = _statusBody;
                }
                if (status) _statusRequests++;
                context.Response.StatusCode = status ? _statusCode : 404;
                var bytes = Encoding.UTF8.GetBytes(body);
                await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
                context.Response.Close();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The spec has finished; stop accepting HTTP probes.
        }
    }

    async Task Destroy()
    {
        if (OperatingSystem.IsWindows()) return;
        await _shutdown.CancelAsync();
        await _serving;
        _listener.Close();
        _shutdown.Dispose();
    }
}
