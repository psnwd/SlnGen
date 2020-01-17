// Copyright (c) Jeff Kluge.
//
// Licensed under the MIT license.

using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SlnGen.UnitTests.Common
{
    public class TestLogger : ISlnGenLogger
    {
        public List<Tuple<string, string>> Errors { get; } = new List<Tuple<string, string>>();

        public List<string> HighImportanceMessages { get; } = new List<string>();

        public List<string> LowImportanceMessages { get; } = new List<string>();

        public List<string> NormalImportanceMessages { get; } = new List<string>();

        public List<Tuple<string, string>> Warnings { get; } = new List<Tuple<string, string>>();

        public void LogError(string message, string code = null) => Errors.Add(new Tuple<string, string>(message, code));

        public void LogMessageHigh(string message, params object[] args) => HighImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public void LogMessageLow(string message, params object[] args) => LowImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public void LogMessageNormal(string message, params object[] args) => NormalImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public void LogWarning(string message, string code = null) => Warnings.Add(new Tuple<string, string>(message, code));
    }
}