// Draws the low-resolution image on screen, pulling each pixel towards the nearest zone palette color.
// Used by ZonePaletteFilter on the PixelRenderer output (a UI RawImage).
Shader "Moonbrew/PaletteFilter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Must match ZonePalette.MaxColors.
            #define MAX_COLORS 32

            sampler2D _MainTex;
            float4 _PaletteColors[MAX_COLORS];
            int _PaletteCount;
            float _PaletteStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            // Oklab: distances there match how different two colors look, unlike raw RGB.
            float3 LinearToOklab(float3 c)
            {
                float l = 0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b;
                float m = 0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b;
                float s = 0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b;
                l = pow(max(l, 0.0), 1.0 / 3.0);
                m = pow(max(m, 0.0), 1.0 / 3.0);
                s = pow(max(s, 0.0), 1.0 / 3.0);
                return float3(
                    0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
                    1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
                    0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 source = tex2D(_MainTex, i.uv) * i.color;
                float3 lab = LinearToOklab(source.rgb);
                float3 nearest = source.rgb;
                float best = 1e9;

                for (int k = 0; k < MAX_COLORS; k++)
                {
                    if (k >= _PaletteCount)
                    {
                        break;
                    }

                    float3 candidate = _PaletteColors[k].rgb;
                    float3 delta = LinearToOklab(candidate) - lab;
                    float distance = dot(delta, delta);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = candidate;
                    }
                }

                return float4(lerp(source.rgb, nearest, _PaletteStrength), source.a);
            }
            ENDCG
        }
    }
}
