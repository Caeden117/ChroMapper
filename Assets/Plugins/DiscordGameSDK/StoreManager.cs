using System.Collections.Generic;

namespace Discord
{
    public partial class StoreManager
    {
        public IEnumerable<Entitlement> GetEntitlements()
        {
            int count = CountEntitlements();
            List<Entitlement> entitlements = new List<Entitlement>();
            for (int i = 0; i < count; i++)
            {
                entitlements.Add(GetEntitlementAt(i));
            }
            return entitlements;
        }

        public IEnumerable<Sku> GetSkus()
        {
            int count = CountSkus();
            List<Sku> skus = new List<Sku>();
            for (int i = 0; i < count; i++)
            {
                skus.Add(GetSkuAt(i));
            }
            return skus;
        }
    }
}
