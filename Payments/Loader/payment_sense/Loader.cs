using Acrelec.Mockingbird.Feather.Peripherals.Models;
using Acrelec.Mockingbird.Feather.Peripherals.Payment;
using Acrelec.Mockingbird.Feather.Peripherals.Payment.Model;
using Acrelec.Mockingbird.Feather.Peripherals.Settings;
using Acrelec.Mockingbird.Interfaces.Peripherals;
using Acrelec.Mockingbird.Payment.Contracts;
using Acrelec.Mockingbird.Payment.ExtensionMethods;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Reflection;

namespace Acrelec.Mockingbird.Payment
{
    using Payment = Feather.Peripherals.Payment.Model.Payment;

    [Export(typeof(IPayment))]
    public class Loader : IPaymentCard
    {
        /// <summary>
        /// The factory details of the Payment including a list of settings
        /// </summary>
        private readonly Payment _currentPaymentInitConfig;

        /// <summary>
        /// Driver location for core 4 and above
        /// </summary>
        private string driverLocation = string.Empty;

        /// <summary>
        /// Object in charge of log saving
        /// </summary>
        private readonly ILogger _logger;

        private ChannelFactory<IPaymentService> _channelFactory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        [ImportingConstructor]
        public Loader(ILogger logger)
        {
            _logger = logger;

            driverLocation = $@"C:\Acrelec\Core\Peripherals\Payments\Drivers\{Constants.PAYMENT_NAME}\{DriverVersion}\Driver\{Constants.DRIVER_FOLDER_NAME}";


            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            _logger.Info(Constants.LOG_FILE, $"{assembly.GetTitle()} {assembly.GetFileVersion()} [build timestamp: {assembly.GetBuildTimestamp():yyyy/MM/dd HH:mm:ss}]");

            _logger.Info(Constants.LOG_FILE, "Loader method started...");

            LastStatus = PeripheralStatus.PeripheralNotConfigured();

            //Init the settings
            _currentPaymentInitConfig = new Payment
            {
                Id = Constants.ID,
                PaymentName = Constants.PAYMENT_NAME,
                DriverFolderName = Constants.DRIVER_FOLDER_NAME,
                Type = Constants.PAYMENT_TYPE.ToString(),
                ConfigurationSettings = new List<AdminPeripheralSetting>()
                {

                    //new AdminPeripheralSetting()
                    //{
                    //    ControlType = SettingDataType.SerialPortSelection,
                    //    ControlName = "COM Port number",
                    //    RealName = "Port",
                    //    CurrentValue = "COM9",
                    //    ControlDescription = "Serial communication port for the EFT terminal (VX820)"
                    //},

                    new AdminPeripheralSetting()
                    {
                        ControlType = SettingDataType.Int,
                        ControlName = "POS Number",
                        RealName = "PosNumber",
                        CurrentValue = "1",
                        ControlDescription = "POS Number"
                    }

                }
            };
        }

        public string DriverId => Constants.ID;

        public string PeripheralName => Constants.PAYMENT_NAME;

        public string PeripheralType => Constants.PAYMENT_TYPE.ToString();

        public IPaymentCallbacks PaymentCallbacks { get; set; }

        public PeripheralStatus LastStatus { get; private set; }

        public PaymentCapability Capability => new PaymentCapability()
        {
            AcceptsCash = false,
            CanRefund = false,
            PaymentApplications = null,
            ReceivePayProgressCalls = false
        };

        public int MinAPILevel => 3;

        public string DriverVersion
        {
            get
            {
                try
                {
                    return Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Get Payment Driver Application Version: \r\n{ex.Message}");
                }
                return string.Empty;
            }
        }

        public bool Init()
        {
            try
            {
                _logger.Info(Constants.LOG_FILE, "Initializing payment...");

                //Start the driver Payment application (if it's already open try to close it before starting it)
                LaunchDriver();

                Thread.Sleep(2000);

                var binding = new NetNamedPipeBinding()
                {
                    CloseTimeout = TimeSpan.FromDays(1),
                    OpenTimeout = TimeSpan.FromDays(1),
                    ReceiveTimeout = TimeSpan.FromDays(1),
                    SendTimeout = TimeSpan.FromDays(1)
                };

                // Create communication channel
                _logger.Info(Constants.LOG_FILE, "Creating channel factory...");
                _channelFactory = new ChannelFactory<IPaymentService>(binding,
                    new EndpointAddress($"net.pipe://localhost/{Constants.PAYMENT_NAME}"));

                var parameters = _currentPaymentInitConfig.ConfigurationSettings.ToDictionary(_ => _.RealName, _ => _.CurrentValue);

                var serializedConfiguration = JsonConvert.SerializeObject(parameters);
                _logger.Info(Constants.LOG_FILE, $"Serialized Configuration: {serializedConfiguration}");
                var configuration = JsonConvert.DeserializeObject<RuntimeConfiguration>(serializedConfiguration);

                var proxy = _channelFactory.CreateChannel();
                using (proxy as IDisposable)
                {
                    var result = proxy.Init(configuration);

                    if (result == ResultCode.Success)
                    {
                        LastStatus = PeripheralStatus.PeripheralOK();
                        _logger.Info(Constants.LOG_FILE, "Driver successfully initialized.");
                    }
                    else
                    {
                        LastStatus = PeripheralStatus.PeripheralGenericError();
                        _logger.Info(Constants.LOG_FILE, "Driver failed to initialize.");
                    }

                    return result == ResultCode.Success;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(Constants.LOG_FILE, "Error Message in Init():\n" + ex.Message);
                _logger.Error(Constants.LOG_FILE, "Source Error in Init():\n" + ex.Source);
                _logger.Error(Constants.LOG_FILE, "StackTrace Error in Init():\n" + ex.StackTrace);
                LastStatus = PeripheralStatus.PeripheralGenericError();
                _logger.Error(Constants.LOG_FILE, "Failed to initialize payment driver.");
                _logger.Error(Constants.LOG_FILE, ex.ToString());
                return false;
            }
            finally
            {
                _logger.Info(Constants.LOG_FILE, "Init method finished.");
            }
        }

        /// <summary>
        /// Update all the settings that the driver needs.
        /// This is done when the peripheral is fist loaded (core start) or when it is set from the Admin
        /// </summary>
        /// <param name="configJson">A json containing all the settings of the payment device</param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public bool UpdateSettings(string configJson, bool overwrite = false)
        {
            try
            {
                _logger.Info(Constants.LOG_FILE, "UpdateSettings method started...");
                var payment = JsonConvert.DeserializeObject<Payment>(configJson);

                //If the init was called and in the parameters the overwrite is True 
                //then modify all the settings to the new value
                foreach (var setting in payment.ConfigurationSettings)
                {
                    var currentSetting = _currentPaymentInitConfig.ConfigurationSettings.FirstOrDefault(_ => _.RealName == setting.RealName);
                    if (currentSetting != null)
                    {
                        currentSetting.CurrentValue = setting.CurrentValue;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(Constants.LOG_FILE, "Failed to update payment settings.");
                _logger.Error(Constants.LOG_FILE, ex.ToString());
                return false;
            }
            finally
            {
                _logger.Info(Constants.LOG_FILE, "UpdateSettings method finished.");
            }
        }

        public bool Test()
        {
            try
            {
                //_logger.Info(Constants.LOG_FILE, "Test method started...");

                var proxy = _channelFactory.CreateChannel();
                using (proxy as IDisposable)
                {
                    var result = proxy.Test();

                    LastStatus = result == ResultCode.Success ?
                        PeripheralStatus.PeripheralOK() : PeripheralStatus.PeripheralGenericError();

                    if (result != ResultCode.Success)
                    {
                        _logger.Error(Constants.LOG_FILE, "Payment driver test returned an error.");
                    }
                }

                return LastStatus.Status == 0;
            }
            catch (Exception ex)
            {
                _logger.Error(Constants.LOG_FILE, "Failed to test payment driver.");
                _logger.Error(Constants.LOG_FILE, ex.ToString());
                return false;
            }
            finally
            {
                _logger.Debug(Constants.LOG_FILE, "Test method finished.");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="payRequest"></param>
        /// <param name="payDetails"></param>
        /// <param name="specificStatusDetails"></param>
        /// <param name="wasUncertainPaymentDetected"></param>
        /// <returns></returns>
        public bool Pay(PayRequest payRequest, ref PayDetails payDetails, ref SpecificStatusDetails specificStatusDetails, ref bool wasUncertainPaymentDetected)
        {
            try
            {
                _logger.Info(Constants.LOG_FILE, "Pay method started...");

                _logger.Debug(Constants.LOG_FILE, $"PayRequest: {JsonConvert.SerializeObject(payRequest)}");

                Result<PaymentData> result;

                var proxy = _channelFactory.CreateChannel();
                using (proxy as IDisposable)
                {
                    result = proxy.Pay(payRequest.Amount);
                }

                payDetails = new PayDetails
                {
                    PaidAmount = result.Data?.PaidAmount ?? 0,
                    HasClientReceipt = result.Data?.HasClientReceipt ?? false
                };

                specificStatusDetails = new SpecificStatusDetails()
                {
                    StatusCode = (int)result.ResultCode,
                    Description = result.Message
                };

                //Check the status property of the parameters object to see if the Pay was successful
                if (result.ResultCode == ResultCode.Success && result.Data?.Result == PaymentResult.Successful)
                {
                    _logger.Info(Constants.LOG_FILE, "Payment has succeeded.");

                    LastStatus = PeripheralStatus.PeripheralOK();
                    return true;
                }
                else
                {
                    _logger.Info(Constants.LOG_FILE, "Payment has failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(Constants.LOG_FILE, "Payment exception thrown.");
                _logger.Error(Constants.LOG_FILE, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the current payment application
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            try
            {
                _logger.Info(Constants.LOG_FILE, "Unload method started...");

                _channelFactory = null;

                //Stop the payment application
                var runningProcesses =
                    Process.GetProcessesByName(Constants.PAYMENT_APPLICATION_PROCESS_NAME);
                foreach (var process in runningProcesses)
                {
                    _logger.Info(Constants.LOG_FILE, "Shell driver is already running, killing it.");
                    process.Kill();
                    _logger.Info(Constants.LOG_FILE, "Waiting for process to exit...");
                    process.WaitForExit();
                    _logger.Info(Constants.LOG_FILE, "Running process exited!");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(Constants.LOG_FILE, "Unload exception thrown.");
                _logger.Error(Constants.LOG_FILE, ex.ToString());
                return false;
            }
            finally
            {
                _logger.Info(Constants.LOG_FILE, "Unload method finished.");
            }
        }

        /// <summary>
        /// Method providing factory details
        /// </summary>
        /// <returns></returns>
        public string GetPaymentFactoryDetails()
        {
            _logger.Info(Constants.LOG_FILE, "GetPaymentFactoryDetails method started...");

            var result = JsonConvert.SerializeObject(new { Payment = new[] { _currentPaymentInitConfig } });

            _logger.Info(Constants.LOG_FILE, "GetPaymentFactoryDetails method finished.");

            return result;
        }

        /// <summary>
        /// Check if the Payment Application is started and if it is stop it and start it again
        /// </summary>
        private void LaunchDriver()
        {
            _logger.Info(Constants.LOG_FILE, "LaunchDriver method started...");
            //Stop the payment application

            var runningProcesses = Process.GetProcessesByName(Constants.PAYMENT_APPLICATION_PROCESS_NAME);
            foreach (var process in runningProcesses)
            {
                _logger.Info(Constants.LOG_FILE, "Shell driver is already running, killing it.");
                process.Kill();
                _logger.Info(Constants.LOG_FILE, "Waiting for process to exit...");
                process.WaitForExit();
                _logger.Info(Constants.LOG_FILE, "Running process exited!");
            }

            _logger.Info(Constants.LOG_FILE, $"Lauching payment shell driver {driverLocation + "\\" + Constants.PAYMENT_APPLICATION_NAME}");

            //Start the application
            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                FileName = Constants.PAYMENT_APPLICATION_NAME,
                WorkingDirectory = driverLocation
            };
            Process.Start(startInfo);

            _logger.Info(Constants.LOG_FILE, "LaunchDriver method finished.");
        }
    }
}
