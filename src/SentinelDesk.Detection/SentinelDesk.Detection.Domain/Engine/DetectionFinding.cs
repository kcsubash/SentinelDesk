using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Engine
{
    public sealed record DetectionFinding(string RuleName, string Reason);
    
}
