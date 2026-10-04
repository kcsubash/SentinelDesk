using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Rules
{
    public sealed class NewLocationRule : IDetectionRule
    {
      
        public DetectionResult Evaluate(LoginEvent currentEvent, IUserLoginHistoryStore historyStore)
        {
            if (!currentEvent.WasSuccessful)
                return DetectionResult.NotSuspicious();

            var lastLogin = historyStore.GetLastLogin(currentEvent.UserId);
            if(lastLogin is  null)
                return DetectionResult.NotSuspicious(); //first login ever - nothing to compare against

            return currentEvent.Country != lastLogin.Country
                ? DetectionResult.Suspicious(
                    $"User {currentEvent.UserId} logged in from {currentEvent.Country}, " +
                    $"differing from last known location {lastLogin.Country}.")
                : DetectionResult.NotSuspicious();

        }
    }
}
