Shader "Kido/FairyDustCanvas"
{
 Properties
 {
  [PerRendererData] _MainTex("Paint",2D)="white"{}
  _LineTex("Transparent outline",2D)="black"{}
  _EffectTex("Fairy Dust mask",2D)="black"{}
  _SparkleTime("Sparkle time",Float)=0
  _CanvasSize("Paint resolution",Float)=1024
  _ArtRect("Artwork on page",Vector)=(0.166667,0.166667,0.666667,0.666667)
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
   float _SparkleTime,_CanvasSize,_FillRadius,_PageAspect;float4 _ClipRect,_ArtRect,_FillOrigin;
   v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float hash(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
   float pageAxis(float v,float start,float end)
   {
    if(v<start)return v/max(.001,start)/6.0;
    if(v>end)return 5.0/6.0+(v-end)/max(.001,1-end)/6.0;
    return 1.0/6.0+(v-start)/max(.001,end-start)*2.0/3.0;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float2 uv=float2(pageAxis(i.uv.x,_ArtRect.x,_ArtRect.x+_ArtRect.z),pageAxis(i.uv.y,_ArtRect.y,_ArtRect.y+_ArtRect.w));
    float2 artUV=(uv-1.0/6.0)*1.5;
    float inside=step(0,artUV.x)*step(0,artUV.y)*step(artUV.x,1)*step(artUV.y,1);
    fixed4 paint=tex2D(_MainTex,uv),outlineSample=tex2D(_LineTex,artUV);outlineSample.a*=inside;
    float reveal=1-smoothstep(_FillRadius-.012,_FillRadius,length((i.uv-_FillOrigin.xy)*float2(_PageAspect,1)));
    if(_FillRadius<10)paint=lerp(tex2D(_BeforeFill,uv),paint,reveal);
    float mask=tex2D(_EffectTex,uv).a*paint.a;
    fixed3 baseColor=lerp(fixed3(1,1,1),paint.rgb,paint.a);
    float2 pixel=uv*_CanvasSize,cell=floor(pixel/24.0);
    float seed=hash(cell);
    float2 sparklePoint=frac(pixel/24.0)-(.18+.64*float2(seed,hash(cell+9.0)));
    float2 d=abs(sparklePoint);
    float crossShape=max(saturate(1-d.x/.045)*saturate(1-d.y/.25),saturate(1-d.y/.045)*saturate(1-d.x/.25));
    float glow=saturate(1-length(sparklePoint)/.075)*.34;
    float pulse=.5+.5*sin(_SparkleTime*1.5+seed*6.28318);
    pulse=pulse*pulse*pulse;
    float star=(crossShape+glow)*step(.68,seed)*(.25+.95*pulse);
    float grain=step(.965,hash(floor(pixel)))*.14;
    baseColor*=1-mask*step(.985,hash(floor(pixel)+73.0))*.09;
    baseColor=lerp(baseColor,fixed3(1,1,1),saturate((star+grain)*mask));
    fixed4 result=fixed4(lerp(baseColor,outlineSample.rgb,outlineSample.a),1)*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
