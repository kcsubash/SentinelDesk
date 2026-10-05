namespace SentinelDesk.Detection.Api.Configuration
{
    public sealed class DetectionOptions
    {
        public const string SectionName = "Detection";

        public double MaxTravelSpeedKmh { get; set; } = 900;
        public int BruteForceFailureThreshold { get; set; } = 5;
        public TimeSpan BruteForceWindow { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RecentEventRetention { get; set; } = TimeSpan.FromMinutes(15);

    }
}
