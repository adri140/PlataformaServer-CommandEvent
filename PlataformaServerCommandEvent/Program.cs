using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pdc.Hosting;
using Pdc.Messaging;
using Pdc.Messaging.ServiceBus;
using PlataformaServerCommandEvent.Denormalizadores;
using PlataformaServerCommandEvent.Internals;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PlataformaServerCommandEvent
{
    class Program
    {
        static void Main(string[] args)
        {
            Program pro = new Program();
            try
            {
                runMe(pro);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("presiona para salir...");
            Console.ReadLine();

            try
            {
                stop(pro);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async static void runMe(Program pro)
        {
            await pro.RunServer();
        }

        public async static void stop(Program pro)
        {
            await pro.StopServer();
            Thread.Sleep(1000);
        }

        private readonly IConfiguration configuration;
        private readonly IServiceProvider services;
        private IHostedService boundedContext;
        private IServiceScope scope;

        public Program()
        {
            configuration = GetConfiguration();
            services = GetBoundedContextServices();
        }

        public async Task RunServer()
        {
            scope = services.CreateScope();
            using (scope)
            {
                boundedContext = services.GetRequiredService<IHostedService>();
                await boundedContext.StartAsync(default);
            }
        }

        public async Task StopServer()
        {
            using (scope)
            {
                await boundedContext.StopAsync(default);
            }
        }

        private static IConfiguration GetConfiguration()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var c = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "DistributedRedisCache:InstanceName", "Cache." },
                    { "RedisDistributedLocks:InstanceName", "Locks." },
                    { "DocumentDBPersistence:Database", "Tests" },
                    { "DocumentDBPersistence:Collection", "Events" },
                    { "ProcessManager:Sender:EntityPath", "core-test-commands" },
                    { "BoundedContext:Publisher:EntityPath", "core-test-events" },
                    { "CommandHandler:Receiver:EntityPath", "core-test-commands" },
                    { "Denormalization:Subscribers:0:EntityPath", "core-test-events" },
                    { "Denormalization:Subscribers:0:SubscriptionName", "core-test-events-denormalizers" }
                })
                .AddUserSecrets(assembly, optional: true)
                .AddEnvironmentVariables()
                .Build();

#if !DEBUG
            return new ConfigurationBuilder()
                .AddConfiguration(c)
                .AddAzureKeyVault(c["AzureKeyVault:Uri"], c["AzureKeyVault:ClientId"], c["AzureKeyVault:ClientSecret"])
                .Build();
#else
            return c;
#endif
        }

        private IServiceProvider GetBoundedContextServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddDebug());

            //recive commands y los transforma a eventos
            services.AddAzureServiceBusCommandReceiver(
                builder =>
                {
                    builder.AddCommandHandler<CreateWebUser, CreateWebUserHandler>();
                    builder.AddCommandHandler<UpdateWebUser, UpdateWebUserHandler>();
                    builder.AddCommandHandler<DeleteWebUser, DeleteWebUserHandler>();
                },
                new Dictionary<string, Action<CommandBusOptions>>
                {
                    ["Core"] = options => configuration.GetSection("CommandHandler:Receiver").Bind(options),
                });

            //recivir eventos
            /*services.AddAzureServiceBusEventSubscriber(
                builder =>
                {
                    builder.AddDenormalizer<Pdc.Integration.Denormalization.Customer, CustomerDenormalizer>();
                    builder.AddDenormalizer<Pdc.Integration.Denormalization.CustomerDetail, CustomerDetailDenormalizer>();
                },
                new Dictionary<string, Action<EventBusOptions>>
                {
                    ["Core"] = options => configuration.GetSection("Denormalization:Subscribers:0").Bind(options),
                });*/

            //enviar eventos
            services.AddAggregateRootFactory();
            services.AddUnitOfWork();
            services.AddDocumentDBPersistence(options => configuration.GetSection("DocumentDBPersistence").Bind(options));
            services.AddRedisDistributedLocks(options => configuration.GetSection("RedisDistributedLocks").Bind(options));
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = configuration["DistributedRedisCache:Configuration"];
                options.InstanceName = configuration["DistributedRedisCache:InstanceName"];
            });

            //services.AddDbContext<PurchaseOrdersDbContext>(options => options.UseSqlite(connection));

            //services.AddAzureServiceBusCommandSender(options => configuration.GetSection("ProcessManager:Sender").Bind(options));
            services.AddAzureServiceBusEventPublisher(options => configuration.GetSection("BoundedContext:Publisher").Bind(options)); //publicador de eventos

            services.AddHostedService<HostedService>();

            return services.BuildServiceProvider();
        }
    }
}
