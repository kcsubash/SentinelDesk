using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Abstractions
{
    public interface IDetectionRule
    {
        DetectionResult Evaluate(LoginEvent currentEvent, IUserLoginHistoryStore historyStore);
    }
}
