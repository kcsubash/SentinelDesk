namespace SentinelDesk.Detection.Api.Configuration
{
    public sealed class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string Url { get; set; } = string.Empty;
    }
}
