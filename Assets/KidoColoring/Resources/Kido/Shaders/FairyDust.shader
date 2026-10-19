Shader "Kido/FairyDustCanvas"
{
 Properties
 {
  [PerRendererData] _MainTex("Paint",2D)="white"{}
  _LineTex("Transparent outline",2D)="black"{}
  _EffectTex("Fairy Dust mask",2D)="black"{}
  _SparkleTime("Sparkle time",Float)=0
  _CanvasSize("Paint resolution",Float)=1024
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
   sampler2D _MainTex,_LineTex,_EffectTex;
   float _SparkleTime,_CanvasSize;float4 _ClipRect;
   v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float hash(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 paint=tex2D(_MainTex,i.uv),line=tex2D(_LineTex,i.uv);
    float mask=tex2D(_EffectTex,i.uv).a*paint.a;
    fixed3 baseColor=lerp(fixed3(1,1,1),paint.rgb,paint.a);
    float2 pixel=i.uv*_CanvasSize,cell=floor(pixel/28.0);
    float seed=hash(cell);
    float2 point=frac(pixel/28.0)-(.2+.6*float2(seed,hash(cell+9.0)));
    float2 d=abs(point);
    float crossShape=max(saturate(1-d.x/.018)*saturate(1-d.y/.17),saturate(1-d.y/.018)*saturate(1-d.x/.17));
    float glow=saturate(1-length(point)/.055)*.25;
    float pulse=.5+.5*sin(_SparkleTime*1.5+seed*6.28318);
    pulse=pulse*pulse*pulse;
    float star=(crossShape+glow)*step(.87,seed)*(.10+.55*pulse);
    float grain=step(.955,hash(floor(pixel)))*.17;
    baseColor*=1-mask*step(.985,hash(floor(pixel)+73.0))*.09;
    baseColor=lerp(baseColor,fixed3(1,1,1),saturate((star+grain)*mask));
    fixed4 result=fixed4(lerp(baseColor,line.rgb,line.a),1)*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
