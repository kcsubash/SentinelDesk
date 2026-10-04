using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Rules
{
    public sealed class BruteForceRule
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _window;

        public BruteForceRule(int failureThreshold = 5, TimeSpan? window = null)
        {
            _failureThreshold = failureThreshold;
            _window = window ?? TimeSpan.FromMinutes(5);
        }

        public DetectionResult Evaluate(LoginEvent currentEvent, IEnumerable<LoginEvent> recentEventsForUser)
        {
            if (currentEvent.WasSuccessful)
                return DetectionResult.NotSuspicious();

            var windowStart = currentEvent.Timestamp - _window;

            var recentFailures = recentEventsForUser
                                .Where(e => e.UserId == currentEvent.UserId)
                                .Where(e => !e.WasSuccessful)
                                .Where(e => e.Timestamp >= windowStart && e.Timestamp <= currentEvent.Timestamp)
                                .Count();

            return recentFailures >= _failureThreshold
                                    ? DetectionResult.Suspicious($"{recentFailures} failed login attempts for user {currentEvent.UserId} " +
                                    $"within {_window.TotalMinutes} minutes.")
                                    : DetectionResult.NotSuspicious();
        }

    }
}
