using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Engine;
using SentinelDesk.Detection.Domain.Entities;
using SentinelDesk.Detection.Domain.Rules;
using SentinelDesk.Detection.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Tests.Engine
{
    public class DetectionEngineTests
    {
        private static readonly DateTimeOffset BaseTime = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

        private static DetectionEngine CreateEngine(
        IUserLoginHistoryStore historyStore,
        params IDetectionRule[] rules) =>
        new(rules, new BruteForceRule(), historyStore, new InMemoryRecentLoginEventStore());

        private static LoginEvent CreateEvent(
            DateTimeOffset timestamp,
            bool wasSuccessful,
            string country = "US",
            double latitude = 39.29,
            double longitude = -76.61) => new()
            {
                Id = Guid.NewGuid(),
                UserId = "user-1",
                IpAddress = "203.0.113.10",
                Country = country,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = timestamp,
                WasSuccessful = wasSuccessful
            };

        private static LoginEvent CreateMoscowEvent(DateTimeOffset timestamp, bool wasSuccessful) =>
            CreateEvent(timestamp, wasSuccessful, country: "RU", latitude: 55.75, longitude: 37.62);

        private sealed class StubRule : IDetectionRule
        {
            private readonly DetectionResult _result;

            public StubRule(DetectionResult result) => _result = result;

            public DetectionResult Evaluate(LoginEvent loginEvent, IUserLoginHistoryStore historyStore) => _result;
        }

        private sealed class SpyRule : IDetectionRule
        {
            public bool WasCalled { get; private set; }
            public LoginEvent? LastLoginSeen { get; private set; }

            public DetectionResult Evaluate(LoginEvent loginEvent, IUserLoginHistoryStore historyStore)
            {
                WasCalled = true;
                LastLoginSeen = historyStore.GetLastLogin(loginEvent.UserId);
                return DetectionResult.NotSuspicious();
            }
        }


        [Fact]
        public void Process_NoRuleFires_ReturnsEmpty()
        {
            var engine = CreateEngine(new InMemoryUserLoginHistoryStore(),
                new StubRule(DetectionResult.NotSuspicious()));

            var findings = engine.Process(CreateEvent(BaseTime, wasSuccessful: true));

            Assert.Empty(findings);
        }

        [Fact]
        public void Process_SuspiciousRule_ReturnsOnlyItsFinding()
        {
            var engine = CreateEngine(new InMemoryUserLoginHistoryStore(),
                new StubRule(DetectionResult.NotSuspicious()),
                new StubRule(DetectionResult.Suspicious("stub reason")));

            var findings = engine.Process(CreateEvent(BaseTime, wasSuccessful: true));

            Assert.Equal("stub reason", Assert.Single(findings).Reason);
        }

        [Fact]
        public void Process_RunsRulesBeforeRecordingLogin()
        {
            var historyStore = new InMemoryUserLoginHistoryStore();
            var spy = new SpyRule();
            var engine = CreateEngine(historyStore, spy);
            var loginEvent = CreateEvent(BaseTime, wasSuccessful: true);

            engine.Process(loginEvent);

            Assert.True(spy.WasCalled);
            Assert.Null(spy.LastLoginSeen);
            Assert.Same(loginEvent, historyStore.GetLastLogin("user-1"));
        }

        [Fact]
        public void Process_FailedLogin_DoesNotOverwriteHistory()
        {
            var historyStore = new InMemoryUserLoginHistoryStore();
            var engine = CreateEngine(historyStore);
            var success = CreateEvent(BaseTime, wasSuccessful: true);

            engine.Process(success);
            engine.Process(CreateEvent(BaseTime.AddMinutes(1), wasSuccessful: false));

            Assert.Same(success, historyStore.GetLastLogin("user-1"));
        }

        [Fact]
        public void Process_BruteForce_FiresOnThresholdEvent()
        {
            var engine = CreateEngine(new InMemoryUserLoginHistoryStore());

            for (var i = 0; i < 4; i++)
            {
                var early = engine.Process(CreateEvent(BaseTime.AddSeconds(i * 10), wasSuccessful: false));
                Assert.Empty(early);
            }

            var findings = engine.Process(CreateEvent(BaseTime.AddSeconds(40), wasSuccessful: false));

            Assert.Single(findings);
        }

        [Fact]
        public void Process_FailedAttemptFromFarAway_DoesNotHideImpossibleTravel()
        {
            var engine = CreateEngine(new InMemoryUserLoginHistoryStore(), new ImpossibleTravelRule());

            engine.Process(CreateEvent(BaseTime, wasSuccessful: true));
            var afterFailure = engine.Process(CreateMoscowEvent(BaseTime.AddMinutes(1), wasSuccessful: false));
            var afterSuccess = engine.Process(CreateMoscowEvent(BaseTime.AddMinutes(2), wasSuccessful: true));

            Assert.Empty(afterFailure);
            Assert.Single(afterSuccess);
        }

    }
}
