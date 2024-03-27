using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Polly;
using System.Net.Http;

namespace PollyDemos.OutputHelpers;

    public record struct DemoProgress(Statistic[] Statistics, IEnumerable<ColoredMessage> Messages)
    {
        public DemoProgress(Statistic[] statistics, ColoredMessage message)
            : this(statistics, new[] { message })
        {
        }
    }

    public enum Color
    {
        Default,
        Green,
        Red,
        Yellow,
        Magenta,
        White
    }
    public record struct ColoredMessage(string Message, Color Color)
    {
        public ColoredMessage(string message)
            : this(message, Color.Default)
        {
        }
    }
    public record struct Statistic(string Description, double Value, Color Color)
    {
        public Statistic(string description, double value)
            : this(description, value, Color.Default)
        {
        }
    }
    public abstract class DemoBase
    {
        protected int TotalRequests;
        protected int EventualSuccesses;
        protected int EventualFailures;
        protected int Retries;

        // In the case of WPF the stdIn is redirected.
        protected static bool ShouldTerminateByKeyPress() => !Console.IsInputRedirected && Console.KeyAvailable;

        public abstract string Description { get; }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress);

        public abstract Statistic[] LatestStatistics { get; }

        protected DemoProgress ProgressWithMessage(string message)
            => new(LatestStatistics, new ColoredMessage(message));

        protected DemoProgress ProgressWithMessage(string message, Color color)
            => new(LatestStatistics, new ColoredMessage(message, color));

        protected void PrintHeader(IProgress<DemoProgress> progress)
        {
            progress.Report(ProgressWithMessage(GetType().Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));
        }

    }
