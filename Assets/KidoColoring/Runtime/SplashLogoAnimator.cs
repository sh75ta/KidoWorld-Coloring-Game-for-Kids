using UnityEngine;
using UnityEngine.UI;

namespace Kido.Coloring
{
    // Uses non-overlapping UV slices of the supplied original logo texture.
    // At rest the slices reproduce the source pixels exactly; animation does
    // not redraw, recolor or allocate new textures.
    public sealed class SplashLogoAnimator:MonoBehaviour
    {
        // Pixel cuts follow the nine letters: K i d o / W o r l d.
        static readonly RectInt[] Cuts={
            new RectInt(0,0,330,560),new RectInt(330,0,160,560),
            new RectInt(490,0,300,560),new RectInt(790,0,364,560),
            new RectInt(0,560,380,354),new RectInt(380,560,225,354),
            new RectInt(605,560,155,354),new RectInt(760,560,105,354),
            new RectInt(865,560,289,354)};
        readonly RectTransform[] letters=new RectTransform[Cuts.Length];
        float started;
        bool configured;

        public void Configure(Texture texture)
        {
            if(!texture)return;
            float width=texture.width,height=texture.height;
            for(int i=0;i<Cuts.Length;i++)
            {
                var cut=Cuts[i];var rect=KidoUI.Rect("Logo letter "+(i+1),transform);
                var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
                float left=cut.x/width,right=cut.xMax/width,top=1-cut.y/height,bottom=1-cut.yMax/height;
                KidoUI.Place(rect,left,bottom,right,top);
                image.uvRect=new Rect(left,bottom,right-left,top-bottom);letters[i]=rect;
            }
            started=Time.unscaledTime;configured=true;
        }
        void Update()
        {
            if(!configured)return;bool motion=!Feedback.Current||Feedback.Current.Motion;
            float elapsed=Time.unscaledTime-started;
            for(int i=0;i<letters.Length;i++)
            {
                var rect=letters[i];if(!rect)continue;
                float t=motion?Mathf.Clamp01((elapsed-i*.085f)/.38f):1;
                float bounce=t>0&&t<1?Mathf.Sin(t*Mathf.PI):0;
                rect.localScale=Vector3.one*(1+bounce*.13f);
                rect.anchoredPosition=new Vector2(0,bounce*24);
            }
        }
    }
}
