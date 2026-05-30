Shader "QerlkKeeper/NeonOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0, 0.8, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _OutlineColor;
            float  _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                // Expansión en view space — evita huecos en esquinas de meshes con normales hard
                float4 viewPos    = mul(UNITY_MATRIX_MV, v.vertex);
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                viewPos.xy       += viewNormal.xy * _OutlineWidth;
                o.pos             = mul(UNITY_MATRIX_P, viewPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    Fallback Off
}
