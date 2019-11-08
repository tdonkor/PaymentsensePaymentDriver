using Acrelec.Mockingbird.Payment.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Payment
{
    public class PaymentSenseRestApi : IDisposable
    {
        private string username;
        private string password;
        private string requestId;
        private string url;
        private string tid;
        private string currency;
        private string installerId;
        private string softwareHouseId;
        private string mediaType;

        AppConfiguration configFile;

        /// <summary>
        /// initialise config file params
        /// </summary>
        public PaymentSenseRestApi()
        {
            configFile = AppConfiguration.Instance;
            username = configFile.UserName;
            password = configFile.Password;
            url = configFile.UserAccountUrl;
            tid = configFile.Tid;
            currency = configFile.Currency;
            installerId = configFile.InstallerId;
            softwareHouseId = "ST185L09"; //hardcoded value
            mediaType = configFile.MediaType;
        }

        /// <summary>
        /// Starts a transaction on the terminal with the given TID.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public DiagnosticErrMsg Pay(int value, out TransactionDetails result)
        {
            int amount;
            RestClient client = Authenticate(url + "/pac/terminals/" + tid + "/transactions");
            bool signatureRequired = false;

            //initialise the result - fill this with the customer reciept to display
            result = null;

            //check value
            amount = Utils.GetNumericAmountValue(value);

            if (amount == 0)
            {
                throw new Exception("Error in Amount value...");
            }

            var request = new RestRequest(Method.POST);
            request = RequestParams(request);
            request.AddParameter("Sale", "{\r\n  \"transactionType\": \"SALE\",\r\n  \"amount\": " + value + ",\r\n  \"currency\": \"" + configFile.Currency + "\"\r\n}", ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);

            //check reponse isSuccessful
            if (response.IsSuccessful)
            {
                //deserialise response
                TransactionResp tranResponse = JsonConvert.DeserializeObject<TransactionResp>(response.Content);
                requestId = tranResponse.RequestId;

                //poll for result every 1 seconds block until finish
                while (true)
                {
                    Thread.Sleep(1000);
                    response = GetTransactionData(requestId, configFile.UserAccountUrl);

                    if ((response.Content.Contains("SIGNATURE_VERIFICATION")) && (signatureRequired == false))
                    {
                        signatureRequired = true;
                        response = SignaturePutRequest(requestId, url);
                    }

                    if (response.Content.Contains("TRANSACTION_FINISHED"))
                    {
                        break;
                    }
                }

            }

            //deserialise response
            result = JsonConvert.DeserializeObject<TransactionDetails>(response.Content);

            if (result.TransactionResult.Contains("SUCCESSFUL"))
            {
                return DiagnosticErrMsg.OK;
            }
            else
                return DiagnosticErrMsg.NOTOK;
        }

        /// <summary>
        ///  Gets data for the transaction with the given requestId.
        /// </summary>
        /// <param name="responseStr"></param>
        /// <param name="url"></param>
        public IRestResponse GetTransactionData(string requestId, string url)
        {

            RestClient client = Authenticate(url + "/pac/terminals/" + tid + "/transactions/" + requestId);
            var request = new RestRequest(Method.GET);
            request = RequestParams(request);

            IRestResponse response = client.Execute(request);

            return response;
        }

        /// <summary>
        /// Reverse the swipe card and set the signature to false
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public IRestResponse SignaturePutRequest(string requestId, string url)
        {
            RestClient client = Authenticate(url + "/pac/terminals/" + tid + "/transactions/" + requestId + "/signature");
            var request = new RestRequest(Method.PUT);
            request = RequestParams(request);
            request.AddParameter("Signature", "{\r\n  \"accepted\": false\r\n}", ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);

            return response;
        }

        /// <summary>
        /// Authenticate the users username and password
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private RestClient Authenticate(string url)
        {
            return new RestClient(url)
            {
                Authenticator = new HttpBasicAuthenticator(username, password)
            };
        }

        /// <summary>
        /// The Request Parameters for the REST API calls 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private RestRequest RequestParams(RestRequest request)
        {
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", mediaType);
            request.AddHeader("Software-House-Id", softwareHouseId);
            request.AddHeader("Installer-Id", installerId);
            request.AddHeader("Connection", "keep-alive");

            return request;
        }

        public void Dispose()
        {
           
        }
    }

    /// <summary>
    /// structure of the transaction response 
    /// </summary>
    class TransactionResp
    {
        public string Location { get; set; }
        public string RequestId { get; set; }
    }
}
