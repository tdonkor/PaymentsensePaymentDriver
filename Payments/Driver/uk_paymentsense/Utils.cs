using Acrelec.Library.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Payment
{
    public enum DiagnosticErrMsg : short
    {
        OK = 0,
        NOTOK = 1
    }


    public class Utils
    {


        /// <summary>
        /// Check the numeric value of the amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static int GetNumericAmountValue(int amount)
        {

            if (amount <= 0)
            {
                Log.Info("Invalid pay amount");
                amount = 0;
            }

            return amount;
        }

        /// <summary>
        /// Get the Currency Symbol String
        /// </summary>
        public static string GetCurrencySymbol(string symbol)
        {
            string CurrencySymbol = "";

            switch (symbol)
            {
                case "GBP":
                    CurrencySymbol = "£";
                    break;
                case "USD":
                case "CAD":
                case "AUD":
                    CurrencySymbol = "$";
                    break;
                case "EUR":
                    CurrencySymbol = "€";
                    break;
                case "JPY":
                case "CNY":
                    CurrencySymbol = "¥";
                    break;
                default: /* Do Nothing */ break;
            }

            return CurrencySymbol;
        }

    }
}
