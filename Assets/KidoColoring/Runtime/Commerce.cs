using System;
using UnityEngine;

namespace Kido.Coloring
{
    public enum StoreOutcomeKind { Purchased, Restored, Cancelled, Pending, Failed, Unavailable }

    public sealed class StoreOutcome
    {
        public StoreOutcomeKind kind;
        public string message;
        public bool GrantsAccess => kind==StoreOutcomeKind.Purchased || kind==StoreOutcomeKind.Restored;
        public static StoreOutcome Of(StoreOutcomeKind kind,string message="")=>new StoreOutcome{kind=kind,message=message??""};
    }

    // A platform adapter must return Purchased/Restored only after Google Play or
    // Apple has verified ownership. UI code never grants an entitlement directly.
    public interface IStoreBillingProvider
    {
        bool IsConfigured { get; }
        bool IsDevelopment { get; }
        string LocalizedPrice { get; }
        void Purchase(string productId,Action<StoreOutcome> completed);
        void Restore(string productId,Action<StoreOutcome> completed);
    }

    sealed class UnavailableBillingProvider:IStoreBillingProvider
    {
        public bool IsConfigured=>false;
        public bool IsDevelopment=>false;
        public string LocalizedPrice=>"";
        public void Purchase(string productId,Action<StoreOutcome> completed)=>completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Unavailable,"Store billing is not configured."));
        public void Restore(string productId,Action<StoreOutcome> completed)=>completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Unavailable,"Store billing is not configured."));
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    sealed class DevelopmentBillingProvider:IStoreBillingProvider
    {
        readonly string price;
        public DevelopmentBillingProvider(string price){this.price=string.IsNullOrWhiteSpace(price)?"Store price":price;}
        public bool IsConfigured=>true;
        public bool IsDevelopment=>true;
        public string LocalizedPrice=>price;
        public void Purchase(string productId,Action<StoreOutcome> completed)=>completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Purchased,"Development purchase simulation."));
        public void Restore(string productId,Action<StoreOutcome> completed)=>completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Restored,"Development restore simulation."));
    }
#endif

    public sealed class PurchaseService:IEntitlementSource
    {
        const string VerifiedKey="KidoWorld.FullAccess.v1";
        const string DevelopmentKey="KidoWorld.DevelopmentFullAccess.v1";
        readonly IStoreBillingProvider provider;
        readonly string productId,entitlementKey;

        PurchaseService(IStoreBillingProvider provider,string productId)
        {
            this.provider=provider??new UnavailableBillingProvider();
            this.productId=productId??"";
            entitlementKey=this.provider.IsDevelopment?DevelopmentKey:VerifiedKey;
        }

        public static PurchaseService Create(GameSettings settings)
        {
            if(settings==null)throw new ArgumentNullException(nameof(settings));
            IStoreBillingProvider provider=new UnavailableBillingProvider();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(settings.developmentPurchaseSimulation)provider=new DevelopmentBillingProvider(settings.developmentPrice);
#endif
#if UNITY_IOS
            string product=settings.appleProductId;
#else
            string product=settings.googleProductId;
#endif
            return new PurchaseService(provider,product);
        }

        public bool HasFullAccess=>PlayerPrefs.GetInt(entitlementKey,0)==1;
        public bool PurchasesAvailable=>provider.IsConfigured && !string.IsNullOrWhiteSpace(productId);
        public string LocalizedPrice=>provider.LocalizedPrice;
        public bool IsDevelopment=>provider.IsDevelopment;

        public void Purchase(Action<StoreOutcome> completed)
        {
            if(!PurchasesAvailable){completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Unavailable,"Store billing is not configured."));return;}
            provider.Purchase(productId,result=>Finish(result,completed));
        }

        public void Restore(Action<StoreOutcome> completed)
        {
            if(!PurchasesAvailable){completed?.Invoke(StoreOutcome.Of(StoreOutcomeKind.Unavailable,"Store billing is not configured."));return;}
            provider.Restore(productId,result=>Finish(result,completed));
        }

        void Finish(StoreOutcome result,Action<StoreOutcome> completed)
        {
            result=result??StoreOutcome.Of(StoreOutcomeKind.Failed,"The store returned no result.");
            if(result.GrantsAccess)
            {
                PlayerPrefs.SetInt(entitlementKey,1);
                PlayerPrefs.Save();
            }
            completed?.Invoke(result);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ClearDevelopmentEntitlement()
        {PlayerPrefs.DeleteKey(DevelopmentKey);PlayerPrefs.Save();}
#endif
    }
}
