using Microsoft.Extensions.Options;
using SentinelDesk.Detection.Api.Configuration;
using SentinelDesk.Detection.Domain.Abstractions;
using SentinelDesk.Detection.Domain.Engine;
using SentinelDesk.Detection.Domain.Entities;
using SentinelDesk.Detection.Infrastructure.DataGeneration;

namespace SentinelDesk.Detection.Api.Workers
{
    public sealed class LoginSimulationWorker : BackgroundService
    {
        // The generator spaces burst attempts 3 seconds apart.
        private const int BurstSpacingSeconds = 3;

        private static readonly IReadOnlyList<string> UserIds 
            = SyntheticLoginEventGenerator.UserIds;

        private readonly IFindingPublisher _publisher;
        private readonly SyntheticLoginEventGenerator _generator;
        private readonly DetectionEngine _engine;
        private readonly LoginSimulationOptions _options;
        private readonly int _burstSize;
        private readonly ILogger<LoginSimulationWorker> _logger;

        public LoginSimulationWorker(
            SyntheticLoginEventGenerator generator,
            DetectionEngine engine,
            IOptions<LoginSimulationOptions> simulationOptions,
            IOptions<DetectionOptions> detectionOptions,
            ILogger<LoginSimulationWorker> logger,
            IFindingPublisher pubisher)
        {
            _generator = generator;
            _engine = engine;
            _options = simulationOptions.Value;
            _burstSize = detectionOptions.Value.BruteForceFailureThreshold;
            _logger = logger;
            _publisher = pubisher;
        }


        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Login simulation is disabled.");
                return;
            }

            _logger.LogInformation("Login simulation started. Interval: {Interval}.", _options.Interval);

            using var timer = new PeriodicTimer(_options.Interval);

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                       await RunTickAsync(DateTimeOffset.UtcNow, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Login Simulation tick failed.");
                    }
                }
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Login simulation stopped.");
            }
        }

        private async Task RunTickAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            var userId = UserIds[Random.Shared.Next(UserIds.Count)];
            var (scenario, events) = GenerateScenario(userId, now);

            if (scenario != "Normal")
                _logger.LogInformation("Simulating {scenario} for {UserId}.", scenario, userId);

            foreach (var loginEvent in events)
            {
                foreach (var finding in _engine.Process(loginEvent))
                {
                    _logger.LogWarning("Suspicious activity for {UserId} [{RuleName}]: {Reason}",
                         loginEvent.UserId, finding.RuleName, finding.Reason);
                    await _publisher.PublishAsync(loginEvent, finding, cancellationToken);
                }
            }
        
        }

        private (string scenario, List<LoginEvent> Events) GenerateScenario(string userId, DateTimeOffset now)
        {
            if (Random.Shared.NextDouble() >= _options.AttackScenarioProbability)
                return ("Normal", new List<LoginEvent> { _generator.GenerateNormalLogin(userId, now) });

            switch (Random.Shared.Next(3))
            {
                case 0:
                    var burstStart = now.AddSeconds(-BurstSpacingSeconds * (_burstSize - 1));
                    return ("BruteForce", _generator.GenerateBruteForceBurst(userId, _burstSize, burstStart));

                case 1:
                    var (first, second) = _generator.GenerateImpossibleTravelPair(userId, now.AddMinutes(-10));
                    return ("ImpossibleTravel", new List<LoginEvent> { first, second });

                default:
                    return ("NewLocation", new List<LoginEvent> { _generator.GenerateNewLocationLogin(userId, now) });


            }
        }
    }
}
