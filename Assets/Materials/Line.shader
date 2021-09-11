Shader "Unlit/line"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0.2,0.2,0.2,1)

            // Add these to avoid warning
            _StencilComp("Stencil Comparison", Float) = 8
            _Stencil("Stencil ID", Float) = 0
            _StencilOp("Stencil Operation", Float) = 0
            _StencilWriteMask("Stencil Write Mask", Float) = 255
            _StencilReadMask("Stencil Read Mask", Float) = 255
            _ColorMask("Color Mask", Float) = 15

    }

        SubShader
        {
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 100
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil{

            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert alpha
                #pragma fragment frag alpha
                // make fog work
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
                };

                float4 _Color;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    //fixed4 col = tex2D(_MainTex, i.uv); // This is not working on 2d graphic. IDK why.
                    fixed y = sin(i.uv.g + 2*abs(i.uv.r - 0.5)-_Time*200) + 0.3 ;
                y = clamp(y, 0, 1);
                    fixed a = _Color.a + y;
                    return  fixed4(lerp(fixed3(1, 1, 1) ,_Color.rgb,y), clamp(a,0,1));
                }
                ENDCG
            }
        }
}
