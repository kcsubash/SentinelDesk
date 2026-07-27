using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Contracts.Events;

public sealed record SuspiciousActivityDetected
(
    Guid AlertId,
    string UserId,
    string RuleTriggered,
    string Description,
    DateTimeOffset DetectedAt
);

