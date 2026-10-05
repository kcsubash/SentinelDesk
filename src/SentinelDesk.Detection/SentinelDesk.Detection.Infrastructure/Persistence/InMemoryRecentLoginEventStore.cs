using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Infrastructure.Persistence
{
    public sealed class InMemoryRecentLoginEventStore : IRecentLoginEventStore
    {
        private readonly ConcurrentDictionary<string, List<LoginEvent>> _eventsByUser = new();
        private readonly TimeSpan _retention;
        public InMemoryRecentLoginEventStore(TimeSpan? retention = null)
        {
            _retention = retention ?? TimeSpan.FromMinutes(15);

            if (_retention <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retention), "Retention must be positive.");

            
        }

        public void Add(LoginEvent loginEvent)
        {
            ArgumentNullException.ThrowIfNull(loginEvent);

            var events = _eventsByUser.GetOrAdd(loginEvent.UserId, _ => new List<LoginEvent>());
            var cutoff = loginEvent.Timestamp - _retention;

            lock(events)
            {
                events.Add(loginEvent);
                events.RemoveAll(e => e.Timestamp < cutoff);
            }
        }

        public IReadOnlyCollection<LoginEvent> GetRecent(string userId, DateTimeOffset since)
        {
            if (!_eventsByUser.TryGetValue(userId, out var events))
                return Array.Empty<LoginEvent>();

            lock (events)
            {
                return events.Where(e => e.Timestamp >= since).ToList();
            }
        }
    }
}
