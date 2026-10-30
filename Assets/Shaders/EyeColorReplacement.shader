Shader "Custom/UI/EyeColorReplacement"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Eye Color Settings)]
        _TargetEyeColor ("Target Eye Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _SourceHueMin ("Source Hue Min", Range(0, 1)) = 0.74
        _SourceHueMax ("Source Hue Max", Range(0, 1)) = 0.94
        _MinSaturation ("Min Saturation", Range(0, 1)) = 0.20
        _MinValue ("Min Value", Range(0, 1)) = 0.10
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.03

        [Header(Eye Region Gate)]
        _EyeMinV ("Eye Region Min V", Range(0, 1)) = 0.50
        _EyeMinU ("Eye Region Min U", Range(0, 1)) = 0.35
        _EyeMaxU ("Eye Region Max U", Range(0, 1)) = 0.65

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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _TargetEyeColor;
            float _SourceHueMin;
            float _SourceHueMax;
            float _MinSaturation;
            float _MinValue;
            float _Smoothness;
            float _EyeMinV;
            float _EyeMinU;
            float _EyeMaxU;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Restrict eye recoloring strictly to the eye region (V >= _EyeMinV, U in [_EyeMinU, _EyeMaxU]).
                // In all expression sprites, eyes are strictly at V in [0.52, 0.65], and the mouth is at V <= 0.47.
                // This completely prevents open mouth, pink lips, and tongue from ever being recolored!
                if (color.a > 0.05 && IN.texcoord.y >= _EyeMinV && IN.texcoord.x >= _EyeMinU && IN.texcoord.x <= _EyeMaxU)
                {
                    float3 hsv = rgb2hsv(color.rgb);
                    float3 targetHsv = rgb2hsv(_TargetEyeColor.rgb);

                    float inHue = smoothstep(_SourceHueMin - _Smoothness, _SourceHueMin, hsv.x) *
                                  (1.0 - smoothstep(_SourceHueMax, _SourceHueMax + _Smoothness, hsv.x));

                    float inSat = smoothstep(_MinSaturation - _Smoothness, _MinSaturation, hsv.y);
                    float inVal = smoothstep(_MinValue - _Smoothness, _MinValue, hsv.z);

                    float isEye = saturate(inHue * inSat * inVal);

                    if (isEye > 0.01)
                    {
                        float3 newHsv = float3(targetHsv.x, lerp(hsv.y, targetHsv.y, 0.7), hsv.z);
                        float3 replacedRgb = hsv2rgb(newHsv);
                        color.rgb = lerp(color.rgb, replacedRgb, isEye);
                    }
                }

                return color;
            }
            ENDCG
        }
    }
}
