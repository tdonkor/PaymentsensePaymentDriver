using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using Acrelec.Mockingbird.Payment.ExtensionMethods;
using System;
using System.ServiceModel;
using System.Threading;

namespace Acrelec.Mockingbird.Payment
{
    internal class Program
    {
        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        public const string NAME = "UK_PAYMENTSENSE";

        private static void Main(string[] args)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Log.Info($"{assembly.GetTitle()} {assembly.GetFileVersion()} [build timestamp: {assembly.GetBuildTimestamp():yyyy/MM/dd HH:mm:ss}]");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            var appConfig = AppConfiguration.Instance;

            using (var host = new ServiceHost(typeof(PaymentService), new Uri("net.pipe://localhost")))
            using (new Heartbeat())
            {
                host.AddServiceEndpoint(typeof(IPaymentService), new NetNamedPipeBinding(), NAME);
                host.Open();

                Log.Info("Driver Service Running...");

                ManualResetEvent.WaitOne();
            }

            Log.Info("Driver application requested to shut down.");
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Log.Info("Shell driver application exiting");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error((e.ExceptionObject as Exception).ToString());
        }
    }
}

