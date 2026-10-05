namespace SentinelDesk.Detection.Api.Configuration
{
    public sealed class LoginSimulationOptions
    {
        public const string SectionName = "LoginSimulation";

        public bool Enabled { get; set; } = true;
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
        public double AttackScenarioProbability { get; set; } = 0.15;
    }
}
