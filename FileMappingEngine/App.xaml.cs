using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Database;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace FileMappingEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();
            base.OnStartup(e);

            var services = new ServiceCollection();

            ConfigureServices(services, configuration);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services, IConfiguration configuration)
        {
            // Services
            services.AddSingleton<FileService>();
            services.AddSingleton<DataService>();
            services.AddSingleton<MappingService>();
            services.AddSingleton<MappingRepository>();
            services.AddSingleton<AppManager>();
            services.AddSingleton(sp =>
            {
                string connectionString =
                    configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Connection string not found.");

                return new DbConnFactory(connectionString);
            });

            // UI
            services.AddTransient<MainWindow>();
        }
    }

}
