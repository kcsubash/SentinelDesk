using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Entities
{
    public sealed class LoginEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string UserId { get; init; }
        public required string IpAddress { get; init; }
        public required string Country { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
        public required bool WasSuccessful { get; init; }
    }
}
