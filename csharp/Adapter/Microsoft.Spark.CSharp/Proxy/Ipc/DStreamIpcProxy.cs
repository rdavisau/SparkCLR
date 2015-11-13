﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Interop.Ipc;

namespace Microsoft.Spark.CSharp.Proxy.Ipc
{
    internal class DStreamIpcProxy : IDStreamProxy
    {
        internal readonly JvmObjectReference jvmDStreamReference;

        internal DStreamIpcProxy(JvmObjectReference jvmDStreamReference)
        {
            this.jvmDStreamReference = jvmDStreamReference;
        }

        public int SlideDuration
        {
            get
            {
                string durationId = (string)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "slideDuration");
                return (int)(double)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(new JvmObjectReference(durationId), "milliseconds");
            }
        }

        public IDStreamProxy Window(int windowSeconds, int slideSeconds = 0)
        {
            string windowId = null;
            var windowDurationReference = SparkCLRIpcProxy.JvmBridge.CallConstructor("org.apache.spark.streaming.Duration", new object[] { windowSeconds * 1000 });

            if (slideSeconds <= 0)
            {
                windowId = (string)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "window", new object[] { windowDurationReference });
                return new DStreamIpcProxy(new JvmObjectReference(windowId));
            }

            var slideDurationReference = SparkCLRIpcProxy.JvmBridge.CallConstructor("org.apache.spark.streaming.Duration", new object[] { slideSeconds * 1000 });
            windowId = (string)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "window", new object[] { windowDurationReference, slideDurationReference });

            return new DStreamIpcProxy(new JvmObjectReference(windowId));
        }

        public IDStreamProxy AsJavaDStream()
        {
            var id = (string)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "asJavaDStream");
            return new DStreamIpcProxy(new JvmObjectReference(id));
        }

        public void CallForeachRDD(byte[] func, string deserializer)
        {
            SparkCLRIpcProxy.JvmBridge.CallStaticJavaMethod("org.apache.spark.streaming.api.csharp.CSharpDStream", "callForeachRDD", new object[] { jvmDStreamReference, func, deserializer });
        }


        public void Print(int num = 10)
        {
            SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "print", new object[] { num });
        }

        public void Persist(StorageLevelType storageLevelType)
        {
            var jstorageLevel = SparkContextIpcProxy.GetJavaStorageLevel(storageLevelType);
            SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "persist", new object[] { jstorageLevel });
        }

        public void Checkpoint(long intervalMs)
        {
            var jinterval = SparkCLRIpcProxy.JvmBridge.CallConstructor("org.apache.spark.streaming.Duration", new object[] { intervalMs });
            SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "checkpoint", new object[] { jinterval });
        }

        public IRDDProxy[] Slice(long fromUnixTime, long toUnixTime)
        {
            return ((List<JvmObjectReference>)SparkCLRIpcProxy.JvmBridge.CallNonStaticJavaMethod(jvmDStreamReference, "slice", new object[] { fromUnixTime, toUnixTime }))
                .Select(obj => new RDDIpcProxy(obj)).ToArray();
        }
    }
}