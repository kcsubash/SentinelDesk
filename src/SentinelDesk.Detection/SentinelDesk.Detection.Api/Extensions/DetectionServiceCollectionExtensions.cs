using Microsoft.Extensions.Options;
using SentinelDesk.Detection.Api.Configuration;
using SentinelDesk.Detection.Api.Workers;
using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Engine;
using SentinelDesk.Detection.Domain.Rules;
using SentinelDesk.Detection.Infrastructure.DataGeneration;
using SentinelDesk.Detection.Infrastructure.Persistence;

namespace SentinelDesk.Detection.Api.Extensions
{
    public static class DetectionServiceCollectionExtensions
    {
        public static IServiceCollection AddDetection(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DetectionOptions>()
                .Bind(configuration.GetSection(DetectionOptions.SectionName))
                .Validate(o => o.MaxTravelSpeedKmh > 0,
                    "Detection:MaxTravelSpeedKmh must be positive.")
                .Validate(o => o.BruteForceFailureThreshold > 0,
                    "Detection:BruteForceFailureThreshold must be positive.")
                .Validate(o => o.BruteForceWindow > TimeSpan.Zero,
                    "Detection:BruteForceWindow must be positive.")
                .Validate(o => o.RecentEventRetention >= o.BruteForceWindow,
                    "Detection:RecentEventRetention must be at least BruteForceWindow.")
                .ValidateOnStart();

            services.AddOptions<LoginSimulationOptions>()
              .Bind(configuration.GetSection(LoginSimulationOptions.SectionName))
              .Validate(o => o.Interval >= TimeSpan.FromSeconds(1),
                  "LoginSimulation: Interval must be at least 1 second.")
              .Validate(o => o.AttackScenarioProbability is >= 0 and <= 1,
                  "LoginSimulation: AttackScenarioProbability must be between 0 and 1.")
              .ValidateOnStart();

            // Stores: hold state, must be singletons.
            services.AddSingleton<IUserLoginHistoryStore, InMemoryUserLoginHistoryStore>();
            services.AddSingleton<IRecentLoginEventStore>(sp =>
                new InMemoryRecentLoginEventStore(GetOptions(sp).RecentEventRetention));

            // Rules: every IDetectionRule registered here is injected into the engine.
            services.AddSingleton<IDetectionRule>(sp =>
                new ImpossibleTravelRule(GetOptions(sp).MaxTravelSpeedKmh));
            services.AddSingleton<IDetectionRule, NewLocationRule>();

            services.AddSingleton(sp =>
            {
                var options = GetOptions(sp);
                return new BruteForceRule(options.BruteForceFailureThreshold, options.BruteForceWindow);
            });

            services.AddSingleton<DetectionEngine>();

            services.AddSingleton<SyntheticLoginEventGenerator>();
            services.AddHostedService<LoginSimulationWorker>();

            return services;
        }

        private static DetectionOptions GetOptions(IServiceProvider sp) =>
            sp.GetRequiredService<IOptions<DetectionOptions>>().Value;
        
    }
}
