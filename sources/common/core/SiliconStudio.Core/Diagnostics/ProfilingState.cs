﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SiliconStudio.Core.Diagnostics
{
    /// <summary>
    /// A profiler state contains information of a portion of code being profiled. See remarks.
    /// </summary>
    /// <remarks>
    /// This struct is not intended to be used directly but only through <see cref="Profiler.Begin()"/>.
    /// You can still attach some attributes to it while profiling a portion of code.
    /// </remarks>
    public struct ProfilingState : IDisposable
    {
        private static readonly Logger Logger = Profiler.Logger;
        private readonly int profilingId;
        private readonly ProfilingKey profilingKey;
        private bool isEnabled;
        private ProfilerDisposeEventDelegate disposeProfileDelegate;
        private Dictionary<object, object> attributes;
        private long startTime;
        private string beginText;

        internal ProfilingState(int profilingId, ProfilingKey profilingKey, bool isEnabled)
        {
            this.profilingId = profilingId;
            this.profilingKey = profilingKey;
            this.isEnabled = isEnabled;
            this.disposeProfileDelegate = null;
            attributes = null;
            beginText = null;
            startTime = 0;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized
        {
            get
            {
                return profilingKey != null;
            }
        }

        /// <summary>
        /// Gets the profiling unique identifier.
        /// </summary>
        /// <value>The profiling unique identifier.</value>
        public int ProfilingId
        {
            get
            {
                return profilingId;
            }
        }

        /// <summary>
        /// Gets the profiling key.
        /// </summary>
        /// <value>The profiling key.</value>
        public ProfilingKey ProfilingKey
        {
            get
            {
                return profilingKey;
            }
        }

        /// <summary>
        /// Gets or sets the dispose profile delegate.
        /// </summary>
        /// <value>The dispose profile delegate.</value>
        public ProfilerDisposeEventDelegate DisposeDelegate
        {
            get
            {
                return disposeProfileDelegate;
            }
            set
            {
                disposeProfileDelegate = value;
            }
        }

        /// <summary>
        /// Checks if the profiling key is enabled and update this instance. See remarks.
        /// </summary>
        /// <remarks>
        /// This can be used for long running profiling that are using markers and want to log markers if 
        /// the profiling was activated at runtime.
        /// </remarks>
        public void CheckIfEnabled()
        {
            isEnabled = Profiler.IsEnabled(profilingKey);
        }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Value of a key.</returns>
        /// <remarks>If profiling was not enabled for this profile key, the attribute is not stored</remarks>
        public object GetAttribute(string key)
        {
            if (attributes == null)
            {
                return null;
            }
            object result;
            attributes.TryGetValue(key, out result);
            return result;
        }

        /// <summary>
        /// Sets the attribute value for a specified key. See remarks.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <remarks>If profiling was not enabled for this profile key, the attribute is not stored</remarks>
        public void SetAttribute(string key, object value)
        {
            // If profiling is not enabled, doesn't store anything
            if (!isEnabled) return;

            if (attributes == null)
            {
                attributes = new Dictionary<object, object>();
            }
            attributes[key] = value;
        }

        public void Dispose()
        {
            // Perform a Start event only if the profiling is running
            if (!isEnabled) return;

            // Give a chance to the profiling to end and put some property in this profiler state
            disposeProfileDelegate?.Invoke(ref this);

            End();
        }

        /// <summary>
        /// Emits a Begin profiling event.
        /// </summary>
        public void Begin()
        {
            EmitEvent(ProfilingMessageType.Begin);
        }

        /// <summary>
        /// Emits a Begin profiling event with the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Begin(string text)
        {
            EmitEvent(ProfilingMessageType.Begin, text);
        }

        /// <summary>
        /// Emits a Begin profiling event with the specified formatted text.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="textFormatArguments">The text format arguments.</param>
        public void Begin(string textFormat, params object[] textFormatArguments)
        {
            EmitEvent(ProfilingMessageType.Begin, textFormat, textFormatArguments);
        }

        /// <summary>
        /// Emits a Begin event with the specified formatted text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void Begin(string text, ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Begin, text, value0, value1, value2, value3);
        }

        /// <summary>
        /// Emits a Begin event with the specified formatted text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void Begin(ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Begin, null, value0, value1, value2, value3);
        }

        /// <summary>
        /// Emits a Mark event.
        /// </summary>
        public void Mark()
        {
            EmitEvent(ProfilingMessageType.Mark);
        }

        /// <summary>
        /// Emits a Mark event with the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Mark(string text)
        {
            EmitEvent(ProfilingMessageType.Mark, text);
        }

        /// <summary>
        /// Emits a Mark event with the specified formatted text.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="textFormatArguments">The text format arguments.</param>
        public void Mark(string textFormat, params object[] textFormatArguments)
        {
            EmitEvent(ProfilingMessageType.Mark, textFormat, textFormatArguments);
        }

        /// <summary>
        /// Emits a Mark profiling event with the specified text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void Mark(string text, ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Mark, text, value0, value1, value2, value3);
        }

        /// <summary>
        /// Emits a Mark profiling event with the specified text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void Mark(ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Mark, null, value0, value1, value2, value3);
        }

        /// <summary>
        /// Emits a End profiling event.
        /// </summary>
        public void End()
        {
            EmitEvent(ProfilingMessageType.End);
        }

        /// <summary>
        /// Emits a End profiling event with the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void End(string text)
        {
            EmitEvent(ProfilingMessageType.End, text);
        }

        /// <summary>
        /// Emits a End profiling event with the specified formatted text.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="textFormatArguments">The text format arguments.</param>
        public void End(string textFormat, params object[] textFormatArguments)
        {
            EmitEvent(ProfilingMessageType.End, textFormat, textFormatArguments);
        }

        /// <summary>
        /// Emits a End profiling event with the specified custom value.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void End(string text, ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.End, text, value0, value1, value2, value3);
        }

        /// <summary>
        /// Emits a End profiling event with the specified custom value.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value0">Can be int, float, long or double</param>
        /// <param name="value1">Can be int, float, long or double</param>
        /// <param name="value2">Can be int, float, long or double</param>
        /// <param name="value3">Can be int, float, long or double</param>
        public void End(ProfilingCustomValue? value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.End, null, value0, value1, value2, value3);
        }

        private void EmitEvent(ProfilingMessageType profilingType, string text = null)
        {
            // Perform a Mark event only if the profiling is running
            if (!isEnabled) return;

            var timeStamp = Stopwatch.GetTimestamp();

            // In the case of begin/end, reuse the text from the `begin`event 
            // if the text is null for `end` event.
            if (text == null && profilingType != ProfilingMessageType.Mark)
                text = beginText;

            if (profilingType == ProfilingMessageType.Begin)
            {
                startTime = timeStamp;
                beginText = text;
            }
            else if (profilingType == ProfilingMessageType.End)
            {
                beginText = null;
            }

            // Create profiler event
            // TODO ideally we should make a copy of the attributes
            var profilerEvent = new ProfilingEvent(profilingId, profilingKey, profilingType, timeStamp, timeStamp - startTime, text, attributes);

            // Send profiler event to Profiler
            Profiler.ProcessEvent(ref profilerEvent);
        }

        private void EmitEvent(ProfilingMessageType profilingType, string textFormat, params object[] textFormatArguments)
        {
            // Perform a Mark event only if the profiling is running
            if (!isEnabled) return;

            var timeStamp = Stopwatch.GetTimestamp();

            // In the case of begin/end, reuse the text from the `begin`event 
            // if the text is null for `end` event.
            var text = textFormat != null ? string.Format(textFormat, textFormatArguments) : profilingType == ProfilingMessageType.Mark ? null : beginText;

            if (profilingType == ProfilingMessageType.Begin)
            {
                startTime = timeStamp;
                beginText = text;
            }
            else if (profilingType == ProfilingMessageType.End)
            {
                beginText = null;
            }

            // Create profiler event
            // TODO ideally we should make a copy of the attributes
            var profilerEvent = new ProfilingEvent(profilingId, profilingKey, profilingType, timeStamp, timeStamp - startTime, text, attributes);

            // Send profiler event to Profiler
            Profiler.ProcessEvent(ref profilerEvent);
        }

        private void EmitEvent(ProfilingMessageType profilingType, string text, ProfilingCustomValue? value0, ProfilingCustomValue? value1, ProfilingCustomValue? value2, ProfilingCustomValue? value3)
        {
            // Perform a Mark event only if the profiling is running
            if (!isEnabled) return;

            var timeStamp = Stopwatch.GetTimestamp();

            if (profilingType == ProfilingMessageType.Begin)
            {
                startTime = timeStamp;
            }

            //this actually stores the LAST text into beginText so to be able to add it at the end
            if(profilingType != ProfilingMessageType.End && text != null)
            {
                beginText = text;
            }

            // Create profiler event
            var profilerEvent = new ProfilingEvent(profilingId, profilingKey, profilingType, timeStamp, timeStamp - startTime, beginText ?? text, attributes, value0, value1, value2, value3);

            if (profilingType == ProfilingMessageType.End)
            {
                beginText = null;
            }

            // Send profiler event to Profiler
            Profiler.ProcessEvent(ref profilerEvent);
        }

        private TimeSpan GetElapsedTime()
        {
            var delta = Stopwatch.GetTimestamp() - startTime;
            return new TimeSpan((delta * 10000000) / Stopwatch.Frequency);
        }
    }
}