// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Shared;

    abstract class DefaultDelegatingHandler : IDelegatingHandler
    {
        static readonly Task<Message> DummyResultObject = Task.FromResult((Message)null);

        static int DefaultDelegatingHandler_Id = 0;
        protected int Handler_Id = 0;
        protected string Handler_Type = "DefaultDelegatingHandler";
        int innerHandlerInitializing;
        int innerHandlerInitialized;
        IDelegatingHandler innerHandler;

        protected DefaultDelegatingHandler(IPipelineContext context)
        {
            Handler_Id = DefaultDelegatingHandler_Id++;
            Debug.WriteLine(".ctor Handler_Id id = " + Handler_Id);
            this.Context = context;
        }

        public IPipelineContext Context { get; protected set; }

        public ContinuationFactory<IDelegatingHandler> ContinuationFactory { get; set; }

        public IDelegatingHandler InnerHandler
        {
            get
            {
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.InnerHandler.get - this.innerHandler = " + (this.innerHandler == null ? "null" : "not null"));
                return Volatile.Read(ref this.innerHandlerInitialized) == 0 ? this.EnsureInnerHandlerInitialized() : Volatile.Read(ref this.innerHandler);
            }
            protected set
            {
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.InnerHandler.set - value = " + (value == null ? "null" : "not null"));
                if (Interlocked.CompareExchange(ref this.innerHandlerInitializing, 1, 0) == 0)
                {
                    Volatile.Write(ref this.innerHandler, value);
                    Volatile.Write(ref this.innerHandlerInitialized, 1);
                }
                else
                {
                    Volatile.Write(ref this.innerHandler, value);
                }
            }
        }

        public virtual Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.OpenAsync()");
            return this.InnerHandler?.OpenAsync(explicitOpen, cancellationToken) ?? TaskConstants.Completed;
        }
        
        public virtual Task CloseAsync()
        {
            if (this.InnerHandler == null)
            {
                return TaskConstants.Completed;
            }
            else
            {
                Task closeTask = this.InnerHandler.CloseAsync();
                closeTask.ContinueWith(t => GC.SuppressFinalize(this), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                return closeTask;
            }
        }

        public virtual Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.ReceiveAsync(cancellationToken) ?? DummyResultObject;
        }

        public virtual Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.ReceiveAsync(timeout, cancellationToken) ?? DummyResultObject;
        }

        public virtual Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.CompleteAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.AbandonAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.RejectAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendEventAsync(message, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendEventAsync(messages, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.EnableMethodsAsync(cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.DisableMethodsAsync(cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendMethodResponseAsync(methodResponse, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.EnableTwinPatchAsync(cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendTwinGetAsync(cancellationToken) ?? Task.FromResult((Twin)null);
        }
        
        public virtual Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendTwinPatchAsync(reportedProperties, cancellationToken) ?? TaskConstants.Completed;
        }

        private TwinUpdateCallback twinUpdateHandler;
        public virtual TwinUpdateCallback TwinUpdateHandler
        {
            set
            {
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.TwinUpdateHandler.set()");

                if (this.InnerHandler != null)
                {
                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.TwinUpdateHandler.set() - this.InnerHandler != null");
                    this.InnerHandler.TwinUpdateHandler = value;
                }
                else
                {
                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.TwinUpdateHandler.set() - this.InnerHandler == null");
                    this.twinUpdateHandler = value;
                }
            }
            get
            {
                if (this.InnerHandler != null)
                {
                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.TwinUpdateHandler.get() - this.InnerHandler != null");
                    return this.InnerHandler.TwinUpdateHandler;
                }
                else
                {
                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.TwinUpdateHandler.set() - this.InnerHandler == null");
                    return this.twinUpdateHandler;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            this.innerHandler?.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);   
            GC.SuppressFinalize(this);
        }

        ~DefaultDelegatingHandler()
        {
            this.Dispose(false);
        }

        IDelegatingHandler EnsureInnerHandlerInitialized()
        {
            Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized()");

            if (Interlocked.CompareExchange(ref this.innerHandlerInitializing, 1 /*new value*/, 0 /*to compare*/) == 0)
            {
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized() - initializing...");
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized() - calling this.ContinuationFactory?.Invoke()...");

                IDelegatingHandler result = this.ContinuationFactory?.Invoke(this.Context);

                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized() - setting innerHandler...");
                Volatile.Write(ref this.innerHandler, result);
                Volatile.Write(ref this.innerHandlerInitialized, 1);
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized() - return...");
                return result;
            }
            else
            {
                SpinWait.SpinUntil(() => Volatile.Read(ref this.innerHandlerInitialized) != 1);
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] DefaultDelegatingHandler.EnsureInnerHandlerInitialized() - return...");
                return Volatile.Read(ref this.innerHandler);
            }
        }
    }
}
