using System.IO;
using System.Net;
using System.Text;
using iDeal.Base;
using iDeal.SignatureProviders;
using System.Xml.Linq;
using System.Xml;
using System;

namespace iDeal.Http
{
    public class iDealHttpRequest : IiDealHttpRequest
    {
        public iDealResponse SendRequest(iDealRequest idealRequest, ISignatureProvider signatureProvider, string url, IiDealHttpResponseHandler iDealHttpResponseHandler, ref iDealException exception)
        {
            // Create request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ProtocolVersion = HttpVersion.Version11;
            request.ContentType = "text/xml;charset=UTF-8";
            request.Method = "POST";
            

            // Set content
            XmlDocument requestXml = idealRequest.ToXml(signatureProvider);
            requestXml = signatureProvider.SignXmlFile(requestXml);
            var postBytes = Encoding.UTF8.GetBytes(requestXml.OuterXml);

            // Send
            var requestStream = request.GetRequestStream();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            // Return result
            var response = (HttpWebResponse)request.GetResponse();
            string responseRead = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return iDealHttpResponseHandler.HandleResponse(responseRead, signatureProvider, ref exception);
        }
    }
}
