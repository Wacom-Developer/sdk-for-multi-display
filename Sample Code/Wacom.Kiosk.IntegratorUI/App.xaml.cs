using System.Windows;
using Microsoft.Extensions.Logging;
using Ninject;
using Integrator.Logger.Serilogger;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IKernel container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureContainer();
            ComposeObjects();
            Current.MainWindow.Show();
        }

        private void ConfigureContainer()
        {
            this.container = new StandardKernel();
           container.Bind<ILogger>().To<Serilogger>().InTransientScope();
            //container.Bind<ILogger>().To<NlogLogger>().InTransientScope();
        }

        private void ComposeObjects()
        {
            Current.MainWindow = this.container.Get<MainWindow>();
        }
    }
}
