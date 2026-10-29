using MassTransit;
using SentinelDesk.Contracts.Events;
using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Engine;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Infrastructure.Messaging
{
    public sealed class MassTransitFindingPublisher : IFindingPublisher
    {

        private readonly IBus bus;

        public MassTransitFindingPublisher(IBus _bus) => bus = _bus; 

        public Task PublishAsync(LoginEvent loginEvent, DetectionFinding finding, CancellationToken cancellationToken)
        {
            var message = new SuspiciousActivityDetected(
                AlertId: Guid.NewGuid(),
                UserId: loginEvent.UserId,
                RuleTriggered: finding.RuleName,
                Description: finding.Reason,
                DetectedAt: loginEvent.Timestamp
                );

            return bus.Publish( message, cancellationToken );
        }
    }
}
