using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using SentinelDesk.Detection.Domain.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Engine
{
    public sealed class DetectionEngine
    {
        private readonly IReadOnlyList<IDetectionRule> _rules;
        private readonly BruteForceRule _bruteForceRule;
        private readonly IUserLoginHistoryStore _historyStore;
        private readonly IRecentLoginEventStore _recentEventStore;

        public DetectionEngine(
            IEnumerable<IDetectionRule> rules,
            BruteForceRule bruteForceRule,
            IUserLoginHistoryStore historyStore,
            IRecentLoginEventStore recentEventStore
            )
        {
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(bruteForceRule);
            ArgumentNullException.ThrowIfNull(historyStore);
            ArgumentNullException.ThrowIfNull(recentEventStore);

            _rules = rules.ToList();
            _bruteForceRule = bruteForceRule;
            _historyStore = historyStore;
            _recentEventStore = recentEventStore;
        }

        public IReadOnlyList<DetectionFinding> Process(LoginEvent loginEvent)
        {
            ArgumentNullException.ThrowIfNull(loginEvent);

            var findings = new List<DetectionFinding>();


            // 1. Check, then record: history must still hold the previous successful login.
            foreach (var rule in _rules)
            {
                var result = rule.Evaluate(loginEvent, _historyStore);
                if (result.IsSuspicious)
                    findings.Add(new DetectionFinding(rule.Name, result.Reason ?? string.Empty));
            }


            // 2. Record, then check: the current event must be in the window before counting.
            _recentEventStore.Add(loginEvent);
            var since = loginEvent.Timestamp - _bruteForceRule.Window;
            var recentEvents = _recentEventStore.GetRecent(loginEvent.UserId, since);

            var bruteForceResult = _bruteForceRule.Evaluate(loginEvent, recentEvents);
            if (bruteForceResult.IsSuspicious)
                findings.Add(new DetectionFinding(_bruteForceRule.Name, bruteForceResult.Reason ?? string.Empty));


            // 3. Only successful logins become the new baseline.
            if (loginEvent.WasSuccessful)
                _historyStore.RecordLogin(loginEvent);

            return findings;

        }
    }
}
