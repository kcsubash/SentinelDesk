using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Infrastructure.Persistence
{
    public sealed class InMemoryUserLoginHistoryStore : IUserLoginHistoryStore
    {
        private readonly ConcurrentDictionary<string, LoginEvent> _lastLoginsByUser = new();

        public LoginEvent? GetLastLogin(string userId)
        {
            return _lastLoginsByUser.TryGetValue(userId, out var loginEvent)
                ? loginEvent : null;
        }

        public void RecordLogin(LoginEvent loginEvent)
        {
            _lastLoginsByUser.AddOrUpdate(
                loginEvent.UserId,
                loginEvent,
                (_, _) => loginEvent);
        }
    }
}
