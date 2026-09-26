// UI 스프라이트의 바깥 경계를 따라 얇은 외곽선을 그린다. UISpriteOutline 컴포넌트가 메시를 넓히고 값을 넣어준다.
// 선은 그림 바깥에만 그린다. 안쪽을 칠하면 그림이 깎여 보인다.
// 넓힌 영역(원본 UV 밖)은 투명으로 본다. 텍스처가 Clamp라 그대로 읽으면 가장자리 픽셀이 길게 늘어난다.
Shader "Hub/UI Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineUV ("Outline Width (UV)", Vector) = (0,0,0,0)
        _UVRect ("Sprite UV Rect", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            fixed4 _OutlineColor;
            float4 _OutlineUV;
            float4 _UVRect;

            // 12방향이면 얇은 선에서 모서리가 각져 보이지 않는다.
            static const float2 DIRECTIONS[12] =
            {
                float2(1, 0), float2(0.866, 0.5), float2(0.5, 0.866),
                float2(0, 1), float2(-0.5, 0.866), float2(-0.866, 0.5),
                float2(-1, 0), float2(-0.866, -0.5), float2(-0.5, -0.866),
                float2(0, -1), float2(0.5, -0.866), float2(0.866, -0.5)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half AlphaAt(float2 uv)
            {
                half inside = step(_UVRect.x, uv.x) * step(_UVRect.y, uv.y) * step(uv.x, _UVRect.z) * step(uv.y, _UVRect.w);
                return (tex2D(_MainTex, uv) + _TextureSampleAdd).a * inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                tex.a = AlphaAt(i.uv);

                half around = 0;
                [unroll] for (int n = 0; n < 12; n++)
                {
                    around = max(around, AlphaAt(i.uv + DIRECTIONS[n] * _OutlineUV.xy));
                }

                // 선 색은 Image.color에 물들지 않게 두고 투명도만 따른다. 페이드·먹히는 연출 때 선도 같이 사라져야 한다.
                half4 body = tex * i.color;
                half lineAlpha = around * (1 - tex.a) * _OutlineColor.a * i.color.a;

                fixed4 color;
                color.a = body.a + lineAlpha * (1 - body.a);
                color.rgb = (body.rgb * body.a + _OutlineColor.rgb * lineAlpha * (1 - body.a)) / max(color.a, 0.0001);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
