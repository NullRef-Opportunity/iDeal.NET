using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace iDeal.SignatureProviders
{
    public class SignatureProvider : ISignatureProvider
    {
        private readonly X509Certificate2 _privateCertificate;
        private readonly X509Certificate2 _publicCertificate;

        public SignatureProvider(X509Certificate2 privateCertificate, X509Certificate2 publicCertificate)
        {
            _privateCertificate = privateCertificate;
            _publicCertificate = publicCertificate;
        }

        public XmlDocument SignXmlFile(XmlDocument doc, bool privateKey)
        {
            string signatureCanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
            string signatureMethod = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            string digestMethod = @"http://www.w3.org/2001/04/xmlenc#sha256";

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(doc);

            // Add the key to the SignedXml document. 
            CspParameters cspParams = new CspParameters(24);
            cspParams.KeyContainerName = "XML_DISG_RSA_KEY";
            RSACryptoServiceProvider key = new RSACryptoServiceProvider(2048, cspParams);

            X509Certificate2 signingCertificate;
            if (privateKey)
            {
                signingCertificate = GetMerchantCertificate();
                key.FromXmlString(signingCertificate.PrivateKey.ToXmlString(true)); /*assign the new key to signer's SigningKey */
            }
            else
            {
                signingCertificate = GetAcquirerCertificate();
                key.FromXmlString(signingCertificate.PublicKey.Key.ToXmlString(false)); /*assign the new key to signer's SigningKey */
            }
            signedXml.SigningKey = key;

            signedXml.KeyInfo = new KeyInfo();
            signedXml.KeyInfo.AddClause(new KeyInfoName(signingCertificate.Thumbprint));

            signedXml.SignedInfo.CanonicalizationMethod = signatureCanonicalizationMethod;
            signedXml.SignedInfo.SignatureMethod = signatureMethod;

            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            reference.DigestMethod = digestMethod;

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save 
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            return doc;
        }

        private X509Certificate2 GetMerchantCertificate()
        {
            string thumbprint = _privateCertificate.Thumbprint;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 card = null;
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (!cert.HasPrivateKey) continue;

                if (cert.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    card = cert;
                    break;
                }
            }
            store.Close();

            return card;
        }

        private X509Certificate2 GetAcquirerCertificate()
        {
            string thumbprint = _publicCertificate.Thumbprint;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 card = null;
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (cert.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    card = cert;
                    break;
                }
            }
            store.Close();

            return card;
        }
        
        /// <summary>
        /// Gets the digital signature used in each request send to the ideal api (stored in xml field tokenCode)
        /// </summary>
        /// <param name="messageDigest">Concatenation of designated fields from the request. Varies between types of request, consult iDeal Merchant Integratie Gids</param>
        public string GetSignature(string messageDigest)
        {
            // Step 1: Create a 160 bit message digest
            var hash = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(messageDigest));

            //Step 2: Sign with 1024 bits private key (RSA)
            var rsaCryptoServiceProvider = (RSACryptoServiceProvider)_privateCertificate.PrivateKey; // Create rsa crypto provider from private key contained in certificate, weirdest cast ever!
            var encryptedMessage = rsaCryptoServiceProvider.SignHash(hash, "SHA1");

            // Step 3: Base64 encode string for storage in xml request
            return Convert.ToBase64String(encryptedMessage);
        }

        /// <summary>
        /// Verifies the digital signature used in status responses from the ideal api (stored in xml field signature value)
        /// </summary>
        /// <param name="signature">Signature provided by ideal api, stored in signature value xml field</param>
        /// <param name="messageDigest">Concatenation of designated fields from the status response</param>
        //public bool VerifySignature(string signature, string messageDigest)
        //{
        //    // Step 1: Create a 160 bit message digest to compare with the one provided in the signature
        //    var hash = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(messageDigest));
            
        //    // Step 2: Base 64 deocde signature
        //    var decodedSignature = System.Convert.FromBase64String(signature);

        //    // Step 3: Verify signature with public key
        //    var rsaCryptoServiceProvider = (RSACryptoServiceProvider)_publicCertificate.PublicKey.Key;
        //    return rsaCryptoServiceProvider.VerifyHash(hash, "SHA1", decodedSignature);
        //}

        public bool VerifySignature(XElement xDocument)
        {
            bool result = false;
            
            XNamespace xmlNamespaceSignature = "http://www.w3.org/2000/09/xmldsig#";
            string signatureValue = xDocument.Element(xmlNamespaceSignature + "Signature").Element(xmlNamespaceSignature + "SignatureValue").Value;

            //var xmlSignatureDocument = new XmlDocument();
            //using (var xmlReader = xDocument.Element(xmlNamespaceSignature + "Signature").CreateReader())
            //{
            //    xmlSignatureDocument.Load(xmlReader);
            //}
            
            //Remove current signature
            xDocument.Element(xmlNamespaceSignature + "Signature").Remove();
            result = VerifyDataWithSignature(xDocument.ToString(), signatureValue);

            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            result = VerifyDataWithSignature(xmlDocument.OuterXml, signatureValue);
            //hash xmldocument
            XmlDocument hashedResponse = SignXmlFile(xmlDocument, false);

            //Compare Signature values
            //if (signatureValue == hashedResponse.SelectSingleNode("SignatureValue").InnerText)
            //{
            //    result = true;
            //}
            return result;
        }

        public bool VerifyDataWithSignature(string data, string signature)
        {
            // Step 1: Create a 160 bit message digest to compare with the one provided in the signature
            var hash = new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(data));

            // Step 2: Base 64 deocde signature
            var decodedSignature = System.Convert.FromBase64String(signature);

            // Step 3: Verify signature with public key
            var rsaCryptoServiceProvider = (RSACryptoServiceProvider)_publicCertificate.PublicKey.Key;
            bool result = rsaCryptoServiceProvider.VerifyHash(hash, "SHA256", decodedSignature);
            return result;
        }


        /// <summary>
        /// Gets thumbprint of acceptant's certificate, used in each request to the ideal api (stored in field token)
        /// </summary>
        public string GetThumbprintAcceptantCertificate()
        {
            return _privateCertificate.Thumbprint;
        }

        /// <summary>
        /// Gets thumbprint of the acquirer's certificate, used in status response from ideal api
        /// </summary>
        public string GetThumbprintAcquirerCertificate()
        {
            return _publicCertificate.Thumbprint;
        }

        
    }
}
