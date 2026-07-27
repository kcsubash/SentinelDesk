using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Core.Domain.Entities
{
    public sealed class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string Username { get; init; }
        public required string PasswordHash { get; init; }
        public required string Role { get; init; } // "Admin" or "Viewer" for now
    }
}
