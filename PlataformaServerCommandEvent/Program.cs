using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pdc.Messaging;
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
                // Start in their own thread a BoundedContext context that will receive the request Command and publish the change Events
                pro.EndToEndCreateUser();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("presiona para salir...");
            Console.ReadLine();
            pro.cancellAsync();
        }

        private readonly SqliteConnection connection;
        private readonly IConfiguration configuration;
        private CancellationTokenSource cancellationTokenSource;
        private Task denormalizationWorker;
        private Task boundedContextWorker;

        public Program()
        {
            configuration = GetConfiguration();
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
        }

        public async Task EndToEndCreateUser()
        {
            // Prepare test data
            cancellationTokenSource = new CancellationTokenSource();

            // Start in their own thread a Denormalization context that will receive the BoundedContext's events
            var denormalizationServices = GetDenormalizationServices();
            denormalizationWorker = ExecuteDenormalizationAsync(denormalizationServices, cancellationTokenSource.Token);

            // Start in their own thread a BoundedContext context that will receive the request Command and publish the change Events
            var boundedContextServices = GetBoundedContextServices();
            boundedContextWorker = ExecuteBoundedContextAsync(boundedContextServices, cancellationTokenSource.Token);

            // Simulates a client requesting for a change using a Command
            var commandSender = GetProcessManagerServices().GetRequiredService<ICommandSender>();

            var id = Guid.NewGuid().ToString();
            var name = Guid.NewGuid().ToString();
            var code = Guid.NewGuid().ToString();

            await commandSender.SendAsync(new CreateWebUser(id) { username = name, usercode = code });

            name = Guid.NewGuid().ToString();

            await commandSender.SendAsync(new UpdateWebUser(id) { username = name });

            await commandSender.SendAsync(new DeleteWebUser(id));

            // Let the workers do their job and signal them to stop
            //await Task.Delay(30000);

            //cancellationTokenSource.Cancel();
            //await Task.WhenAll(denormalizationWorker, boundedContextWorker);

            // Assert state
            //casos de prueba, si son iguales ok si no algo esta mal
            //var dbContext = denormalizationServices.GetRequiredService<PurchaseOrdersDbContext>();

            //var userWeb = await dbContext.WebUsers.FindAsync(id);

            //if (userWeb == null) throw new Exception("El objeto es nullo");

            //if(!userWeb.usercode.Equals(code)) throw new Exception("El codigo no es igual");

            //if (!userWeb.username.Equals(name)) throw new Exception("El nombre no es igual");

            Console.WriteLine("Programa terminado");
        }

        public async Task cancellAsync()
        {
            cancellationTokenSource.Cancel();
            await Task.WhenAll(denormalizationWorker, boundedContextWorker);
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

        private IServiceProvider GetProcessManagerServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddDebug());

            services.AddAzureServiceBusCommandSender(options => options.Bind(configuration.GetSection("ProcessManager:Sender")));

            return services.BuildServiceProvider();
        }

        private IServiceProvider GetBoundedContextServices()
        {
            var services = new ServiceCollection();

            services.AddCommandHandler(options => options.Bind(configuration.GetSection("CommandHandler")));

            services.AddLogging(builder => builder.AddDebug());
            services.AddAggregateRootFactory();
            services.AddUnitOfWork(options => { });
            services.AddDocumentDBPersistence(options => options.Bind(configuration.GetSection("DocumentDBPersistence")));
            services.AddRedisDistributedLocks(options => options.Bind(configuration.GetSection("RedisDistributedLocks")));
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = configuration["DistributedRedisCache:Configuration"];
                options.InstanceName = configuration["DistributedRedisCache:InstanceName"];
            });
            services.AddAzureServiceBusEventPublisher(options => options.Bind(configuration.GetSection("BoundedContext:Publisher")));

            services.AddCommandHandler<CreateWebUser, CreateWebUserHandler>();
            services.AddCommandHandler<UpdateWebUser, UpdateWebUserHandler>();
            services.AddCommandHandler<DeleteWebUser, DeleteWebUserHandler>();

            return services.BuildServiceProvider();
        }

        private IServiceProvider GetDenormalizationServices()
        {
            var services = new ServiceCollection();

            services.AddDenormalization(options => options.Bind(configuration.GetSection("Denormalization")));

            services.AddLogging(builder => builder.AddDebug());
            services.AddDbContext<PurchaseOrdersDbContext>(options => options.UseSqlite(connection));

            services.AddDenormalizer<PlataformaServerCommandEvent.Denormalizadores.WebUser, WebUserDenormalizer>();

            return services.BuildServiceProvider();
        }

        private static async Task ExecuteBoundedContextAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            using (var scope = services.CreateScope())
            {
                var boundedContext = services.GetRequiredService<IHostedService>();

                try
                {
                    await boundedContext.StartAsync(default);

                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);
                }
                catch (TaskCanceledException)
                {

                }
                finally
                {
                    await boundedContext.StopAsync(default);
                }
            }
        }

        private static async Task ExecuteDenormalizationAsync(IServiceProvider services, CancellationToken cancellationToken)
        {

            using (var scope = services.CreateScope())
            {
                var dbContext = services.GetRequiredService<PurchaseOrdersDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            var denormalization = services.GetRequiredService<IHostedService>();

            try
            {
                await denormalization.StartAsync(default);

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);

            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                await denormalization.StopAsync(default);
            }
        }
    }
}
