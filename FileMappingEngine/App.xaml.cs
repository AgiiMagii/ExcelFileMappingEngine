using FileMappingEngine.Lib;
using FileMappingEngine.Lib.Database;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FileMappingEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Services
            services.AddSingleton<FileService>();
            services.AddSingleton<DataService>();
            services.AddSingleton<MappingService>();
            services.AddSingleton<MappingRepository>();
            services.AddSingleton<AppManager>();
            services.AddSingleton(sp => new DbConnFactory("Host=localhost;Port=5432;Username=postgres;Password=GerberaSpotlight;Database=fme"));

            // UI
            services.AddTransient<MainWindow>();
        }
    }

}
