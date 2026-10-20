using UnityEngine;
using UnityEngine.UI;

namespace Kido.Coloring
{
    // One bounded mesh for all particles, no GameObjects or arrays per gesture.
    // A custom Graphic must declare its renderer (Image/RawImage do; Graphic itself does not).
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class KidsEffects:MaskableGraphic
    {
        struct Particle {public Vector2 origin,velocity;public Color color;public float born,life,size,spin;public bool shower;}
        public const float ShowerSeconds=1.6f;
        readonly Particle[] pool=new Particle[192];int cursor;
        public void Clear(){System.Array.Clear(pool,0,pool.Length);SetVerticesDirty();}
        public static KidsEffects Get(RectTransform parent)
        {
            var existing=parent.Find("Feedback particles");if(existing)return existing.GetComponent<KidsEffects>();
            var rect=KidoUI.Rect("Feedback particles",parent);KidoUI.Place(rect,0,0,1,1);
            var result=rect.gameObject.AddComponent<KidsEffects>();result.raycastTarget=false;return result;
        }
        public void Burst(Vector2 screenPosition,Color color)
        {
            transform.SetAsLastSibling();RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPosition,null,out var local);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12+Random.Range(-.12f,.12f);
                Add(local,new Vector2(Mathf.Cos(a)*Random.Range(45,130),Mathf.Sin(a)*Random.Range(55,145)+65),color,1.4f,Random.Range(5,11),false);
            }
        }
        public void Shower()
        {
            transform.SetAsLastSibling();var r=rectTransform.rect;
            for(int i=0;i<96;i++)Add(new Vector2(Random.Range(r.xMin,r.xMax),r.yMax+Random.Range(0,160)),
                new Vector2(Random.Range(-65,65),Random.Range(-640,-340)),KidoUI.Hex(KidoUI.HexColors[i%18]),ShowerSeconds,Random.Range(7,15),true);
        }
        void Add(Vector2 origin,Vector2 velocity,Color color,float life,float size,bool shower)
        {
            pool[cursor]=new Particle{origin=origin,velocity=velocity,color=color,born=Time.unscaledTime,life=life,size=size,spin=Random.Range(-4,4),shower=shower};
            cursor=(cursor+1)%pool.Length;SetVerticesDirty();
        }
        void Update()
        {
            bool live=false;for(int i=0;i<pool.Length;i++)if(pool[i].life>0){live=true;if(Time.unscaledTime-pool[i].born>=pool[i].life)pool[i].life=0;}
            if(live)SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float now=Time.unscaledTime;
            for(int i=0;i<pool.Length;i++)
            {
                var p=pool[i];float age=now-p.born;if(p.life<=0||age>p.life)continue;
                float t=age/p.life;Vector2 center=p.origin+p.velocity*age+Vector2.down*(p.shower?70:110)*age*age;
                var col=p.color;col.a*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,t));float size=p.size*(1-.45f*t),angle=p.spin*age;
                int start=vh.currentVertCount;vh.AddVert(center,col,Vector2.zero);
                for(int k=0;k<10;k++)
                {float a=angle+k*Mathf.PI/5,r=size*(k%2==0?1:.42f);vh.AddVert(center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*r,col,Vector2.zero);}
                for(int k=0;k<10;k++)vh.AddTriangle(start,start+1+k,start+1+(k+1)%10);
            }
        }
    }
    public sealed class GentleFloat:MonoBehaviour
    {
        public float Phase,Amplitude=5;
        RectTransform rect;Vector2 initial;bool ready;
        public void Capture(){rect=(RectTransform)transform;initial=rect.anchoredPosition;ready=true;}
        void Start(){if(!ready)Capture();}
        void LateUpdate()
        {
            if(!ready)return;bool motion=!Feedback.Current||Feedback.Current.Motion;
            float t=Time.unscaledTime*.8f+Phase;
            rect.anchoredPosition=initial+(motion?new Vector2(Mathf.Sin(t)*Amplitude,Mathf.Cos(t*.8f)*Amplitude):Vector2.zero);
            rect.localRotation=Quaternion.Euler(0,0,motion?Mathf.Sin(t*.6f)*1.5f:0);
        }
    }
}
