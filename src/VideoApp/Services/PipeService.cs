﻿/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of VideoApp.
 *
 *  VideoApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  VideoApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with VideoApp. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace VideoApp.Services;

using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Hosting;
using VideoApp.Core;
using VideoApp.Core.Services;

class PipeService : IHostedService
{
    private const string PIPE_NAME = "com.albertakhmetov.videoapp.instancepipe";

    public static async Task SendData(string data)
    {
        using (var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out, PipeOptions.Asynchronous))
        {
            await pipeClient.ConnectAsync(1000);

            using (var writer = new StreamWriter(pipeClient, Encoding.UTF8))
            {
                await writer.WriteAsync(data);
                await writer.FlushAsync();
            }
        }
    }

    private readonly ISingleInstanceService singleInstanceService;
    private CancellationTokenSource tokenSource;

    public PipeService(ISingleInstanceService singleInstanceService)
    {
        this.singleInstanceService = singleInstanceService.NotNull();
        tokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(async x =>
        {
            var token = (CancellationToken)x!;

            while (true)
            {
                using (var pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    await pipeServer.WaitForConnectionAsync(token);

                    using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
                    {
                        var receivedData = await reader.ReadToEndAsync();

                        singleInstanceService.OnActivated(receivedData);
                    }
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }, tokenSource.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return tokenSource.CancelAsync();
    }
}
