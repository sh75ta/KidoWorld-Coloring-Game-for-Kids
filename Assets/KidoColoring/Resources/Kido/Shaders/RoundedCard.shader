Shader "Kido/RoundedCard"
{
 // Frame, paper and artwork drawn from one signed-distance rounded rectangle: the artwork is
 // clipped by the inner edge and the inner/outer corners are concentric by construction.
 Properties
 {
  [PerRendererData] _MainTex("Artwork",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _BorderColor("Frame colour",Color)=(0,0,0,1)
  _Size("Card size in layout units",Vector)=(300,300,0,0)
  _Radius("Outer corner radius in layout units",Float)=30
  _Border("Frame thickness in layout units",Float)=3
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
   sampler2D _MainTex;fixed4 _BorderColor,_Color;float4 _ClipRect,_Size;float _Radius,_Border;
   v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float roundedBox(float2 p,float2 halfSize,float r)
   {
    float2 q=abs(p)-halfSize+r;
    return length(max(q,0.0))+min(max(q.x,q.y),0.0)-r;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float2 halfSize=_Size.xy*.5,p=(i.uv-.5)*_Size.xy;
    float2 innerHalf=max(halfSize-_Border,1.0);
    float outer=roundedBox(p,halfSize,_Radius);
    float inner=roundedBox(p,innerHalf,max(_Radius-_Border,0.0));
    float aa=max(fwidth(outer),.0001);
    float outerA=saturate(.5-outer/aa),innerA=saturate(.5-inner/aa);
    fixed4 art=tex2D(_MainTex,p/innerHalf*.5+.5);
    fixed3 paper=lerp(fixed3(1,1,1),art.rgb,art.a);
    fixed4 result=fixed4(lerp(_BorderColor.rgb,paper,innerA),outerA)*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
