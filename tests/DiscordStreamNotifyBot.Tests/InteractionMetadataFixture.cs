using Discord.Interactions;
using Discord.WebSocket;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace DiscordStreamNotifyBot.Tests
{
    internal sealed class InteractionMetadataFixture : IDisposable
    {
        public DiscordSocketClient Client { get; }
        public InteractionService Interactions { get; }
        public InteractionHandler Handler { get; }

        private InteractionMetadataFixture(
            DiscordSocketClient client,
            InteractionService interactions,
            InteractionHandler handler)
        {
            Client = client;
            Interactions = interactions;
            Handler = handler;
        }

        public static async Task<InteractionMetadataFixture> CreateAsync()
        {
            var client = new DiscordSocketClient();
            var interactions = new InteractionService(client, new InteractionServiceConfig
            {
                UseCompiledLambda = true,
                EnableAutocompleteHandlers = true,
                LocalizationManager = new DescriptionOnlyLocalizationManager()
            });
            var services = new MetadataServiceProvider(client, interactions);

            await interactions.AddModulesAsync(typeof(DescriptionOnlyLocalizationManager).Assembly, services);

            var handler = new InteractionHandler(services, interactions, client, null, null, null, null);
            return new InteractionMetadataFixture(client, interactions, handler);
        }

        public void Dispose()
        {
            Interactions.Dispose();
            Client.Dispose();
        }

        private sealed class MetadataServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceScope
        {
            private readonly Dictionary<Type, object> _services = new();

            public IServiceProvider ServiceProvider => this;

            public MetadataServiceProvider(DiscordSocketClient client, InteractionService interactions)
            {
                _services[typeof(IServiceProvider)] = this;
                _services[typeof(IServiceScopeFactory)] = this;
                _services[typeof(DiscordSocketClient)] = client;
                _services[typeof(InteractionService)] = interactions;
                _services[typeof(YoutubeMemberRoleService)] =
                    RuntimeHelpers.GetUninitializedObject(typeof(YoutubeMemberRoleService));
            }

            public IServiceScope CreateScope()
                => this;

            public void Dispose()
            {
            }

            public object GetService(Type serviceType)
            {
                if (_services.TryGetValue(serviceType, out object service))
                    return service;

                if (serviceType.IsAbstract || serviceType.IsInterface)
                    return null;

                service = RuntimeHelpers.GetUninitializedObject(serviceType);
                _services[serviceType] = service;
                return service;
            }
        }
    }
}
