Shader "Custom/SniperScopeCircular"
{
    Properties
    {
        _MainTex ("Scene Texture", 2D) = "white" {}
        _Crosshair ("Crosshair", 2D) = "white" {}
        _Glare ("Glare", 2D) = "white" {}
        _ShiftAmount ("Depth Shift", float) = 5
        _ScopeRadius ("Scope Radius", Range(0,1)) = 0.4
        _EdgeSoftness ("Edge Softness", Range(0,1)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 centerShift : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _Crosshair;
            sampler2D _Glare;
            float4 _MainTex_ST;
            float _ShiftAmount;
            float _ScopeRadius;
            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.centerShift = float4(UnityObjectToViewPos(v.vertex), 1);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                // Máscara circular com alpha suave
                float alphaMask = smoothstep(1.0 - _ScopeRadius, 1.0 - (_ScopeRadius - _EdgeSoftness), dist);

                // Inverte: centro = 1, borda = 0
                alphaMask = saturate(1.0 - alphaMask);

                // deslocamento com profundidade
                float2 shiftedUv = i.uv + float2(i.centerShift.x * _ShiftAmount, i.centerShift.y * _ShiftAmount);
                shiftedUv = clamp(shiftedUv, 0.0, 1.0);

                // texturas
                fixed4 scene = tex2D(_MainTex, shiftedUv);
                fixed4 crosshair = tex2D(_Crosshair, i.uv);
                fixed4 glare = tex2D(_Glare, i.uv);

                // composição
                float4 finalColor = scene * (1 - crosshair.a) + crosshair * crosshair.a + glare * glare.a;

                // aplica máscara circular com transparência
                finalColor.rgb *= alphaMask;
                finalColor.a = alphaMask;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}