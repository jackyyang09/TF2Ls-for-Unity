// Alternative version of an existing TF2 shading implementation that applies tints to .tga textures that have an alpha mask
// Allows for adding "paint" to hats that support the feature
// $blendtintbybasealpha is always assumed to be 1 for performance purposes. Do use the original shader for unpainted items
// Original: https://forum.unity.com/threads/team-fortress-2-toon-shader-in-unity-free-version.93194/
Shader "Toon/Team Fortress 2 - Painted" {
    Properties{
        _Paint("Paint", Color) = (1, 1, 1, 1)
        _RimColor("Rim Color", Color) = (0.97,0.88,1,0.75)
        _RimPower("Rim Power", Float) = 2.5
        _MainTex("Diffuse (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap("Normal (Normal)", 2D) = "bump" {}
        _SpecularTex("Specular Level (R) Gloss (G) Rim Mask (B)", 2D) = "gray" {}
        _RampTex("Toon Ramp (RGB)", 2D) = "white" {}
        [Toggle] _Emission("Use Emission", Int) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _BlendTintColorOverBase("Blend Tint Over Base", Range(0, 1)) = 1
        _Cutoff("Alphatest Cutoff", Range(0, 1)) = 0
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }

            CGPROGRAM
                #pragma surface surf TF2 alphatest:_Cutoff
                #pragma target 3.0

            struct Input
            {
                float2 uv_MainTex;
                float3 worldNormal;
                INTERNAL_DATA
            };

            sampler2D _MainTex, _SpecularTex, _BumpMap, _RampTex;
            float4 _RimColor;
            float _RimPower;
            int _Emission;
            float4 _EmissionColor;
            float3 _Paint;
            float _BlendTintColorOverBase;

            inline fixed4 LightingTF2(SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
            {
                fixed3 h = normalize(lightDir + viewDir);

                fixed NdotL = dot(s.Normal, lightDir) * 0.5 + 0.5;
                fixed d = NdotL * atten;
                fixed3 ramp = tex2D(_RampTex, float2(d,d)).rgb;

                float nh = max(0, dot(s.Normal, h));
                float spec = pow(nh, s.Gloss * 128) * s.Specular;

                fixed4 c;
                c.rgb = ((s.Albedo * ramp * _LightColor0.rgb + _LightColor0.rgb * spec) * (atten * 2));
                c.a = s.Alpha;
                return c;
            }

            void surf(Input IN, inout SurfaceOutput o)
            {
                // Apply a mask onto a solid fill
                float3 maskedTint = tex2D(_MainTex, IN.uv_MainTex).a * _Paint.rgb ;
                // If $blendtintcoloroverbase is 1, also multiply the tint with the original map
                float3 tintedBase = lerp (tex2D(_MainTex, IN.uv_MainTex).rgb, float3(1, 1, 1), _BlendTintColorOverBase);
                // Blend the original map with an inverted version of the alpha mask
                float3 invertedMaskedAlbedo = (1.0 - tex2D(_MainTex, IN.uv_MainTex).a) * tex2D(_MainTex, IN.uv_MainTex).rgb;

                // Combine all properties
                o.Albedo = maskedTint * tintedBase + invertedMaskedAlbedo;
                o.Alpha = tex2D(_MainTex, IN.uv_MainTex).a;
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));

                float3 specGloss = tex2D(_SpecularTex, IN.uv_MainTex).rgb;
                o.Specular = specGloss.r;
                o.Gloss = specGloss.g;

                half3 rim = pow(max(0, dot(float3(0, 1, 0), WorldNormalVector(IN, o.Normal))), _RimPower) * _RimColor.rgb * _RimColor.a * specGloss.b;
                o.Emission = rim + _Emission * _EmissionColor;
            }
            ENDCG
        }
            Fallback "Transparent/Cutout/Bumped Specular"
}