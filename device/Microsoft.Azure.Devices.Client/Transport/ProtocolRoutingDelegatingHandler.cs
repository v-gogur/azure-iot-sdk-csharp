// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
#if !PCL
    using System.Net.Sockets;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;

    /// <summary>
    /// Transport handler router. 
    /// Tries to open open connection in the protocol order it was set. 
    /// If fails tries to open the next one, etc.
    /// </summary>
    class ProtocolRoutingDelegatingHandler : DefaultDelegatingHandler
    {
        internal delegate IDelegatingHandler TransportHandlerFactory(IotHubConnectionString iotHubConnectionString, ITransportSettings transportSettings);

        public ProtocolRoutingDelegatingHandler(IPipelineContext context):
            base(context)
        {
            Handler_Type = "ProtocolRoutingDelegatingHandler";
            Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "][" + Handler_Type + "] .ctor ProtocolRoutingDelegatingHandler");
        }

        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.OpenAsync()");
            await this.TryOpenPrioritizedTransportsAsync(explicitOpen, cancellationToken);
        }

        async Task TryOpenPrioritizedTransportsAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync() - 1");

            Exception lastException = null;
            // Concrete Device Client creation was deferred. Use prioritized list of transports.
            foreach (ITransportSettings transportSetting in this.Context.Get<ITransportSettings[]>())
            {
                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 2");

                if (cancellationToken.IsCancellationRequested)
                {

                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 3 --> Cancelled!");

                    return;
                }

                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 3 --> NOT Cancelled!");

                try
                {
                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 4 --> Context.Set()");

                    this.Context.Set(transportSetting);

                    if (this.InnerHandler == null)
                    {
                        Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 5 --> InnerHandler = ... ");
                        this.InnerHandler = this.ContinuationFactory(this.Context);
                    }

                    Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 6 --> OpenAsync()");
                    // Try to open a connection with this transport
                    await base.OpenAsync(explicitOpen, cancellationToken);
                }
                catch (Exception exception)
                {
                    try
                    {
                        if (this.InnerHandler != null)
                        {
                            await this.CloseAsync();
                        }
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        //ignore close failures    
                    }

                    if (!(exception is IotHubCommunicationException ||
                          exception is TimeoutException ||
#if !PCL
                          exception is SocketException ||
#endif
                          exception is AggregateException))
                    {
                        throw;
                    }

                    var aggregateException = exception as AggregateException;
                    if (aggregateException != null)
                    {
                        ReadOnlyCollection<Exception> innerExceptions = aggregateException.Flatten().InnerExceptions;
                        if (!innerExceptions.Any(x => x is IotHubCommunicationException ||
#if !PCL
                            x is SocketException ||
#endif
                            x is TimeoutException))
                        {
                            throw;
                        }
                    }

                    lastException = exception;

                    // open connection failed. Move to next transport type
                    continue;
                }

                Debug.WriteLine("[" + Environment.CurrentManagedThreadId + "][" + Handler_Id + "] ProtocolRoutingDelegatingHandler.TryOpenPrioritizedTransportsAsync - 7 --> returning...");
                return;
            }

            if (lastException != null)
            {
                throw new IotHubCommunicationException("Unable to open transport", lastException);
            }
        }
    }
}