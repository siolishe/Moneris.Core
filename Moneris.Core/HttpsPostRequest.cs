using System;
using System.Net;
using System.Text;

namespace Moneris.Core
{
            public class HttpsPostRequest
        {
            private readonly string _apiToken;
            private readonly WebProxy _proxy;
            private readonly string _status;
            private readonly string _storeId;
            private readonly Transaction _transaction;
            private readonly string _url;
            private Receipt _receiptObj = new Receipt();

            public HttpsPostRequest(string host, string store, string apiTok, Transaction t)
            {
                _storeId = store;
                _apiToken = apiTok;
                _transaction = t;
                _url = "https://" + host + ":443/gateway2/servlet/MpgRequest";
                SendRequest();
            }

            public HttpsPostRequest(
                string host,
                string store,
                string apiTok,
                string statusCheck,
                Transaction t)
            {
                _storeId = store;
                _apiToken = apiTok;
                _status = statusCheck;
                _transaction = t;
                _url = "https://" + host + ":443/gateway2/servlet/MpgRequest";
                SendRequest();
            }

            public HttpsPostRequest(
                string host,
                string store,
                string apiTok,
                Transaction t,
                WebProxy prxy)
            {
                _proxy = prxy;
                _storeId = store;
                _apiToken = apiTok;
                _transaction = t;
                _url = "https://" + host + ":443/gateway2/servlet/MpgRequest";
                SendRequest();
            }

            public void SendRequest()
            {
                var bytes = Encoding.ASCII.GetBytes(ToXml());
                try
                {
                    var httpWebRequest = (HttpWebRequest) WebRequest.CreateDefault(new Uri(_url));
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    if (_proxy != null)
                    {
                        httpWebRequest.Proxy = _proxy;
                        httpWebRequest.Credentials = _proxy.Credentials;
                    }

                    httpWebRequest.ContentLength = bytes.Length;
                    httpWebRequest.UserAgent = "DOTNET - 2.5.3 - Resolver";
                    var requestStream = httpWebRequest.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Flush();
                    var responseStream = httpWebRequest.GetResponse().GetResponseStream();
                    _receiptObj = new Receipt(responseStream);
                    requestStream.Close();
                    responseStream?.Close();
                }
                catch (Exception ex)
                {
                    _receiptObj = new Receipt();
                    Console.WriteLine("Message: " + ex.Message);
                }
            }

            public Receipt GetReceipt()
            {
                return _receiptObj;
            }

            public string ToXml()
            {
                const string str = "<?xml version=\"1.0\"?>";
                return _status != null && _status == "true"
                    ? new StringBuilder(str + "<request><store_id>" + _storeId + "</store_id><api_token>" +
                                        _apiToken + "</api_token><status_check>" + _status + "</status_check>" +
                                        _transaction.ToXml() + "</request>").ToString()
                    : _status == "false"
                        ? new StringBuilder(str + "<request><store_id>" + _storeId + "</store_id><api_token>" +
                                            _apiToken + "</api_token><status_check>" + _status +
                                            "</status_check>" + _transaction.ToXml() + "</request>").ToString()
                        : new StringBuilder(str + "<request><store_id>" + _storeId + "</store_id><api_token>" +
                                            _apiToken + "</api_token>" + _transaction.ToXml() + "</request>")
                            .ToString();
            }
        }

}