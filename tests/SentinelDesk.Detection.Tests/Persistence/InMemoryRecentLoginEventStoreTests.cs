using SentinelDesk.Detection.Domain.Entities;
using SentinelDesk.Detection.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Tests.Persistence
{
    public class InMemoryRecentLoginEventStoreTests
    {
        private static readonly DateTimeOffset BaseTime = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

        private static LoginEvent CreateEvent(string userId, DateTimeOffset timestamp) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = "203.0.113.10",
            Country = "US",
            Latitude = 39.29,
            Longitude = -76.61,
            Timestamp = timestamp,
            WasSuccessful = false
        };

        [Fact]
        public void GetRecent_UnknownUser_ReturnsEmpty()
        {
            var store = new InMemoryRecentLoginEventStore();

            var result = store.GetRecent("user-1", BaseTime);

            Assert.Empty(result);
        }

        [Fact]
        public void GetRecent_IncludesEventExactlyAtSince_ExcludesOlder()
        {
            var store = new InMemoryRecentLoginEventStore();
            var older = CreateEvent("user-1", BaseTime.AddSeconds(-1));
            var atBoundary = CreateEvent("user-1", BaseTime);
            var newer = CreateEvent("user-1", BaseTime.AddMinutes(1));
            store.Add(older);
            store.Add(atBoundary);
            store.Add(newer);

            var result = store.GetRecent("user-1", BaseTime);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(older, result);
            Assert.Contains(atBoundary, result);
            Assert.Contains(newer, result);
        }

        [Fact]
        public void GetRecent_ReturnsOnlyRequestedUsersEvents()
        {
            var store = new InMemoryRecentLoginEventStore();
            var user1Event = CreateEvent("user-1", BaseTime);
            store.Add(user1Event);
            store.Add(CreateEvent("user-2", BaseTime));

            var result = store.GetRecent("user-1", BaseTime.AddMinutes(-1));

            Assert.Same(user1Event, Assert.Single(result));
        }

        [Fact]
        public void Add_EvictsEventsOlderThanRetention()
        {
            var store = new InMemoryRecentLoginEventStore(TimeSpan.FromMinutes(10));
            var stale = CreateEvent("user-1", BaseTime);
            var fresh = CreateEvent("user-1", BaseTime.AddMinutes(11));
            store.Add(stale);
            store.Add(fresh);

            var result = store.GetRecent("user-1", DateTimeOffset.MinValue);

            Assert.Same(fresh, Assert.Single(result));
        }

        [Fact]
        public void Add_KeepsEventExactlyAtRetentionBoundary()
        {
            var store = new InMemoryRecentLoginEventStore(TimeSpan.FromMinutes(10));
            var atBoundary = CreateEvent("user-1", BaseTime);
            var latest = CreateEvent("user-1", BaseTime.AddMinutes(10));
            store.Add(atBoundary);
            store.Add(latest);

            var result = store.GetRecent("user-1", DateTimeOffset.MinValue);

            Assert.Equal(2, result.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_NonPositiveRetention_Throws(int minutes)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new InMemoryRecentLoginEventStore(TimeSpan.FromMinutes(minutes)));
        }

    }

}
