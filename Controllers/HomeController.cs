using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.IO;
using System.Text;

using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;

namespace PaypalMVC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult PostToPayPal(string item , string amount, string custom)
        {
            PaypalMVC.Models.Paypal paypal = new Models.Paypal();
            paypal.cmd = "_xclick";
            paypal.business = ConfigurationManager.AppSettings["BusinessAccountKey"];

            bool useSandbox = Convert.ToBoolean(ConfigurationManager.AppSettings["UseSandbox"]);
            if (useSandbox)
                ViewBag.actionURl = "https://www.sandbox.paypal.com/cgi-bin/webscr";
            else
                ViewBag.actionURl = "https://www.paypal.com/cgi-bin/webscr";

            paypal.cancel_return = System.Configuration.ConfigurationManager.AppSettings["CancelURL"];
            paypal.@return = ConfigurationManager.AppSettings["ReturnURL"];
            paypal.notify_url = ConfigurationManager.AppSettings["NotifyURL"];

            paypal.currency_code = ConfigurationManager.AppSettings["CurrencyCode"];

            paypal.item_name = item;
            paypal.amount = amount;
            paypal.custom = custom;
            return View(paypal);
        }
/*
        public ActionResult Members()
        {
            TransactionSearchRequestType request = new TransactionSearchRequestType();
            request.StartDate = DateTime.Now.AddDays(-1000).ToString("yyyy-MM-ddTHH:mm:ss");
            TransactionSearchReq wrapper = new TransactionSearchReq();
            wrapper.TransactionSearchRequest = request;
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService();
            TransactionSearchResponseType transactionDetails = service.TransactionSearch(wrapper);

            // Check for API return status

            CurrContext.Items.Add("Response_apiName", "TransactionSearch");
            CurrContext.Items.Add("Response_redirectURL", null);
            CurrContext.Items.Add("Response_requestPayload", service.getLastRequest());
            CurrContext.Items.Add("Response_responsePayload", service.getLastResponse());

            Dictionary<string, string> keyParameters = new Dictionary<string, string>();
            keyParameters.Add("Correlation Id", response.CorrelationID);
            keyParameters.Add("API Result", response.Ack.ToString());

            if (response.Errors != null && response.Errors.Count > 0)
            {
                CurrContext.Items.Add("Response_error", response.Errors);
            }
            else
            {
                CurrContext.Items.Add("Response_error", null);
            }

            if(!response.Ack.Equals(AckCodeType.FAILURE))
            {
                keyParameters.Add("Total matching transactions", response.PaymentTransactions.Count.ToString());

                for (int i = 0; i < response.PaymentTransactions.Count; i++ )
                {
                    PaymentTransactionSearchResultType result = response.PaymentTransactions[i];
                    String label = "Result " + (i+1);
                    keyParameters.Add(label + " Payer", result.Payer);
                    keyParameters.Add(label + " Transaction Id", result.TransactionID);
                    keyParameters.Add(label + " Payment status", result.Status);
                    keyParameters.Add(label + " Payment timestamp", result.Timestamp);
                    keyParameters.Add(label + " Transaction type", result.Type);
                    if (result.NetAmount != null)
                    {
                        keyParameters.Add(label + " Net amount",
                            result.NetAmount.value + result.NetAmount.currencyID.ToString());
                    }
                    if (result.GrossAmount != null)
                    {
                        keyParameters.Add(label + " Gross amount",
                            result.GrossAmount.value + result.GrossAmount.currencyID.ToString());
                    }
                }
            }
            CurrContext.Items.Add("Response_keyResponseObject", keyParameters);
            Server.Transfer("../APIResponse.aspx");

        }
*/
        public ActionResult IPN()
        {
            // Receive IPN request from PayPal and parse all the variables returned
            var formVals = new Dictionary<string, string>();
            formVals.Add("cmd", "_notify-validate");
 
            string response = GetPayPalResponse(formVals);
 
            if (response == "VERIFIED")
            {
                string transactionID = ViewBag.tID = Request["txn_id"];
                string sAmountPaid = ViewBag.amount = Request["mc_gross"];
                string userID = ViewBag.userID = Request["custom"];

                ViewBag.Message = "Thanks for purchase, enjoy benefits of the Premium membership!";

                //validate the order
                Decimal amountPaid = 0;
                Decimal.TryParse(sAmountPaid, out amountPaid);
 
                if (sAmountPaid == "5.00")
                {
                    // take the information returned and store this into a subscription table
                    return View();
                }
                else
                {
                    // let fail - this is the IPN so there is no viewer
                    ViewBag.Message = "Sorry, transaction error occured, please let our support service to handle the issue. Please save information below for your reference:";
                    return View();
                }
            }
 
            return RedirectToAction("Index", "Home");
        }

        string GetPayPalResponse(Dictionary<string, string> formVals)
        {
 
            // Parse the variables
            // Choose whether to use sandbox or live environment
            bool useSandbox = Convert.ToBoolean(ConfigurationManager.AppSettings["UseSandbox"]);
            string paypalUrl = useSandbox ? "https://www.sandbox.paypal.com/cgi-bin/webscr" : "https://www.paypal.com/cgi-bin/webscr";
 
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(paypalUrl);
 
            // Set values for the request back
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
 
            byte[] param = Request.BinaryRead(Request.ContentLength);
            string strRequest = Encoding.ASCII.GetString(param);
 
            StringBuilder sb = new StringBuilder();
            sb.Append(strRequest);
 
            foreach (string key in formVals.Keys)
            {
                sb.AppendFormat("&{0}={1}", key, formVals[key]);
            }
            strRequest += sb.ToString();
            req.ContentLength = strRequest.Length;
 
            //for proxy
            //WebProxy proxy = new WebProxy(new Uri("http://urlort#");
            //req.Proxy = proxy;
            //Send the request to PayPal and get the response
            string response = "";
            using (StreamWriter streamOut = new StreamWriter(req.GetRequestStream(), System.Text.Encoding.ASCII))
            {
 
                streamOut.Write(strRequest);
                streamOut.Close();
                using (StreamReader streamIn = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    response = streamIn.ReadToEnd();
                }
            }
 
            return response;
       }

    }
}
