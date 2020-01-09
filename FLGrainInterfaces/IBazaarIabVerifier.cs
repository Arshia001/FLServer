using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IBazaarIabVerifier : IGrainWithIntegerKey
    {
        Task<IabPurchaseResult> VerifyBazaarPurchase(string sku, string token);
    }
}
