using System;

namespace Kido.Coloring
{
    // No live advertising, billing, tracking, or simulated payment success is included.
    // A verified store adapter must supply entitlements before production monetization is enabled.
    public sealed class AccessPolicy
    {
        readonly GameSettings settings;
        public bool PurchasesAvailable => false;
        public bool AdsAvailable => false;
        public bool IsPreview => settings.previewAllCanvases;
        public AccessPolicy(GameSettings settings){this.settings=settings;}
        public bool CanOpen(string id)
        {return settings.previewAllCanvases || Array.IndexOf(settings.freeCanvasIds??Array.Empty<string>(),id)>=0;}
    }
}
