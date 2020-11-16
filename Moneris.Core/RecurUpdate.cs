using System.Collections;
using System.Text;

namespace Moneris.Core
{
            public class RecurUpdate : Transaction
        {
            private static readonly string[] XmlTags = new string[9]
            {
                "order_id",
                "cust_id",
                "recur_amount",
                "pan",
                "expdate",
                "add_num_recurs",
                "total_num_recurs",
                "hold",
                "terminate"
            };

            public RecurUpdate(Hashtable recurUpdate)
                : base(recurUpdate, XmlTags)
            {
            }

            public RecurUpdate(string orderId)
                : base(XmlTags)
            {
                TransactionParams.Add(nameof(orderId), orderId);
            }

            public void SetCustId(string custId)
            {
                TransactionParams.Add(nameof(custId), custId);
            }

            public void SetRecurAmount(string recurAmount)
            {
                TransactionParams.Add(nameof(recurAmount), recurAmount);
            }

            public void SetPan(string pan)
            {
                TransactionParams.Add(nameof(pan), pan);
            }

            public void SetExpiryDate(string expiryDate)
            {
                TransactionParams.Add("expdate", expiryDate);
            }

            public void SetAddNumRecurs(string addNumRecurs)
            {
                TransactionParams.Add(nameof(addNumRecurs), addNumRecurs);
            }

            public void SetTotalNumRecurs(string totalNumRecurs)
            {
                TransactionParams.Add(nameof(totalNumRecurs), totalNumRecurs);
            }

            public void SetHold(string hold)
            {
                TransactionParams.Add(nameof(hold), hold);
            }

            public void SetTerminate(string terminate)
            {
                TransactionParams.Add(nameof(terminate), terminate);
            }

            public override string ToXml()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("<recur_update>");
                stringBuilder.Append(base.ToXml());
                stringBuilder.Append("</recur_update>");
                return stringBuilder.ToString();
            }
        }

}