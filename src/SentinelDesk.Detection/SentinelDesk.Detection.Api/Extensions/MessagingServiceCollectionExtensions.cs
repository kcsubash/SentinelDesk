using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;
using SentinelDesk.Detection.Api.Configuration;
using System.Security.Authentication;

namespace SentinelDesk.Detection.Api.Extensions
{
    public static class MessagingServiceCollectionExtensions
    {
        private const ushort TlsPort = 5671;

        public static IServiceCollection AddMessaging( 
            this IServiceCollection services, IConfiguration configuration)
            {
            services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(o => IsValidUrl(o.Url),
            "RabbitMq:Url must look like amqps://user:password@host/vhost. Set it in user secrets.")
            .ValidateOnStart();

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    var uri = new Uri(options.Url);
                    var credentials = uri.UserInfo.Split(':', 2);
                    var virtualHost = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

                    cfg.Host(uri.Host, TlsPort, virtualHost, h =>
                    {
                        h.Username(Uri.UnescapeDataString(credentials[0]));
                        h.Password(Uri.UnescapeDataString(credentials[1]));
                        h.UseSsl(s => s.Protocol = SslProtocols.Tls12);
                    });
                });
            });

            return services;

            }

        private static bool IsValidUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == "amqps"
            && uri.UserInfo.Contains(':')
            && uri.AbsolutePath.Trim('/').Length > 0;
    }
}
