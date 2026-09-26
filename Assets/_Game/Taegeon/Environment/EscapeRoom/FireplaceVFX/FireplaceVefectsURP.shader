Shader "Taegeon/Fireplace Vefects URP"
{
Properties
	{
		_Noise_01_Texture("Noise_01_Texture", 2D) = "white" {}
		_Noise_02_Texture("Noise_02_Texture", 2D) = "white" {}
		_MaskTexture("Mask Texture", 2D) = "white" {}
		_MaskMoveTexture("Mask Move Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,0)
		_NoiseDistortionTexture("Noise Distortion Texture", 2D) = "white" {}
		_Noise01Scale("Noise 01 Scale", Vector) = (0.8,0.8,0,0)
		_Noise02Scale("Noise 02 Scale", Vector) = (1,1,0,0)
		_NoiseDistortionScale("Noise Distortion Scale", Vector) = (1,1,0,0)
		_Noise01Speed("Noise 01 Speed", Vector) = (0.5,0.5,0,0)
		_Noise02Speed("Noise 02 Speed", Vector) = (-0.2,0.4,0,0)
		_MaskMoveScale("Mask Move Scale", Vector) = (1,1,0,0)
		_MaskScale("Mask Scale", Vector) = (1,1,0,0)
		_MaskOffset("Mask Offset", Vector) = (0,0,0,0)
		_NoiseDistortionSpeed("Noise Distortion Speed", Vector) = (0.2,0.25,0,0)
		_MaskMultiply("Mask Multiply", Float) = 1
		_MaskMoveMultiply("Mask Move Multiply", Float) = 1
		_NoisesMultiply("Noises Multiply", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		_NoisesPower("Noises Power", Float) = 1
		_MaskMovePower("Mask Move Power", Float) = 1
		_Distortion("Distortion", Float) = 1
		_DistortionIntensity("Distortion Intensity", Float) = 0
		_OpacityBoost("Opacity Boost", Float) = 5
		_EmissionIntensity("Emission Intensity", Float) = 1
		_WindSpeed("Wind Speed", Float) = 1
		_MaskSpeed("Mask Speed", Float) = 0
		_Dissolve("Dissolve", Float) = 0
		[Space(13)][Header(AR)][Space(13)]_Cull1("Cull", Float) = 2
		_Src1("Src", Float) = 5
		_Dst1("Dst", Float) = 10
		_ZWrite1("ZWrite", Float) = 0
		_ZTest1("ZTest", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	
SubShader
{
    Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
    Pass
    {
        Tags { "LightMode"="SRPDefaultUnlit" }
        Cull [_Cull1]
        ZWrite Off
        ZTest LEqual
        Blend [_Src1] [_Dst1]
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        uniform float _Cull1;
		uniform float _Src1;
		uniform float _Dst1;
		uniform float _ZTest1;
		uniform float _ZWrite1;
		uniform float4 _Color;
		uniform sampler2D _MaskTexture;
		uniform sampler2D _NoiseDistortionTexture;
		uniform float _WindSpeed;
		uniform float2 _NoiseDistortionSpeed;
		uniform float2 _NoiseDistortionScale;
		uniform float _Distortion;
		uniform float _DistortionIntensity;
		uniform float _MaskSpeed;
		uniform float2 _MaskScale;
		uniform float2 _MaskOffset;
		uniform float _MaskPower;
		uniform float _MaskMultiply;
		uniform sampler2D _MaskMoveTexture;
		uniform float2 _MaskMoveScale;
		uniform float _MaskMovePower;
		uniform float _MaskMoveMultiply;
		uniform sampler2D _Noise_01_Texture;
		uniform float2 _Noise01Speed;
		uniform float2 _Noise01Scale;
		uniform sampler2D _Noise_02_Texture;
		uniform float2 _Noise02Speed;
		uniform float2 _Noise02Scale;
		uniform float _NoisesPower;
		uniform float _NoisesMultiply;
		uniform float _EmissionIntensity;
		uniform float _OpacityBoost;
		uniform float _Dissolve;

		
        struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float4 uv : TEXCOORD0; float4 customData : TEXCOORD1; };
        struct Input { float4 positionCS : SV_POSITION; half4 vertexColor : COLOR; float2 uv_texcoord : TEXCOORD0; float4 uv2_texcoord2 : TEXCOORD1; };
        Input vert(Attributes v)
        {
            Input o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.vertexColor = v.color;
            o.uv_texcoord = v.uv.xy;
            o.uv2_texcoord2 = v.customData;
            return o;
        }
        half4 frag(Input i) : SV_Target
        {
            float windSpeed13 = ( _WindSpeed * _Time.y );
			float2 uv_TexCoord15 = i.uv_texcoord * _NoiseDistortionScale;
			float2 panner18 = ( windSpeed13 * _NoiseDistortionSpeed + uv_TexCoord15);
			float Distortion31 = ( ( tex2D( _NoiseDistortionTexture, panner18 ).r * 0.1 ) * _Distortion );
			float2 appendResult44 = (float2(_MaskSpeed , 0.0));
			float2 uv_TexCoord39 = i.uv_texcoord * _MaskScale + _MaskOffset;
			float2 panner49 = ( windSpeed13 * appendResult44 + uv_TexCoord39);
			float2 uv_TexCoord50 = i.uv_texcoord * _MaskMoveScale;
			float2 appendResult52 = (float2(i.uv2_texcoord2.z , i.uv2_texcoord2.w));
			float2 uv_TexCoord28 = i.uv_texcoord * _Noise01Scale;
			float2 panner45 = ( windSpeed13 * _Noise01Speed + uv_TexCoord28);
			float2 uv_TexCoord35 = i.uv_texcoord * _Noise02Scale;
			float2 panner43 = ( windSpeed13 * _Noise02Speed + uv_TexCoord35);
			float Noises77 = saturate( ( pow( ( tex2D( _Noise_01_Texture, ( Distortion31 + panner45 ) ).r * tex2D( _Noise_02_Texture, ( Distortion31 + panner43 ) ).r ) , _NoisesPower ) * _NoisesMultiply ) );
			float temp_output_82_0 = ( saturate( ( saturate( ( pow( tex2D( _MaskTexture, ( ( Distortion31 * _DistortionIntensity ) + panner49 ) ).r , _MaskPower ) * _MaskMultiply ) ) * saturate( ( pow( tex2D( _MaskMoveTexture, ( uv_TexCoord50 + appendResult52 ) ).r , _MaskMovePower ) * _MaskMoveMultiply ) ) ) ) * Noises77 );
			half3 emission = ( ( ( _Color * i.vertexColor ) * temp_output_82_0 ) * _EmissionIntensity ).rgb;
			float temp_output_90_0 = ( saturate( ( temp_output_82_0 * _OpacityBoost ) ) - ( i.uv2_texcoord2.x + _Dissolve ) );
			half alpha = saturate( ( i.vertexColor.a * temp_output_90_0 ) );
		
            return half4(emission, alpha);
        }
        ENDHLSL
    }
}
}