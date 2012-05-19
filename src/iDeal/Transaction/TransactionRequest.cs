﻿using System;
using System.Xml.Linq;
using iDeal.Base;
using iDeal.SignatureProviders;

namespace iDeal.Transaction
{
    public class TransactionRequest : iDealRequest
    {
        private string _merchantReturnUrl;
        private string _purchaseId;
        private TimeSpan? _expirationPeriod;
        private string _description;
        private string _entranceCode;
        
        /// <summary>
        /// Unique identifier of issuer
        /// </summary>
        public int IssuerId { get; private set; }
        
        /// <summary>
        /// Url to which consumer is redirected after authorizing the payment
        /// </summary>
        public string MerchantReturnUrl
        {
            get
            {
                return _merchantReturnUrl;
            }
            set
            {
                if (value.IsNullEmptyOrWhiteSpace())
                    throw new InvalidOperationException("Merchant url is required");
                _merchantReturnUrl = value.Trim();
            }
        }
        
        /// <summary>
        /// Unique id determined by the acceptant, which will eventuelly show on the bank account
        /// </summary>
        public string PurchaseId
        {
            get { return _purchaseId; }
            set
            {
                if (value.IsNullEmptyOrWhiteSpace())
                    throw new InvalidOperationException("Purchase id is required");
                if (value.Length > 16)
                    throw new InvalidOperationException("Purchase id cannot contain more than 16 characters");
                _purchaseId = value;
            }
        }

        /// <summary>
        /// Amount measured in cents
        /// </summary>
        public int Amount { get; private set; }

        /// <summary>
        /// Time until consumer has to have paid, otherwise the transaction is marked as expired by the issuer (consumer's bank)
        /// </summary>
        public TimeSpan? ExpirationPeriod
        {
            get { return _expirationPeriod; }
            set
            {
                if (value.HasValue)
                {
                    if (value.Value.TotalMinutes < 1)
                        throw new InvalidOperationException("Minimum expiration period is one minute");
                    if (value.Value.TotalMinutes > 60)
                        throw new InvalidOperationException("Maximum expiration period is 1 hour");
                }
                _expirationPeriod = value;
            }
        }

        /// <summary>
        /// Description ordered product (no html tags!)
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                if (value.Trim().Length > 32)
                    throw new InvalidOperationException("Description cannot contain more than 32 characters");
                _description = value.Trim();
            }
        }

        /// <summary>
        /// Unique code generated by acceptant by which consumer can be identified
        /// </summary>
        public string EntranceCode
        {
            get { return _entranceCode; }
            set
            {
                if (value.IsNullEmptyOrWhiteSpace())
                    throw new InvalidOperationException("Entrance code is required");
                if (value.Length > 40)
                    throw new InvalidOperationException("Entrance code cannot contain more than 40 characters");
                _entranceCode = value;
            }
        }

        public override string MessageDigest
        {
            get
            {
                return
                    CreateDateTimeStamp +
                    IssuerId.ToString().PadLeft(4, '0') +
                    MerchantId.PadLeft(9, '0') +
                    MerchantSubId +
                    MerchantReturnUrl +
                    PurchaseId +
                    Amount +
                    "EUR" +
                    "nl" +
                    Description +
                    EntranceCode;
            }
        }

        public TransactionRequest(string merchantId, int? subId, int issuerId, string merchantReturnUrl, string purchaseId, int amount, TimeSpan? expirationPeriod, string description, string entranceCode)
        {
            MerchantId = merchantId;
            MerchantSubId = subId ?? 0; // If no sub id is specified, sub id should be 0
            IssuerId = issuerId;
            MerchantReturnUrl = merchantReturnUrl;
            PurchaseId = purchaseId;
            Amount = amount;
            ExpirationPeriod = expirationPeriod;
            Description = description;
            EntranceCode = entranceCode;
        }

        public override string ToXml(ISignatureProvider signatureProvider)
        {
            XNamespace xmlNamespace = "http://www.idealdesk.com/Message";

            var directoryRequestXmlMessage =
                new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(xmlNamespace + "AcquirerTrxReq",
                        new XAttribute("version", "1.1.0"),
                        new XElement(xmlNamespace + "createDateTimeStamp", CreateDateTimeStamp),
                        new XElement(xmlNamespace + "Issuer",
                            new XElement(xmlNamespace + "issuerID", IssuerId.ToString().PadLeft(4,'0'))
                        ),
                        new XElement(xmlNamespace + "Merchant",
                            new XElement(xmlNamespace + "merchantID", MerchantId.PadLeft(9, '0')),
                            new XElement(xmlNamespace + "subID", MerchantSubId),
                            new XElement(xmlNamespace + "authentication", "SHA1_RSA"),
                            new XElement(xmlNamespace + "token", signatureProvider.GetThumbprint()),
                            new XElement(xmlNamespace + "tokenCode", signatureProvider.GetSignature(MessageDigest)),
                            new XElement(xmlNamespace + "merchantReturnURL", MerchantReturnUrl)
                        ),
                        new XElement(xmlNamespace + "Transaction",
                            new XElement(xmlNamespace + "purchaseID", PurchaseId),
                            new XElement(xmlNamespace + "amount", Amount),
                            new XElement(xmlNamespace + "currency", "EUR"),
                            new XElement(xmlNamespace + "expirationPeriod", "PT" + Convert.ToInt32(Math.Floor(ExpirationPeriod.Value.TotalSeconds)) + "S"),
                            new XElement(xmlNamespace + "language", "nl"),
                            new XElement(xmlNamespace + "description", Description),
                            new XElement(xmlNamespace + "entranceCode", EntranceCode)
                        )
                    )
                );

            //return directoryRequestXmlMessage.Declaration + directoryRequestXmlMessage.ToString(SaveOptions.OmitDuplicateNamespaces);
            return directoryRequestXmlMessage.Declaration + directoryRequestXmlMessage.ToString(SaveOptions.None);
        }
    }
}
