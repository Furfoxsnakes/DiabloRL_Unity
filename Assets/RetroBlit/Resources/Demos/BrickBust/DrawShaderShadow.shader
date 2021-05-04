Shader "Unlit/DrawShaderShadow"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        Ztest never
        Zwrite off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Lowest target for greatest cross-platform compatiblilty
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 flags : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct frag_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 data : TEXCOORD1;
                float2 screen_pos : TEXCOORD2;
                fixed4 color : COLOR;
            };

            sampler2D _SpritesTexture;
            float4 _ShadowColor;

            float2 _DisplaySize;
            float4 _Clip;
            float4 _GlobalTint;

            /*** Insert custom shader properties here ***/

            frag_in vert(vert_in i)
            {
                frag_in o;
                o.uv = i.uv;
                o.data = float3(i.vertex.z, i.flags.x, i.flags.y);
                o.color = i.color;

                // Get onscreen position of the vertex
                o.vertex = UnityObjectToClipPos(float4(i.vertex.xy, 0, 1));
                o.screen_pos = ComputeScreenPos(o.vertex) * float4(_DisplaySize.xy, 1, 1);

                /*** Insert custom vertex shader code here ***/

                return o;
            }

            // Performs test against the clipping region
            float clip_test(float2 p, float2 bottom_left, float2 top_right)
            {
                float2 s = step(bottom_left, p) - step(top_right, p);
                return s.x * s.y;
            }

            float4 frag(frag_in i) : SV_Target
            {
                // 0 if we're drawing from a spritesheet texture, 1 if not
                int solid_color_flag = int(i.data.y);

                float4 sprite_pixel_color = (tex2D(_SpritesTexture, i.uv) * (1 - solid_color_flag)) + (float4)solid_color_flag;

                // Perform clip test on the pixel
                sprite_pixel_color.a *= clip_test(i.screen_pos.xy, _Clip.xy, _Clip.zw);

                // Multiply in vertex alpha and current global alpha setting
                sprite_pixel_color *= i.color;
                sprite_pixel_color.a *= _GlobalTint;

                /*** Insert custom fragment shader code here ***/

                sprite_pixel_color.rgb = _ShadowColor.rgb;
                sprite_pixel_color.a *= _ShadowColor.a;

                return sprite_pixel_color;
            }
            ENDCG
        }
    }
}
