Shader "Unlit/WavyMaskShader"
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
                float3 uv : TEXCOORD0;
                float2 screen_pos : TEXCOORD2;
                fixed4 color : COLOR;
            };

            sampler2D _SpritesTexture;

            float2 _DisplaySize;
            float4 _Clip;
            float4 _GlobalTint;

            /*** Insert custom shader properties here ***/
            sampler2D Mask;
            float Wave;

            frag_in vert(vert_in i)
            {
                frag_in o;
                o.uv = float3(i.uv, i.vertex.z);
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
                float solid_color_flag = 1 - i.uv.z;

                float4 sprite_pixel_color = (tex2D(_SpritesTexture, i.uv) * (1 - solid_color_flag)) + (float4)solid_color_flag;

                // Perform clip test on the pixel
                sprite_pixel_color.a *= clip_test(i.screen_pos.xy, _Clip.xy, _Clip.zw);

                // Multiply in vertex alpha and current global alpha setting
                sprite_pixel_color *= i.color;
                sprite_pixel_color.a *= _GlobalTint;

                /*** Insert custom fragment shader code here ***/

                // Sample the mask texture
                i.uv.x += sin(Wave + i.uv.y * 8) * 0.025;
                i.uv.y += cos(Wave - i.uv.x * 8) * 0.015;
                float4 mask_color = tex2D(Mask, i.uv).rgba;

                // Multiply the sprite pixel by mask color
                return sprite_pixel_color * mask_color;
            }
            ENDCG
        }
    }
}
