Shader "Unlit/GridOverlayShader"
{
    Properties
    {
        _GridSize ("Grid Size", Float) = 1
        _LineColor ("Line Color", Color) = (1,1,1,0.25)
        _Thickness ("Thickness", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _GridSize;
            float4 _LineColor;
            float _Thickness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 grid = abs(frac(i.worldPos.xy / _GridSize));
                float2 gridMask  = step(grid, _Thickness);
                float mask = max(gridMask .x, gridMask .y);
                return float4(_LineColor.rgb, _LineColor.a * mask);
            }
            ENDCG
        }
    }
}
