using System;
using System.Xml.Linq;
using iDeal.Base;
using iDeal.SignatureProviders;
using Security.Cryptography;
using System.Security.Cryptography;
using System.Xml;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;

namespace iDeal.Directory
{
    public class DirectoryRequest : iDealRequest
    {
        public DirectoryRequest(string merchantId, int? subId)
        {
            MerchantId = merchantId;
            MerchantSubId = subId ?? 0; // If no sub id is specified, sub id should be 0
        }

        public override string MessageDigest
        {
            get { return CreateDateTimeStamp + MerchantId + MerchantSubId; }
        }

        /// <summary>
        /// Creates xml representation of directory request
        /// </summary>
        public override XmlDocument ToXml(ISignatureProvider signatureProvider)
        {
            XNamespace xmlNamespace = "http://www.idealdesk.com/ideal/messages/mer-acq/3.3.1";
            XNamespace xmlNamespaceSignature = "http://www.w3.org/2000/09/xmldsig#";

            var requestXmlMessage =
               new XDocument(
                   new XDeclaration("1.0", "UTF-8", null),
                   new XElement(xmlNamespace + "DirectoryReq",
                       new XAttribute("version", "3.3.1"),
                       new XElement(xmlNamespace + "createDateTimestamp", CreateDateTimeStamp),
                       new XElement(xmlNamespace + "Merchant",
                           new XElement(xmlNamespace + "merchantID", MerchantId.PadLeft(9, '0')),
                           new XElement(xmlNamespace + "subID", MerchantSubId)
                       )
                   )
               );

            var xmlDocument = new XmlDocument();
            using (var xmlReader = requestXmlMessage.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }

            return xmlDocument;
        }

    }
}
