using iDeal.Base;
using iDeal.SignatureProviders;
using System;

namespace iDeal.Http
{
    public interface IiDealHttpResponseHandler
    {
        iDealResponse HandleResponse(string response, ISignatureProvider signatureProvider, ref iDealException exception);
    }
}
