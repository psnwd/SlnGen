// Copyright (c) Jeff Kluge.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using SlnGen.Common;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SlnGen.ConsoleApp
{
    /// <summary>
    /// Handles logging.
    /// </summary>
    internal class LoggingService : ISlnGenLogger, IDisposable
    {
        private readonly LogBuildEventDelegate _logBuildEvent;

        private int _errorLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <param name="projectCollection">A <see cref="ProjectCollection" /> to use for logging.</param>
        public LoggingService(ProjectCollection projectCollection)
        {
            _logBuildEvent = Delegate.CreateDelegate(
                typeof(LogBuildEventDelegate),
                typeof(ProjectCollection).GetProperty("LoggingService", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(projectCollection),
                "LogBuildEvent") as LogBuildEventDelegate;

            if (_logBuildEvent == null)
            {
                throw new InvalidOperationException();
            }

            _logBuildEvent(new BuildStartedEventArgs("Build Started", null, environmentOfBuild: Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(i => (string)i.Key).ToDictionary(i => (string)i.Key, i => (string)i.Value)));
        }

        private delegate void LogBuildEventDelegate(BuildEventArgs buildEventArgs);

        /// <summary>
        /// Gets a value indicating whether or not any errors have been logged.
        /// </summary>
        public bool HasLoggedErrors => _errorLogged != 0;

        /// <inheritdoc/>
        public void Dispose()
        {
            _logBuildEvent(new BuildFinishedEventArgs(HasLoggedErrors ? "Failed" : "Success", null, !HasLoggedErrors));
        }

        /// <inheritdoc/>
        public void LogError(string message, string code = null)
        {
            Interlocked.CompareExchange(ref _errorLogged, 1, 0);

            _logBuildEvent(new BuildErrorEventArgs(
                subcategory: null,
                code: code,
                file: "SlnGen",
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null));
        }

        /// <inheritdoc/>
        public void LogMessageHigh(string message, params object[] args) => _logBuildEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.High, DateTime.UtcNow, args));

        /// <inheritdoc/>
        public void LogMessageLow(string message, params object[] args) => _logBuildEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Low, DateTime.UtcNow, args));

        /// <inheritdoc/>
        public void LogMessageNormal(string message, params object[] args) => _logBuildEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Normal, DateTime.UtcNow, args));

        /// <inheritdoc/>
        public void LogWarning(string message, string code = null)
        {
            _logBuildEvent(new BuildWarningEventArgs(null, code, "SlnGen", 0, 0, 0, 0, message, null, null));
        }
    }
}