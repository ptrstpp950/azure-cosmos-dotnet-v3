//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace CosmosBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal struct Summary
    {
        private const int MsPerSecond = 1000;

        public long SuccessfulOpsCount;
        public long FailedOpsCount;
        public double RuCharges;
        public double ElapsedMs;

        public double Rups()
        {
            return Math.Round(
                    Math.Min((this.RuCharges / this.ElapsedMs) * MsPerSecond, this.RuCharges),
                    2);
        }

        public double Rps()
        {
            long total = this.SuccessfulOpsCount + this.FailedOpsCount;
            return Math.Round(
                    Math.Min((total / this.ElapsedMs) * MsPerSecond, total),
                    2);
        }

        public void Print(long globalTotal)
        {
            Utility.TeePrint("Stats, total: {0,5}   success: {1,5}   fail: {2,3}   RPs: {3,5}   RUps: {4,5}",
                globalTotal,
                this.SuccessfulOpsCount,
                this.FailedOpsCount,
                this.Rps(),
                this.Rups());
        }

        public static Summary operator +(Summary arg1, Summary arg2)
        {
            return new Summary()
            {
                SuccessfulOpsCount = arg1.SuccessfulOpsCount + arg2.SuccessfulOpsCount,
                FailedOpsCount = arg1.FailedOpsCount + arg2.FailedOpsCount,
                RuCharges = arg1.RuCharges + arg2.RuCharges,
                ElapsedMs = arg1.ElapsedMs + arg2.ElapsedMs,
            };
        }

        public static Summary operator -(Summary arg1, Summary arg2)
        {
            return new Summary()
            {
                SuccessfulOpsCount = arg1.SuccessfulOpsCount - arg2.SuccessfulOpsCount,
                FailedOpsCount = arg1.FailedOpsCount - arg2.FailedOpsCount,
                RuCharges = arg1.RuCharges - arg2.RuCharges,
                ElapsedMs = arg1.ElapsedMs - arg2.ElapsedMs,
            };
        }
    }
}
