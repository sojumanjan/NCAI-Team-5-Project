Shader "UI/Hover Outline"
{
 Properties {
 [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
 _OutlineWidth ("Outline Width (screen pixels)", Float) = 0
 _UVRect ("Sprite UV Rect", Vector) = (0,0,1,1)
 _StencilComp ("Stencil Comparison", Float) = 8
 _Stencil ("Stencil ID", Float) = 0
 _StencilOp ("Stencil Operation", Float) = 0
 _StencilWriteMask ("Stencil Write Mask", Float) = 255
 _StencilReadMask ("Stencil Read Mask", Float) = 255
 _ColorMask ("Color Mask", Float) = 15
 [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
 }
 SubShader {
 Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
 Blend SrcAlpha OneMinusSrcAlpha
 ColorMask [_ColorMask]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
 struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; };
 sampler2D _MainTex; fixed4 _TextureSampleAdd; float4 _ClipRect; float4 _UVRect; float _OutlineWidth;
 v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
 float alphaAt(float2 uv) {
 float inside=step(_UVRect.x,uv.x)*step(_UVRect.y,uv.y)*step(uv.x,_UVRect.z)*step(uv.y,_UVRect.w);
 return (tex2D(_MainTex,clamp(uv,_UVRect.xy,_UVRect.zw))+_TextureSampleAdd).a*inside;
 }
 fixed4 frag(v2f i):SV_Target {
 fixed4 color=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
 float2 dx=ddx(i.uv)*_OutlineWidth;
 float2 dy=ddy(i.uv)*_OutlineWidth;
 if(_OutlineWidth>0.001) {
 float minimum=1;
 [unroll] for(int n=0;n<16;n++) {
 float angle=n*0.3926990817;
 minimum=min(minimum,alphaAt(i.uv+cos(angle)*dx+sin(angle)*dy));
 }
 float edge=1-smoothstep(0.1,0.6,minimum);
 color.rgb=lerp(color.rgb,float3(1,1,1),edge);
 }
 #ifdef UNITY_UI_CLIP_RECT
 color.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
 #endif
 #ifdef UNITY_UI_ALPHACLIP
 clip(color.a-.001);
 #endif
 return color;
 }
 ENDCG
 }
 }
}