// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Reads the data of 'message' events from a Server-Sent Events stream.</summary>
internal static class DirectMcpServerSentEvents
{
    /// <summary>Yields the data of each complete 'message' event, joining multi-line data with line feeds.</summary>
    /// <param name="reader">The event stream, which bounds each line.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The data of each message event.</returns>
    /// <exception cref="DirectMcpMessageTooLarge">When one event's data exceeds the reader's limit.</exception>
    internal static async IAsyncEnumerable<string> Read(DirectMcpBoundedReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var data = new StringBuilder();
        var hasData = false;
        var type = "message";
        while (await reader.ReadLine(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                if (hasData && type == "message")
                {
                    yield return data.ToString();
                }

                data.Clear();
                hasData = false;
                type = "message";
                continue;
            }

            if (line[0] == ':')
            {
                continue;
            }

            var colon = line.IndexOf(':');
            var field = colon < 0 ? line : line[..colon];
            var value = colon < 0 ? string.Empty : line[(colon + 1)..];
            if (value.StartsWith(' '))
            {
                value = value[1..];
            }

            switch (field)
            {
                case "data":
                    if (data.Length + value.Length + 1 > reader.Limit)
                    {
                        throw new DirectMcpMessageTooLarge(reader.Limit);
                    }

                    if (hasData)
                    {
                        data.Append('\n');
                    }

                    data.Append(value);
                    hasData = true;
                    break;
                case "event":
                    type = value.Length == 0 ? "message" : value;
                    break;
            }
        }

        // A stream that ends mid-event dispatches nothing, as the Server-Sent Events specification requires.
    }
}
