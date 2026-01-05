// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2025 Bart Suelze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Gallio.Common.Diagnostics;
using Gallio.Common.Policies;
using Gallio.Common.Remoting;
using Gallio.Model.Isolation;
using Gallio.Model.Isolation.Messages;

namespace Gallio.AutoCAD.Plugin
{
  /// <summary>
  /// .NET 8 adapter for TestIsolationClient functionality using the new RPC layer.
  /// </summary>
  /// <remarks>
  /// This replaces the old TestIsolationClient which relied on .NET Remoting (not available in .NET 8).
  /// </remarks>
  public class TestIsolationClientAdapter : IDisposable
  {
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(2);

    private readonly BinaryIpcClientChannel clientChannel;
    private readonly BinaryIpcServerChannel serverChannel;
    private readonly Guid linkId;
    private readonly CancellationTokenSource cancellationTokenSource;

    /// <summary>
    /// Creates a test isolation client adapter.
    /// </summary>
    /// <param name="ipcPortName">The IPC port name.</param>
    /// <param name="linkId">The unique id of the client/server pair.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ipcPortName"/> is null.</exception>
    public TestIsolationClientAdapter(string ipcPortName, Guid linkId)
    {
      if (ipcPortName == null)
        throw new ArgumentNullException(nameof(ipcPortName));

      this.linkId = linkId;
      this.cancellationTokenSource = new CancellationTokenSource();

      clientChannel = new BinaryIpcClientChannel(ipcPortName);
      serverChannel = new BinaryIpcServerChannel(ipcPortName + ".ClientCallback");
    }

    /// <inheritdoc />
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client adapter.
    /// </summary>
    /// <param name="disposing">True if <see cref="Dispose()"/> was called directly.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        serverChannel?.Dispose();
        clientChannel?.Dispose();
      }
    }

    /// <summary>
    /// Runs isolated tasks until the server shuts down.
    /// </summary>
    /// <remarks>
    /// This is a synchronous wrapper around RunAsync() for backward compatibility.
    /// </remarks>
    public void Run()
    {
      try
      {
        RunAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        UnhandledExceptionPolicy.Report(
            "An exception occurred while processing messages. Assuming the server is no longer available.",
            ex);
      }
    }

    /// <summary>
    /// Runs isolated tasks until the server shuts down (async version).
    /// </summary>
    public async Task RunAsync()
    {
      try
      {
        // Start both channels
        await clientChannel.StartAsync(cancellationTokenSource.Token);
        await serverChannel.StartAsync(cancellationTokenSource.Token);

        // Get the message exchange link service
        var link = clientChannel.GetService<ITestIsolationMessageService>(
            GetMessageExchangeLinkServiceName(linkId));

        if (link == null)
          throw new InvalidOperationException(
              "Failed to get ITestIsolationMessageService from server");

        // Message processing loop
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
          var message = await link.ReceiveMessageAsync(PollTimeout, cancellationTokenSource.Token);

          if (message == null)
            continue;

          if (message is ShutdownMessage)
            break;

          if (message is RunIsolatedTaskMessage taskMessage)
          {
            await RunIsolatedTaskAsync(link, taskMessage);
          }
        }
      }
      catch (OperationCanceledException)
      {
        // Expected during shutdown
      }
      catch (Exception ex)
      {
        UnhandledExceptionPolicy.Report(
            "An exception occurred while processing messages. Assuming the server is no longer available.",
            ex);
      }
    }

    private static async Task RunIsolatedTaskAsync(
        ITestIsolationMessageService link,
        RunIsolatedTaskMessage message)
    {
      object? result = null;

      try
      {
        // Create and run the isolated task
        var isolatedTask = (IsolatedTask)Activator.CreateInstance(message.IsolatedTaskType)!;
        result = isolatedTask.Run(message.Arguments);
      }
      catch (Exception ex)
      {
        // Send error response
        await link.SendMessageAsync(new IsolatedTaskFinishedMessage
        {
          Id = message.Id,
          Exception = new ExceptionData(ex)
        });
        return;
      }

      // Send success response
      await link.SendMessageAsync(new IsolatedTaskFinishedMessage
      {
        Id = message.Id,
        Result = result
      });
    }

    private static string GetMessageExchangeLinkServiceName(Guid uniqueId)
    {
      return "TestIsolationServer.MessageExchangeLink." + uniqueId;
    }
  }

  /// <summary>
  /// Service interface for test isolation message exchange (.NET 8 compatible).
  /// </summary>
  /// <remarks>
  /// This replaces IMessageExchangeLink which relied on MarshalByRefObject.
  /// </remarks>
  public interface ITestIsolationMessageService
  {
    /// <summary>
    /// Receives the next message asynchronously.
    /// </summary>
    /// <param name="timeout">The maximum amount of time to wait for a message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next message, or null if a timeout occurred.</returns>
    Task<Common.Messaging.Message?> ReceiveMessageAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMessageAsync(
        Common.Messaging.Message message,
        CancellationToken cancellationToken = default);
  }
}
