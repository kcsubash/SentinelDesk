using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Abstractions
{
    public sealed class DetectionResult
    {
        public required bool IsSuspicious { get; init; }
        public string? Reason { get; init; }

        public static DetectionResult NotSuspicious() => new() { IsSuspicious = false };

        public static DetectionResult Suspicious(string reason) =>
            new() { IsSuspicious = true, Reason = reason };

    }
}
