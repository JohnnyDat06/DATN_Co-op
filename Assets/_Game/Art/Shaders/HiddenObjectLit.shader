Shader "Custom/MagicMirror/HiddenObjectLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry+2" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True" // Bỏ qua các hiệu ứng chiếu bóng khác
        }

        // TẮT ĐỔ BÓNG TẠI ĐÂY
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            sampler2D _BaseMap;
            float4 _BaseColor;

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                half4 texColor = tex2D(_BaseMap, input.uv) * _BaseColor;
                
                Light mainLight = GetMainLight();
                half3 normal = normalize(input.normalWS);
                half dotNL = saturate(dot(normal, mainLight.direction));
                
                half3 diffuse = texColor.rgb * mainLight.color * dotNL;
                half3 ambient = texColor.rgb * 0.4; // Tăng ambient lên một chút để vật thể rõ hơn

                return half4(diffuse + ambient, texColor.a);
            }
            ENDHLSL
        }
    }
    // KHÔNG CÓ FALLBACK -> Unity sẽ không tìm được ShadowCaster pass mặc định của Lit
    Fallback Off 
}
