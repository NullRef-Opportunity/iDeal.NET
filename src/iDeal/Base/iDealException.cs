using System;
using System.Xml.Linq;

namespace iDeal.Base
{
    public class iDealException : SystemException
    {
        public DateTime CreateDateTimeStamp { get; private set; }
        public string ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public string ErrorDetail { get; private set; }
        public string ConsumerMessage { get; set; }
        public string ErrorDescriptionCombined
        {
            get
            {
                return "Code: " + ErrorCode + ", Message: " + ErrorMessage + ", Detail: " + ErrorDetail;
            }
        }

        public iDealException(XElement xDocument)
        {
            XNamespace xmlNamespace = "http://www.idealdesk.com/ideal/messages/mer-acq/3.3.1";

            CreateDateTimeStamp = DateTime.Parse(xDocument.Element(xmlNamespace + "createDateTimestamp").Value);

            ErrorCode = xDocument.Element(xmlNamespace + "Error").Element(xmlNamespace + "errorCode").Value;
            ErrorMessage = xDocument.Element(xmlNamespace + "Error").Element(xmlNamespace + "errorMessage").Value;
            ErrorDetail = xDocument.Element(xmlNamespace + "Error").Element(xmlNamespace + "errorDetail").Value;
            ConsumerMessage = xDocument.Element(xmlNamespace + "Error").Element(xmlNamespace + "consumerMessage").Value;
        }
    }
}
