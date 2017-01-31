// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;

    class DeviceClientPipelineBuilder : IDeviceClientPipelineBuilder
    {
        static int DeviceClientPipelineBuilder_Seed = 0;
        protected int DeviceClientPipelineBuilder_Id = 0;

        readonly List<ContinuationFactory<IDelegatingHandler>> pipeline = new List<ContinuationFactory<IDelegatingHandler>>();

        public DeviceClientPipelineBuilder()
        {
            DeviceClientPipelineBuilder_Id = DeviceClientPipelineBuilder_Seed++;
            Debug.WriteLine("DeviceClientPipelineBuilder_Id = " + DeviceClientPipelineBuilder_Id);
        }

        public IDeviceClientPipelineBuilder With(ContinuationFactory<IDelegatingHandler> delegatingHandlerCreator)
        {
            Debug.WriteLine("DeviceClientPipelineBuilder_Id = " + DeviceClientPipelineBuilder_Id + " with");

            this.pipeline.Add(delegatingHandlerCreator);
            return this;
        }

        public IDelegatingHandler Build(IPipelineContext context)
        {
            Debug.WriteLine("DeviceClientPipelineBuilder_Id = " + DeviceClientPipelineBuilder_Id + " build");

            if (this.pipeline.Count == 0)
            {
                throw new InvalidOperationException("Pipeline is not setup");
            }

            IDelegatingHandler root = this.WrapContinuationFactory(0)(context);
            return root;
        }

        ContinuationFactory<IDelegatingHandler> WrapContinuationFactory(int currentId)
        {
            Debug.WriteLine("DeviceClientPipelineBuilder_Id = " + DeviceClientPipelineBuilder_Id + " WrapContinuationFactory");

            ContinuationFactory<IDelegatingHandler> current = this.pipeline[currentId];
            if (currentId == this.pipeline.Count - 1)
            {
                return current;
            }
            ContinuationFactory<IDelegatingHandler> next = this.WrapContinuationFactory(currentId + 1);
            ContinuationFactory<IDelegatingHandler> currentHandlerFactory = current;
            current = ctx =>
            {
                IDelegatingHandler delegatingHandler = currentHandlerFactory(ctx);
                delegatingHandler.ContinuationFactory = next;
                return delegatingHandler;
            };
            return current;
        }
    }
}