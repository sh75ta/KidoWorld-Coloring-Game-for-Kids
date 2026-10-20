Shader "Kido/FairyDustCanvas"
{
 Properties
 {
  [PerRendererData] _MainTex("Paint",2D)="white"{}
  _LineTex("Transparent outline",2D)="black"{}
  _EffectTex("Fairy Dust mask",2D)="black"{}
  _SparkleTime("Sparkle time",Float)=0
  _PageSize("Paint page size in pixels",Vector)=(2048,1536,0,0)
  _PageMap("Display UV to page UV (scale xy, offset zw)",Vector)=(1,1,0,0)
  _ArtMap("Display UV to artwork UV (scale xy, offset zw)",Vector)=(1,1,0,0)
  _Thumb("Thumbnail render (ignore vertex colour)",Float)=0
  _BeforeFill("Previous paint",2D)="white"{}
  _FillOrigin("Fill origin",Vector)=(0,0,0,0)
  _FillRadius("Fill radius",Float)=100
  _PageAspect("Page aspect ratio",Float)=1
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
   sampler2D _MainTex,_LineTex,_EffectTex,_BeforeFill;
   float _SparkleTime,_FillRadius,_PageAspect,_Thumb;float4 _ClipRect,_PageSize,_PageMap,_ArtMap,_FillOrigin;
   v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float hash(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
   float twinkle(float seed,float t,float speed){float s=.5+.5*sin(t*speed+seed*6.28318);return s*s*s;}
   // One layer of four-point star sparkles. Each star lives inside its own cell and its arms
   // never reach the cell border, so nothing is clipped into squares.
   float sparkleLayer(float2 pixel,float cellSize,float density,float arm,float width,float glowRadius,float t,float speed)
   {
    float2 g=pixel/cellSize,cell=floor(g);float seed=hash(cell+cellSize);
    if(seed<1.0-density)return 0;
    float2 p=frac(g)-(.32+.36*float2(hash(cell+3.1),hash(cell+7.7)));
    float2 d=abs(p);
    float plus=max(saturate(1-d.x/width)*saturate(1-d.y/arm),saturate(1-d.y/width)*saturate(1-d.x/arm));
    float glow=saturate(1-length(p)/glowRadius);
    return (plus+glow*glow*.7)*(.3+.9*twinkle(seed*13.1,t,speed));
   }
   fixed4 frag(v2f i):SV_Target
   {
    // Both maps are affine and uniform in x/y: strokes keep their shape everywhere on the page.
    float2 uv=i.uv*_PageMap.xy+_PageMap.zw;
    float2 artUV=i.uv*_ArtMap.xy+_ArtMap.zw;
    float inside=step(0,artUV.x)*step(0,artUV.y)*step(artUV.x,1)*step(artUV.y,1);
    fixed4 paint=tex2D(_MainTex,uv),outlineSample=tex2D(_LineTex,artUV);outlineSample.a*=inside;
    float reveal=1-smoothstep(_FillRadius-.012,_FillRadius,length((i.uv-_FillOrigin.xy)*float2(_PageAspect,1)));
    if(_FillRadius<10)paint=lerp(tex2D(_BeforeFill,uv),paint,reveal);
    float mask=tex2D(_EffectTex,uv).a*paint.a;
    fixed3 baseColor=lerp(fixed3(1,1,1),paint.rgb,paint.a);
    float2 pixel=uv*_PageSize.xy;
    // Fairy Dust: brighter body with a slow shimmer, a textured grain and three sizes of white sparkle.
    float shimmer=.5+.5*sin(dot(pixel,float2(.011,.007))+_SparkleTime*.9);
    float fine=step(.80,hash(floor(pixel/2.0)))*(.22+.5*twinkle(hash(floor(pixel/2.0)+5.0),_SparkleTime,2.6));
    float flakes=sparkleLayer(pixel,18.0,.38,.32,.07,.22,_SparkleTime,1.9);
    float stars=sparkleLayer(pixel,60.0,.42,.34,.04,.14,_SparkleTime,1.2);
    float dark=step(.985,hash(floor(pixel)+73.0))*.10;
    float lift=.16+.10*shimmer;
    baseColor*=1-mask*dark;
    baseColor=lerp(baseColor,fixed3(1,1,1),saturate((lift+fine*.6+flakes*.9+stars*1.25)*mask));
    fixed4 vertexColor=lerp(i.color,fixed4(1,1,1,1),_Thumb);
    fixed4 result=fixed4(lerp(baseColor,outlineSample.rgb,outlineSample.a),1)*vertexColor;
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
