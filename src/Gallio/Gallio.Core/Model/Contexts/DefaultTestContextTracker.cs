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

namespace Gallio.Model.Contexts
{
    /// <summary>
    /// The default context tracker tracks the current context by way
    /// of the thread's <see cref="AsyncLocal{T}"/>.
    /// </summary>
    public class DefaultTestContextTracker : ITestContextTracker
    {
        private const int CleanupInterval = 60000;

        private readonly Dictionary<Thread, ITestContext> threadOverrides;
        private ITestContext globalContext;
        private System.Threading.Timer threadCleanupTimer;

        private static readonly AsyncLocal<InternalContextLink> asyncLocalContextLink = new();

        /// <summary>
        /// Initializes the context tracker.
        /// </summary>
        public DefaultTestContextTracker()
        {
            threadOverrides = new Dictionary<Thread, ITestContext>();
        }

        private object SyncRoot => threadOverrides;

        /// <inheritdoc />
        public ITestContext GlobalContext
        {
            get => globalContext;
            set => globalContext = value;
        }

        /// <inheritdoc />
        public ITestContext CurrentContext => GetCurrentContextImpl();

        /// <inheritdoc />
        public IDisposable EnterContext(ITestContext context)
        {
            var previousTopLink = TopContextLinkForCurrentThread;
            TopContextLinkForCurrentThread = new InternalContextLink(previousTopLink, context);
            return new InternalContextCookie(this, previousTopLink);
        }

        /// <inheritdoc />
        public void SetThreadDefaultContext(Thread thread, ITestContext context)
        {
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));

            lock (SyncRoot)
            {
                if (context == null || context == globalContext)
                    threadOverrides.Remove(thread);
                else
                    threadOverrides[thread] = context;

                ConfigureThreadCleanupTimerWithLock();
            }
        }

        /// <inheritdoc />
        public ITestContext GetThreadDefaultContext(Thread thread)
        {
            if (thread == null)
                throw new ArgumentNullException(nameof(thread));

            lock (SyncRoot)
            {
                threadOverrides.TryGetValue(thread, out var context);
                return context ?? globalContext;
            }
        }

        private ITestContext GetCurrentContextImpl()
        {
            var contextLink = TopContextLinkForCurrentThread;
            if (contextLink != null)
                return contextLink.Context;

            return GetThreadDefaultContext(Thread.CurrentThread);
        }

        private void ExitContext(InternalContextLink previousTopLink, int threadId)
        {
            if (Thread.CurrentThread.ManagedThreadId != threadId)
                throw new InvalidOperationException("The context cookie does not belong to this thread.");

            var currentLink = TopContextLinkForCurrentThread;
            while (currentLink != null)
            {
                currentLink = currentLink.ParentLink;
                if (currentLink == previousTopLink)
                {
                    TopContextLinkForCurrentThread = currentLink;
                    return;
                }
            }

            throw new InvalidOperationException("The context has already been exited.");
        }

        private InternalContextLink TopContextLinkForCurrentThread
        {
            get => asyncLocalContextLink.Value;
            set => asyncLocalContextLink.Value = value;
        }

        private void ConfigureThreadCleanupTimerWithLock()
        {
            if (threadOverrides.Count == 0)
            {
                threadCleanupTimer?.Dispose();
                threadCleanupTimer = null;
            }
            else if (threadCleanupTimer == null)
            {
                threadCleanupTimer = new System.Threading.Timer(CleanupThreads, null, CleanupInterval, CleanupInterval);
            }
        }

        private void CleanupThreads(object _)
        {
            lock (SyncRoot)
            {
                var deadThreads = new List<Thread>();

                foreach (var thread in threadOverrides.Keys)
                    if (!thread.IsAlive)
                        deadThreads.Add(thread);

                foreach (var thread in deadThreads)
                    threadOverrides.Remove(thread);

                ConfigureThreadCleanupTimerWithLock();
            }
        }

        // Represents a single link in a chain of contexts per-thread.
        private sealed class InternalContextLink
        {
            public InternalContextLink ParentLink { get; }
            public ITestContext Context { get; }

            public InternalContextLink(InternalContextLink parentLink, ITestContext context)
            {
                ParentLink = parentLink;
                Context = context;
            }
        }

        private sealed class InternalContextCookie : IDisposable
        {
            private readonly DefaultTestContextTracker contextTracker;
            private readonly InternalContextLink previousTopLink;
            private readonly int threadId;

            public InternalContextCookie(DefaultTestContextTracker contextTracker, InternalContextLink previousTopLink)
            {
                this.contextTracker = contextTracker;
                this.previousTopLink = previousTopLink;
                threadId = Thread.CurrentThread.ManagedThreadId;
            }

            public void Dispose()
            {
                contextTracker.ExitContext(previousTopLink, threadId);
            }
        }
    }
}