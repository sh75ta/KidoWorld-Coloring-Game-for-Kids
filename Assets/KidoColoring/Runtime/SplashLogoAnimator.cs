using UnityEngine;
using UnityEngine.UI;

namespace Kido.Coloring
{
    // A continuous mesh keeps the flattened sticker intact. Independent letters
    // cannot be recovered by rectangular cuts: those cuts crossed the artwork.
    // This is a sequential letter-region bounce, not separate letter layers.
    [RequireComponent(typeof(RawImage))]
    public sealed class SplashLogoAnimator:BaseMeshEffect
    {
        public const float Duration=1.3f;
        const int Columns=40,Rows=32;
        static readonly Vector2[] Centers={
            new Vector2(.19f,.65f),new Vector2(.38f,.65f),
            new Vector2(.57f,.65f),new Vector2(.83f,.63f),
            new Vector2(.19f,.21f),new Vector2(.43f,.21f),
            new Vector2(.60f,.21f),new Vector2(.73f,.23f),new Vector2(.87f,.23f)};
        readonly float[] bounces=new float[9];
        float started;
        bool configured,animating;
        public void Configure(Texture texture)
        {
            var image=GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            started=Time.unscaledTime;configured=texture;animating=configured;
            if(graphic)graphic.SetVerticesDirty();
        }
        void Update()
        {
            if(!configured)return;
            bool moving=(!Feedback.Current||Feedback.Current.Motion)&&Time.unscaledTime-started<Duration;
            if(moving||animating){animating=moving;graphic.SetVerticesDirty();}
        }
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive()||!configured)return;
            float elapsed=Time.unscaledTime-started;
            bool motion=!Feedback.Current||Feedback.Current.Motion;
            for(int i=0;i<bounces.Length;i++)
            {
                float t=Mathf.Clamp01((elapsed-i*.10f)/.42f);
                bounces[i]=motion&&t>0&&t<1?Mathf.Sin(t*Mathf.PI):0;
            }
            if(elapsed>=Duration||!motion)return; // Original quad, exact source at rest.
            vh.Clear();var rect=graphic.rectTransform.rect;
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
            {
                var uv=new Vector2((float)x/Columns,(float)y/Rows);
                float lift=0;
                for(int i=0;i<Centers.Length;i++)
                {
                    float dx=(uv.x-Centers[i].x)/.20f,dy=(uv.y-Centers[i].y)/.30f;
                    float weight=Mathf.Clamp01(1-dx*dx-dy*dy);
                    lift+=bounces[i]*weight*weight;
                }
                vh.AddVert(new Vector3(rect.x+uv.x*rect.width,rect.y+uv.y*rect.height+lift*16,0),graphic.color,uv);
            }
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++)
            {
                int a=y*(Columns+1)+x,b=a+1,c=a+Columns+1,d=c+1;
                vh.AddTriangle(a,c,b);vh.AddTriangle(b,c,d);
            }
        }
    }
}
