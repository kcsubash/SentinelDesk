using SentinelDesk.Detection.Domain.Engine;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Abstractions
{
    public interface IFindingPublisher
    {
        Task PublishAsync(LoginEvent loginEvent,DetectionFinding finding, CancellationToken cancellationToken);
    }
}
