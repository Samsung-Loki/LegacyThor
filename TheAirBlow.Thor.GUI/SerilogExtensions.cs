// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace TheAirBlow.Thor.GUI;

public static class SerilogExtensions
{
    const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

    /// <summary>
    /// Write log events to the provided <see cref="System.IO.TextWriter"/>.
    /// </summary>
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    /// <param name="textWriter">The text writer to write log events to.</param>
    /// <param name="outputTemplate">Message template describing the output format.</param>
    /// <param name="restrictedToMinimumLevel">The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static LoggerConfiguration LogWindow(
        this LoggerSinkConfiguration sinkConfiguration,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string outputTemplate = DefaultOutputTemplate,
        IFormatProvider formatProvider = null,
        LoggingLevelSwitch levelSwitch = null)
    {
        if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

        var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        var sink = new LogWindowSink(formatter);
        return sinkConfiguration.Sink(sink, restrictedToMinimumLevel, levelSwitch);
    }

    /// <summary>
    /// Write log events to the provided <see cref="System.IO.TextWriter"/>.
    /// </summary>
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    /// <param name="textWriter">The text writer to write log events to.</param>
    /// <param name="formatter">Text formatter used by sink.</param>
    /// /// <param name="restrictedToMinimumLevel">The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static LoggerConfiguration LogWindow(
        this LoggerSinkConfiguration sinkConfiguration,
        ITextFormatter formatter,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch levelSwitch = null)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));

        var sink = new LogWindowSink(formatter);
        return sinkConfiguration.Sink(sink, restrictedToMinimumLevel, levelSwitch);
    }
}