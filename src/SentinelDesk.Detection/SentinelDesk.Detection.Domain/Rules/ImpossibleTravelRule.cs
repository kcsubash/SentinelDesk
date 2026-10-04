using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SentinelDesk.Detection.Domain.Rules
{
    public sealed class ImpossibleTravelRule : IDetectionRule
    {
        private const double EarthRadiusKm = 6371.0;
        private readonly double _maxPlausibleSpeedKmh;

        public ImpossibleTravelRule(double maxPlausibleSpeedKmh = 900.0)
        {
            _maxPlausibleSpeedKmh = maxPlausibleSpeedKmh;
        }

        public DetectionResult Evaluate(LoginEvent currentEvent, IUserLoginHistoryStore historyStore)
        {
            if (!currentEvent.WasSuccessful)
                return DetectionResult.NotSuspicious();

            var lastLogin = historyStore.GetLastLogin(currentEvent.UserId);
            if (lastLogin is null || !lastLogin.WasSuccessful)
                return DetectionResult.NotSuspicious();

            var hoursElapsed = (currentEvent.Timestamp - lastLogin.Timestamp).TotalHours;
            if (hoursElapsed <= 0)
                return DetectionResult.NotSuspicious(); // out-of-order or duplicate event, not our convern here

            var distanceKm = CalculateDistanceKm(
                                lastLogin.Latitude, lastLogin.Longitude,
                                currentEvent.Latitude, currentEvent.Longitude);

            var impliedSpeedKmh = distanceKm / hoursElapsed;

            return impliedSpeedKmh > _maxPlausibleSpeedKmh ? DetectionResult.Suspicious(
                $"User {currentEvent.UserId} traveled {distanceKm} km in {hoursElapsed} hours " +
                $"(implied speed {impliedSpeedKmh:F0} kh/h) - exceeds plausible travel speed. ")
                : DetectionResult.NotSuspicious();

        }

        private static double CalculateDistanceKm(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            var dLat = DegreesToRadians(latitude2 - latitude1);
            var dLon = DegreesToRadians(longitude2 - longitude1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(latitude1)) * Math.Cos(DegreesToRadians(latitude2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;

        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
        
       
    }
}
