Shader "Satellite Defense/UI/Settings Icon Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowRadius ("Glow Radius", Range(0,0.04)) = 0.026
        _GlowStrength ("Glow Strength", Range(0,2)) = 1.8
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
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _GlowRadius, _GlowStrength;
            v2f vert(appdata v)
            {
                v2f o;
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            // Brightness becomes coverage: the generated black contour stays transparent.
            half coverage(float2 uv)
            {
                fixed4 texel = tex2D(_MainTex, uv);
                return texel.a * dot(texel.rgb, half3(0.299, 0.587, 0.114));
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half core = coverage(i.uv);
                half halo = 0;
                [unroll] for (int ring = 1; ring <= 3; ring++)
                {
                    float radius = _GlowRadius * ring / 3;
                    half weight = (4 - ring) / 48.0;
                    halo += weight * (coverage(i.uv + float2(radius,0)) + coverage(i.uv - float2(radius,0))
                        + coverage(i.uv + float2(0,radius)) + coverage(i.uv - float2(0,radius))
                        + coverage(i.uv + float2(radius,radius) * 0.7071) + coverage(i.uv - float2(radius,radius) * 0.7071)
                        + coverage(i.uv + float2(radius,-radius) * 0.7071) + coverage(i.uv - float2(radius,-radius) * 0.7071));
                }
                fixed4 color = fixed4(i.color.rgb, saturate(core + halo * _GlowStrength) * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
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
