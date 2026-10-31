using System;

namespace Kido.Coloring
{
    public interface IEntitlementSource
    {
        bool HasFullAccess { get; }
        bool PurchasesAvailable { get; }
    }

    public sealed class AccessPolicy
    {
        readonly GameSettings settings;
        readonly IEntitlementSource entitlements;
        public bool HasFullAccess => entitlements != null && entitlements.HasFullAccess;
        public bool PurchasesAvailable => entitlements != null && entitlements.PurchasesAvailable;
        // Version one deliberately ships without advertising. Do not infer ad availability
        // from a bundled SDK: child-directed ads require a separate policy review.
        public bool AdsAvailable => false;
        public bool IsPreview => settings.previewAllCanvases;
        public AccessPolicy(GameSettings settings,IEntitlementSource entitlements=null)
        {this.settings=settings??throw new ArgumentNullException(nameof(settings));this.entitlements=entitlements;}
        public bool CanOpen(string id)
        {return settings.previewAllCanvases || HasFullAccess || Array.IndexOf(settings.freeCanvasIds??Array.Empty<string>(),id)>=0;}
    }
}
