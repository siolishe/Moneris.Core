using System.Collections;
using System.Text;

namespace Moneris.Core
{
    public class Transaction
    {
        protected readonly Hashtable TransactionParams = new Hashtable();
        private readonly string[] _xmlFormatTags;

        public Transaction(Hashtable transHash, string[] xmlFormat)
        {
            TransactionParams = transHash;
            _xmlFormatTags = xmlFormat;
        }

        public Transaction(string[] xmlFormat)
        {
            _xmlFormatTags = xmlFormat;
        }

        public Transaction()
        {
        }

        public virtual string ToXml()
        {
            var sb = new StringBuilder();
            toXML_low(sb, _xmlFormatTags, TransactionParams);
            return sb.ToString();
        }

        private void toXML_low(StringBuilder sb, string[] xmlTags, Hashtable xmlData)
        {
            foreach (var xmlTag in xmlTags)
            {
                var str = (string) xmlData[xmlTag];
                sb.Append("<" + xmlTag + ">" + str + "</" + xmlTag + ">");
            }
        }
    }

}