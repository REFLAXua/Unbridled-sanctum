Shader "Custom/UI/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0.85, 0.87, 0.92, 1.0)
        _OutlineWidth ("Outline Width", Range(0, 20)) = 0
        _AlphaThreshold ("Alpha Cutoff", Range(0.01, 0.5)) = 0.05

        [Header(Glow and Brightness Settings)]
        _HoverBrightness ("Hover Brightness", Range(0.5, 3.0)) = 1.0
        _GlowColor ("Glow Color", Color) = (1.0, 0.94, 0.75, 0.0)

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
            Name "Outline"
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _AlphaThreshold;
            float _HoverBrightness;
            fixed4 _GlowColor;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 rawCol = tex2D(_MainTex, IN.texcoord) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                rawCol.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                // Apply hover brightness and warm sunlight bloom to visible pixels
                half3 litRgb = rawCol.rgb * _HoverBrightness + _GlowColor.rgb * (_GlowColor.a * rawCol.a);
                half4 color = half4(litRgb, rawCol.a);

                // If outline is disabled or negligible, return sprite directly
                if (_OutlineWidth <= 0.05)
                {
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                    #endif
                    return color;
                }

                // If the pixel itself is opaque / visible, keep original art
                if (color.a > _AlphaThreshold)
                {
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                    #endif
                    return color;
                }

                // Pixel is in the transparent fringe: concentric radial sampling for solid thick outline
                float maxA = 0.0;
                float2 stepSize = _MainTex_TexelSize.xy * _OutlineWidth;

                // Outer ring (16 samples at full radius)
                static const float2 outerDirs[16] = {
                    float2( 1.000,  0.000),
                    float2( 0.924,  0.383),
                    float2( 0.707,  0.707),
                    float2( 0.383,  0.924),
                    float2( 0.000,  1.000),
                    float2(-0.383,  0.924),
                    float2(-0.707,  0.707),
                    float2(-0.924,  0.383),
                    float2(-1.000,  0.000),
                    float2(-0.924, -0.383),
                    float2(-0.707, -0.707),
                    float2(-0.383, -0.924),
                    float2( 0.000, -1.000),
                    float2( 0.383, -0.924),
                    float2( 0.707, -0.707),
                    float2( 0.924, -0.383)
                };

                // Inner ring (8 samples at half radius to prevent gaps with large width)
                static const float2 innerDirs[8] = {
                    float2( 0.500,  0.000),
                    float2( 0.354,  0.354),
                    float2( 0.000,  0.500),
                    float2(-0.354,  0.354),
                    float2(-0.500,  0.000),
                    float2(-0.354, -0.354),
                    float2( 0.000, -0.500),
                    float2( 0.354, -0.354)
                };

                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    float2 sampleUv = IN.texcoord + outerDirs[i] * stepSize;
                    half4 sCol = tex2D(_MainTex, sampleUv);
                    maxA = max(maxA, sCol.a);
                }

                [unroll]
                for (int j = 0; j < 8; j++)
                {
                    float2 sampleUv = IN.texcoord + innerDirs[j] * stepSize;
                    half4 sCol = tex2D(_MainTex, sampleUv);
                    maxA = max(maxA, sCol.a);
                }

                if (maxA > _AlphaThreshold)
                {
                    fixed4 outCol = _OutlineColor;
                    outCol.a *= smoothstep(_AlphaThreshold, 0.8, maxA);

                    #ifdef UNITY_UI_CLIP_RECT
                    outCol.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    #endif

                    #ifdef UNITY_UI_ALPHACLIP
                    clip(outCol.a - 0.001);
                    #endif

                    return outCol;
                }

                discard;
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}

