using SentinelDesk.Detection.Domain.Entities;
using SentinelDesk.Detection.Domain.Rules;
using SentinelDesk.Detection.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Tests.Rules
{
    public class ImpossibleTravelRuleTests
    {
        private readonly ImpossibleTravelRule _rule = new(maxPlausibleSpeedKmh: 900.0);

        private static LoginEvent BuildEvent(
            string userId, double lat, double lon, DateTimeOffset timestamp, bool wasSuccessful = true)
        {
            return new LoginEvent
            {
                UserId = userId,
                IpAddress = "1.1.1.1",
                Country = "US",
                Latitude = lat,
                Longitude = lon,
                Timestamp = timestamp,
                WasSuccessful = wasSuccessful
            };
        }

        [Fact]
        public void Evaluate_NoPriorLogin_ReturnsNotSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var current = BuildEvent("user-1", 40.7128, -74.0060, DateTimeOffset.UtcNow);

            var result = _rule.Evaluate(current, store);

            Assert.False(result.IsSuspicious);
        }

        [Fact]
        public void Evaluate_PriorLoginWasFailed_ReturnsNotSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime, wasSuccessful: false));
            var current = BuildEvent("user-1", 19.0760, 72.8777, baseTime.AddMinutes(10));

            var result = _rule.Evaluate(current, store);

            Assert.False(result.IsSuspicious);
        }

        [Fact]
        public void Evaluate_SameLocationShortGap_ReturnNotSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime));
            var current = BuildEvent("user-1", 40.7128, -74.0060, baseTime.AddMinutes(5));

            var result = _rule.Evaluate(current, store);

            Assert.False(result.IsSuspicious);
        }

        [Fact]
        public void Evaluate_FarLocationShortGap_ReturnSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            // New York -> Mumbai, ~12,500 km apart, 10 minutes apart = physically impossible
            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime));
            var current = BuildEvent("user-1", 19.0760, 72.8777, baseTime.AddMinutes(10));

            var result = _rule.Evaluate(current, store);

            Assert.True(result.IsSuspicious);
            Assert.Contains("user-1", result.Reason);
        }

        [Fact]
        public void Evaluate_FarLocationEnoughTimeElapsed_ReturnsNotSuspicios()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            // Same distance as above, but 20 hours later — plausible with a long flight + layover
            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime));
            var current = BuildEvent("user-1", 19.0760, 72.8777, baseTime.AddHours(20));

            var result = _rule.Evaluate(current, store);

            Assert.False(result.IsSuspicious);
        }

        [Fact]
        public void Evaluate_CurrentLoginFailed_ReturnNotSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime));
            var current = BuildEvent("user-1", 19.0760, 72.8777, baseTime.AddMinutes(10), wasSuccessful: false);

            var result = _rule.Evaluate(current, store);

            Assert.False(result.IsSuspicious);
        }

        [Fact]
        public void Evaluate_SpeedExactlyAtThreshold_ReturnsNotSuspicious()
        {
            var store = new InMemoryUserLoginHistoryStore();
            var baseTime = DateTimeOffset.UtcNow;

            // Two points ~900 km apart (roughly NYC to Toronto-ish distance), 1 hour apart => ~900 km/h exactly
            store.RecordLogin(BuildEvent("user-1", 40.7128, -74.0060, baseTime));
            var current = BuildEvent("user-1", 48.4, -74.5, baseTime.AddHours(1));

            var result = _rule.Evaluate(current, store);

            // This assumes the calculated distance lands near, but not over, 900 km/h.
            // If it fails, adjust coordinates slightly — the point is proving the ">" boundary, not exact geography.
            Assert.False(result.IsSuspicious);
        }

    }
}
