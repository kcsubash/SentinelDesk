using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Abstractions
{
    public interface IRecentLoginEventStore
    {
        /// <summary>Adds an event to the user's recent-activity window.</summary>
        void Add(LoginEvent loginEvent);

        /// <summary>Returns the user's events with Timestamp >= since.</summary>
        IReadOnlyCollection<LoginEvent> GetRecent(string userId, DateTimeOffset since);
    }
}
