using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Core.Domain.Entities
{
    public sealed class Alert
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string UserId { get; init; }
        public required string RuleTriggered { get; init; }
        public required string Description { get; init; }
        public required DateTimeOffset DetectedAt { get; init; }
        public AlertStatus Status { get; private set; } = AlertStatus.New;

        public void Acknowledge()
        {
            if (Status != AlertStatus.New)
                throw new InvalidOperationException($"Cannot acknowledge an alert with status {Status}.");

            Status = AlertStatus.Acknowledged;
            
        }

        public void Resolve()
        {
            if (Status == AlertStatus.Resolved)
                throw new InvalidOperationException("Alert is already resolved.");

            Status = AlertStatus.Resolved;
        }
    }
}
