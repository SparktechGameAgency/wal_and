// AreaForge/PixelateImage.shader
// Designed for UnityEngine.UI.Image (Canvas / UI system).
// Includes full stencil support — works correctly inside Mask and ScrollRect.
// Built-in Render Pipeline. For URP, replace Tags and Pass with a URP-compatible block.

Shader "AreaForge/PixelateImage"
{
    Properties
    {
        // ── Required by Unity UI system ───────────────────────────────────────
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ── UI Mask stencil (do not rename — Unity UI writes these) ───────────
        _StencilComp  ("Stencil Comparison", Float) = 8
        _Stencil      ("Stencil ID",         Float) = 0
        _StencilOp    ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask    ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ── Pixelation ────────────────────────────────────────────────────────
        [Header(Pixelation)]
        _PixelSize   ("Pixel Block Size",    Range(1, 256)) = 8
        _PixelScaleX ("Horizontal Scale",    Range(0.1, 4)) = 1.0
        _PixelScaleY ("Vertical Scale",      Range(0.1, 4)) = 1.0

        // Rect dimensions in pixels — set by ImagePixelator.cs at runtime.
        // Keeps pixel block size in screen-pixel units rather than texel units.
        _RectSize    ("Rect Size (px)",      Vector) = (256, 256, 0, 0)

        // ── Colour Depth ──────────────────────────────────────────────────────
        [Header(Colour Depth)]
        [Toggle] _EnableColorDepth ("Crush Colour Depth", Float) = 0
        _ColorDepth ("Colours Per Channel",  Range(2, 256)) = 8

        // ── Pixel Outline ─────────────────────────────────────────────────────
        [Header(Pixel Outline)]
        [Toggle] _EnableOutline  ("Enable Outline", Float) = 0
        _OutlineColor            ("Outline Colour",  Color) = (0,0,0,1)
        _OutlineThickness        ("Outline Thickness", Range(1, 8)) = 1
        _OutlineThreshold        ("Edge Threshold",   Range(0.01, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // ── Stencil (Unity UI masking) ─────────────────────────────────────────
        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIPixelate"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // ── Uniforms ──────────────────────────────────────────────────────
            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;   // atlas texel size (1/atlasW, 1/atlasH, atlasW, atlasH)

            fixed4    _Color;
            fixed4    _TextureSampleAdd;    // used by UI system for font rendering
            float4    _ClipRect;            // rect masking from CanvasGroup

            // Rect dimensions set by C# — more reliable than TexelSize for atlased UI
            float4    _RectSize;            // (width, height, 0, 0) in pixels

            float     _PixelSize;
            float     _PixelScaleX;
            float     _PixelScaleY;

            float     _EnableColorDepth;
            float     _ColorDepth;

            float     _EnableOutline;
            fixed4    _OutlineColor;
            float     _OutlineThickness;
            float     _OutlineThreshold;

            // ── Vertex structs ────────────────────────────────────────────────
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 texcoord    : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;   // needed for UNITY_UI_CLIP_RECT
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex shader ─────────────────────────────────────────────────
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPos   = IN.vertex;
                OUT.vertex     = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord   = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color      = IN.color * _Color;
                return OUT;
            }

            // ── Helpers ───────────────────────────────────────────────────────

            // Snap UV to pixel grid based on RectTransform pixel dimensions.
            // This means _PixelSize = 8 → each block is 8 screen pixels wide/tall.
            float2 PixelateUV(float2 uv)
            {
                float blocksX = _RectSize.x / max(1.0, _PixelSize * _PixelScaleX);
                float blocksY = _RectSize.y / max(1.0, _PixelSize * _PixelScaleY);

                return float2(
                    floor(uv.x * blocksX) / blocksX,
                    floor(uv.y * blocksY) / blocksY
                );
            }

            // Quantise each RGB channel to N discrete levels.
            fixed4 CrushColorDepth(fixed4 col)
            {
                float steps = max(1.0, _ColorDepth - 1.0);
                col.r = floor(col.r * steps + 0.5) / steps;
                col.g = floor(col.g * steps + 0.5) / steps;
                col.b = floor(col.b * steps + 0.5) / steps;
                return col;
            }

            // Sample a neighbour UV offset by N pixel-blocks in UV space.
            float SampleNeighbourAlpha(float2 uv, float dx, float dy)
            {
                float stepX = (_PixelSize * _PixelScaleX * _OutlineThickness) / max(1.0, _RectSize.x);
                float stepY = (_PixelSize * _PixelScaleY * _OutlineThickness) / max(1.0, _RectSize.y);
                return tex2D(_MainTex, uv + float2(dx * stepX, dy * stepY)).a;
            }

            // Draw an outline around transparent edge pixels.
            fixed4 ApplyOutline(fixed4 col, float2 uv)
            {
                if (col.a > _OutlineThreshold) return col;

                float n = SampleNeighbourAlpha(uv,  0,  1);
                float s = SampleNeighbourAlpha(uv,  0, -1);
                float e = SampleNeighbourAlpha(uv,  1,  0);
                float w = SampleNeighbourAlpha(uv, -1,  0);

                if (max(max(n, s), max(e, w)) > _OutlineThreshold)
                    return fixed4(_OutlineColor.rgb, _OutlineColor.a);

                return col;
            }

            // ── Fragment shader ───────────────────────────────────────────────
            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Snap UV to pixel grid
                float2 pixUV = PixelateUV(IN.texcoord);

                // 2. Sample texture (+ UI system font add)
                fixed4 col = (tex2D(_MainTex, pixUV) + _TextureSampleAdd) * IN.color;

                // 3. Crush colour depth (optional)
                if (_EnableColorDepth > 0.5)
                    col = CrushColorDepth(col);

                // 4. Outline (optional)
                if (_EnableOutline > 0.5)
                    col = ApplyOutline(col, pixUV);

                // 5. UI rect clipping (required for CanvasGroup / Mask)
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
                #endif

                // 6. Alpha clip (required for Mask component)
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
