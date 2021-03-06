﻿using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.ServiceModel;


namespace Acrelec.Mockingbird.Payment
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class PaymentService : IPaymentService
    {
        private static readonly string ticketPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ticket");

        /// <summary>
        /// Get the configuratiion data
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Result Init(RuntimeConfiguration configuration)
        {
            Log.Info("Init method started...");

            //initalise confguration file instance
            var configFile = AppConfiguration.Instance;

            try
            {
                if (configuration == null)
                {
                    Log.Info("Can not set configuration to null.");
                    return ResultCode.GenericError;
                }

                if (configuration.PosNumber <= 0)
                {
                    Log.Info($"Invalid PosNumber {configuration.PosNumber}.");
                    return ResultCode.GenericError;
                }

                using (var api = new PaymentSenseRestApi())
                {
                    RuntimeConfiguration.Instance = configuration;
                    Heartbeat.Instance.Start();
                    Log.Info("Init success!");

                    return ResultCode.Success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Init method finished.");
            }
        }

        /// <summary>
        /// Test HeartBeat
        /// </summary>
        /// <returns></returns>
        public Result Test()
        {
            var alive = Heartbeat.Instance?.Alive == true;
            Log.Debug($"Test status: {alive}");
            return alive ? ResultCode.Success : ResultCode.GenericError;
        }

        /// <summary>
        /// Payment method
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public Result<PaymentData> Pay(int amount)
        {
            Log.Info("Pay method started...");
            Log.Info($"Amount = {amount/100.0}.");
            Result<PaymentData> transactionResult = null;
            string reciept = string.Empty;

            try
            {
                if (File.Exists(ticketPath))
                {
                    File.Delete(ticketPath);
                }

                if (amount <= 0)
                {
                    Log.Info("Invalid pay amount...");
                    return ResultCode.GenericError;
                }

                var config = RuntimeConfiguration.Instance;
                var data = new PaymentData();

                Log.Info("Calling payment driver...");

                using (var api = new PaymentSenseRestApi())
                {

                    var payResult = api.Pay(amount, out TransactionDetails payResponse);
                    Log.Info($"Pay Result: {payResult}");


                    // interogate the result check if payResponse not equal to null

                    if (payResponse == null)
                    {
                        Log.Error("Transaction response error...");
                        data.Result = PaymentResult.Failed;
                        PrintErrorTicket(data, string.Empty);
                        return new Result<PaymentData>((ResultCode)payResult, data: data);

                    }
                    else

                    if (payResult != DiagnosticErrMsg.OK && payResponse != null)
                    {
                        Log.Error($"Pay Result = {payResult} Payment Failed...See Stored Ticket");                    
                        data.Result = PaymentResult.Failed;

                        PrintErrorTicket(data, payResponse.TransactionResult);

                        return new Result<PaymentData>((ResultCode)payResult, data: data);
                    }
                    else
                    {
                        data.Result = PaymentResult.Successful;
                       
                        data.PaidAmount = amount;

                        Log.Info($"paid Amount: {data.PaidAmount}");
                        transactionResult = new Result<PaymentData>(ResultCode.Success, data: data);
                        Log.Info($"Payment succeeded transaction result: {transactionResult}");

                        CreateCustomerTicket(payResponse);
                        data.HasClientReceipt = true;
                    }

                    //persist the transaction
                   // PersistTransaction(payResponse);
                }


                return transactionResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Pay method finished...");
            }
        }

        private static void PrintErrorTicket(PaymentData data, string details)
        {
            //print the payment ticket for an error
            // Check details 
            if (details.Contains("TIMED_OUT"))
            {
                CreateCustomerTicket("\nPayment failure.\n\nTransaction has timed out and payment\nmay or may not have been taken.\n\nPlease speak to a member of staff for assistance.\n\n" + "Transaction: " + details);
            }
            else
            {
                CreateCustomerTicket("\nPayment failure with\nyour card or issuer.\n\nNO payment has been taken.\n\nPlease try again with another card,\nor at a manned till.\n\n" + "Transaction: " + details);
            }
            data.HasClientReceipt = true;
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        public void Shutdown()
        {
            Log.Info("Shutting down...");
            Program.ManualResetEvent.Set();
        }

        /// <summary>
        /// Persist the transaction as Text file
        /// with Customer and Merchant receiept
        /// </summary>
        /// <param name="result"></param>
        private static void PersistTransaction(string customerReceipt)
        {
            try
            {
                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_ticket.txt");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                Log.Info("Persisting Customerticket to {0}", outputPath);

                //Write the new ticket
                File.WriteAllText(outputPath, customerReceipt.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Persist Transaction exception.");
                Log.Error(ex);
            }
        }


        //overload the customer ticket to check to return a string
        // output on error
        /// <summary>
        ///  Create Customer Ticket to output the reciept error string
        /// </summary>
        /// <param name="ticket"></param>
        private static void CreateCustomerTicket(string ticket)
        {
            try
            {
                Log.Info($"Persisting Customer ticket to {ticketPath}");

                //Write the new ticket
                File.WriteAllText(ticketPath, ticket);

                //persist the transaction
                 PersistTransaction(ticket);

            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Create Customer Ticket to output the reciept
        /// </summary>
        /// <param name="ticket"></param>
        private static void CreateCustomerTicket(TransactionDetails ticket)
        {
            StringBuilder ticketContent = new StringBuilder();

            //set user message
            ticket.UserMessage = "\n\tPLEASE RETAIN RECEIPT. \n\n\tTHANK YOU.";
            ticketContent.Append($"\nApp Id: {ticket.ApplicationId}\n");
            ticketContent.Append($"App Label: {ticket.ApplicationLabel}\n");
            ticketContent.Append($"AuthCode: {ticket.AuthCode}\n");
            //ticketContent.Append($"Card holder Verification Method: {ticket.CardholderVerificationMethod}\n");
            ticketContent.Append($"Card Scheme Name: {ticket.CardSchemeName}\n");
            ticketContent.Append($"Currency: {ticket.Currency}\n");
            ticketContent.Append($"Start Date: {ticket.DateOfStart}\n");
            ticketContent.Append($"Expiry Date: {ticket.DateOfExpiry}\n");
            ticketContent.Append($"Payment Method: {ticket.PaymentMethod}\n");
            ticketContent.Append($"PAN: {ticket.PrimaryAccountNumber}\n");
            ticketContent.Append($"PAN Num Seq: {ticket.PrimaryAccountNumberSequence}\n");
            ticketContent.Append($"Transaction Id: {ticket.TransactionId}\n");
            ticketContent.Append($"Transaction Num: {ticket.TransactionNumber}\n");
            ticketContent.Append($"Transaction Result: {ticket.TransactionResult}\n");
            ticketContent.Append($"Transaction Time: {ticket.TransactionTime}\n");
            ticketContent.Append($"Transaction Type: {ticket.TransactionType}\n");
            ticketContent.Append($"\nAmount Total: {Utils.GetCurrencySymbol(ticket.Currency)}{(ticket.AmountTotal / 100.0)}\n");
            ticketContent.Append($"\t\n{ticket.UserMessage}\n");  
            try
                {
                Log.Info($"Persisting Customer ticket to {ticketPath}");

                //Write the new ticket to the ticket path
                File.WriteAllText(ticketPath, ticketContent.ToString());

                PersistTransaction(ticketContent.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }
    }
}
