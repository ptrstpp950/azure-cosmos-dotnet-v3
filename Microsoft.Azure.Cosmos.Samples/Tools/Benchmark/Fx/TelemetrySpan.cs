//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    internal struct TelemetrySpan : IDisposable
    {
        private static double[] _latencyHistogram;
        private static int _latencyIndex = -1;

        internal static bool IncludePercentile = false;

        private Stopwatch stopwatch;
        private Func<OperationResult> lazyOperationResult;
        private bool disableTelemetry;

        public static IDisposable StartNew(
            Func<OperationResult> lazyOperationResult,
            bool disableTelemetry)
        {
            if (disableTelemetry || !IncludePercentile)
            {
                return NoOpDisposable.Instance;
            }

            return new TelemetrySpan
            {
                stopwatch = Stopwatch.StartNew(),
                lazyOperationResult = lazyOperationResult,
                disableTelemetry = disableTelemetry
            };
        }

        public void Dispose()
        {
            this.stopwatch.Stop();
            if (!this.disableTelemetry)
            {
                OperationResult operationResult = this.lazyOperationResult();

                if (IncludePercentile)
                {
                    RecordLatency(this.stopwatch.Elapsed.TotalMilliseconds);
                }

                BenchmarkLatencyEventSource.Instance.LatencyDiagnostics(
                    operationResult.DatabseName,
                    operationResult.ContainerName,
                    (int)this.stopwatch.ElapsedMilliseconds,
                    operationResult.LazyDiagnostics);
            }
        }

        private static void RecordLatency(double elapsedMilliseoncds)
        {
            int index = Interlocked.Increment(ref _latencyIndex);
            _latencyHistogram[index] = elapsedMilliseoncds;
        }

        internal static void ResetLatencyHistogram(int totalNumberOfIterations)
        {
            _latencyHistogram = new double[totalNumberOfIterations];
            _latencyIndex = -1;
        }

        internal static double? GetLatencyPercentile(int percentile)
        {
            if (_latencyHistogram == null)
            {
                return null;
            }

            return MathNet.Numerics.Statistics.Statistics.Percentile(_latencyHistogram.Take(_latencyIndex + 1), percentile);
        }

        private class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new NoOpDisposable();

            public void Dispose()
            {
            }
        }
    }
}
