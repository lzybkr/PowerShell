/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Diagnostics.Tracing;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    // Guid is {092ae15a-d5fb-5d8d-9ffd-68891d24c5f6}
    [EventSource(Name = "Microsoft-PowerShell-Profiler")]
    internal class ProfilerEventSource : EventSource
    {
        internal static ProfilerEventSource Log = new ProfilerEventSource();

        public void SequencePoint(Guid ScriptBlockId, int SequencePoint) { WriteEvent(1, ScriptBlockId, SequencePoint); }
    }

    internal class InternalProfiler : EventListener
    {
        struct ProfileData
        {
            public DateTime Timestamp;
            public Guid ScriptId;
            public int SequencePoint;
        }

        List<ProfileData> data = new List<ProfileData>(5000);

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            switch (eventData.EventId)
            {
                case 1:
                    data.Add(new ProfileData
                    {
                        Timestamp = DateTime.UtcNow,
                        ScriptId = (Guid)eventData.Payload[0],
                        SequencePoint = (int)eventData.Payload[1]
                    });
                    break;
            }
        }
    }

    /// <summary>
    /// Profiles a script.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Measure, "Script", RemotingCapability = RemotingCapability.None)]
    public class MeasureScriptCommand : PSCmdlet
    {
        /// <summary>
        ///
        /// </summary>
        [Parameter(Position = 0)]
        public ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        ///
        /// </summary>
        protected override void EndProcessing()
        {
            using (var el = new InternalProfiler())
            {
                try
                {
                    el.EnableEvents(ProfilerEventSource.Log, EventLevel.LogAlways);

                    ScriptBlock.InvokeWithPipe(
                        useLocalScope: false,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                        dollarUnder: null,
                        input: Utils.EmptyArray<object>(),
                        scriptThis: AutomationNull.Value,
                        outputPipe: new Pipe { NullPipe = true },
                        invocationInfo: null);
                }
                finally
                {
                    el.DisableEvents(ProfilerEventSource.Log);
                }
            }
        }
    }
}
