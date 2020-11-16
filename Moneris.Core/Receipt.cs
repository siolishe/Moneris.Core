using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace Moneris.Core
{
            public class Receipt
        {
            private readonly Hashtable _cardHash = new Hashtable();
            private readonly Hashtable _dataKeyHash = new Hashtable();
            private readonly Stack _dataKeyStack = new Stack();

            private readonly string _globalErrorReceipt =
                "<?xml version=\"1.0\"?><response><receipt><ReceiptId>Global Error Receipt</ReceiptId><ReferenceNum>null</ReferenceNum><ResponseCode>null</ResponseCode><ISO>null</ISO> <AuthCode>null</AuthCode><TransTime>null</TransTime><TransDate>null</TransDate><TransType>null</TransType><Complete>false</Complete><Message>null</Message><TransAmount>null</TransAmount><CardType>null</CardType><TransID>null</TransID><TimedOut>null</TimedOut></receipt></response>";

            private readonly Hashtable _responseDataHash = new Hashtable();
            private readonly Hashtable _termIdHash = new Hashtable();
            private readonly XmlTextReader _xtr;
            private Hashtable _correctionHash;
            private string _currentCardType;
            private string _currentDataKey;
            private string _currentTag;
            private string _currentTermId;
            private string _currentTxnType;

            private bool _hasMultipleDataKey;
            private bool _isBatchTotals;
            private bool _isResolveData;
            private Hashtable _purchaseHash;
            private Hashtable _refundHash;
            private Hashtable _resDataHash = new Hashtable();

            public Receipt(Stream aStream)
            {
                _xtr = new XmlTextReader(aStream);
                DoParse();
                _xtr.Close();
            }

            public Receipt()
            {
                _xtr = new XmlTextReader(new StringReader(_globalErrorReceipt));
                DoParse();
                _xtr.Close();
            }

            private void DoParse()
            {
                while (_xtr.Read())
                    switch (_xtr.NodeType)
                    {
                        case XmlNodeType.Element:
                            BeginHandler(_xtr.Name);
                            break;
                        case XmlNodeType.Text:
                            TextHandler(_xtr.Value);
                            break;
                        case XmlNodeType.EndElement:
                            EndHandler(_xtr.Name);
                            break;
                    }
            }

            private void BeginHandler(string tag)
            {
                _currentTag = tag;
                if (tag.Equals("BankTotals"))
                {
                    _isBatchTotals = true;
                    _purchaseHash = new Hashtable();
                    _refundHash = new Hashtable();
                    _correctionHash = new Hashtable();
                }

                if (_isBatchTotals)
                {
                    if (_currentTag.Equals("Purchase"))
                        _currentTxnType = "Purchase";
                    else if (_currentTag.Equals("Refund"))
                        _currentTxnType = "Refund";
                    else if (_currentTag.Equals("Correction"))
                        _currentTxnType = "Correction";
                }

                if (!tag.Equals("ResolveData"))
                    return;
                _isResolveData = true;
                _resDataHash = new Hashtable();
            }

            private void EndHandler(string tag)
            {
                if (tag.Equals("BankTotals"))
                    _isBatchTotals = false;
                if (!tag.Equals("ResolveData"))
                    return;
                _isResolveData = false;
            }

            private void TextHandler(string data)
            {
                if (_isBatchTotals)
                {
                    if (_currentTag.Equals("term_id"))
                    {
                        _currentTermId = data;
                        _cardHash.Add(_currentTermId, new Stack());
                        _purchaseHash.Add(_currentTermId, new Hashtable());
                        _refundHash.Add(_currentTermId, new Hashtable());
                        _correctionHash.Add(_currentTermId, new Hashtable());
                    }
                    else if (_currentTag.Equals("closed"))
                    {
                        _termIdHash.Add(_currentTermId, data);
                    }
                    else if (_currentTag.Equals("CardType"))
                    {
                        ((Stack) _cardHash[_currentTermId]).Push(data);
                        _currentCardType = data;
                        ((Hashtable) _purchaseHash[_currentTermId])[_currentCardType] =
                            new Hashtable();
                        ((Hashtable) _refundHash[_currentTermId])[_currentCardType] =
                            new Hashtable();
                        ((Hashtable) _correctionHash[_currentTermId])[_currentCardType] =
                            new Hashtable();
                    }
                    else if (_currentTag.Equals("Amount"))
                    {
                        if (_currentTxnType.Equals("Purchase"))
                        {
                            ((Hashtable) ((Hashtable) _purchaseHash[_currentTermId])[
                                _currentCardType])["Amount"] = data;
                        }
                        else if (_currentTxnType.Equals("Refund"))
                        {
                            ((Hashtable) ((Hashtable) _refundHash[_currentTermId])[
                                _currentCardType])["Amount"] = data;
                        }
                        else
                        {
                            if (!_currentTxnType.Equals("Correction"))
                                return;
                            ((Hashtable) ((Hashtable) _correctionHash[_currentTermId])[
                                _currentCardType])["Amount"] = data;
                        }
                    }
                    else
                    {
                        if (!_currentTag.Equals("Count"))
                            return;
                        if (_currentTxnType.Equals("Purchase"))
                            ((Hashtable) ((Hashtable) _purchaseHash[_currentTermId])[
                                _currentCardType])["Count"] = data;
                        else if (_currentTxnType.Equals("Refund"))
                            ((Hashtable) ((Hashtable) _refundHash[_currentTermId])[
                                _currentCardType])["Count"] = data;
                        else if (_currentTxnType.Equals("Correction"))
                            ((Hashtable) ((Hashtable) _correctionHash[_currentTermId])[
                                _currentCardType])["Count"] = data;
                    }
                }
                else if (_isResolveData && !data.Equals("null"))
                {
                    if (_currentTag.Equals("data_key"))
                    {
                        _currentDataKey = data;
                        _dataKeyHash.Add(_currentDataKey, new Hashtable());
                        _dataKeyStack.Push(_currentDataKey);
                    }
                    else
                    {
                        ((Hashtable) _dataKeyHash[_currentDataKey])[_currentTag] =
                            data;
                    }

                    _resDataHash[_currentTag] = data;
                }
                else
                {
                    _responseDataHash[_currentTag] = data;
                    if (!_currentTag.Equals("DataKey")) return;
                    if (data.Equals("null"))
                    {
                        _hasMultipleDataKey = true;
                    }
                    else
                    {
                        _currentDataKey = data;
                        _dataKeyHash.Add(_currentDataKey, new Hashtable());
                        _dataKeyStack.Push(_currentDataKey);
                    }
                }
            }

            public string GetPurchaseAmount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _purchaseHash[ecrNo])[cardType])[
                        "Amount"];
                return str ?? "0";
            }

            public string GetPurchaseCount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _purchaseHash[ecrNo])[cardType])[
                        "Count"];
                return str ?? "0";
            }

            public string GetRefundAmount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _refundHash[ecrNo])[cardType])[
                        "Amount"];
                return str ?? "0";
            }

            public string GetRefundCount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _refundHash[ecrNo])[cardType])[
                        "Count"];
                return str ?? "0";
            }

            public string GetCorrectionAmount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _correctionHash[ecrNo])[cardType])[
                        "Amount"];
                return str ?? "0";
            }

            public string GetCorrectionCount(string ecrNo, string cardType)
            {
                var str =
                    (string) ((Hashtable) ((Hashtable) _correctionHash[ecrNo])[cardType])[
                        "Count"];
                return str ?? "0";
            }

            public string GetTerminalStatus(string ecrNo)
            {
                return (string) _termIdHash[ecrNo];
            }

            public string[] GetTerminalIDs()
            {
                var strArray = new string[_termIdHash.Count];
                var enumerator = _termIdHash.GetEnumerator();
                var num = 0;
                while (enumerator.MoveNext())
                    strArray[num++] = (string) enumerator.Key;
                return strArray;
            }

            public string[] GetCreditCards(string ecrNo)
            {
                var stack = (Stack) _cardHash[ecrNo];
                var strArray = new string[stack.Count];
                var enumerator = stack.GetEnumerator();
                var num = 0;
                while (enumerator.MoveNext())
                    strArray[num++] = (string) enumerator.Current;
                return strArray;
            }

            public string GetItdResponse()
            {
                return (string) _responseDataHash["ITDResponse"];
            }

            public string GetCardType()
            {
                return (string) _responseDataHash["CardType"];
            }

            public string GetTransAmount()
            {
                return (string) _responseDataHash["TransAmount"];
            }

            public string GetTxnNumber()
            {
                return (string) _responseDataHash["TransID"];
            }

            public string GetReceiptId()
            {
                return (string) _responseDataHash["ReceiptId"];
            }

            public string GetTransType()
            {
                return (string) _responseDataHash["TransType"];
            }

            public string GetReferenceNum()
            {
                return (string) _responseDataHash["ReferenceNum"];
            }

            public string GetResponseCode()
            {
                return (string) _responseDataHash["ResponseCode"];
            }

            public string GetIso()
            {
                return (string) _responseDataHash["ISO"];
            }

            public string GetBankTotals()
            {
                return (string) _responseDataHash["BankTotals"];
            }

            public string GetMessage()
            {
                return (string) _responseDataHash["Message"];
            }

            public string GetRecurSuccess()
            {
                return (string) _responseDataHash["RecurSuccess"];
            }

            public string GetAuthCode()
            {
                return (string) _responseDataHash["AuthCode"];
            }

            public string GetComplete()
            {
                return (string) _responseDataHash["Complete"];
            }

            public string GetTransDate()
            {
                return (string) _responseDataHash["TransDate"];
            }

            public string GetTransTime()
            {
                return (string) _responseDataHash["TransTime"];
            }

            public string GetTicket()
            {
                return (string) _responseDataHash["Ticket"];
            }

            public string GetTimedOut()
            {
                return (string) _responseDataHash["TimedOut"];
            }

            public string GetAvsResultCode()
            {
                return (string) _responseDataHash["AvsResultCode"];
            }

            public string GetCvdResultCode()
            {
                return (string) _responseDataHash["CvdResultCode"];
            }

            public string GetRecurUpdateSuccess()
            {
                return (string) _responseDataHash["RecurUpdateSuccess"];
            }

            public string GetNextRecurDate()
            {
                return (string) _responseDataHash["NextRecurDate"];
            }

            public string GetCorporateCard()
            {
                return (string) _responseDataHash["CorporateCard"];
            }

            public string GetRecurEndDate()
            {
                return (string) _responseDataHash["RecurEndDate"];
            }

            public string GetDataKey()
            {
                return (string) _responseDataHash["DataKey"];
            }

            public string GetResSuccess()
            {
                return (string) _responseDataHash["ResSuccess"];
            }

            public string GetPaymentType()
            {
                return (string) _responseDataHash["PaymentType"];
            }

            public string GetCavvResultCode()
            {
                return (string) _responseDataHash["CavvResultCode"];
            }

            public string GetCardLevelResult()
            {
                return (string) _responseDataHash["CardLevelResult"];
            }

            public string GetIsVisaDebit()
            {
                return (string) _responseDataHash["IsVisaDebit"];
            }

            public string GetStatusCode()
            {
                return (string) _responseDataHash["status_code"];
            }

            public string GetStatusMessage()
            {
                return (string) _responseDataHash["status_message"];
            }

            public string GetResDataCustId()
            {
                return (string) _resDataHash["cust_id"];
            }

            public string GetResDataPhone()
            {
                return (string) _resDataHash["phone"];
            }

            public string GetResDataEmail()
            {
                return (string) _resDataHash["email"];
            }

            public string GetResDataNote()
            {
                return (string) _resDataHash["note"];
            }

            public string GetResDataPan()
            {
                return (string) _resDataHash["pan"];
            }

            public string GetResDataMaskedPan()
            {
                return (string) _resDataHash["masked_pan"];
            }

            public string GetResDataExpdate()
            {
                return (string) _resDataHash["expdate"];
            }

            public string GetResDataCryptType()
            {
                return (string) _resDataHash["crypt_type"];
            }

            public string GetResDataAvsStreetNumber()
            {
                return (string) _resDataHash["avs_street_number"];
            }

            public string GetResDataAvsStreetName()
            {
                return (string) _resDataHash["avs_street_name"];
            }

            public string GetResDataAvsZipcode()
            {
                return (string) _resDataHash["avs_zipcode"];
            }

            public string GetResDataDataKey()
            {
                return (string) _resDataHash["data_key"];
            }

            public string[] GetDataKeys()
            {
                var strArray = new string[_dataKeyStack.Count];
                var enumerator = _dataKeyStack.GetEnumerator();
                var num = 0;
                while (enumerator.MoveNext())
                    strArray[num++] = (string) enumerator.Current;
                return strArray;
            }

            public string GetExpPaymentType(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["payment_type"];
            }

            public string GetExpCustId(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["cust_id"];
            }

            public string GetExpPhone(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["phone"];
            }

            public string GetExpEmail(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["email"];
            }

            public string GetExpNote(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["note"];
            }

            public string GetExpMaskedPan(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["masked_pan"];
            }

            public string GetExpExpdate(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["expdate"];
            }

            public string GetExpCryptType(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["crypt_type"];
            }

            public string GetExpAvsStreetNumber(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["avs_street_number"];
            }

            public string GetExpAvsStreetName(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["avs_street_name"];
            }

            public string GetExpAvsZipCode(string dataKey)
            {
                return (string) ((Hashtable) _dataKeyHash[dataKey])["avs_zipcode"];
            }

            public string GetInLineForm()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("<html><head><title>Title for Page</title></head>\n");
                stringBuilder.Append("<SCRIPT LANGUAGE=\"Javascript\">\n");
                stringBuilder.Append("<!--\n");
                stringBuilder.Append("function OnLoadEvent()\n");
                stringBuilder.Append("{\n");
                stringBuilder.Append("document.downloadForm.submit();\n");
                stringBuilder.Append("}\n");
                stringBuilder.Append("-->\n");
                stringBuilder.Append("</SCRIPT>\n");
                stringBuilder.Append("<body onload=\"OnLoadEvent()\">\n");
                stringBuilder.Append("<form name=\"downloadForm\" action=\"" + GetMpiAcsUrl() +
                                     "\" method=\"POST\">\n");
                stringBuilder.Append("<noscript>\n");
                stringBuilder.Append("<br>\n");
                stringBuilder.Append("<br>\n");
                stringBuilder.Append("<center>\n");
                stringBuilder.Append("<h1>Processing your 3-D Secure Transaction</h1>\n");
                stringBuilder.Append("<h2>\n");
                stringBuilder.Append("JavaScript is currently disabled or is not supported\n");
                stringBuilder.Append("by your browser.<br>\n");
                stringBuilder.Append("<h3>Please click on the Submit button to continue\n");
                stringBuilder.Append("the processing of your 3-D secure\n");
                stringBuilder.Append("transaction.</h3>");
                stringBuilder.Append("<input type=\"submit\" value=\"Submit\">\n");
                stringBuilder.Append("</center>\n");
                stringBuilder.Append("</noscript>\n");
                stringBuilder.Append("<input type=\"hidden\" name=\"PaReq\" value=\"" + GetMpiPaReq() + "\">\n");
                stringBuilder.Append("<input type=\"hidden\" name=\"MD\" value=\"" + GetMpiMd() + "\">\n");
                stringBuilder.Append(
                    "<input type=\"hidden\" name=\"TermUrl\" value=\"" + GetMpiTermUrl() + "\">\n");
                stringBuilder.Append("</form>\n");
                stringBuilder.Append("</body>\n");
                stringBuilder.Append("</html>\n");
                return stringBuilder.ToString();
            }

            public string GetMpiSuccess()
            {
                return (string) _responseDataHash["MpiSuccess"];
            }

            public string GetMpiMessage()
            {
                return (string) _responseDataHash["MpiMessage"];
            }

            public string GetMpiPaReq()
            {
                return (string) _responseDataHash["MpiPaReq"];
            }

            public string GetMpiTermUrl()
            {
                return (string) _responseDataHash["MpiTermUrl"];
            }

            public string GetMpiMd()
            {
                return (string) _responseDataHash["MpiMD"];
            }

            public string GetMpiAcsUrl()
            {
                return (string) _responseDataHash["MpiACSUrl"];
            }

            public string GetMpiCavv()
            {
                return (string) _responseDataHash["MpiCavv"];
            }

            public string GetMpiPaResVerified()
            {
                return (string) _responseDataHash["MpiPAResVerified"];
            }
        }

}