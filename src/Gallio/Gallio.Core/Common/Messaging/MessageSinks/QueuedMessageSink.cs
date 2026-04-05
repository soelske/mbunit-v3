// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2026 Bart Suelze
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

using Gallio.Common.Policies;

namespace Gallio.Common.Messaging.MessageSinks
{
    /// <summary>
    /// Wraps a <see cref="IMessageSink"/> and queues messages so that messages are
    /// published asynchronously.
    /// </summary>
    /// <remarks>
    /// .NET 8 replacement: uses <see cref="Task.Run"/> instead of the removed
    /// <c>Delegate.BeginInvoke</c> API.
    /// </remarks>
    [Serializable]
    public class QueuedMessageSink : IMessageSink, IDisposable
    {
        private readonly IMessageSink messageSink;
        private readonly Queue<Message> queue;

        // Null when no async publish loop is running; non-null while one is in flight.
        private volatile Task? asyncTask;

        /// <summary>
        /// Creates a queued message sink.
        /// </summary>
        /// <param name="messageSink">The message sink to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageSink"/> is null.</exception>
        public QueuedMessageSink(IMessageSink messageSink)
        {
            if (messageSink == null)
                throw new ArgumentNullException("messageSink");

            this.messageSink = messageSink;
            queue = new Queue<Message>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                Flush();
            }
            catch (Exception ex)
            {
                UnhandledExceptionPolicy.Report("An unhandled exception occurred while flushing a queued message sink.", ex);
            }
        }

        /// <inheritdoc />
        public void Publish(Message message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            message.Validate();

            lock (queue)
            {
                queue.Enqueue(message);

                if (asyncTask == null)
                    asyncTask = Task.Run(AsyncPublishLoop);
            }
        }

        /// <summary>
        /// Flushes the queue by waiting for the background publish loop to finish.
        /// </summary>
        public void Flush()
        {
            Task? snapshot;
            lock (queue)
                snapshot = asyncTask;

            if (snapshot != null && !snapshot.IsCompleted)
                snapshot.Wait();
        }

        private void AsyncPublishLoop()
        {
            for (;;)
            {
                Message message;
                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        asyncTask = null;
                        return;
                    }

                    message = queue.Dequeue();
                }

                try
                {
                    messageSink.Publish(message);
                }
                catch (Exception ex)
                {
                    UnhandledExceptionPolicy.Report(
                        "An unhandled exception occurred while asynchronously publishing a queued message.", ex);
                }
            }
        }
    }
}
