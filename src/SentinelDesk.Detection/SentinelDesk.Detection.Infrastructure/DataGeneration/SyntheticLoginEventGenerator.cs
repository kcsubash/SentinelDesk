using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Infrastructure.DataGeneration
{
    public sealed class SyntheticLoginEventGenerator
    {
        private readonly Random _random = new();

        // Known locations: Country, City (for readability only) Lat, Long
        private static readonly (string Country, string City, double Lat, double Long)[] Locations =
        {
            ("US", "New York", 40.7129, -74.0060),
            ("US", "Los Angeles",34.0522, -118.2437),
            ("IN", "Mumbai",     19.0760,   72.8777),
            ("GB", "London",     51.5074,   -0.1278),
            ("AU", "Sydney",    -33.8688,  151.2093),
            ("DE", "Berlin",     52.5200,   13.4050),
            ("JP", "Tokyo",      35.6762,  139.6503),
            ("BR", "Sao Paulo", -23.5505,  -46.6333),
        };

        // Fixed "home" location per synthetic user - the baseline for anomaly comparisons
        private static readonly Dictionary<string, int> UserHomeLocationIndex = new()
        {
            { "user-1", 0 }, // New York
            { "user-2", 2 }, // Mumbai
            { "user-3", 3 }, // London
            { "user-4", 5 }, // Berlin
            { "user-5", 6 }, // Tokyo
        };

        public static IReadOnlyList<string> UserIds => UserHomeLocationIndex.Keys.ToList();

        // --- Scenario 1: normal login, from the user's home location ---
        public LoginEvent GenerateNormalLogin(string userId, DateTimeOffset? timeStamp = null)
        {
            var loc = HomeLocation(userId);
            return BuildEvent(userId, loc, timeStamp ?? DateTimeOffset.UtcNow, wasSuccessful: true);
        }

        // --- Scenario 2: brute-force - aburst of failed logins, same user, tight time window ---
        public List<LoginEvent> GenerateBruteForceBurst(string userId, int attemptCount, DateTimeOffset startTime)
        {
            var loc = HomeLocation(userId);
            var events = new List<LoginEvent>();

            for (int i = 0; i < attemptCount; i++)
            {
                // Failed attempts a few seconds apart - mimics a script hammering the login endpoint
                var timestamp = startTime.AddSeconds(i * 3);
                events.Add(BuildEvent(userId, loc, timestamp, wasSuccessful: false));
            }

            return events;
        }

        // --- Scenario 3: impossible travel — two successful logins, far apart, short time gap ---
        public (LoginEvent First, LoginEvent Second) GenerateImpossibleTravelPair(string userId, DateTimeOffset firstTimestamp)
        {
            var homeIndex = UserHomeLocationIndex[userId];
            var farIndex = (homeIndex + Locations.Length / 2) % Locations.Length; // roughly opposite side of the list

            var first = BuildEvent(userId, Locations[homeIndex], firstTimestamp, wasSuccessful: true);

            // Second login only minutes later, but from a location that's physically impossible to reach that fast
            var second = BuildEvent(userId, Locations[farIndex], firstTimestamp.AddMinutes(10), wasSuccessful: true);

            return (first, second);

        }


        // --- Scenario 4: new/unusual location — success, but from somewhere other than home ---
        public LoginEvent GenerateNewLocationLogin (string userId, DateTimeOffset? timeStamp = null)
        {
            var homeIndex = UserHomeLocationIndex[userId];
            int newIndex;

            do
            {
                newIndex = _random.Next(Locations.Length);
            } while (newIndex == homeIndex);

            return BuildEvent(userId, Locations[newIndex], timeStamp ?? DateTimeOffset.UtcNow, wasSuccessful: true);
        }


        // --- Scenario 4: Mixed batch: mostly normal noise, for general-purpose testing/demo ---
        public List<LoginEvent> GenerateNormalBatch(int count, DateTimeOffset? startTime = null)
        {
            var events = new List<LoginEvent>();
            var time = startTime ?? DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                var userId = UserIds[_random.Next(UserIds.Count)];
                events.Add(GenerateNormalLogin(userId, time.AddSeconds(i * 5)));
            }

            return events;
        }

        private LoginEvent BuildEvent(string userId, (string Country, string City, double Lat, double Long) location, DateTimeOffset timestamp, bool wasSuccessful)
        {
            return new LoginEvent
            {
                UserId = userId,
                IpAddress = GenerateFakeIp(),
                Country = location.Country,
                Latitude = location.Lat,
                Longitude = location.Long,
                Timestamp = timestamp,
                WasSuccessful = wasSuccessful
            };
        }

        private string GenerateFakeIp()
        {
            return $"{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}";
        }

        private (string Country, string City, double Lat, double Long) HomeLocation(string userId)
        {
            return Locations[UserHomeLocationIndex[userId]];
        }
    }
}
