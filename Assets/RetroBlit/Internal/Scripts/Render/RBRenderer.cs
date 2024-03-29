#if UNITY_2019_3_OR_NEWER
#define MESH_UPLOAD_2019_3
#endif

namespace RetroBlitInternal
{
    using System.Collections.Generic;
#if !MESH_UPLOAD_2019_3
    using UnityEngine;
#else
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;
#endif

    /// <summary>
    /// Renderer subsystem
    /// </summary>
    public sealed class RBRenderer
    {
        /// <summary>
        /// Magic number to quickly verify that this is a valid RetroBlit file
        /// </summary>
        public const ushort RetroBlit_SP_MAGIC = 0x05EF;

        /// <summary>
        /// Version
        /// </summary>
        public const ushort RetroBlit_SP_VERSION = 0x0001;

#if MESH_UPLOAD_2019_3
        /// <summary>
        /// Maximum quads in a single mesh
        /// </summary>
        public const int MAX_QUADS_PER_MESH = 4096;
#else
        /// <summary>
        /// Maximum quads in a single mesh
        /// </summary>
        public const int MAX_QUADS_PER_MESH = 512;
#endif
        /// <summary>
        /// Maximum indices in a single mesh
        /// </summary>
        public const int MAX_INDICES_PER_MESH = 6 * MAX_QUADS_PER_MESH;

        /// <summary>
        /// Maximum vertices in a single mesh
        /// </summary>
        public const int MAX_VERTEX_PER_MESH = 4 * MAX_QUADS_PER_MESH;

        /// <summary>
        /// Current sprite sheet
        /// </summary>
        public SpriteSheetAsset CurrentSpriteSheet;

        /// <summary>
        /// Current texture width
        /// </summary>
        public float CurrentSpriteSheetTextureWidth = 0;

        /// <summary>
        /// Current texture height
        /// </summary>
        public float CurrentSpriteSheetTextureHeight = 0;

        /// <summary>
        /// Current texel width
        /// </summary>
        public float CurrentSpriteSheetTexelWidth = 0;

        /// <summary>
        /// Current texel height
        /// </summary>
        public float CurrentSpriteSheetTexelHeight = 0;

        /// <summary>
        /// System texture
        /// </summary>
        public Texture2D SystemTexture;

        /// <summary>
        /// Used for DrawPixelBuffer only. Null if DrawPixelBuffer is never called
        /// </summary>
        public Texture2D PixelBufferTexture;

        /// <summary>
        /// Is rendering enabled? Rendering is enabled during <see cref="RB.IRetroBlitGame.Render"/> call
        /// </summary>
        public bool RenderEnabled = false;

        /// <summary>
        /// Empty sprite sheet placeholder
        /// </summary>
        public SpriteSheetAsset EmptySpriteSheet;

        private const int MAX_ELLIPSE_RADIUS = RBHardware.HW_MAX_POLY_POINTS / 2;

#if MESH_UPLOAD_2019_3
        private static VertexAttributeDescriptor[] mVertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UNorm16, 2)
        };
#endif

        private FrontBuffer mFrontBuffer = new FrontBuffer();
        private RenderTexture mCurrentRenderTexture = null;

        private MeshStorage mMeshStorage;

        private Material mCurrentDrawMaterial;
        private Material mDrawMaterialRGB;
        private Material mDrawMaterialClear;

        private SpriteSheetAsset mCurrentBatchSprite = null;

        private Texture mPreviousTexture;

        private ShaderAsset mCurrentShader = null;

        private ClipRegion mClipRegion;
        private Rect2i mClip;
        private bool mClipDebug;
        private Color32 mClipDebugColor;
        private List<DebugClipRegion> mDebugClipRegions = new List<DebugClipRegion>();

        private Vector2i mCameraPos;

        private RBAPI mRetroBlitAPI = null;

        private Color32 mCurrentColor = new Color32(255, 255, 255, 255);

        private Vector2i[] mPoints = new Vector2i[RBHardware.HW_MAX_POLY_POINTS];

        private bool mShowFlushDebug = false;
        private Color32 mFlushDebugFontColor = Color.white;
        private Color32 mFlushDebugBackgroundColor = Color.black;

        private int mPropIDGlobalTint;
        private int mPropIDSpritesTexture;
        private int mPropIDClip;
        private int mPropIDDisplaySize;

        private FlushInfo[] mFlushInfo = new FlushInfo[]
        {
            new FlushInfo() { Reason = "Batch Full", Count = 0 },
            new FlushInfo() { Reason = "Spritesheet Change", Count = 0 },
            new FlushInfo() { Reason = "Tilemap Chunk", Count = 0 },
            new FlushInfo() { Reason = "Frame End", Count = 0 },
            new FlushInfo() { Reason = "Clip Change", Count = 0 },
            new FlushInfo() { Reason = "Offscreen Change", Count = 0 },
            new FlushInfo() { Reason = "Present/Effect Apply", Count = 0 },
            new FlushInfo() { Reason = "Shader Apply", Count = 0 },
            new FlushInfo() { Reason = "Shader Reset", Count = 0 },
            new FlushInfo() { Reason = "Set Material", Count = 0 },
            new FlushInfo() { Reason = "Set Texture", Count = 0 },
            new FlushInfo() { Reason = "Draw Pixel Buffer", Count = 0 },
            new FlushInfo() { Reason = "Forced", Count = 0 },
        };

        /// <summary>
        /// Reasons for flushing to Mesh, each flush generates a Unity batch draw call
        /// </summary>
        public enum FlushReason
        {
            /// <summary>
            /// Flushed because batch is full
            /// </summary>
            BATCH_FULL,

            /// <summary>
            /// Flushed because spritesheet changed
            /// </summary>
            SPRITESHEET_CHANGE,

            /// <summary>
            /// Flushed because tilemap chunk was drawn
            /// </summary>
            TILEMAP_CHUNK,

            /// <summary>
            /// Flushed because frame ended
            /// </summary>
            FRAME_END,

            /// <summary>
            /// Flushed because clip region changed
            /// </summary>
            CLIP_CHANGE,

            /// <summary>
            /// Flushed because offscreen SpriteSheet target changed
            /// </summary>
            OFFSCREEN_CHANGE,

            /// <summary>
            /// Flushed because of an effect was applied
            /// </summary>
            EFFECT_APPLY,

            /// <summary>
            /// Flushed because shader changed
            /// </summary>
            SHADER_APPLY,

            /// <summary>
            /// Flushed because shader was reset
            /// </summary>
            SHADER_RESET,

            /// <summary>
            /// Flush because material changed
            /// </summary>
            SET_MATERIAL,

            /// <summary>
            /// Flushed because texture changed
            /// </summary>
            SET_TEXTURE,

            /// <summary>
            /// Flushed because pixel buffer was being copied
            /// </summary>
            PIXEL_BUF,

            /// <summary>
            /// Forced flush
            /// </summary>
            FORCED,
        }

        /// <summary>
        /// Initialize the subsystem
        /// </summary>
        /// <param name="api">Subsystem wrapper reference</param>
        /// <returns>True if successful</returns>
        public bool Initialize(RBAPI api)
        {
            mPropIDGlobalTint = Shader.PropertyToID("_GlobalTint");
            mPropIDSpritesTexture = Shader.PropertyToID("_SpritesTexture");
            mPropIDClip = Shader.PropertyToID("_Clip");
            mPropIDDisplaySize = Shader.PropertyToID("_DisplaySize");

            if (api == null)
            {
                return false;
            }

            EmptySpriteSheet = new EmptySpriteSheetAsset();
            CurrentSpriteSheet = EmptySpriteSheet;

            mRetroBlitAPI = api;

            mDrawMaterialRGB = mRetroBlitAPI.ResourceBucket.LoadMaterial("DrawMaterialRGB");
            if (mDrawMaterialRGB == null)
            {
                return false;
            }

            mDrawMaterialClear = mRetroBlitAPI.ResourceBucket.LoadMaterial("DrawMaterialClear");
            if (mDrawMaterialClear == null)
            {
                return false;
            }

            mMeshStorage = new MeshStorage();

            SetCurrentMaterial(mDrawMaterialRGB);

            if (!GenerateSystemTexture())
            {
                return false;
            }

            SetCurrentTexture(CurrentSpriteSheet.internalState.texture, CurrentSpriteSheet, true);

            return DisplayModeSet(RB.DisplaySize, mRetroBlitAPI.HW.PixelStyle);
        }

        /// <summary>
        /// Set display mode to given resolution and pixel style. Note that this sets only the RetroBlit pixel resolution, and does not affect the native
        /// window size. To change the native window size you can use the Unity Screen.SetResolution() API.
        /// </summary>
        /// <param name="resolution">Resolution</param>
        /// <param name="pixelStyle">Pixel style</param>
        /// <returns>True if mode was successfully set, false otherwise</returns>
        public bool DisplayModeSet(Vector2i resolution, RB.PixelStyle pixelStyle)
        {
            // Create main render target, and offscreen
            if (!mFrontBuffer.Resize(resolution, mRetroBlitAPI))
            {
                return false;
            }

            Onscreen();

            mRetroBlitAPI.HW.DisplaySize = resolution;
            mRetroBlitAPI.HW.PixelStyle = pixelStyle;

            return true;
        }

        /// <summary>
        /// Clear the display
        /// </summary>
        /// <param name="color">RGB color</param>
        public void Clear(Color32 color)
        {
            if (!RenderEnabled)
            {
                return;
            }

            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = mCurrentRenderTexture;
            GL.Clear(true, true, color);
            UnityEngine.RenderTexture.active = rt;

            // Drop whatever we may have been rendering before
            ResetMesh();
        }

        /// <summary>
        /// Clear a region of the render target. Useful for clearing spritesheets to alpha 0.
        /// </summary>
        /// <param name="color">RGB color</param>
        /// <param name="rect">Region to clear</param>
        public void ClearRect(Color32 color, Rect2i rect)
        {
            var prevMaterial = mCurrentDrawMaterial;
            SetCurrentMaterial(mDrawMaterialClear);

            DrawRectFill(rect.x, rect.y, rect.width, rect.height, color, 0, 0);

            SetCurrentMaterial(prevMaterial);
        }

        /// <summary>
        /// Draw a texture at given position
        /// </summary>
        /// <param name="srcX">Source x</param>
        /// <param name="srcY">Source y</param>
        /// <param name="srcWidth">Source width</param>
        /// <param name="srcHeight">Source height</param>
        /// <param name="destX">Destination x</param>
        /// <param name="destY">Destination y</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        public void DrawTexture(int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, int destWidth, int destHeight)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int sx0 = srcX;
            int sy0 = srcY;
            int sx1 = sx0 + srcWidth;
            int sy1 = sy0 + srcHeight;

            int dx0 = 0;
            int dy0 = 0;
            int dx1;
            int dy1;

            dx1 = dx0 + destWidth;
            dy1 = dy0 + destHeight;

#if MESH_UPLOAD_2019_3
            ushort ux0 = (ushort)(sx0 * CurrentSpriteSheetTexelWidth);
            ushort uy0 = (ushort)((CurrentSpriteSheetTextureHeight - sy0) * CurrentSpriteSheetTexelHeight);
            ushort ux1 = (ushort)(sx1 * CurrentSpriteSheetTexelWidth);
            ushort uy1 = (ushort)((CurrentSpriteSheetTextureHeight - sy1) * CurrentSpriteSheetTexelHeight);
#else
            float ux0 = sx0 / CurrentSpriteSheetTextureWidth;
            float uy0 = 1.0f - (sy0 / CurrentSpriteSheetTextureHeight);
            float ux1 = sx1 / CurrentSpriteSheetTextureWidth;
            float uy1 = 1.0f - (sy1 / CurrentSpriteSheetTextureHeight);
#endif

            Color32 color = mCurrentColor;

            dx0 -= mCameraPos.x - destX;
            dy0 -= mCameraPos.y - destY;

            dx1 -= mCameraPos.x - destX;
            dy1 -= mCameraPos.y - destY;

            // Early clip test
            if (dx1 < mClipRegion.x0 || dy1 < mClipRegion.y0 || dx0 > mClipRegion.x1 || dy0 > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 1;

            v.pos_x = dx0;
            v.pos_y = dy0;

            v.u = ux0;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx1;
            v.pos_y = dy0;

            v.u = ux1;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx1;
            v.pos_y = dy1;

            v.u = ux1;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx0;
            v.pos_y = dy1;

            v.u = ux0;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = dx0;
            mMeshStorage.Verticies[i + 0].y = dy0;
            mMeshStorage.Verticies[i + 0].z = 1;

            mMeshStorage.Verticies[i + 1].x = dx1;
            mMeshStorage.Verticies[i + 1].y = dy0;
            mMeshStorage.Verticies[i + 1].z = 1;

            mMeshStorage.Verticies[i + 2].x = dx1;
            mMeshStorage.Verticies[i + 2].y = dy1;
            mMeshStorage.Verticies[i + 2].z = 1;

            mMeshStorage.Verticies[i + 3].x = dx0;
            mMeshStorage.Verticies[i + 3].y = dy1;
            mMeshStorage.Verticies[i + 3].z = 1;

            mMeshStorage.Uvs[i + 0].x = ux0;
            mMeshStorage.Uvs[i + 0].y = uy0;
            mMeshStorage.Uvs[i + 1].x = ux1;
            mMeshStorage.Uvs[i + 1].y = uy0;
            mMeshStorage.Uvs[i + 2].x = ux1;
            mMeshStorage.Uvs[i + 2].y = uy1;
            mMeshStorage.Uvs[i + 3].x = ux0;
            mMeshStorage.Uvs[i + 3].y = uy1;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a texture at given position
        /// </summary>
        /// <param name="srcX">Source x</param>
        /// <param name="srcY">Source y</param>
        /// <param name="srcWidth">Source width</param>
        /// <param name="srcHeight">Source height</param>
        /// <param name="destX">Destination x</param>
        /// <param name="destY">Destination y</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        /// <param name="flags">Flags</param>
        public void DrawTexture(int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, int destWidth, int destHeight, int flags)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int sx0 = srcX;
            int sy0 = srcY;
            int sx1 = sx0 + srcWidth;
            int sy1 = sy0 + srcHeight;

            int dx0 = 0;
            int dy0 = 0;
            int dx1;
            int dy1;

            if ((flags & RB.ROT_90_CW) == 0)
            {
                dx1 = dx0 + destWidth;
                dy1 = dy0 + destHeight;
            }
            else
            {
                dx1 = dx0 + destHeight;
                dy1 = dy0 + destWidth;
            }

#if MESH_UPLOAD_2019_3
            ushort ux0, uy0, ux1, uy1;

            ushort ux0raw = (ushort)(sx0 * CurrentSpriteSheetTexelWidth);
            ushort uy0raw = (ushort)((CurrentSpriteSheetTextureHeight - sy0) * CurrentSpriteSheetTexelHeight);
            ushort ux1raw = (ushort)(sx1 * CurrentSpriteSheetTexelWidth);
            ushort uy1raw = (ushort)((CurrentSpriteSheetTextureHeight - sy1) * CurrentSpriteSheetTexelHeight);
#else
            float ux0, uy0, ux1, uy1;

            float ux0raw = sx0 / CurrentSpriteSheetTextureWidth;
            float uy0raw = 1.0f - (sy0 / CurrentSpriteSheetTextureHeight);
            float ux1raw = sx1 / CurrentSpriteSheetTextureWidth;
            float uy1raw = 1.0f - (sy1 / CurrentSpriteSheetTextureHeight);
#endif

            if ((flags & RB.FLIP_H) == 0)
            {
                ux0 = ux0raw;
                ux1 = ux1raw;
            }
            else
            {
                ux0 = ux1raw;
                ux1 = ux0raw;
            }

            if ((flags & RB.FLIP_V) == 0)
            {
                uy0 = uy0raw;
                uy1 = uy1raw;
            }
            else
            {
                uy0 = uy1raw;
                uy1 = uy0raw;
            }

            Color32 color = mCurrentColor;

            dx0 -= mCameraPos.x - destX;
            dy0 -= mCameraPos.y - destY;

            dx1 -= mCameraPos.x - destX;
            dy1 -= mCameraPos.y - destY;

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

            // Early clip test
            if (dx1 < mClipRegion.x0 || dy1 < mClipRegion.y0 || dx0 > mClipRegion.x1 || dy0 > mClipRegion.y1)
            {
                return;
            }

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 1;

            if ((flags & RB.ROT_90_CW) == 0)
            {
                v.pos_x = dx0;
                v.pos_y = dy0;

                v.u = ux0;
                v.v = uy0;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx1;
                v.pos_y = dy0;

                v.u = ux1;
                v.v = uy0;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx1;
                v.pos_y = dy1;

                v.u = ux1;
                v.v = uy1;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx0;
                v.pos_y = dy1;

                v.u = ux0;
                v.v = uy1;

                mMeshStorage.Verticies[i++] = v;
            }
            else
            {
                v.pos_x = dx1;
                v.pos_y = dy0;

                v.u = ux0;
                v.v = uy0;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx1;
                v.pos_y = dy1;

                v.u = ux1;
                v.v = uy0;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx0;
                v.pos_y = dy1;

                v.u = ux1;
                v.v = uy1;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = dx0;
                v.pos_y = dy0;

                v.u = ux0;
                v.v = uy1;

                mMeshStorage.Verticies[i++] = v;
            }

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            if ((flags & RB.ROT_90_CW) == 0)
            {
                mMeshStorage.Verticies[i + 0].x = dx0;
                mMeshStorage.Verticies[i + 0].y = dy0;
                mMeshStorage.Verticies[i + 0].z = 1;

                mMeshStorage.Verticies[i + 1].x = dx1;
                mMeshStorage.Verticies[i + 1].y = dy0;
                mMeshStorage.Verticies[i + 1].z = 1;

                mMeshStorage.Verticies[i + 2].x = dx1;
                mMeshStorage.Verticies[i + 2].y = dy1;
                mMeshStorage.Verticies[i + 2].z = 1;

                mMeshStorage.Verticies[i + 3].x = dx0;
                mMeshStorage.Verticies[i + 3].y = dy1;
                mMeshStorage.Verticies[i + 3].z = 1;
            }
            else
            {
                mMeshStorage.Verticies[i + 0].x = dx1;
                mMeshStorage.Verticies[i + 0].y = dy0;
                mMeshStorage.Verticies[i + 0].z = 1;

                mMeshStorage.Verticies[i + 1].x = dx1;
                mMeshStorage.Verticies[i + 1].y = dy1;
                mMeshStorage.Verticies[i + 1].z = 1;

                mMeshStorage.Verticies[i + 2].x = dx0;
                mMeshStorage.Verticies[i + 2].y = dy1;
                mMeshStorage.Verticies[i + 2].z = 1;

                mMeshStorage.Verticies[i + 3].x = dx0;
                mMeshStorage.Verticies[i + 3].y = dy0;
                mMeshStorage.Verticies[i + 3].z = 1;
            }

            mMeshStorage.Uvs[i + 0].x = ux0;
            mMeshStorage.Uvs[i + 0].y = uy0;
            mMeshStorage.Uvs[i + 1].x = ux1;
            mMeshStorage.Uvs[i + 1].y = uy0;
            mMeshStorage.Uvs[i + 2].x = ux1;
            mMeshStorage.Uvs[i + 2].y = uy1;
            mMeshStorage.Uvs[i + 3].x = ux0;
            mMeshStorage.Uvs[i + 3].y = uy1;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a texture at given position, rotation
        /// </summary>
        /// <param name="srcX">Source x</param>
        /// <param name="srcY">Source y</param>
        /// <param name="srcWidth">Source width</param>
        /// <param name="srcHeight">Source height</param>
        /// <param name="destX">Destination x</param>
        /// <param name="destY">Destination y</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        /// <param name="pivotX">Rotation pivot point x</param>
        /// <param name="pivotY">Rotation pivot point y</param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawTexture(int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, int destWidth, int destHeight, int pivotX, int pivotY, float rotation)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int sx0 = srcX;
            int sy0 = srcY;
            int sx1 = sx0 + srcWidth;
            int sy1 = sy0 + srcHeight;

            int dx0 = 0;
            int dy0 = 0;
            int dx1;
            int dy1;

            dx1 = dx0 + destWidth;
            dy1 = dy0 + destHeight;

            // Wrap the angle first to values between 0 and 360
            rotation = RBUtil.WrapAngle(rotation);

#if MESH_UPLOAD_2019_3
            ushort ux0, uy0, ux1, uy1;

            ushort ux0raw = (ushort)(sx0 * CurrentSpriteSheetTexelWidth);
            ushort uy0raw = (ushort)((CurrentSpriteSheetTextureHeight - sy0) * CurrentSpriteSheetTexelHeight);
            ushort ux1raw = (ushort)(sx1 * CurrentSpriteSheetTexelWidth);
            ushort uy1raw = (ushort)((CurrentSpriteSheetTextureHeight - sy1) * CurrentSpriteSheetTexelHeight);
#else
            float ux0, uy0, ux1, uy1;

            float ux0raw = sx0 / CurrentSpriteSheetTextureWidth;
            float uy0raw = 1.0f - (sy0 / CurrentSpriteSheetTextureHeight);
            float ux1raw = sx1 / CurrentSpriteSheetTextureWidth;
            float uy1raw = 1.0f - (sy1 / CurrentSpriteSheetTextureHeight);
#endif

            ux0 = ux0raw;
            ux1 = ux1raw;

            uy0 = uy0raw;
            uy1 = uy1raw;

            Color32 color = mCurrentColor;

            Vector3 p1, p2, p3, p4;

            p1 = new Vector3(dx0, dy0, 0);
            p2 = new Vector3(dx1, dy0, 0);
            p3 = new Vector3(dx1, dy1, 0);
            p4 = new Vector3(dx0, dy1, 0);

            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
            matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

            p1 = matrix.MultiplyPoint3x4(p1);
            p2 = matrix.MultiplyPoint3x4(p2);
            p3 = matrix.MultiplyPoint3x4(p3);
            p4 = matrix.MultiplyPoint3x4(p4);

            p1.x += pivotX;
            p1.y += pivotY;

            p2.x += pivotX;
            p2.y += pivotY;

            p3.x += pivotX;
            p3.y += pivotY;

            p4.x += pivotX;
            p4.y += pivotY;

            p1.x -= mCameraPos.x - destX;
            p1.y -= mCameraPos.y - destY;

            p2.x -= mCameraPos.x - destX;
            p2.y -= mCameraPos.y - destY;

            p3.x -= mCameraPos.x - destX;
            p3.y -= mCameraPos.y - destY;

            p4.x -= mCameraPos.x - destX;
            p4.y -= mCameraPos.y - destY;

            // Early clip test
            if (p1.x < mClipRegion.x0 && p2.x < mClipRegion.x0 && p3.x < mClipRegion.x0 && p4.x < mClipRegion.x0)
            {
                return;
            }
            else if (p1.x > mClipRegion.x1 && p2.x > mClipRegion.x1 && p3.x > mClipRegion.x1 && p4.x > mClipRegion.x1)
            {
                return;
            }
            else if (p1.y < mClipRegion.y0 && p2.y < mClipRegion.y0 && p3.y < mClipRegion.y0 && p4.y < mClipRegion.y0)
            {
                // Note that Y axis is inverted by this point, have to invert it back before checking against clip
                return;
            }
            else if (p1.y > mClipRegion.y1 && p2.y > mClipRegion.y1 && p3.y > mClipRegion.y1 && p4.y > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 1;

            v.pos_x = p1.x;
            v.pos_y = p1.y;

            v.u = ux0;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2.x;
            v.pos_y = p2.y;

            v.u = ux1;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3.x;
            v.pos_y = p3.y;

            v.u = ux1;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p4.x;
            v.pos_y = p4.y;

            v.u = ux0;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            p1.z = p2.z = p3.z = p4.z = 1;

            mMeshStorage.Verticies[i + 0] = p1;
            mMeshStorage.Verticies[i + 1] = p2;
            mMeshStorage.Verticies[i + 2] = p3;
            mMeshStorage.Verticies[i + 3] = p4;

            mMeshStorage.Uvs[i + 0].x = ux0;
            mMeshStorage.Uvs[i + 0].y = uy0;
            mMeshStorage.Uvs[i + 1].x = ux1;
            mMeshStorage.Uvs[i + 1].y = uy0;
            mMeshStorage.Uvs[i + 2].x = ux1;
            mMeshStorage.Uvs[i + 2].y = uy1;
            mMeshStorage.Uvs[i + 3].x = ux0;
            mMeshStorage.Uvs[i + 3].y = uy1;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a texture at given position, rotation
        /// </summary>
        /// <param name="srcX">Source x</param>
        /// <param name="srcY">Source y</param>
        /// <param name="srcWidth">Source width</param>
        /// <param name="srcHeight">Source height</param>
        /// <param name="destX">Destination x</param>
        /// <param name="destY">Destination y</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        /// <param name="pivotX">Rotation pivot point x</param>
        /// <param name="pivotY">Rotation pivot point y</param>
        /// <param name="rotation">Rotation in degrees</param>
        /// <param name="flags">Flags</param>
        public void DrawTexture(int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, int destWidth, int destHeight, int pivotX, int pivotY, float rotation, int flags)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int sx0 = srcX;
            int sy0 = srcY;
            int sx1 = sx0 + srcWidth;
            int sy1 = sy0 + srcHeight;

            int dx0 = 0;
            int dy0 = 0;
            int dx1;
            int dy1;

            if ((flags & RB.ROT_90_CW) == 0)
            {
                dx1 = dx0 + destWidth;
                dy1 = dy0 + destHeight;
            }
            else
            {
                dx1 = dx0 + destHeight;
                dy1 = dy0 + destWidth;
            }

            // Wrap the angle first to values between 0 and 360
            rotation = RBUtil.WrapAngle(rotation);

#if MESH_UPLOAD_2019_3
            ushort ux0, uy0, ux1, uy1;

            ushort ux0raw = (ushort)(sx0 * CurrentSpriteSheetTexelWidth);
            ushort uy0raw = (ushort)((CurrentSpriteSheetTextureHeight - sy0) * CurrentSpriteSheetTexelHeight);
            ushort ux1raw = (ushort)(sx1 * CurrentSpriteSheetTexelWidth);
            ushort uy1raw = (ushort)((CurrentSpriteSheetTextureHeight - sy1) * CurrentSpriteSheetTexelHeight);
#else
            float ux0, uy0, ux1, uy1;

            float ux0raw = sx0 / CurrentSpriteSheetTextureWidth;
            float uy0raw = 1.0f - (sy0 / CurrentSpriteSheetTextureHeight);
            float ux1raw = sx1 / CurrentSpriteSheetTextureWidth;
            float uy1raw = 1.0f - (sy1 / CurrentSpriteSheetTextureHeight);
#endif

            if ((flags & RB.FLIP_H) == 0)
            {
                ux0 = ux0raw;
                ux1 = ux1raw;
            }
            else
            {
                ux0 = ux1raw;
                ux1 = ux0raw;
            }

            if ((flags & RB.FLIP_V) == 0)
            {
                uy0 = uy0raw;
                uy1 = uy1raw;
            }
            else
            {
                uy0 = uy1raw;
                uy1 = uy0raw;
            }

            Color32 color = mCurrentColor;

            Vector3 p1, p2, p3, p4;

            if ((flags & RB.ROT_90_CW) == 0)
            {
                p1 = new Vector3(dx0, dy0, 0);
                p2 = new Vector3(dx1, dy0, 0);
                p3 = new Vector3(dx1, dy1, 0);
                p4 = new Vector3(dx0, dy1, 0);
            }
            else
            {
                p1 = new Vector3(dx1, dy0, 0);
                p2 = new Vector3(dx1, dy1, 0);
                p3 = new Vector3(dx0, dy1, 0);
                p4 = new Vector3(dx0, dy0, 0);
            }

            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
            matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

            p1 = matrix.MultiplyPoint3x4(p1);
            p2 = matrix.MultiplyPoint3x4(p2);
            p3 = matrix.MultiplyPoint3x4(p3);
            p4 = matrix.MultiplyPoint3x4(p4);

            p1.x += pivotX;
            p1.y += pivotY;

            p2.x += pivotX;
            p2.y += pivotY;

            p3.x += pivotX;
            p3.y += pivotY;

            p4.x += pivotX;
            p4.y += pivotY;

            p1.x -= mCameraPos.x - destX;
            p1.y -= mCameraPos.y - destY;

            p2.x -= mCameraPos.x - destX;
            p2.y -= mCameraPos.y - destY;

            p3.x -= mCameraPos.x - destX;
            p3.y -= mCameraPos.y - destY;

            p4.x -= mCameraPos.x - destX;
            p4.y -= mCameraPos.y - destY;

            // Early clip test
            if (p1.x < mClipRegion.x0 && p2.x < mClipRegion.x0 && p3.x < mClipRegion.x0 && p4.x < mClipRegion.x0)
            {
                return;
            }
            else if (p1.x > mClipRegion.x1 && p2.x > mClipRegion.x1 && p3.x > mClipRegion.x1 && p4.x > mClipRegion.x1)
            {
                return;
            }
            else if (p1.y < mClipRegion.y0 && p2.y < mClipRegion.y0 && p3.y < mClipRegion.y0 && p4.y < mClipRegion.y0)
            {
                // Note that Y axis is inverted by this point, have to invert it back before checking against clip
                return;
            }
            else if (p1.y > mClipRegion.y1 && p2.y > mClipRegion.y1 && p3.y > mClipRegion.y1 && p4.y > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 1;

            v.pos_x = p1.x;
            v.pos_y = p1.y;

            v.u = ux0;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2.x;
            v.pos_y = p2.y;

            v.u = ux1;
            v.v = uy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3.x;
            v.pos_y = p3.y;

            v.u = ux1;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p4.x;
            v.pos_y = p4.y;

            v.u = ux0;
            v.v = uy1;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            p1.z = p2.z = p3.z = p4.z = 1;

            mMeshStorage.Verticies[i + 0] = p1;
            mMeshStorage.Verticies[i + 1] = p2;
            mMeshStorage.Verticies[i + 2] = p3;
            mMeshStorage.Verticies[i + 3] = p4;

            mMeshStorage.Uvs[i + 0].x = ux0;
            mMeshStorage.Uvs[i + 0].y = uy0;
            mMeshStorage.Uvs[i + 1].x = ux1;
            mMeshStorage.Uvs[i + 1].y = uy0;
            mMeshStorage.Uvs[i + 2].x = ux1;
            mMeshStorage.Uvs[i + 2].y = uy1;
            mMeshStorage.Uvs[i + 3].x = ux0;
            mMeshStorage.Uvs[i + 3].y = uy1;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw nine-slice sprite.
        /// </summary>
        /// <param name="destRect">Destination rectangle</param>
        /// <param name="srcTopLeftCorner">Source rectangle of the top left corner</param>
        /// <param name="flagsTopLeftCorner">Render flags for top left corner</param>
        /// <param name="srcTopSide">Source rectangle of the top side</param>
        /// <param name="flagsTopSide">Render flags for top side</param>
        /// <param name="srcTopRightCorner">Source rectangle of the top right corner</param>
        /// <param name="flagsTopRightCorner">Render flaps for top right corner</param>
        /// <param name="srcLeftSide">Source rectangle of the left side</param>
        /// <param name="flagsLeftSide">Render flags for left side</param>
        /// <param name="srcMiddle">Source rectangle of the middle</param>
        /// <param name="srcRightSide">Render flags for right side</param>
        /// <param name="flagsRightSide">Source rectangle of the right side</param>
        /// <param name="srcBottomLeftCorner">Render flags for bottom left corner</param>
        /// <param name="flagsBottomLeftCorner">Source rectangle of the bottom left corner</param>
        /// <param name="srcBottomSide">Render flags for bottom side</param>
        /// <param name="flagsBottomSide">Source rectangle of the bottom side</param>
        /// <param name="srcBottomRightCorner">Render flags for bottom right corner</param>
        /// <param name="flagsBottomRightCorner">Source rectangle of the bottom right corner</param>
        public void DrawNineSlice(
            Rect2i destRect,
            Rect2i srcTopLeftCorner,
            int flagsTopLeftCorner,
            Rect2i srcTopSide,
            int flagsTopSide,
            Rect2i srcTopRightCorner,
            int flagsTopRightCorner,
            Rect2i srcLeftSide,
            int flagsLeftSide,
            Rect2i srcMiddle,
            Rect2i srcRightSide,
            int flagsRightSide,
            Rect2i srcBottomLeftCorner,
            int flagsBottomLeftCorner,
            Rect2i srcBottomSide,
            int flagsBottomSide,
            Rect2i srcBottomRightCorner,
            int flagsBottomRightCorner)
        {
            if (destRect.width < srcTopLeftCorner.width + srcBottomRightCorner.width ||
                destRect.height < srcTopLeftCorner.height + srcBottomRightCorner.height)
            {
                return;
            }

            int bottomOffset = destRect.height - srcBottomLeftCorner.height;
            int rightOffset = destRect.width - srcTopRightCorner.width;

            int xOffset = srcTopLeftCorner.width;
            while (xOffset < rightOffset && srcTopSide.width > 0)
            {
                int remainingWidth = rightOffset - xOffset;
                int width = Mathf.Min(remainingWidth, srcTopSide.width);

                // Top & Bottom horizontal
                DrawTexture(srcTopSide.x, srcTopSide.y, width, srcTopSide.height, destRect.x + xOffset, destRect.y, width, srcTopSide.height, flagsTopSide);
                DrawTexture(srcBottomSide.x, srcBottomSide.y, width, srcBottomSide.height, destRect.x + xOffset, destRect.y + bottomOffset, width, srcBottomSide.height, flagsBottomSide);

                xOffset += srcTopSide.width;
            }

            int yOffset = srcTopLeftCorner.height;
            while (yOffset < bottomOffset && srcLeftSide.height > 0)
            {
                int remainingHeight = bottomOffset - yOffset;
                int height = Mathf.Min(remainingHeight, srcLeftSide.height);

                // Left & Right verticals
                if ((flagsLeftSide & RB.ROT_90_CW) != 0)
                {
                    DrawTexture(srcLeftSide.x, srcLeftSide.y, height, srcLeftSide.height, destRect.x, destRect.y + yOffset, height, srcLeftSide.height, flagsLeftSide);
                    DrawTexture(srcRightSide.x, srcRightSide.y, height, srcRightSide.height, destRect.x + rightOffset, destRect.y + yOffset, height, srcRightSide.height, flagsRightSide);
                }
                else
                {
                    DrawTexture(srcLeftSide.x, srcLeftSide.y, srcLeftSide.width, height, destRect.x, destRect.y + yOffset, srcLeftSide.width, height, flagsLeftSide);
                    DrawTexture(srcRightSide.x, srcRightSide.y, srcRightSide.width, height, destRect.x + rightOffset, destRect.y + yOffset, srcRightSide.width, height, flagsRightSide);
                }

                yOffset += srcLeftSide.height;
            }

            yOffset = srcTopLeftCorner.height;
            while (yOffset < bottomOffset && srcMiddle.height > 0)
            {
                int remainingHeight = bottomOffset - yOffset;
                int height = Mathf.Min(remainingHeight, srcMiddle.height);

                xOffset = srcTopLeftCorner.width;
                while (xOffset < rightOffset)
                {
                    int remainingWidth = rightOffset - xOffset;
                    int width = Mathf.Min(remainingWidth, srcMiddle.width);

                    // Center
                    DrawTexture(srcMiddle.x, srcMiddle.y, width, height, destRect.x + xOffset, destRect.y + yOffset, width, height);

                    xOffset += srcMiddle.width;
                }

                yOffset += srcMiddle.height;
            }

            /* Top left corner */
            DrawTexture(srcTopLeftCorner.x, srcTopLeftCorner.y, srcTopLeftCorner.width, srcTopLeftCorner.height, destRect.x, destRect.y, srcTopLeftCorner.width, srcTopLeftCorner.height, flagsTopLeftCorner);

            /* Bottom left corner */
            DrawTexture(srcBottomLeftCorner.x, srcBottomLeftCorner.y, srcBottomLeftCorner.width, srcBottomLeftCorner.height, destRect.x, destRect.y + bottomOffset, srcBottomLeftCorner.width, srcBottomLeftCorner.height, flagsBottomLeftCorner);

            /* Top right corner */
            DrawTexture(srcTopRightCorner.x, srcTopRightCorner.y, srcTopRightCorner.width, srcTopRightCorner.height, destRect.x + rightOffset, destRect.y, srcTopRightCorner.width, srcTopRightCorner.height, flagsTopRightCorner);

            /* Bottom right corner */
            DrawTexture(srcBottomRightCorner.x, srcBottomRightCorner.y, srcBottomRightCorner.width, srcBottomRightCorner.height, destRect.x + rightOffset, destRect.y + bottomOffset, srcBottomRightCorner.width, srcBottomRightCorner.height, flagsBottomRightCorner);
        }

        /// <summary>
        /// Draw nine-slice sprite.
        /// </summary>
        /// <param name="destRect">Destination rectangle</param>
        /// <param name="srcTopLeftCornerID">Sprite ID of the top left corner</param>
        /// <param name="flagsTopLeftCorner">Render flags for top left corner</param>
        /// <param name="srcTopSideID">Sprite ID of the top side</param>
        /// <param name="flagsTopSide">Render flags for top side</param>
        /// <param name="srcTopRightCornerID">Sprite ID of the top right corner</param>
        /// <param name="flagsTopRightCorner">Render flaps for top right corner</param>
        /// <param name="srcLeftSideID">Sprite ID of the left side</param>
        /// <param name="flagsLeftSide">Render flags for left side</param>
        /// <param name="srcMiddleID">Sprite ID of the middle</param>
        /// <param name="srcRightSideID">Sprite ID for right side</param>
        /// <param name="flagsRightSide">Render flags for the right side</param>
        /// <param name="srcBottomLeftCornerID">Sprite ID for bottom left corner</param>
        /// <param name="flagsBottomLeftCorner">Render flags for bottom left corner</param>
        /// <param name="srcBottomSideID">Sprite ID for bottom side</param>
        /// <param name="flagsBottomSide">Render flags of bottom side</param>
        /// <param name="srcBottomRightCornerID">Sprite ID bottom right corner</param>
        /// <param name="flagsBottomRightCorner">Render flags of bottom right corner</param>
        public void DrawNineSlice(
            Rect2i destRect,
            PackedSpriteID srcTopLeftCornerID,
            int flagsTopLeftCorner,
            PackedSpriteID srcTopSideID,
            int flagsTopSide,
            PackedSpriteID srcTopRightCornerID,
            int flagsTopRightCorner,
            PackedSpriteID srcLeftSideID,
            int flagsLeftSide,
            PackedSpriteID srcMiddleID,
            PackedSpriteID srcRightSideID,
            int flagsRightSide,
            PackedSpriteID srcBottomLeftCornerID,
            int flagsBottomLeftCorner,
            PackedSpriteID srcBottomSideID,
            int flagsBottomSide,
            PackedSpriteID srcBottomRightCornerID,
            int flagsBottomRightCorner)
        {
            var srcTopLeftCorner = PackedSpriteGet(srcTopLeftCornerID.id);
            var srcTopSide = PackedSpriteGet(srcTopSideID.id);
            var srcTopRightCorner = PackedSpriteGet(srcTopRightCornerID.id);
            var srcLeftSide = PackedSpriteGet(srcLeftSideID.id);
            var srcMiddle = PackedSpriteGet(srcMiddleID.id);
            var srcRightSide = PackedSpriteGet(srcRightSideID.id);
            var srcBottomLeftCorner = PackedSpriteGet(srcBottomLeftCornerID.id);
            var srcBottomSide = PackedSpriteGet(srcBottomSideID.id);
            var srcBottomRightCorner = PackedSpriteGet(srcBottomRightCornerID.id);

            if (destRect.width < srcTopLeftCorner.Size.width + srcBottomRightCorner.Size.width ||
                destRect.height < srcTopLeftCorner.Size.height + srcBottomRightCorner.Size.height)
            {
                return;
            }

            int bottomOffset = destRect.height - srcBottomLeftCorner.Size.height;
            int rightOffset = destRect.width - srcTopRightCorner.Size.width;

            int xOffset = srcTopLeftCorner.Size.width;
            while (xOffset < rightOffset && srcTopSide.Size.width > 0)
            {
                int remainingWidth = rightOffset - xOffset;
                int width = Mathf.Min(remainingWidth, srcTopSide.Size.width);

                // Top & Bottom horizontal
                RB.DrawSprite(srcTopSide, new Rect2i(destRect.x + xOffset, destRect.y, width, srcTopSide.Size.height), flagsTopSide);
                RB.DrawSprite(srcBottomSide, new Rect2i(destRect.x + xOffset, destRect.y + bottomOffset, width, srcBottomSide.Size.height), flagsBottomSide);

                xOffset += srcTopSide.Size.width;
            }

            int yOffset = srcTopLeftCorner.Size.height;
            while (yOffset < bottomOffset && srcLeftSide.Size.height > 0)
            {
                int remainingHeight = bottomOffset - yOffset;
                int height = Mathf.Min(remainingHeight, srcLeftSide.Size.height);

                // Left & Right verticals
                if ((flagsLeftSide & RB.ROT_90_CW) != 0)
                {
                    RB.DrawSprite(srcLeftSide, new Rect2i(destRect.x, destRect.y + yOffset, height, srcLeftSide.Size.height), flagsLeftSide);
                    RB.DrawSprite(srcRightSide, new Rect2i(destRect.x + rightOffset, destRect.y + yOffset, height, srcRightSide.Size.height), flagsRightSide);
                }
                else
                {
                    RB.DrawSprite(srcLeftSide, new Rect2i(destRect.x, destRect.y + yOffset, srcLeftSide.Size.width, height), flagsLeftSide);
                    RB.DrawSprite(srcRightSide, new Rect2i(destRect.x + rightOffset, destRect.y + yOffset, srcRightSide.Size.width, height), flagsRightSide);
                }

                yOffset += srcLeftSide.Size.height;
            }

            yOffset = srcTopLeftCorner.Size.height;
            while (yOffset < bottomOffset && srcMiddle.Size.height > 0)
            {
                int remainingHeight = bottomOffset - yOffset;
                int height = Mathf.Min(remainingHeight, srcMiddle.Size.height);

                xOffset = srcTopLeftCorner.Size.width;
                while (xOffset < rightOffset)
                {
                    int remainingWidth = rightOffset - xOffset;
                    int width = Mathf.Min(remainingWidth, srcMiddle.Size.width);

                    // Center
                    RB.DrawSprite(srcMiddle, new Rect2i(destRect.x + xOffset, destRect.y + yOffset, width, height));

                    xOffset += srcMiddle.Size.width;
                }

                yOffset += srcMiddle.Size.height;
            }

            /* Top left corner */
            RB.DrawSprite(srcTopLeftCorner, new Rect2i(destRect.x, destRect.y, srcTopLeftCorner.Size.width, srcTopLeftCorner.Size.height), flagsTopLeftCorner);

            /* Bottom left corner */
            RB.DrawSprite(srcBottomLeftCorner, new Rect2i(destRect.x, destRect.y + bottomOffset, srcBottomLeftCorner.Size.width, srcBottomLeftCorner.Size.height), flagsBottomLeftCorner);

            /* Top right corner */
            RB.DrawSprite(srcTopRightCorner, new Rect2i(destRect.x + rightOffset, destRect.y, srcTopRightCorner.Size.width, srcTopRightCorner.Size.height), flagsTopRightCorner);

            /* Bottom right corner */
            RB.DrawSprite(srcBottomRightCorner, new Rect2i(destRect.x + rightOffset, destRect.y + bottomOffset, srcBottomRightCorner.Size.width, srcBottomRightCorner.Size.height), flagsBottomRightCorner);
        }

        /// <summary>
        /// Draw a single pixel
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="color">RGB color</param>
        public void DrawPixel(int x, int y, Color32 color)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            x -= mCameraPos.x;
            y -= mCameraPos.y;

            if (x < mClipRegion.x0 || x > mClipRegion.x1 || y < mClipRegion.y0 || y > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            // Draw pixel with just one triangle, make sure it passes through the middle of the pixel,
            // by extending its sides a bit. This should gurantee that it gets rasterized
#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = x;
            v.pos_y = y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x + 1.2f;
            v.pos_y = y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x;
            v.pos_y = y + 1.2f;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
#else
            mMeshStorage.Verticies[i + 0].x = x;
            mMeshStorage.Verticies[i + 0].y = y;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = x + 1.2f;
            mMeshStorage.Verticies[i + 1].y = y;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = x;
            mMeshStorage.Verticies[i + 2].y = y + 1.2f;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;

            i += 3;

            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a pixel buffer to render target
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="destWidth">Destination width</param>
        /// <param name="destHeight">Destination height</param>
        /// <param name="pixels">Pixel array</param>
        /// <param name="srcWidth">Source width</param>
        /// <param name="srcHeight">source height</param>
        /// <param name="pivotX">Rotation pivot x</param>
        /// <param name="pivotY">Rotation pivot y</param>
        /// <param name="rotation">Rotation angle</param>
        /// <param name="flags">Flags</param>
        public void DrawPixelBuffer(int x, int y, int destWidth, int destHeight, Color32[] pixels, int srcWidth, int srcHeight, int pivotX, int pivotY, float rotation, int flags)
        {
            x -= mCameraPos.x;
            y -= mCameraPos.y;

            bool pixelsChanged = (flags & RB.PIXEL_BUFFER_UNCHANGED) == 0;

            // Y axis is reversed in buffer copy, so flip the vertical flip flag
            flags ^= RB.FLIP_V;

            /* Should not clip here, let DrawTexture() clip, because we may want to update Pixel Buffer even if
             * draw will ultimately be clipped, because next time the user calls pixelsChanged may be false, and pixels data
             * may be invalid/blank */
            if (!pixelsChanged && PixelBufferTexture == null)
            {
                return;
            }

            if (pixelsChanged)
            {
                // Create PixelBuffer texture if there isn't one, or it isn't big enough
                if (PixelBufferTexture == null || PixelBufferTexture.width < srcWidth || PixelBufferTexture.height < srcHeight)
                {
                    PixelBufferTexture = new Texture2D(srcWidth, srcHeight, TextureFormat.ARGB32, false);
                    PixelBufferTexture.filterMode = FilterMode.Point;
                    PixelBufferTexture.wrapMode = TextureWrapMode.Clamp;

                    if (PixelBufferTexture == null)
                    {
                        // Failed to create texture
                        Debug.LogError("Failed to create texture for DrawPixelBuffer, the requested dimensions (" + srcWidth + " x " + srcHeight + ") may be invalid.");
                        return;
                    }
                }

                // First copy pixels to the PixelBufferTexture
                PixelBufferTexture.SetPixels32(0, 0, srcWidth, srcHeight, pixels);
                PixelBufferTexture.Apply();
            }

            // Save previous texture state
            var prevTexture = mPreviousTexture;
            var prevTextureWidth = CurrentSpriteSheetTextureWidth;
            var prevTextureHeight = CurrentSpriteSheetTextureHeight;
#if MESH_UPLOAD_2019_3
            var prevTexelWidth = CurrentSpriteSheetTexelWidth;
            var prevTexelHeight = CurrentSpriteSheetTexelHeight;
#endif

            SetCurrentTexture(PixelBufferTexture, null, false);

            CurrentSpriteSheetTextureWidth = PixelBufferTexture.width;
            CurrentSpriteSheetTextureHeight = PixelBufferTexture.height;
#if MESH_UPLOAD_2019_3
            CurrentSpriteSheetTexelWidth = 0xFFFF / (float)PixelBufferTexture.width;
            CurrentSpriteSheetTexelHeight = 0xFFFF / (float)PixelBufferTexture.height;
#endif

            if (rotation == 0)
            {
                DrawTexture(0, 0, srcWidth, srcHeight, x, y, destWidth, destHeight, flags);
            }
            else
            {
                DrawTexture(0, 0, srcWidth, srcHeight, x, y, destWidth, destHeight, pivotX, pivotY, rotation, flags);
            }

            // Restore previous settings
            SetCurrentTexture(prevTexture, null, false);
            CurrentSpriteSheetTextureWidth = prevTextureWidth;
            CurrentSpriteSheetTextureHeight = prevTextureHeight;
#if MESH_UPLOAD_2019_3
            CurrentSpriteSheetTexelWidth = prevTexelWidth;
            CurrentSpriteSheetTexelHeight = prevTexelHeight;

#endif
        }

        /// <summary>
        /// Draw a triangle outline
        /// </summary>
        /// <param name="p0X">X of first point of the triangle</param>
        /// <param name="p0Y">Y of first point of the triangle</param>
        /// <param name="p1X">X of second point of the triangle</param>
        /// <param name="p1Y">Y of second point of the triangle</param>
        /// <param name="p2X">X of third point of the triangle</param>
        /// <param name="p2Y">Y of third point of the triangle</param>
        /// <param name="color">RGB color</param>
        public void DrawTriangle(int p0X, int p0Y, int p1X, int p1Y, int p2X, int p2Y, Color32 color)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            mPoints[0].x = p0X;
            mPoints[0].y = p0Y;

            mPoints[1].x = p1X;
            mPoints[1].y = p1Y;

            mPoints[2].x = p2X;
            mPoints[2].y = p2Y;

            mPoints[3].x = p0X;
            mPoints[3].y = p0Y;

            DrawLineStrip(mPoints, 4, color);
        }

        /// <summary>
        /// Draw a triangle outline
        /// </summary>
        /// <param name="p0X">X of first point of the triangle</param>
        /// <param name="p0Y">Y of first point of the triangle</param>
        /// <param name="p1X">X of second point of the triangle</param>
        /// <param name="p1Y">Y of second point of the triangle</param>
        /// <param name="p2X">X of third point of the triangle</param>
        /// <param name="p2Y">Y of third point of the triangle</param>
        /// <param name="color">RGB color</param>
        /// <param name="pivotX">Rotation pivot point as an x offset from <paramref name="p0X"/></param>
        /// <param name="pivotY">Rotation pivot point as an y offset from <paramref name="p0Y"/></param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawTriangle(int p0X, int p0Y, int p1X, int p1Y, int p2X, int p2Y, Color32 color, int pivotX, int pivotY, float rotation = 0)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            rotation = RBUtil.WrapAngle(rotation);

            Vector3 fp0 = new Vector3(0, 0);
            Vector3 fp1 = new Vector3(p1X - p0X, p1Y - p0Y);
            Vector3 fp2 = new Vector3(p2X - p0X, p2Y - p0Y);

            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
            matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

            fp0 = matrix.MultiplyPoint3x4(fp0);
            fp1 = matrix.MultiplyPoint3x4(fp1);
            fp2 = matrix.MultiplyPoint3x4(fp2);

            fp0.x += pivotX + p0X;
            fp0.y += pivotY + p0Y;

            fp1.x += pivotX + p0X;
            fp1.y += pivotY + p0Y;

            fp2.x += pivotX + p0X;
            fp2.y += pivotY + p0Y;

            mPoints[0].x = Mathf.RoundToInt(fp0.x);
            mPoints[0].y = Mathf.RoundToInt(fp0.y);

            mPoints[1].x = Mathf.RoundToInt(fp1.x);
            mPoints[1].y = Mathf.RoundToInt(fp1.y);

            mPoints[2].x = Mathf.RoundToInt(fp2.x);
            mPoints[2].y = Mathf.RoundToInt(fp2.y);

            mPoints[3].x = Mathf.RoundToInt(fp0.x);
            mPoints[3].y = Mathf.RoundToInt(fp0.y);

            DrawLineStrip(mPoints, 4, color);
        }

        /// <summary>
        /// Draw a filled triangle
        /// </summary>
        /// <param name="p0X">X of first point of the triangle</param>
        /// <param name="p0Y">Y of first point of the triangle</param>
        /// <param name="p1X">X of second point of the triangle</param>
        /// <param name="p1Y">Y of second point of the triangle</param>
        /// <param name="p2X">X of third point of the triangle</param>
        /// <param name="p2Y">Y of third point of the triangle</param>
        /// <param name="color">RGB color</param>
        public void DrawTriangleFill(int p0X, int p0Y, int p1X, int p1Y, int p2X, int p2Y, Color32 color)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            p0X -= mCameraPos.x;
            p0Y -= mCameraPos.y;

            p1X -= mCameraPos.x;
            p1Y -= mCameraPos.y;

            p2X -= mCameraPos.x;
            p2Y -= mCameraPos.y;

            // Early clip test
            if ((p0X < mClipRegion.x0 && p1X < mClipRegion.x0 && p2X < mClipRegion.x0) ||
                (p0Y < mClipRegion.y0 && p1Y < mClipRegion.y0 && p2Y < mClipRegion.y0) ||
                (p0X > mClipRegion.x1 && p1X > mClipRegion.x1 && p2X > mClipRegion.x1) ||
                (p0Y > mClipRegion.y1 && p1Y > mClipRegion.y1 && p2Y > mClipRegion.y1))
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = p0X;
            v.pos_y = p0Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p1X;
            v.pos_y = p1Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2X;
            v.pos_y = p2Y;

            mMeshStorage.Verticies[i++] = v;

            // Skip ahead 4 even though we only used 3, this is because flush checks expects max of 6 indices per 4 verts
            i++;

            // It's cheaper to draw the triangle twice in two different windings than to check triangle winding
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
#else
            mMeshStorage.Verticies[i + 0].x = p0X;
            mMeshStorage.Verticies[i + 0].y = p0Y;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = p1X;
            mMeshStorage.Verticies[i + 1].y = p1Y;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = p2X;
            mMeshStorage.Verticies[i + 2].y = p2Y;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;

            // Skip ahead 4 even though we only used 3, this is because flush checks expects max of 6 indices per 4 verts
            i += 4;

            // It's cheaper to draw the triangle twice in two different windings than to check triangle winding
            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 3;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a filled triangle
        /// </summary>
        /// <param name="p0X">X of first point of the triangle</param>
        /// <param name="p0Y">Y of first point of the triangle</param>
        /// <param name="p1X">X of second point of the triangle</param>
        /// <param name="p1Y">Y of second point of the triangle</param>
        /// <param name="p2X">X of third point of the triangle</param>
        /// <param name="p2Y">Y of third point of the triangle</param>
        /// <param name="color">RGB color</param>
        /// <param name="pivotX">Rotation pivot point as an X offset from p0</param>
        /// <param name="pivotY">Rotation pivot point as an Y offset from p0</param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawTriangleFill(int p0X, int p0Y, int p1X, int p1Y, int p2X, int p2Y, Color32 color, int pivotX, int pivotY, float rotation = 0)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            rotation = RBUtil.WrapAngle(rotation);

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            Vector3 fp0 = new Vector3(0, 0);
            Vector3 fp1 = new Vector3(p1X - p0X, p1Y - p0Y);
            Vector3 fp2 = new Vector3(p2X - p0X, p2Y - p0Y);

            if (rotation != 0)
            {
                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

                fp0 = matrix.MultiplyPoint3x4(fp0);
                fp1 = matrix.MultiplyPoint3x4(fp1);
                fp2 = matrix.MultiplyPoint3x4(fp2);

                fp0.x += pivotX;
                fp0.y += pivotY;

                fp1.x += pivotX;
                fp1.y += pivotY;

                fp2.x += pivotX;
                fp2.y += pivotY;
            }

            fp0.x += -mCameraPos.x + p0X;
            fp0.y += -mCameraPos.y + p0Y;

            fp1.x += -mCameraPos.x + p0X;
            fp1.y += -mCameraPos.y + p0Y;

            fp2.x += -mCameraPos.x + p0X;
            fp2.y += -mCameraPos.y + p0Y;

            // Early clip test
            if ((fp0.x < mClipRegion.x0 && fp1.x < mClipRegion.x0 && fp2.x < mClipRegion.x0) ||
                (fp0.y < mClipRegion.y0 && fp1.y < mClipRegion.y0 && fp2.y < mClipRegion.y0) ||
                (fp0.x > mClipRegion.x1 && fp1.x > mClipRegion.x1 && fp2.x > mClipRegion.x1) ||
                (fp0.y > mClipRegion.y1 && fp1.y > mClipRegion.y1 && fp2.y > mClipRegion.y1))
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = fp0.x;
            v.pos_y = fp0.y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = fp1.x;
            v.pos_y = fp1.y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = fp2.x;
            v.pos_y = fp2.y;

            mMeshStorage.Verticies[i++] = v;

            // Skip ahead 4 even though we only used 3, this is because flush checks expects max of 6 indices per 4 verts
            i++;

            // It's cheaper to draw the triangle twice in two different windings than to check triangle winding
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
#else
            fp0.z = fp1.z = fp2.z = 0;

            mMeshStorage.Verticies[i + 0] = fp0;
            mMeshStorage.Verticies[i + 1] = fp1;
            mMeshStorage.Verticies[i + 2] = fp2;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;

            i += 4; // Skip ahead 4 even though we only used 3, this is because flush checks expects max of 6 indices per 4 verts

            // It's cheaper to draw the triangle twice in two different windings than to check triangle winding
            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 3;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a rectangle outline
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        /// <param name="color">RGB color</param>
        public void DrawRect(int x, int y, int width, int height, Color32 color)
        {
            if (width < 0 || height < 0)
            {
                return;
            }

            if (width <= 2 || height <= 2)
            {
                DrawRectFill(x, y, width, height, color, 0, 0);
            }
            else
            {
                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 4)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 4)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                int dx0 = 0;
                int dy0 = 0;
                int dx1 = dx0 + width;
                int dy1 = dy0 + height;

                // Fast color multiply
                color.r = (byte)((color.r * mCurrentColor.r) / 255);
                color.g = (byte)((color.g * mCurrentColor.g) / 255);
                color.b = (byte)((color.b * mCurrentColor.b) / 255);
                color.a = (byte)((color.a * mCurrentColor.a) / 255);

                dx0 -= mCameraPos.x - x;
                dy0 -= mCameraPos.y - y;

                dx1 -= mCameraPos.x - x;
                dy1 -= mCameraPos.y - y;

                // Early clip test
                if (dx1 < mClipRegion.x0 || dy1 < mClipRegion.y0 || dx0 > mClipRegion.x1 || dy0 > mClipRegion.y1)
                {
                    return;
                }

                int i = mMeshStorage.CurrentVertex;
                int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
                Vertex v;

                // Only have to set color once, will reuse for all vertices
                v.color_r = color.r;
                v.color_g = color.g;
                v.color_b = color.b;
                v.color_a = color.a;
                v.pos_z = 0;

                v.u = 0;
                v.v = 0;

                // Top line
                v.pos_x = dx0 + 1 - 0.1f;
                v.pos_y = dy0 - 0.1f;

                mMeshStorage.Verticies[i] = v;

                v.pos_x = dx1 - 1 + 0.1f;
                v.pos_y = dy0 + 0.5f;

                mMeshStorage.Verticies[i + 1] = v;

                v.pos_x = dx0 + 1 - 0.1f;
                v.pos_y = dy0 + 1.1f;

                mMeshStorage.Verticies[i + 2] = v;

                // Bottom line
                v.pos_x = dx0 + 1 - 0.1f;
                v.pos_y = dy1 - 1.1f;

                mMeshStorage.Verticies[i + 3] = v;

                v.pos_x = dx1 - 1 + 0.1f;
                v.pos_y = dy1 - 0.5f;

                mMeshStorage.Verticies[i + 4] = v;

                v.pos_x = dx0 + 1 - 0.1f;
                v.pos_y = dy1 + 0.1f;

                mMeshStorage.Verticies[i + 5] = v;

                // Left line
                v.pos_x = dx0 - 0.1f;
                v.pos_y = dy0 - 0.1f;

                mMeshStorage.Verticies[i + 6] = v;

                v.pos_x = dx0 + 1.1f;
                v.pos_y = dy0 - 0.1f;

                mMeshStorage.Verticies[i + 7] = v;

                v.pos_x = dx0 + 0.5f;
                v.pos_y = dy1 + 0.1f;

                mMeshStorage.Verticies[i + 8] = v;

                // Right line
                v.pos_x = dx1 - 1.1f;
                v.pos_y = dy0 - 0.1f;

                mMeshStorage.Verticies[i + 9] = v;

                v.pos_x = dx1 + 0.1f;
                v.pos_y = dy0 - 0.1f;

                mMeshStorage.Verticies[i + 10] = v;

                v.pos_x = dx1 - 0.5f;
                v.pos_y = dy1 + 0.1f;

                mMeshStorage.Verticies[i + 11] = v;

                mMeshStorage.Indices[j++] = i;
                mMeshStorage.Indices[j++] = i + 1;
                mMeshStorage.Indices[j++] = i + 2;

                mMeshStorage.Indices[j++] = i + 3;
                mMeshStorage.Indices[j++] = i + 1 + 3;
                mMeshStorage.Indices[j++] = i + 2 + 3;

                mMeshStorage.Indices[j++] = i + 6;
                mMeshStorage.Indices[j++] = i + 1 + 6;
                mMeshStorage.Indices[j++] = i + 2 + 6;

                mMeshStorage.Indices[j++] = i + 9;
                mMeshStorage.Indices[j++] = i + 1 + 9;
                mMeshStorage.Indices[j++] = i + 2 + 9;

                i += 12;
#else
                // Top line
                mMeshStorage.Verticies[i + 0].x = dx0 + 1 - 0.1f;
                mMeshStorage.Verticies[i + 0].y = dy0 - 0.1f;
                mMeshStorage.Verticies[i + 0].z = 0;

                mMeshStorage.Verticies[i + 1].x = dx1 - 1 + 0.1f;
                mMeshStorage.Verticies[i + 1].y = dy0 + 0.5f;
                mMeshStorage.Verticies[i + 1].z = 0;

                mMeshStorage.Verticies[i + 2].x = dx0 + 1 - 0.1f;
                mMeshStorage.Verticies[i + 2].y = dy0 + 1.1f;
                mMeshStorage.Verticies[i + 2].z = 0;

                // Bottom line
                mMeshStorage.Verticies[i + 3].x = dx0 + 1 - 0.1f;
                mMeshStorage.Verticies[i + 3].y = dy1 - 1.1f;
                mMeshStorage.Verticies[i + 3].z = 0;

                mMeshStorage.Verticies[i + 4].x = dx1 - 1 + 0.1f;
                mMeshStorage.Verticies[i + 4].y = dy1 - 0.5f;
                mMeshStorage.Verticies[i + 4].z = 0;

                mMeshStorage.Verticies[i + 5].x = dx0 + 1 - 0.1f;
                mMeshStorage.Verticies[i + 5].y = dy1 + 0.1f;
                mMeshStorage.Verticies[i + 5].z = 0;

                // Left line
                mMeshStorage.Verticies[i + 6].x = dx0 - 0.1f;
                mMeshStorage.Verticies[i + 6].y = dy0 - 0.1f;
                mMeshStorage.Verticies[i + 6].z = 0;

                mMeshStorage.Verticies[i + 7].x = dx0 + 1.1f;
                mMeshStorage.Verticies[i + 7].y = dy0 - 0.1f;
                mMeshStorage.Verticies[i + 7].z = 0;

                mMeshStorage.Verticies[i + 8].x = dx0 + 0.5f;
                mMeshStorage.Verticies[i + 8].y = dy1 + 0.1f;
                mMeshStorage.Verticies[i + 8].z = 0;

                // Right line
                mMeshStorage.Verticies[i + 9].x = dx1 - 1.1f;
                mMeshStorage.Verticies[i + 9].y = dy0 - 0.1f;
                mMeshStorage.Verticies[i + 9].z = 0;

                mMeshStorage.Verticies[i + 10].x = dx1 + 0.1f;
                mMeshStorage.Verticies[i + 10].y = dy0 - 0.1f;
                mMeshStorage.Verticies[i + 10].z = 0;

                mMeshStorage.Verticies[i + 11].x = dx1 - 0.5f;
                mMeshStorage.Verticies[i + 11].y = dy1 + 0.1f;
                mMeshStorage.Verticies[i + 11].z = 0;

                mMeshStorage.Colors[i + 0] = color;
                mMeshStorage.Colors[i + 1] = color;
                mMeshStorage.Colors[i + 2] = color;

                mMeshStorage.Colors[i + 3] = color;
                mMeshStorage.Colors[i + 4] = color;
                mMeshStorage.Colors[i + 5] = color;

                mMeshStorage.Colors[i + 6] = color;
                mMeshStorage.Colors[i + 7] = color;
                mMeshStorage.Colors[i + 8] = color;

                mMeshStorage.Colors[i + 9] = color;
                mMeshStorage.Colors[i + 10] = color;
                mMeshStorage.Colors[i + 11] = color;

                mMeshStorage.Indices[j++] = i;
                mMeshStorage.Indices[j++] = i + 1;
                mMeshStorage.Indices[j++] = i + 2;

                mMeshStorage.Indices[j++] = i + 3;
                mMeshStorage.Indices[j++] = i + 1 + 3;
                mMeshStorage.Indices[j++] = i + 2 + 3;

                mMeshStorage.Indices[j++] = i + 6;
                mMeshStorage.Indices[j++] = i + 1 + 6;
                mMeshStorage.Indices[j++] = i + 2 + 6;

                mMeshStorage.Indices[j++] = i + 9;
                mMeshStorage.Indices[j++] = i + 1 + 9;
                mMeshStorage.Indices[j++] = i + 2 + 9;

                i += 12;
#endif

                mMeshStorage.CurrentVertex = i;
                mMeshStorage.CurrentIndex = j;
            }
        }

        /// <summary>
        /// Draw a rectangle outline
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        /// <param name="color">RGB color</param>
        /// <param name="pivotX">Rotation pivot point x</param>
        /// <param name="pivotY">Rotation pivot point y</param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawRect(int x, int y, int width, int height, Color32 color, int pivotX, int pivotY, float rotation = 0)
        {
            if (width < 0 || height < 0)
            {
                return;
            }

            if (width <= 2 || height <= 2)
            {
                DrawRectFill(x, y, width, height, color, 0, 0);
            }
            else
            {
                rotation = RBUtil.WrapAngle(rotation);

                Vector3 p1, p2, p3, p4;

                p1 = new Vector3(0, 0, 0);
                p2 = new Vector3(width, 0, 0);
                p3 = new Vector3(width, -height, 0);
                p4 = new Vector3(0, -height, 0);

                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, -rotation), Vector3.one);
                matrix *= Matrix4x4.TRS(new Vector3(-pivotX, pivotY, 0), Quaternion.identity, Vector3.one);

                p1 = matrix.MultiplyPoint3x4(p1);
                p2 = matrix.MultiplyPoint3x4(p2);
                p3 = matrix.MultiplyPoint3x4(p3);
                p4 = matrix.MultiplyPoint3x4(p4);

                p1.x += pivotX + x;
                p1.y -= pivotY + y;

                p2.x += pivotX + x;
                p2.y -= pivotY + y;

                p3.x += pivotX + x;
                p3.y -= pivotY + y;

                p4.x += pivotX + x;
                p4.y -= pivotY + y;

                p1.y = -p1.y;
                p2.y = -p2.y;
                p3.y = -p3.y;
                p4.y = -p4.y;

                int p1X = Mathf.RoundToInt(p1.x);
                int p1Y = Mathf.RoundToInt(p1.y);
                int p2X = Mathf.RoundToInt(p2.x);
                int p2Y = Mathf.RoundToInt(p2.y);
                int p3X = Mathf.RoundToInt(p3.x);
                int p3Y = Mathf.RoundToInt(p3.y);
                int p4X = Mathf.RoundToInt(p4.x);
                int p4Y = Mathf.RoundToInt(p4.y);

                DrawLine(p1X, p1Y, p2X, p2Y, color, 0, 0, 0);
                DrawLine(p2X, p2Y, p3X, p3Y, color, 0, 0, 0);
                DrawLine(p3X, p3Y, p4X, p4Y, color, 0, 0, 0);
                DrawLine(p4X, p4Y, p1X, p1Y, color, 0, 0, 0);
            }
        }

        /// <summary>
        /// Draw a filled rectangle
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        /// <param name="color">RGB color</param>
        public void DrawRectFill(int x, int y, int width, int height, Color32 color)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int dx0 = 0;
            int dy0 = 0;
            int dx1 = dx0 + width;
            int dy1 = dy0 + height;

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            dx0 -= mCameraPos.x - x;
            dy0 -= mCameraPos.y - y;

            dx1 -= mCameraPos.x - x;
            dy1 -= mCameraPos.y - y;

            // Early clip test
            if (dx1 < mClipRegion.x0 || dy1 < mClipRegion.y0 || dx0 > mClipRegion.x1 || dy0 > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = dx0;
            v.pos_y = dy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx1;
            v.pos_y = dy0;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx1;
            v.pos_y = dy1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = dx0;
            v.pos_y = dy1;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = dx0;
            mMeshStorage.Verticies[i + 0].y = dy0;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = dx1;
            mMeshStorage.Verticies[i + 1].y = dy0;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = dx1;
            mMeshStorage.Verticies[i + 2].y = dy1;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Verticies[i + 3].x = dx0;
            mMeshStorage.Verticies[i + 3].y = dy1;
            mMeshStorage.Verticies[i + 3].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a filled rectangle
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        /// <param name="color">RGB color</param>
        /// <param name="pivotX">Rotation pivot point x</param>
        /// <param name="pivotY">Rotation pivot point y</param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawRectFill(int x, int y, int width, int height, Color32 color, int pivotX, int pivotY, float rotation = 0)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            // If width or height is 1 then we're better off drawing ortho line because its made of just 1 triangle
            if ((width == 1 || height == 1) && rotation == 0)
            {
                DrawOrthoLine(x, y, x + width - 1, y + height - 1, color);
                return;
            }

            int dx0 = 0;
            int dy0 = 0;
            int dx1 = dx0 + width;
            int dy1 = dy0 + height;

            // Wrap the angle first to values between 0 and 360
            if (rotation != 0)
            {
                rotation = RBUtil.WrapAngle(rotation);
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            Vector3 p1, p2, p3, p4;

            p1 = new Vector3(dx0, dy0, 0);
            p2 = new Vector3(dx1, dy0, 0);
            p3 = new Vector3(dx1, dy1, 0);
            p4 = new Vector3(dx0, dy1, 0);

            if (rotation != 0)
            {
                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

                p1 = matrix.MultiplyPoint3x4(p1);
                p2 = matrix.MultiplyPoint3x4(p2);
                p3 = matrix.MultiplyPoint3x4(p3);
                p4 = matrix.MultiplyPoint3x4(p4);

                p1.x += pivotX;
                p1.y += pivotY;

                p2.x += pivotX;
                p2.y += pivotY;

                p3.x += pivotX;
                p3.y += pivotY;

                p4.x += pivotX;
                p4.y += pivotY;
            }

            p1.x -= mCameraPos.x - x;
            p1.y -= mCameraPos.y - y;

            p2.x -= mCameraPos.x - x;
            p2.y -= mCameraPos.y - y;

            p3.x -= mCameraPos.x - x;
            p3.y -= mCameraPos.y - y;

            p4.x -= mCameraPos.x - x;
            p4.y -= mCameraPos.y - y;

            // Early clip test
            if (p1.x < mClipRegion.x0 && p2.x < mClipRegion.x0 && p3.x < mClipRegion.x0 && p4.x < mClipRegion.x0)
            {
                return;
            }
            else if (p1.x > mClipRegion.x1 && p2.x > mClipRegion.x1 && p3.x > mClipRegion.x1 && p4.x > mClipRegion.x1)
            {
                return;
            }
            else if (p1.y < mClipRegion.y0 && p2.y < mClipRegion.y0 && p3.y < mClipRegion.y0 && p4.y < mClipRegion.y0)
            {
                // Note that Y axis is inverted by this point, have to invert it back before checking against clip
                return;
            }
            else if (p1.y > mClipRegion.y1 && p2.y > mClipRegion.y1 && p3.y > mClipRegion.y1 && p4.y > mClipRegion.y1)
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = p1.x;
            v.pos_y = p1.y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2.x;
            v.pos_y = p2.y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3.x;
            v.pos_y = p3.y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p4.x;
            v.pos_y = p4.y;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            p1.z = p2.z = p3.z = p4.z = 0;

            mMeshStorage.Verticies[i + 0] = p1;
            mMeshStorage.Verticies[i + 1] = p2;
            mMeshStorage.Verticies[i + 2] = p3;
            mMeshStorage.Verticies[i + 3] = p4;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a vertical line with no checks
        /// </summary>
        /// <param name="x">Start x</param>
        /// <param name="y1">Start y</param>
        /// <param name="y2">End y</param>
        public void DrawVerticalLineNoChecks(int x, int y1, int y2)
        {
            x -= mCameraPos.x;
            y1 -= mCameraPos.y;
            y2 -= mCameraPos.y;

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = mCurrentColor.r;
            v.color_g = mCurrentColor.g;
            v.color_b = mCurrentColor.b;
            v.color_a = mCurrentColor.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = x - 0.1f;
            v.pos_y = y1 - 0.1f;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x + 1.1f;
            v.pos_y = y1 - 0.1f;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x + 0.5f;
            v.pos_y = y2 + 1.1f;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
#else
            mMeshStorage.Verticies[i + 0].x = x - 0.1f;
            mMeshStorage.Verticies[i + 0].y = y1 - 0.1f;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = x + 1.1f;
            mMeshStorage.Verticies[i + 1].y = y1 - 0.1f;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = x + 0.5f;
            mMeshStorage.Verticies[i + 2].y = y2 + 1.1f;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Colors[i + 0] = mCurrentColor;
            mMeshStorage.Colors[i + 1] = mCurrentColor;
            mMeshStorage.Colors[i + 2] = mCurrentColor;

            i += 3;

            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a vertical line with no checks
        /// </summary>
        /// <param name="x1">Start x</param>
        /// <param name="x2">End x</param>
        /// <param name="y">Start y</param>
        public void DrawHorizontalLineNoChecks(int x1, int x2, int y)
        {
            x1 -= mCameraPos.x;
            x2 -= mCameraPos.x;
            y -= mCameraPos.y;

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = mCurrentColor.r;
            v.color_g = mCurrentColor.g;
            v.color_b = mCurrentColor.b;
            v.color_a = mCurrentColor.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = x1 - 0.1f;
            v.pos_y = y - 0.1f;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x2 + 1.1f;
            v.pos_y = y + 0.5f;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x1 - 0.1f;
            v.pos_y = y + 1.1f;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
#else
            mMeshStorage.Verticies[i + 0].x = x1 - 0.1f;
            mMeshStorage.Verticies[i + 0].y = y - 0.1f;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = x2 + 1.1f;
            mMeshStorage.Verticies[i + 1].y = y + 0.5f;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = x1 - 0.1f;
            mMeshStorage.Verticies[i + 2].y = y + 1.1f;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Colors[i + 0] = mCurrentColor;
            mMeshStorage.Colors[i + 1] = mCurrentColor;
            mMeshStorage.Colors[i + 2] = mCurrentColor;

            i += 3;

            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a rect fill with no checks
        /// </summary>
        /// <param name="x1">Start x</param>
        /// <param name="y1">Start y</param>
        /// <param name="x2">End x</param>
        /// <param name="y2">End y</param>
        public void DrawRectFillNoChecks(int x1, int y1, int x2, int y2)
        {
            x1 -= mCameraPos.x;
            x2 -= mCameraPos.x;
            y1 -= mCameraPos.y;
            y2 -= mCameraPos.y;

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = mCurrentColor.r;
            v.color_g = mCurrentColor.g;
            v.color_b = mCurrentColor.b;
            v.color_a = mCurrentColor.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = x1;
            v.pos_y = y1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x2;
            v.pos_y = y1;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x2;
            v.pos_y = y2;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = x1;
            v.pos_y = y2;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = x1;
            mMeshStorage.Verticies[i + 0].y = y1;
            mMeshStorage.Verticies[i + 0].z = 0;

            mMeshStorage.Verticies[i + 1].x = x2;
            mMeshStorage.Verticies[i + 1].y = y1;
            mMeshStorage.Verticies[i + 1].z = 0;

            mMeshStorage.Verticies[i + 2].x = x2;
            mMeshStorage.Verticies[i + 2].y = y2;
            mMeshStorage.Verticies[i + 2].z = 0;

            mMeshStorage.Verticies[i + 3].x = x1;
            mMeshStorage.Verticies[i + 3].y = y2;
            mMeshStorage.Verticies[i + 3].z = 0;

            mMeshStorage.Colors[i + 0] = mCurrentColor;
            mMeshStorage.Colors[i + 1] = mCurrentColor;
            mMeshStorage.Colors[i + 2] = mCurrentColor;
            mMeshStorage.Colors[i + 3] = mCurrentColor;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw a system font glyph
        /// </summary>
        /// <param name="posX">Position x</param>
        /// <param name="posY">Position y</param>
        /// <param name="rectIndex">Rectangle index in the glyph definition</param>
        /// <param name="systemGlyphRects">Collection of system glyphs for this font</param>
        public void DrawSystemGlyph(int posX, int posY, int rectIndex, RBFontBuiltin.GlyphRect[] systemGlyphRects)
        {
            int lastHorizontal = rectIndex + systemGlyphRects[rectIndex].x2 + 1;
            int lastVertical = rectIndex + systemGlyphRects[rectIndex].x2 + systemGlyphRects[rectIndex].y2 + 1;

            // Do a flush check on the entire glyph, which will be made of multiple horizontal and vertical lines
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < (systemGlyphRects[rectIndex].x2 + systemGlyphRects[rectIndex].y2) * 3)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < (systemGlyphRects[rectIndex].x2 + systemGlyphRects[rectIndex].y2) * 3)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            rectIndex++;

            int x1, x2, y1, y2, i, j;

#if MESH_UPLOAD_2019_3
            Vertex v;
#endif

            for (; rectIndex < lastHorizontal; rectIndex++)
            {
                x1 = posX + systemGlyphRects[rectIndex].x1;
                x2 = posX + systemGlyphRects[rectIndex].x2;
                y1 = posY + systemGlyphRects[rectIndex].y1;

                x1 -= mCameraPos.x;
                x2 -= mCameraPos.x;
                y1 -= mCameraPos.y;

                i = mMeshStorage.CurrentVertex;
                j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
                // Only have to set color once, will reuse for all vertices
                v.color_r = mCurrentColor.r;
                v.color_g = mCurrentColor.g;
                v.color_b = mCurrentColor.b;
                v.color_a = mCurrentColor.a;
                v.pos_z = 0;

                v.u = 0;
                v.v = 0;

                v.pos_x = x1 - 0.1f;
                v.pos_y = y1 - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = x2 + 1.1f;
                v.pos_y = y1 + 0.5f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = x1 - 0.1f;
                v.pos_y = y1 + 1.1f;

                mMeshStorage.Verticies[i++] = v;

                mMeshStorage.Indices[j++] = (ushort)(i - 3);
                mMeshStorage.Indices[j++] = (ushort)(i - 2);
                mMeshStorage.Indices[j++] = (ushort)(i - 1);
#else
                mMeshStorage.Verticies[i + 0].x = x1 - 0.1f;
                mMeshStorage.Verticies[i + 0].y = y1 - 0.1f;
                mMeshStorage.Verticies[i + 0].z = 0;

                mMeshStorage.Verticies[i + 1].x = x2 + 1.1f;
                mMeshStorage.Verticies[i + 1].y = y1 + 0.5f;
                mMeshStorage.Verticies[i + 1].z = 0;

                mMeshStorage.Verticies[i + 2].x = x1 - 0.1f;
                mMeshStorage.Verticies[i + 2].y = y1 + 1.1f;
                mMeshStorage.Verticies[i + 2].z = 0;

                mMeshStorage.Colors[i + 0] = mCurrentColor;
                mMeshStorage.Colors[i + 1] = mCurrentColor;
                mMeshStorage.Colors[i + 2] = mCurrentColor;

                i += 3;

                mMeshStorage.Indices[j++] = i - 3;
                mMeshStorage.Indices[j++] = i - 2;
                mMeshStorage.Indices[j++] = i - 1;
#endif

                mMeshStorage.CurrentVertex = i;
                mMeshStorage.CurrentIndex = j;
            }

            for (; rectIndex < lastVertical; rectIndex++)
            {
                x1 = posX + systemGlyphRects[rectIndex].x1;
                y1 = posY + systemGlyphRects[rectIndex].y1;
                y2 = posY + systemGlyphRects[rectIndex].y2;

                x1 -= mCameraPos.x;
                y1 -= mCameraPos.y;
                y2 -= mCameraPos.y;

                i = mMeshStorage.CurrentVertex;
                j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
                // Only have to set color once, will reuse for all vertices
                v.color_r = mCurrentColor.r;
                v.color_g = mCurrentColor.g;
                v.color_b = mCurrentColor.b;
                v.color_a = mCurrentColor.a;
                v.pos_z = 0;

                v.u = 0;
                v.v = 0;

                v.pos_x = x1 - 0.1f;
                v.pos_y = y1 - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = x1 + 1.1f;
                v.pos_y = y1 - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = x1 + 0.5f;
                v.pos_y = y2 + 1.1f;

                mMeshStorage.Verticies[i++] = v;

                mMeshStorage.Indices[j++] = (ushort)(i - 3);
                mMeshStorage.Indices[j++] = (ushort)(i - 2);
                mMeshStorage.Indices[j++] = (ushort)(i - 1);
#else
                mMeshStorage.Verticies[i + 0].x = x1 - 0.1f;
                mMeshStorage.Verticies[i + 0].y = y1 - 0.1f;
                mMeshStorage.Verticies[i + 0].z = 0;

                mMeshStorage.Verticies[i + 1].x = x1 + 1.1f;
                mMeshStorage.Verticies[i + 1].y = y1 - 0.1f;
                mMeshStorage.Verticies[i + 1].z = 0;

                mMeshStorage.Verticies[i + 2].x = x1 + 0.5f;
                mMeshStorage.Verticies[i + 2].y = y2 + 1.1f;
                mMeshStorage.Verticies[i + 2].z = 0;

                mMeshStorage.Colors[i + 0] = mCurrentColor;
                mMeshStorage.Colors[i + 1] = mCurrentColor;
                mMeshStorage.Colors[i + 2] = mCurrentColor;

                i += 3;

                mMeshStorage.Indices[j++] = i - 3;
                mMeshStorage.Indices[j++] = i - 2;
                mMeshStorage.Indices[j++] = i - 1;
#endif

                mMeshStorage.CurrentVertex = i;
                mMeshStorage.CurrentIndex = j;
            }
        }

        /// <summary>
        /// Draw line from lp0 to lp1
        /// </summary>
        /// <param name="lp0X">Start point x</param>
        /// <param name="lp0Y">Start point y</param>
        /// <param name="lp1X">End point x</param>
        /// <param name="lp1Y">End point y</param>
        /// <param name="color">RGB color</param>
        public void DrawLine(int lp0X, int lp0Y, int lp1X, int lp1Y, Color32 color)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            // Trivial straight lines, use ortho, faster
            if (lp0X == lp1X || lp0Y == lp1Y)
            {
                DrawOrthoLine(lp0X, lp0Y, lp1X, lp1Y, color);
                return;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            float lp0fX = lp0X;
            float lp0fY = lp0Y;
            float lp1fX = lp1X;
            float lp1fY = lp1Y;

            lp0fX -= mCameraPos.x;
            lp0fY -= mCameraPos.y;

            lp1fX -= mCameraPos.x;
            lp1fY -= mCameraPos.y;

            // Early clip test
            if (lp0fX < mClipRegion.x0 && lp1fX < mClipRegion.x0)
            {
                return;
            }
            else if (lp0fX > mClipRegion.x1 && lp1fX > mClipRegion.x1)
            {
                return;
            }
            else if (lp0fY < mClipRegion.y0 && lp1fY < mClipRegion.y0)
            {
                return;
            }
            else if (lp0fY > mClipRegion.y1 && lp1fY > mClipRegion.y1)
            {
                return;
            }

            float dirX = lp1fX - lp0fX;
            float dirY = lp1fY - lp0fY;
            float p0X, p0Y;
            float p1X, p1Y;
            float p2X, p2Y;
            float p3X, p3Y;

            /* Figure out which quadrant the angle is in
             *
             * \      0     /
             *   \        /
             *     \    /
             *       \/
             * 3     /\     1
             *     /    \
             *   /        \
             * /      2     \
             */
            if (System.Math.Abs(dirX) > System.Math.Abs(dirY))
            {
                if (dirX > 0)
                {
                    // quadrant 1
                    p0X = lp0fX;
                    p0Y = lp0fY;
                    p1X = lp1fX;
                    p1Y = lp1fY;

                    p0X += 0.5f;
                    p1X += 0.5f;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;
                }
                else
                {
                    // quadrant 3
                    p1X = lp0fX;
                    p1Y = lp0fY;
                    p0X = lp1fX;
                    p0Y = lp1fY;

                    p0X += 0.5f;
                    p1X += 0.5f;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;
                }
            }
            else
            {
                if (dirY < 0)
                {
                    // quadrant 0
                    p0X = lp0fX;
                    p0Y = lp0fY;
                    p1X = lp1fX;
                    p1Y = lp1fY;

                    p0Y += 0.5f;
                    p1Y += 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;
                }
                else
                {
                    // quadrant 2
                    p1X = lp0fX;
                    p1Y = lp0fY;
                    p0X = lp1fX;
                    p0Y = lp1fY;

                    p0Y += 0.5f;
                    p1Y += 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;
                }
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = p0X;
            v.pos_y = p0Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p1X;
            v.pos_y = p1Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2X;
            v.pos_y = p2Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3X;
            v.pos_y = p3Y;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = p0X;
            mMeshStorage.Verticies[i + 0].y = p0Y;
            mMeshStorage.Verticies[i + 0].z = 0;
            mMeshStorage.Verticies[i + 1].x = p1X;
            mMeshStorage.Verticies[i + 1].y = p1Y;
            mMeshStorage.Verticies[i + 1].z = 0;
            mMeshStorage.Verticies[i + 2].x = p2X;
            mMeshStorage.Verticies[i + 2].y = p2Y;
            mMeshStorage.Verticies[i + 2].z = 0;
            mMeshStorage.Verticies[i + 3].x = p3X;
            mMeshStorage.Verticies[i + 3].y = p3Y;
            mMeshStorage.Verticies[i + 3].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw line from lp0 to lp1
        /// </summary>
        /// <param name="lp0X">Start point x</param>
        /// <param name="lp0Y">Start point y</param>
        /// <param name="lp1X">End point x</param>
        /// <param name="lp1Y">End point y</param>
        /// <param name="color">RGB color</param>
        /// <param name="pivotX">Rotation pivot point x</param>
        /// <param name="pivotY">Rotation pivot point y</param>
        /// <param name="rotation">Rotation in degrees</param>
        public void DrawLine(int lp0X, int lp0Y, int lp1X, int lp1Y, Color32 color, int pivotX, int pivotY, float rotation = 0)
        {
            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            rotation = RBUtil.WrapAngle(rotation);

            if (rotation == 0)
            {
                if (lp0X == lp1X || lp0Y == lp1Y)
                {
                    DrawOrthoLine(lp0X, lp0Y, lp1X, lp1Y, color);
                    return;
                }
            }
            else
            {
                // Adjust pivot so that it originates from top left corner of an imaginary rectangle that would encompass the line
                int topX = lp0X < lp1X ? lp0X : lp1X;
                int topY = lp0Y < lp1Y ? lp0Y : lp1Y;

                pivotX += topX;
                pivotY += topY;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            Vector3 lp0f = new Vector3(lp0X, lp0Y, 0);
            Vector3 lp1f = new Vector3(lp1X, lp1Y, 0);

            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
            matrix *= Matrix4x4.TRS(new Vector3(-pivotX, -pivotY, 0), Quaternion.identity, Vector3.one);

            lp0f = matrix.MultiplyPoint3x4(lp0f);
            lp1f = matrix.MultiplyPoint3x4(lp1f);

            lp0f.x += pivotX;
            lp0f.y += pivotY;

            lp1f.x += pivotX;
            lp1f.y += pivotY;

            lp0f.x -= mCameraPos.x;
            lp0f.y -= mCameraPos.y;

            lp1f.x -= mCameraPos.x;
            lp1f.y -= mCameraPos.y;

            // Early clip test
            if (lp0f.x < mClipRegion.x0 && lp1f.x < mClipRegion.x0)
            {
                return;
            }
            else if (lp0f.x > mClipRegion.x1 && lp1f.x > mClipRegion.x1)
            {
                return;
            }
            else if (lp0f.y < mClipRegion.y0 && lp1f.y < mClipRegion.y0)
            {
                return;
            }
            else if (lp0f.y > mClipRegion.y1 && lp1f.y > mClipRegion.y1)
            {
                return;
            }

            float dirX = lp1f.x - lp0f.x;
            float dirY = lp1f.y - lp0f.y;
            float p0X, p0Y;
            float p1X, p1Y;
            float p2X, p2Y;
            float p3X, p3Y;

            /* Figure out which quadrant the angle is in
             *
             * \      0     /
             *   \        /
             *     \    /
             *       \/
             * 3     /\     1
             *     /    \
             *   /        \
             * /      2     \
             */
            if (System.Math.Abs(dirX) > System.Math.Abs(dirY))
            {
                if (dirX > 0)
                {
                    // quadrant 1
                    p0X = lp0f.x;
                    p0Y = lp0f.y;
                    p1X = lp1f.x;
                    p1Y = lp1f.y;

                    p0X += 0.5f;
                    p1X += 0.5f;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;
                }
                else
                {
                    // quadrant 3
                    p1X = lp0f.x;
                    p1Y = lp0f.y;
                    p0X = lp1f.x;
                    p0Y = lp1f.y;

                    p0X += 0.5f;
                    p1X += 0.5f;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;
                }
            }
            else
            {
                if (dirY < 0)
                {
                    // quadrant 0
                    p0X = lp0f.x;
                    p0Y = lp0f.y;
                    p1X = lp1f.x;
                    p1Y = lp1f.y;

                    p0Y += 0.5f;
                    p1Y += 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;
                }
                else
                {
                    // quadrant 2
                    p1X = lp0f.x;
                    p1Y = lp0f.y;
                    p0X = lp1f.x;
                    p0Y = lp1f.y;

                    p0Y += 0.5f;
                    p1Y += 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    p1X += sideDirX * 0.5f;
                    p1Y += sideDirY * 0.5f;
                    p2X += sideDirX * 0.5f;
                    p2Y += sideDirY * 0.5f;

                    p0X += sideDirX * -0.5f;
                    p0Y += sideDirY * -0.5f;
                    p3X += sideDirX * -0.5f;
                    p3Y += sideDirY * -0.5f;
                }
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = p0X;
            v.pos_y = p0Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p1X;
            v.pos_y = p1Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2X;
            v.pos_y = p2Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3X;
            v.pos_y = p3Y;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = p0X;
            mMeshStorage.Verticies[i + 0].y = p0Y;
            mMeshStorage.Verticies[i + 0].z = 0;
            mMeshStorage.Verticies[i + 1].x = p1X;
            mMeshStorage.Verticies[i + 1].y = p1Y;
            mMeshStorage.Verticies[i + 1].z = 0;
            mMeshStorage.Verticies[i + 2].x = p2X;
            mMeshStorage.Verticies[i + 2].y = p2Y;
            mMeshStorage.Verticies[i + 2].z = 0;
            mMeshStorage.Verticies[i + 3].x = p3X;
            mMeshStorage.Verticies[i + 3].y = p3Y;
            mMeshStorage.Verticies[i + 3].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw line from lp0 to lp1
        /// </summary>
        /// <param name="lp0X">Start point x</param>
        /// <param name="lp0Y">Start point y</param>
        /// <param name="lp1X">End point x</param>
        /// <param name="lp1Y">End point y</param>
        /// <param name="color">RGB color</param>
        /// <param name="startPixel">Render the first pixel in the line</param>
        /// <param name="endPixel">Render the last pixel in the line</param>
        public void DrawLineWithCaps(int lp0X, int lp0Y, int lp1X, int lp1Y, Color32 color, bool startPixel = true, bool endPixel = true)
        {
            //// CheckFlush should already be called by caller

            if (lp0X == lp1X && lp0Y == lp1Y)
            {
                if (!startPixel && !endPixel)
                {
                    // Do nothing
                    return;
                }

                DrawPixel(lp0X, lp0Y, color);
                return;
            }

            // Trivial straight lines, use rect, faster
            if (lp0X == lp1X)
            {
                if (!startPixel)
                {
                    if (lp0Y < lp1Y)
                    {
                        lp0Y++;
                    }
                    else
                    {
                        lp0Y--;
                    }
                }

                if (!endPixel)
                {
                    if (lp0Y < lp1Y)
                    {
                        lp1Y--;
                    }
                    else
                    {
                        lp1Y++;
                    }
                }

                DrawOrthoLine(lp0X, lp0Y, lp1X, lp1Y, color);
                return;
            }

            if (lp0Y == lp1Y)
            {
                if (!startPixel)
                {
                    if (lp0X < lp1X)
                    {
                        lp0X++;
                    }
                    else
                    {
                        lp0X--;
                    }
                }

                if (!endPixel)
                {
                    if (lp0X < lp1X)
                    {
                        lp1X--;
                    }
                    else
                    {
                        lp1X++;
                    }
                }

                DrawOrthoLine(lp0X, lp0Y, lp1X, lp1Y, color);
                return;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            lp0X -= mCameraPos.x;
            lp0Y -= mCameraPos.y;

            lp1X -= mCameraPos.x;
            lp1Y -= mCameraPos.y;

            // Early clip test
            if (lp0X < mClipRegion.x0 && lp1X < mClipRegion.x0)
            {
                return;
            }
            else if (lp0X > mClipRegion.x1 && lp1X > mClipRegion.x1)
            {
                return;
            }
            else if (lp0Y < mClipRegion.y0 && lp1Y < mClipRegion.y0)
            {
                return;
            }
            else if (lp0Y > mClipRegion.y1 && lp1Y > mClipRegion.y1)
            {
                return;
            }

            int dirX = lp1X - lp0X;
            int dirY = lp1Y - lp0Y;
            float p0X, p0Y;
            float p1X, p1Y;
            float p2X, p2Y;
            float p3X, p3Y;

            /* Figure out which quadrant the angle is in
             *
             * \      0     /
             *   \        /
             *     \    /
             *       \/
             * 3     /\     1
             *     /    \
             *   /        \
             * /      2     \
             */
            if (RBUtil.FastIntAbs(dirX) > RBUtil.FastIntAbs(dirY))
            {
                if (dirX > 0)
                {
                    // quadrant 1
                    p0X = lp0X + 0.5f;
                    p0Y = lp0Y;
                    p1X = lp1X + 0.5f;
                    p1Y = lp1Y;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    if (startPixel)
                    {
                        p0X += sideDirX * -0.5f;
                        p0Y += sideDirY * -0.5f;
                        p3X += sideDirX * -0.5f;
                        p3Y += sideDirY * -0.5f;
                    }
                    else
                    {
                        p0X += sideDirX * 0.5f;
                        p0Y += sideDirY * 0.5f;
                        p3X += sideDirX * 0.5f;
                        p3Y += sideDirY * 0.5f;
                    }

                    if (endPixel)
                    {
                        p1X += sideDirX * 0.5f;
                        p1Y += sideDirY * 0.5f;
                        p2X += sideDirX * 0.5f;
                        p2Y += sideDirY * 0.5f;
                    }
                    else
                    {
                        p1X += sideDirX * -0.5f;
                        p1Y += sideDirY * -0.5f;
                        p2X += sideDirX * -0.5f;
                        p2Y += sideDirY * -0.5f;
                    }
                }
                else
                {
                    // quadrant 3
                    p0X = lp1X + 0.5f;
                    p0Y = lp1Y;
                    p1X = lp0X + 0.5f;
                    p1Y = lp0Y;
                    p2X = p1X;
                    p2Y = p1Y + 1.0f;
                    p3X = p0X;
                    p3Y = p0Y + 1.0f;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    if (startPixel)
                    {
                        p1X += sideDirX * 0.5f;
                        p1Y += sideDirY * 0.5f;
                        p2X += sideDirX * 0.5f;
                        p2Y += sideDirY * 0.5f;
                    }
                    else
                    {
                        p1X += sideDirX * -0.5f;
                        p1Y += sideDirY * -0.5f;
                        p2X += sideDirX * -0.5f;
                        p2Y += sideDirY * -0.5f;
                    }

                    if (endPixel)
                    {
                        p0X += sideDirX * -0.5f;
                        p0Y += sideDirY * -0.5f;
                        p3X += sideDirX * -0.5f;
                        p3Y += sideDirY * -0.5f;
                    }
                    else
                    {
                        p0X += sideDirX * 0.5f;
                        p0Y += sideDirY * 0.5f;
                        p3X += sideDirX * 0.5f;
                        p3Y += sideDirY * 0.5f;
                    }
                }
            }
            else
            {
                if (dirY < 0)
                {
                    // quadrant 0
                    p0X = lp0X;
                    p0Y = lp0Y + 0.5f;
                    p1X = lp1X;
                    p1Y = lp1Y + 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    if (startPixel)
                    {
                        p0X += sideDirX * -0.5f;
                        p0Y += sideDirY * -0.5f;
                        p3X += sideDirX * -0.5f;
                        p3Y += sideDirY * -0.5f;
                    }
                    else
                    {
                        p0X += sideDirX * 0.5f;
                        p0Y += sideDirY * 0.5f;
                        p3X += sideDirX * 0.5f;
                        p3Y += sideDirY * 0.5f;
                    }

                    if (endPixel)
                    {
                        p1X += sideDirX * 0.5f;
                        p1Y += sideDirY * 0.5f;
                        p2X += sideDirX * 0.5f;
                        p2Y += sideDirY * 0.5f;
                    }
                    else
                    {
                        p1X += sideDirX * -0.5f;
                        p1Y += sideDirY * -0.5f;
                        p2X += sideDirX * -0.5f;
                        p2Y += sideDirY * -0.5f;
                    }
                }
                else
                {
                    // quadrant 2
                    p0X = lp1X;
                    p0Y = lp1Y + 0.5f;
                    p1X = lp0X;
                    p1Y = lp0Y + 0.5f;
                    p2X = p1X + 1.0f;
                    p2Y = p1Y;
                    p3X = p0X + 1.0f;
                    p3Y = p0Y;

                    var sideDirX = p1X - p0X;
                    var sideDirY = p1Y - p0Y;
                    float sideLen = Mathf.Sqrt((sideDirX * sideDirX) + (sideDirY * sideDirY));
                    sideDirX /= sideLen;
                    sideDirY /= sideLen;

                    if (startPixel)
                    {
                        p1X += sideDirX * 0.5f;
                        p1Y += sideDirY * 0.5f;
                        p2X += sideDirX * 0.5f;
                        p2Y += sideDirY * 0.5f;
                    }
                    else
                    {
                        p1X += sideDirX * -0.5f;
                        p1Y += sideDirY * -0.5f;
                        p2X += sideDirX * -0.5f;
                        p2Y += sideDirY * -0.5f;
                    }

                    if (endPixel)
                    {
                        p0X += sideDirX * -0.5f;
                        p0Y += sideDirY * -0.5f;
                        p3X += sideDirX * -0.5f;
                        p3Y += sideDirY * -0.5f;
                    }
                    else
                    {
                        p0X += sideDirX * 0.5f;
                        p0Y += sideDirY * 0.5f;
                        p3X += sideDirX * 0.5f;
                        p3Y += sideDirY * 0.5f;
                    }
                }
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = p0X;
            v.pos_y = p0Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p1X;
            v.pos_y = p1Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p2X;
            v.pos_y = p2Y;

            mMeshStorage.Verticies[i++] = v;

            v.pos_x = p3X;
            v.pos_y = p3Y;

            mMeshStorage.Verticies[i++] = v;

            mMeshStorage.Indices[j++] = (ushort)(i - 4);
            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);

            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
            mMeshStorage.Indices[j++] = (ushort)(i - 4);
#else
            mMeshStorage.Verticies[i + 0].x = p0X;
            mMeshStorage.Verticies[i + 0].y = p0Y;
            mMeshStorage.Verticies[i + 0].z = 0;
            mMeshStorage.Verticies[i + 1].x = p1X;
            mMeshStorage.Verticies[i + 1].y = p1Y;
            mMeshStorage.Verticies[i + 1].z = 0;
            mMeshStorage.Verticies[i + 2].x = p2X;
            mMeshStorage.Verticies[i + 2].y = p2Y;
            mMeshStorage.Verticies[i + 2].z = 0;
            mMeshStorage.Verticies[i + 3].x = p3X;
            mMeshStorage.Verticies[i + 3].y = p3Y;
            mMeshStorage.Verticies[i + 3].z = 0;

            mMeshStorage.Colors[i + 0] = color;
            mMeshStorage.Colors[i + 1] = color;
            mMeshStorage.Colors[i + 2] = color;
            mMeshStorage.Colors[i + 3] = color;

            i += 4;

            mMeshStorage.Indices[j++] = i - 4;
            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;

            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
            mMeshStorage.Indices[j++] = i - 4;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        /// <summary>
        /// Draw ellipse
        /// </summary>
        /// <param name="centerX">X Center of ellipse</param>
        /// <param name="centerY">Y Center of ellipse</param>
        /// <param name="radiusX">X Radius</param>
        /// <param name="radiusY">Y Radius</param>
        /// <param name="color">RGB color</param>
        public void DrawEllipse(int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            if (radiusX <= 0 || radiusY <= 0)
            {
                return;
            }

            Rect2i userClipRegion = RB.ClipGet();
            Rect2i bounds = new Rect2i(centerX - radiusX - 1, centerY - radiusY - 1, (radiusX * 2) + 2, (radiusY * 2) + 2);
            var cameraPos = RB.CameraGet();
            bounds.x -= cameraPos.x;
            bounds.y -= cameraPos.y;
            if (!userClipRegion.Intersects(bounds))
            {
                return;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            DrawEllipseInternal(centerX, centerY, radiusX, radiusY, color);
        }

        /// <summary>
        /// Draw filled ellipse
        /// </summary>
        /// <param name="centerX">X Center of ellipse</param>
        /// <param name="centerY">Y Center of ellipse</param>
        /// <param name="radiusX">X Radius</param>
        /// <param name="radiusY">Y Radius</param>
        /// <param name="color">RGB color</param>
        /// <param name="inverse">Do an inverted fill?</param>
        public void DrawEllipseFill(int centerX, int centerY, int radiusX, int radiusY, Color32 color, bool inverse)
        {
            Rect2i userClipRegion = RB.ClipGet();
            Rect2i bounds = new Rect2i(centerX - radiusX - 1, centerY - radiusY - 1, (radiusX * 2) + 2, (radiusY * 2) + 2);
            var cameraPos = RB.CameraGet();
            bounds.x -= cameraPos.x;
            bounds.y -= cameraPos.y;
            if (!userClipRegion.Intersects(bounds))
            {
                return;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            if (!inverse)
            {
                DrawEllipseFillInternal(centerX, centerY, radiusX, radiusY, color);
            }
            else
            {
                DrawEllipseFillInverseInternal(centerX, centerY, radiusX, radiusY, color);
            }
        }

        /// <summary>
        /// Draw a prepared mesh to screen
        /// </summary>
        /// <param name="mesh">Mesh to draw</param>
        /// <param name="drawPosX">Draw position x</param>
        /// <param name="drawPosY">Draw position y</param>
        /// <param name="rect">Rect to check against clip region</param>
        /// <param name="translateToCamera">Apply camera offset</param>
        /// <param name="texture">Texture to render the mesh with</param>
        public void DrawPreparedMesh(Mesh mesh, int drawPosX, int drawPosY, Rect2i rect, bool translateToCamera, Texture texture)
        {
            if (!RenderEnabled)
            {
                return;
            }

            if (mesh == null)
            {
                return;
            }

            // Early clip test
            var clipRect = new Rect2i(mClipRegion.x0, mClipRegion.y0, mClipRegion.x1 - mClipRegion.x0, mClipRegion.y1 - mClipRegion.y0);

            if (!rect.Intersects(clipRect))
            {
                return;
            }

            Flush(FlushReason.TILEMAP_CHUNK);

            SetShaderValues();
            SetShaderGlobalTint(mCurrentColor);

            Graphics.SetRenderTarget(mCurrentRenderTexture);
            SetCurrentTexture(texture, null, false);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, mCurrentRenderTexture.width, mCurrentRenderTexture.height, 0);

            Vector3 posFinal;
            if (translateToCamera)
            {
                posFinal = new Vector3(-mCameraPos.x, -mCameraPos.y, 0);
            }
            else
            {
                posFinal = Vector3.zero;
            }

            posFinal.x += drawPosX;
            posFinal.y += drawPosY;

            for (int pass = 0; pass < mCurrentDrawMaterial.passCount; pass++)
            {
                mFlushInfo[(int)FlushReason.TILEMAP_CHUNK].Count++;
                mCurrentDrawMaterial.SetPass(pass);

                Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(posFinal, Quaternion.identity, Vector3.one));
            }

            if (CurrentSpriteSheet != null)
            {
                SetCurrentTexture(CurrentSpriteSheet.internalState.texture, CurrentSpriteSheet, false);
            }
            else
            {
                SetCurrentTexture(null, null, false);
            }

            SetShaderGlobalTint(Color.white);

            GL.PopMatrix();
        }

        /// <summary>
        /// Set camera position
        /// </summary>
        /// <param name="pos">Camera position</param>
        public void CameraSet(Vector2i pos)
        {
            mCameraPos = pos;
        }

        /// <summary>
        /// Get camera position
        /// </summary>
        /// <returns>Camera position</returns>
        public Vector2i CameraGet()
        {
            return mCameraPos;
        }

        /// <summary>
        /// Start renderer for the frame
        /// </summary>
        public void StartRender()
        {
            if (!RenderEnabled)
            {
                return;
            }

            ResetMesh();

            mFrontBuffer.Reset();
            ShaderReset();

            mPreviousTexture = null;

            mDebugClipRegions.Clear();

            Onscreen();
            CameraSet(Vector2i.zero);
            AlphaSet(255);
            TintColorSet(new Color32(255, 255, 255, 255));
            RB.ClipReset();

            mCurrentBatchSprite = CurrentSpriteSheet;

            if (CurrentSpriteSheet != null)
            {
                SetCurrentTexture(CurrentSpriteSheet.internalState.texture, CurrentSpriteSheet, true);
            }
            else
            {
                SetCurrentTexture(null, null, true);
            }
        }

        /// <summary>
        /// End renderer for the frame. This also applies some renderer based post-processing effects by drawing
        /// on top of anything else the user may have drawn
        /// </summary>
        public void FrameEnd()
        {
            if (!RenderEnabled)
            {
                return;
            }

            Flush(FlushReason.FRAME_END);

            var drawState = StoreState();

            Onscreen();
            CameraSet(Vector2i.zero);
            AlphaSet(255);
            TintColorSet(new Color32(255, 255, 255, 255));
            RB.ClipReset();
            ShaderReset();

            mRetroBlitAPI.Effects.ApplyRenderTimeEffects();

            mFrontBuffer.FrameEnd(mRetroBlitAPI);

            if (mShowFlushDebug)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int lineCount = 0;
                for (int i = 0; i < mFlushInfo.Length; i++)
                {
                    if (mFlushInfo[i].Count > 0)
                    {
                        if (lineCount > 0)
                        {
                            sb.Append("\n");
                        }

                        sb.Append(mFlushInfo[i].Reason);
                        sb.Append(": ");
                        sb.Append(mFlushInfo[i].Count);

                        mFlushInfo[i].Count = 0;

                        lineCount++;
                    }
                }

                string flushString = sb.ToString();
                var flushStringSize = RB.PrintMeasure(flushString);
                RB.DrawRectFill(new Rect2i(0, 0, flushStringSize.x + 8, flushStringSize.y + 8), mFlushDebugBackgroundColor);
                RB.Print(new Vector2i(4, 4), mFlushDebugFontColor, sb.ToString());
            }

#if EVAL_ONLY
            var evalSize = RB.PrintMeasure("Evaluation Only!");
            var evalRect = new Rect2i(
                RB.DisplaySize.width - evalSize.width - 2,
                RB.DisplaySize.height - evalSize.height - 2,
                evalSize.width + 4,
                evalSize.height + 4);

            RB.DrawRectFill(evalRect, Color.black);
            RB.Print(new Vector2i(evalRect.x + 1, evalRect.y + 1), Color.white, "@w144Evaluation Only!");
#endif

            Flush(FlushReason.FRAME_END);

            RestoreState(drawState);

            mFlushInfo[(int)FlushReason.EFFECT_APPLY].Count++;
        }

        /// <summary>
        /// Draw all clip regions
        /// </summary>
        public void DrawClipRegions()
        {
            for (int i = 0; i < mDebugClipRegions.Count; i++)
            {
                var rect = mDebugClipRegions[i].region;
                DrawRect(rect.x, rect.y, rect.width, rect.height, mDebugClipRegions[i].color, 0, 0);
            }
        }

        /// <summary>
        /// Set clip region
        /// </summary>
        /// <param name="rect">Region</param>
        public void ClipSet(Rect2i rect)
        {
            Rect2i origRect = rect;

            if (rect.width < 0 || rect.height < 0)
            {
                return;
            }

            int x0 = rect.x;
            int y0 = rect.y;
            int x1 = x0 + rect.width - 1;
            int y1 = y0 + rect.height - 1;

            if (x0 != mClipRegion.x0 || x1 != mClipRegion.x1 || y0 != mClipRegion.y0 || y1 != mClipRegion.y1)
            {
                Flush(FlushReason.CLIP_CHANGE);

                mClipRegion.x0 = x0;
                mClipRegion.y0 = y0;
                mClipRegion.x1 = x1;
                mClipRegion.y1 = y1;
            }

            mClip = rect;

            if (mClipDebug)
            {
                DebugClipRegion region;
                region.region = origRect;
                region.color = mClipDebugColor;

                mDebugClipRegions.Add(region);
            }
        }

        /// <summary>
        /// Get clip region
        /// </summary>
        /// <returns>Clip region</returns>
        public Rect2i ClipGet()
        {
            return mClip;
        }

        /// <summary>
        /// Set clip debug state
        /// </summary>
        /// <param name="enabled">Enable/Disabled flag</param>
        /// <param name="color">RGBA color</param>
        public void ClipDebugSet(bool enabled, Color32 color)
        {
            mClipDebug = enabled;
            mClipDebugColor = color;
        }

        /// <summary>
        /// Set flush debug state
        /// </summary>
        /// <param name="enabled">Enabled/Disabled flag</param>
        /// <param name="fontColor">Font RGBA color</param>
        /// <param name="backgroundColor">Background RGBA color</param>
        public void FlashDebugSet(bool enabled, Color32 fontColor, Color32 backgroundColor)
        {
            mFlushDebugBackgroundColor = backgroundColor;
            mFlushDebugFontColor = fontColor;
            mShowFlushDebug = enabled;
        }

        /// <summary>
        /// Set alpha transparency
        /// </summary>
        /// <param name="a">Alpha value</param>
        public void AlphaSet(byte a)
        {
            mCurrentColor.a = a;
        }

        /// <summary>
        /// Get alpha transparency value
        /// </summary>
        /// <returns>Alpha value</returns>
        public byte AlphaGet()
        {
            return mCurrentColor.a;
        }

        /// <summary>
        /// Set Tint color to apply to drawing. Alpha ignored, use AlphaSet
        /// </summary>
        /// <param name="tintColor">Tint color</param>
        public void TintColorSet(Color32 tintColor)
        {
            mCurrentColor.r = tintColor.r;
            mCurrentColor.g = tintColor.g;
            mCurrentColor.b = tintColor.b;
        }

        /// <summary>
        /// Get current tint color
        /// </summary>
        /// <returns>Tint color</returns>
        public Color32 TintColorGet()
        {
            return new Color32(mCurrentColor.r, mCurrentColor.g, mCurrentColor.b, 255);
        }

        /// <summary>
        /// Create render texture
        /// </summary>
        /// <param name="size">Dimensions</param>
        /// <returns>Render texture</returns>
        public RenderTexture RenderTextureCreate(Vector2i size)
        {
            RenderTexture tex = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            if (tex == null)
            {
                return null;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 0;
            tex.antiAliasing = 1;

            tex.autoGenerateMips = false;
            tex.depth = 0;
            tex.useMipMap = false;

            tex.Create();

            return tex;
        }

        /// <summary>
        /// Set the offscreen target by spritesheet target index, also resets the clipping region to cover the new render target
        /// </summary>
        /// <param name="asset">Spritesheet asset</param>
        public void OffscreenTarget(SpriteSheetAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            if (mCurrentRenderTexture != asset.internalState.texture)
            {
                Flush(FlushReason.OFFSCREEN_CHANGE);
            }

            mCurrentRenderTexture = asset.internalState.texture;
            RB.ClipReset();

            mRetroBlitAPI.PixelCamera.SetRenderTarget(mCurrentRenderTexture);

            if (asset.internalState.needsClear)
            {
                Clear(new Color32(0, 0, 0, 0));
                asset.internalState.needsClear = false;
            }
        }

        /// <summary>
        /// Get current render texture
        /// </summary>
        /// <returns>Render texture</returns>
        public Texture CurrentRenderTexture()
        {
            return mCurrentRenderTexture;
        }

        /// <summary>
        /// Set the current render target to the display, also resets the clipping region to cover the display
        /// </summary>
        public void Onscreen()
        {
            if (mCurrentRenderTexture != mFrontBuffer.Texture)
            {
                Flush(FlushReason.OFFSCREEN_CHANGE);
            }

            mCurrentRenderTexture = mFrontBuffer.Texture;
            RB.ClipReset();

            mRetroBlitAPI.PixelCamera.SetRenderTarget(mCurrentRenderTexture);
        }

        /// <summary>
        /// Get scanline effect info
        /// </summary>
        /// <param name="pixelSize">Size of the game pixels in native display pixel size</param>
        /// <param name="offset">Offset in system texture</param>
        /// <param name="length">Length of scanline in system texture</param>
        public void GetScanlineOffsetLength(float pixelSize, out int offset, out int length)
        {
            if (pixelSize < 1)
            {
                pixelSize = 1;
            }

            offset = length = 0;
            offset = (int)pixelSize;
            length = offset;
        }

        /// <summary>
        /// Get PackedSprite for given spriteID
        /// </summary>
        /// <param name="spriteID">PackedSprite ID</param>
        /// <param name="asset">Sprite pack</param>
        /// <returns>PackedSprite</returns>
        public PackedSprite PackedSpriteGet(int spriteID, SpriteSheetAsset asset = null)
        {
            if (asset == null)
            {
                asset = CurrentSpriteSheet;
            }

            if (asset == null)
            {
                return default(PackedSprite);
            }

            var spritePack = asset.internalState.spritePack;
            if (spritePack == null)
            {
                return default(PackedSprite);
            }

            if (!spritePack.sprites.ContainsKey(spriteID))
            {
                return default(PackedSprite);
            }

            return spritePack.sprites[spriteID];
        }

        /// <summary>
        /// Unset spritesheet if it matches the given spritesheet (spritesheet is being unloaded)
        /// </summary>
        /// <param name="asset">Sprite sheet</param>
        public void SpriteSheetUnset(SpriteSheetAsset asset)
        {
            if (CurrentSpriteSheet == asset)
            {
                SpriteSheetSet(EmptySpriteSheet);
            }
        }

        /// <summary>
        /// Set the current sprite sheet to use
        /// </summary>
        /// <param name="asset">Sprite sheet</param>
        public void SpriteSheetSet(SpriteSheetAsset asset)
        {
            if (asset == null || asset.internalState.texture == null)
            {
                asset = EmptySpriteSheet;
            }

            // Flush if changing textures
            if (mCurrentBatchSprite != asset)
            {
                Flush(FlushReason.SPRITESHEET_CHANGE);
            }

            CurrentSpriteSheet = asset;
            if (CurrentSpriteSheet.internalState.texture != null)
            {
                SetCurrentTexture(CurrentSpriteSheet.internalState.texture, CurrentSpriteSheet, false);
            }
            else
            {
                SetCurrentTexture(null, null, false);
            }

            CurrentSpriteSheetTextureWidth = CurrentSpriteSheet.internalState.textureWidth;
            CurrentSpriteSheetTextureHeight = CurrentSpriteSheet.internalState.textureHeight;
#if MESH_UPLOAD_2019_3
            CurrentSpriteSheetTexelWidth = CurrentSpriteSheet.internalState.texelWidth;
            CurrentSpriteSheetTexelHeight = CurrentSpriteSheet.internalState.texelHeight;
#endif
        }

        /// <summary>
        /// Save the current effects state at this point, and move to next front buffer.
        /// </summary>
        public void EffectApplyNow()
        {
            var drawState = StoreState();

            bool wasOnFrontBuffer = false;
            if (mCurrentRenderTexture == mFrontBuffer.Texture)
            {
                wasOnFrontBuffer = true;
            }

            Onscreen();
            CameraSet(Vector2i.zero);
            AlphaSet(255);
            TintColorSet(new Color32(255, 255, 255, 255));
            RB.ClipReset();
            ShaderReset();

            mRetroBlitAPI.Effects.ApplyRenderTimeEffects();

            Flush(FlushReason.EFFECT_APPLY);

            mFrontBuffer.NextBuffer(mRetroBlitAPI);

            RestoreState(drawState);

            if (wasOnFrontBuffer)
            {
                mCurrentRenderTexture = mFrontBuffer.Texture;
            }

            ClearTransparent(mFrontBuffer.Texture);
        }

        /// <summary>
        /// Maximum radius of a circle
        /// </summary>
        /// <param name="center">Center of circle</param>
        /// <returns>Max radius</returns>
        public int MaxCircleRadiusForCenter(Vector2i center)
        {
            int maxEdgeDistance = 0;
            if (center.x > maxEdgeDistance)
            {
                maxEdgeDistance = center.x;
            }

            if (center.y > maxEdgeDistance)
            {
                maxEdgeDistance = center.y;
            }

            if (RB.DisplaySize.width - center.x > maxEdgeDistance)
            {
                maxEdgeDistance = RB.DisplaySize.width - center.x;
            }

            if (RB.DisplaySize.height - center.y > maxEdgeDistance)
            {
                maxEdgeDistance = RB.DisplaySize.height - center.y;
            }

            int maxRadius = (int)Mathf.Sqrt(2 * maxEdgeDistance * maxEdgeDistance) + 1;

            return maxRadius;
        }

        /// <summary>
        /// Get current front buffer
        /// </summary>
        /// <returns>Front buffer</returns>
        public FrontBuffer GetFrontBuffer()
        {
            return mFrontBuffer;
        }

        /// <summary>
        /// Clear the given RenderTexture to a transparent color
        /// </summary>
        /// <param name="texture">Texture to clear</param>
        public void ClearTransparent(RenderTexture texture)
        {
            Color32 clearColor = new Color32(0, 0, 0, 0);

            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = texture;
            GL.Clear(true, true, clearColor);
            UnityEngine.RenderTexture.active = rt;

            ResetMesh();
        }

        /// <summary>
        /// Set the current shader
        /// </summary>
        /// <param name="shader">Shader</param>
        public void ShaderSet(ShaderAsset shader)
        {
            if (shader == null)
            {
                ShaderReset();
                return;
            }

            SetCurrentMaterial(shader.shader);
            mCurrentShader = shader;

            if (CurrentSpriteSheet == null)
            {
                SetCurrentTexture(null, null, true);
            }
            else
            {
                SetCurrentTexture(CurrentSpriteSheet.internalState.texture, CurrentSpriteSheet, true);
            }
        }

        /// <summary>
        /// Apply the shader now, by flushing
        /// </summary>
        public void ShaderApplyNow()
        {
            Flush(FlushReason.SHADER_APPLY);
        }

        /// <summary>
        /// Reset the shader to default*
        /// </summary>
        public void ShaderReset()
        {
            SetCurrentMaterial(mDrawMaterialRGB);

            Flush(FlushReason.SHADER_RESET);

            mCurrentShader = null;
        }

        /// <summary>
        /// Get the shader Material
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>Material</returns>
        public Material ShaderGetMaterial(ShaderAsset shader)
        {
            if (shader == null)
            {
                return null;
            }

            return shader.shader;
        }

        /// <summary>
        /// Get the shader parameters
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>Shader parameters</returns>
        public RetroBlitShader ShaderParameters(ShaderAsset shader)
        {
            if (shader == null)
            {
                return null;
            }

            return shader.shader;
        }

        /// <summary>
        /// Get the current display surface
        /// </summary>
        /// <returns>Display surface</returns>
        public Texture DisplaySurfaceGet()
        {
            return mCurrentRenderTexture;
        }

        /// <summary>
        /// Currently used mesh storage
        /// </summary>
        /// <returns>MeshStorage</returns>
        public MeshStorage CurrentMeshStorage()
        {
            return mMeshStorage;
        }

        /// <summary>
        /// Flush vertex buffer to screen
        /// </summary>
        /// <param name="reason">Reason for flushing</param>
        public void Flush(FlushReason reason)
        {
            if (!RenderEnabled)
            {
                ResetMesh();
                return;
            }

            if (mMeshStorage.CurrentIndex == 0)
            {
                // Nothing to flush
                mCurrentBatchSprite = CurrentSpriteSheet;
                return;
            }

            SetShaderValues();

            if (mMeshStorage.CurrentVertex > 0)
            {
#if MESH_UPLOAD_2019_3
                var mesh = mMeshStorage.UploadMesh();
#else
                var mesh = mMeshStorage.ReduceAndUploadMesh();
#endif
                if (mesh == null)
                {
                    // Could not get mesh, will not be able to draw, drop vertices instead
                    ResetMesh();
                    mCurrentBatchSprite = CurrentSpriteSheet;
                    return;
                }

                Graphics.SetRenderTarget(mCurrentRenderTexture);

                GL.PushMatrix();
                GL.LoadPixelMatrix(0, mCurrentRenderTexture.width, mCurrentRenderTexture.height, 0);

                // If we're using a custom shader then apply chosen filters to all offscreen surfaces
                if (mCurrentShader != null)
                {
                    RetroBlitShader shader = mCurrentShader.shader;
                    var filters = shader.GetOffscreenFilters();
                    for (int i = 0; i < filters.Count; i++)
                    {
                        RenderTexture tex = null;
                        if (filters[i].spriteSheet != null)
                        {
                            tex = filters[i].spriteSheet.internalState.texture;
                        }

                        if (tex != null)
                        {
                            tex.filterMode = filters[i].filterMode;
                        }
                    }
                }

                for (int pass = 0; pass < mCurrentDrawMaterial.passCount; pass++)
                {
                    mFlushInfo[(int)reason].Count++;
                    if (mCurrentDrawMaterial.SetPass(pass))
                    {
#if MESH_UPLOAD_2019_3
                        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
#else
                        Graphics.DrawMeshNow(mesh.Mesh, Matrix4x4.identity);
#endif
                    }
                }

                // Revert any filter changes
                if (mCurrentShader != null)
                {
                    RetroBlitShader shader = mCurrentShader.shader;
                    var filters = shader.GetOffscreenFilters();
                    for (int i = 0; i < filters.Count; i++)
                    {
                        RenderTexture tex = null;
                        if (filters[i].spriteSheet != null)
                        {
                            tex = filters[i].spriteSheet.internalState.texture;
                        }

                        if (tex != null)
                        {
                            tex.filterMode = FilterMode.Point;
                        }
                    }
                }

                GL.PopMatrix();

                ResetMesh();
            }

            mCurrentBatchSprite = CurrentSpriteSheet;
        }

        /// <summary>
        /// Game shutting down, clean up any used unmanaged resources
        /// </summary>
        public void CleanUp()
        {
            mMeshStorage.CleanUp();
        }

        /// <summary>
        /// Draw a straight orthogonal line
        /// </summary>
        /// <param name="p0X">Start point x</param>
        /// <param name="p0Y">Start point y</param>
        /// <param name="p1X">End point x</param>
        /// <param name="p1Y">End point y</param>
        /// <param name="color">RGB color</param>
        private void DrawOrthoLine(int p0X, int p0Y, int p1X, int p1Y, Color32 color)
        {
            // Don't need to check for CheckFlush here, all callers of this API should check instead

            // Make sure p0 is before p1
            if (p0X > p1X || p0Y > p1Y)
            {
                int tpX = p0X;
                int tpY = p0Y;
                p0X = p1X;
                p0Y = p1Y;
                p1X = tpX;
                p1Y = tpY;
            }

            p0X -= mCameraPos.x;
            p0Y -= mCameraPos.y;
            p1X -= mCameraPos.x;
            p1Y -= mCameraPos.y;

            if ((p0X < mClipRegion.x0 && p1X < mClipRegion.x0) || (p0X > mClipRegion.x1 && p1X > mClipRegion.x1))
            {
                return;
            }

            if ((p0Y < mClipRegion.y0 && p1Y < mClipRegion.y0) || (p0Y > mClipRegion.y1 && p1Y > mClipRegion.y1))
            {
                return;
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            // Draw the line just one triangle, make sure it passes through the middle of the pixel
            // Horizontal
            if (p0Y == p1Y)
            {
                v.pos_x = p0X - 0.1f;
                v.pos_y = p0Y - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = p1X + 1.1f;
                v.pos_y = p1Y + 0.5f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = p0X - 0.1f;
                v.pos_y = p0Y + 1.1f;

                mMeshStorage.Verticies[i++] = v;
            }
            else
            {
                v.pos_x = p0X - 0.1f;
                v.pos_y = p0Y - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = p0X + 1.1f;
                v.pos_y = p0Y - 0.1f;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = p1X + 0.5f;
                v.pos_y = p1Y + 1.1f;

                mMeshStorage.Verticies[i++] = v;
            }

            mMeshStorage.Indices[j++] = (ushort)(i - 3);
            mMeshStorage.Indices[j++] = (ushort)(i - 2);
            mMeshStorage.Indices[j++] = (ushort)(i - 1);
#else
            // Draw the line just one triangle, make sure it passes through the middle of the pixel
            // Horizontal
            if (p0Y == p1Y)
            {
                mMeshStorage.Verticies[i + 0].x = p0X - 0.1f;
                mMeshStorage.Verticies[i + 0].y = p0Y - 0.1f;
                mMeshStorage.Verticies[i + 0].z = 0;

                mMeshStorage.Verticies[i + 1].x = p1X + 1.1f;
                mMeshStorage.Verticies[i + 1].y = p1Y + 0.5f;
                mMeshStorage.Verticies[i + 1].z = 0;

                mMeshStorage.Verticies[i + 2].x = p0X - 0.1f;
                mMeshStorage.Verticies[i + 2].y = p0Y + 1.1f;
                mMeshStorage.Verticies[i + 2].z = 0;

                mMeshStorage.Colors[i + 0] = color;
                mMeshStorage.Colors[i + 1] = color;
                mMeshStorage.Colors[i + 2] = color;
            }
            else
            {
                mMeshStorage.Verticies[i + 0].x = p0X - 0.1f;
                mMeshStorage.Verticies[i + 0].y = p0Y - 0.1f;
                mMeshStorage.Verticies[i + 0].z = 0;

                mMeshStorage.Verticies[i + 1].x = p0X + 1.1f;
                mMeshStorage.Verticies[i + 1].y = p0Y - 0.1f;
                mMeshStorage.Verticies[i + 1].z = 0;

                mMeshStorage.Verticies[i + 2].x = p1X + 0.5f;
                mMeshStorage.Verticies[i + 2].y = p1Y + 1.1f;
                mMeshStorage.Verticies[i + 2].z = 0;

                mMeshStorage.Colors[i + 0] = color;
                mMeshStorage.Colors[i + 1] = color;
                mMeshStorage.Colors[i + 2] = color;
            }

            i += 3;

            mMeshStorage.Indices[j++] = i - 3;
            mMeshStorage.Indices[j++] = i - 2;
            mMeshStorage.Indices[j++] = i - 1;
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        private DrawState StoreState()
        {
            DrawState state = new DrawState();

            state.Alpha = AlphaGet();
            state.CameraPos = CameraGet();
            state.Clip = ClipGet();
            state.CurrentRenderTexture = mCurrentRenderTexture;
            state.TintColor = TintColorGet();
            state.CurrentMaterial = mCurrentDrawMaterial;

            return state;
        }

        private void RestoreState(DrawState state)
        {
            AlphaSet(state.Alpha);
            CameraSet(state.CameraPos);
            ClipSet(state.Clip);
            mCurrentRenderTexture = state.CurrentRenderTexture;
            TintColorSet(state.TintColor);
            SetCurrentMaterial(state.CurrentMaterial);
        }

        private void DrawEllipseInternal(int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            int horizontalCount;
            bool rotated;
            int count;

            if (radiusX < 128 && radiusY < 128)
            {
                count = GetEllipseLines32(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }
            else
            {
                count = GetEllipseLines64(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }

            DrawEllipseLineList(centerX, centerY, count, color, horizontalCount, rotated);
        }

        private void DrawEllipseFillInternal(int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            int horizontalCount;
            bool rotated;
            int count;

            // If the ellipse excedees maximum size then draw a filled rect instead
            if (radiusX > MAX_ELLIPSE_RADIUS || radiusY > MAX_ELLIPSE_RADIUS)
            {
                DrawRectFill(centerX - radiusX, centerY - radiusY, (radiusX * 2) + 1, (radiusY * 2) + 1, color);
                return;
            }

            if (radiusX < 128 && radiusY < 128)
            {
                count = GetEllipseLines32(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }
            else
            {
                count = GetEllipseLines64(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }

            DrawEllipseLineListFilled(centerX, centerY, count, color, horizontalCount, rotated);
        }

        private void DrawEllipseFillInverseInternal(int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            int horizontalCount;
            bool rotated;
            int count;

            if (radiusX < 128 && radiusY < 128)
            {
                count = GetEllipseLines32(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }
            else
            {
                count = GetEllipseLines64(centerX, centerY, radiusX, radiusY, out horizontalCount, out rotated);
            }

            DrawEllipseLineListInverseFilled(centerX, centerY, radiusX, radiusY, count, color, horizontalCount, rotated);
        }

        private int GetEllipseLines32(int centerX, int centerY, int radiusX, int radiusY, out int horizontalCount, out bool rotated)
        {
            horizontalCount = 0;
            rotated = false;

            int rx, ry;

            if (radiusX > radiusY)
            {
                rx = radiusY;
                ry = radiusX;
                rotated = true;
            }
            else
            {
                rx = radiusX;
                ry = radiusY;
            }

            if (rx < 0 || ry < 0)
            {
                return 0;
            }

            if (rx > MAX_ELLIPSE_RADIUS || ry > MAX_ELLIPSE_RADIUS)
            {
                return 0;
            }

            int radiusXSq = rx * rx;
            int radiusYSq = ry * ry;
            int x = 0, y = ry;
            int p;
            int px = 0;
            int py = 2 * radiusXSq * y;

            int i = 0;
            int lastY = y;
            mPoints[i].x = x;
            mPoints[i].y = y;
            i++;

            p = radiusYSq - (radiusXSq * ry) + (radiusXSq / 4);

            while (px < py)
            {
                x++;
                px = px + (2 * radiusYSq);

                if (p < 0)
                {
                    p = p + radiusYSq + px;
                }
                else
                {
                    y--;
                    py = py - (2 * radiusXSq);
                    p = p + radiusYSq + px - py;
                }

                if (y != lastY)
                {
                    // Finish off last line
                    mPoints[i].x = x - 1;
                    mPoints[i].y = lastY;
                    i++;

                    // Start new line
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;
                }

                lastY = y;
            }

            // Finish off last line
            if (i % 2 == 1)
            {
                mPoints[i].x = x;
                mPoints[i].y = mPoints[i - 1].y;
                i++;
            }

            horizontalCount = i;
            int lastX = 0;
            bool firstLoop = true;

            p = ((radiusYSq * ((x + 1) * x)) + 1) + (radiusXSq * (y - 1) * (y - 1)) - (radiusXSq * radiusYSq);

            while (y > 0)
            {
                y--;
                py = py - (2 * radiusXSq);
                if (p > 0)
                {
                    p = p + radiusXSq - py;
                }
                else
                {
                    x++;
                    px = px + (2 * radiusYSq);
                    p = p + radiusXSq - py + px;
                }

                if (firstLoop)
                {
                    lastX = x;
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;

                    firstLoop = false;
                }

                if (x != lastX)
                {
                    // Finish off last line
                    mPoints[i].x = lastX;
                    mPoints[i].y = y + 1;
                    i++;

                    // Start new line
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;
                }

                lastX = x;
            }

            // Finish off last line
            if (i % 2 == 1)
            {
                mPoints[i].x = mPoints[i - 1].x;
                mPoints[i].y = y;
                i++;
            }

            return i;
        }

        private int GetEllipseLines64(int centerX, int centerY, int radiusX, int radiusY, out int horizontalCount, out bool rotated)
        {
            horizontalCount = 0;
            rotated = false;

            int rx, ry;

            if (radiusX > radiusY)
            {
                rx = radiusY;
                ry = radiusX;
                rotated = true;
            }
            else
            {
                rx = radiusX;
                ry = radiusY;
            }

            if (rx < 0 || ry < 0)
            {
                return 0;
            }

            if (rx > MAX_ELLIPSE_RADIUS || ry > MAX_ELLIPSE_RADIUS)
            {
                return 0;
            }

            long radiusXSq = rx * rx;
            long radiusYSq = ry * ry;
            int x = 0, y = ry;
            long p;
            long px = 0;
            long py = 2 * radiusXSq * y;

            int i = 0;
            int lastY = y;
            mPoints[i].x = x;
            mPoints[i].y = y;
            i++;

            p = radiusYSq - (radiusXSq * ry) + (radiusXSq / 4);

            while (px < py)
            {
                x++;
                px = px + (2 * radiusYSq);

                if (p < 0)
                {
                    p = p + radiusYSq + px;
                }
                else
                {
                    y--;
                    py = py - (2 * radiusXSq);
                    p = p + radiusYSq + px - py;
                }

                if (y != lastY)
                {
                    // Finish off last line
                    mPoints[i].x = x - 1;
                    mPoints[i].y = lastY;
                    i++;

                    // Start new line
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;
                }

                lastY = y;
            }

            // Finish off last line
            if (i % 2 == 1)
            {
                mPoints[i].x = x;
                mPoints[i].y = mPoints[i - 1].y;
                i++;
            }

            horizontalCount = i;
            int lastX = 0;
            bool firstLoop = true;

            p = ((radiusYSq * ((x + 1) * x)) + 1) + (radiusXSq * (y - 1) * (y - 1)) - (radiusXSq * radiusYSq);

            while (y > 0)
            {
                y--;
                py = py - (2 * radiusXSq);
                if (p > 0)
                {
                    p = p + radiusXSq - py;
                }
                else
                {
                    x++;
                    px = px + (2 * radiusYSq);
                    p = p + radiusXSq - py + px;
                }

                if (firstLoop)
                {
                    lastX = x;
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;

                    firstLoop = false;
                }

                if (x != lastX)
                {
                    // Finish off last line
                    mPoints[i].x = lastX;
                    mPoints[i].y = y + 1;
                    i++;

                    // Start new line
                    mPoints[i].x = x;
                    mPoints[i].y = y;
                    i++;
                }

                lastX = x;
            }

            // Finish off last line
            if (i % 2 == 1)
            {
                mPoints[i].x = mPoints[i - 1].x;
                mPoints[i].y = y;
                i++;
            }

            return i;
        }

        private void DrawLineStrip(Vector2i[] points, int pointCount, Color32 color)
        {
            if (pointCount < 2 || pointCount > points.Length)
            {
                return;
            }

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6 * (pointCount + 1))
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6 * (pointCount + 1))
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int i = 0;
            for (; i < pointCount - 2; i++)
            {
                if (points[i] != points[i + 1])
                {
                    DrawLineWithCaps(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, true, false);
                }
            }

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6)
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6)
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            i++;
            if (points[i].x != points[0].x || points[i].y != points[0].y)
            {
                DrawLineWithCaps(points[i - 1].x, points[i - 1].y, points[i].x, points[i].y, color, true, true);
            }
            else
            {
                DrawLineWithCaps(points[i - 1].x, points[i - 1].y, points[i].x, points[i].y, color, true, false);
            }
        }

        private void DrawLineList(Vector2i[] points, int pointCount, Color32 color)
        {
            if (pointCount < 2 || pointCount > points.Length)
            {
                return;
            }

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 6 * (pointCount + 1))
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 6 * (pointCount + 1))
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int i = 0;
            for (; i < pointCount - 1; i += 2)
            {
                DrawLineWithCaps(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, true, false);
            }
        }

        private void DrawEllipseLineList(int cx, int cy, int pointCount, Color32 color, int horizontalLastIndex, bool rotated)
        {
            if (pointCount < 2)
            {
                return;
            }

            if (pointCount < 2 || pointCount > mPoints.Length)
            {
                return;
            }

            Color32 previousTintColor = TintColorGet();
            TintColorSet(color);

            byte previousAlpha = AlphaGet();
            AlphaSet(color.a);

            Vector2i p1, p2;

            if (!rotated)
            {
                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                // Horizontal sides
                p1 = mPoints[0];
                p2 = mPoints[1];
                DrawHorizontalLineNoChecks(cx + p1.x - p2.x, cx + p2.x, cy + p2.y);
                DrawHorizontalLineNoChecks(cx + p1.x - p2.x, cx + p2.x, cy - p2.y);

                int i = 2;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawHorizontalLineNoChecks(cx + p1.x, cx + p2.x, cy + p2.y);
                        DrawHorizontalLineNoChecks(cx - p2.x, cx - p1.x, cy + p2.y);

                        // If there is a line that ends at y == 0, then we want to shorten
                        // the reflect so we don't get a repeat pixel at y == 0 from the horizontal
                        // line above. If the line is a single pixel then skip it altogether
                        if (p1.y == 0)
                        {
                            continue;
                        }
                        else if (p2.y == 0)
                        {
                            p2.y++;
                        }

                        DrawHorizontalLineNoChecks(cx + p1.x, cx + p2.x, cy - p2.y);
                        DrawHorizontalLineNoChecks(cx - p2.x, cx - p1.x, cy - p2.y);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawVerticalLineNoChecks(cx + p2.x, cy + p2.y, cy + p1.y);
                        DrawVerticalLineNoChecks(cx + p2.x, cy - p1.y, cy - p2.y);

                        // If there is a line that ends at y == 0, then we want to shorten
                        // the reflect so we don't get a repeat pixel at y == 0 from the horizontal
                        // line above. If the line is a single pixel then skip it altogether
                        if (p1.x == 0)
                        {
                            continue;
                        }
                        else if (p2.x == 0)
                        {
                            p2.x++;
                        }

                        DrawVerticalLineNoChecks(cx - p2.x, cy + p2.y, cy + p1.y);
                        DrawVerticalLineNoChecks(cx - p2.x, cy - p1.y, cy - p2.y);
                    }
                }

                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                // Vertical sides
                if (i <= pointCount - 2)
                {
                    p1 = mPoints[i];
                    p2 = mPoints[i + 1];

                    DrawVerticalLineNoChecks(cx + p1.x, cy - p1.y, cy + p2.y + p1.y);
                    DrawVerticalLineNoChecks(cx - p1.x, cy - p1.y, cy + p2.y + p1.y);
                }
            }
            else
            {
                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                // Horizontal sides
                p1 = mPoints[0];
                p2 = mPoints[1];
                DrawVerticalLineNoChecks(cx + p2.y, cy + p1.x - p2.x, cy + p2.x);
                DrawVerticalLineNoChecks(cx - p2.y, cy + p1.x - p2.x, cy + p2.x);

                int i = 2;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawVerticalLineNoChecks(cx + p1.y, cy + p1.x, cy + p2.x);
                        DrawVerticalLineNoChecks(cx - p1.y, cy + p1.x, cy + p2.x);

                        // If there is a line that ends at y == 0, then we want to shorten
                        // the reflect so we don't get a repeat pixel at y == 0 from the horizontal
                        // line above. If the line is a single pixel then skip it altogether
                        if (p1.x == 0)
                        {
                            continue;
                        }
                        else if (p2.x == 0)
                        {
                            p2.x++;
                        }

                        DrawVerticalLineNoChecks(cx + p1.y, cy - p2.x, cy - p1.x);
                        DrawVerticalLineNoChecks(cx - p1.y, cy - p2.x, cy - p1.x);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawHorizontalLineNoChecks(cx + p2.y, cx + p1.y, cy + p1.x);
                        DrawHorizontalLineNoChecks(cx + p2.y, cx + p1.y, cy - p1.x);

                        // If there is a line that ends at y == 0, then we want to shorten
                        // the reflect so we don't get a repeat pixel at y == 0 from the horizontal
                        // line above. If the line is a single pixel then skip it altogether
                        if (p1.y == 0)
                        {
                            continue;
                        }
                        else if (p2.y == 0)
                        {
                            p2.y++;
                        }

                        DrawHorizontalLineNoChecks(cx - p1.y, cx - p2.y, cy + p1.x);
                        DrawHorizontalLineNoChecks(cx - p1.y, cx - p2.y, cy - p1.x);
                    }
                }

                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                // Vertical sides
                if (i <= pointCount - 2)
                {
                    p1 = mPoints[i];
                    p2 = mPoints[i + 1];

                    DrawHorizontalLineNoChecks(cx - p1.y, cx + p2.y + p1.y, cy + p1.x);
                    DrawHorizontalLineNoChecks(cx - p1.y, cx + p2.y + p1.y, cy - p1.x);
                }
            }

            TintColorSet(previousTintColor);
            AlphaSet(previousAlpha);
        }

        private void DrawEllipseLineListFilled(int cx, int cy, int pointCount, Color32 color, int horizontalLastIndex, bool rotated)
        {
            if (pointCount < 2)
            {
                return;
            }

            if (pointCount < 2 || pointCount > mPoints.Length)
            {
                return;
            }

            Color32 previousTintColor = TintColorGet();
            TintColorSet(color);

            byte previousAlpha = AlphaGet();
            AlphaSet(color.a);

            Vector2i p1, p2;

            if (!rotated)
            {
                // Horizontal rect
                int i = 0;
                int w, h;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawHorizontalLineNoChecks(cx - p2.x, cx + p2.x, cy - p1.y);
                        DrawHorizontalLineNoChecks(cx - p2.x, cx + p2.x, cy + p1.y);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        w = (p1.x * 2) + 1;
                        h = p1.y - p2.y + 1;

                        if (h > 1)
                        {
                            DrawRectFillNoChecks(cx - p1.x, cy - p1.y, cx + p1.x + 1, cy - p2.y + 1);
                            DrawRectFillNoChecks(cx - p1.x, cy + p2.y, cx + p1.x + 1, cy + p1.y + 1);
                        }
                        else
                        {
                            DrawHorizontalLineNoChecks(cx - p1.x, cx - p1.x + w - 1, cy - p1.y);
                            DrawHorizontalLineNoChecks(cx - p1.x, cx - p1.x + w - 1, cy + p1.y);
                        }
                    }
                }

                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                // Vertical sides
                if (i <= pointCount - 2)
                {
                    p1 = mPoints[i];
                    p2 = mPoints[i + 1];

                    DrawRectFillNoChecks(cx - p1.x, cy - p1.y, cx + p1.x + 1, cy + p1.y + 1);
                }
            }
            else
            {
                int i = 0;
                int w, h;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawVerticalLineNoChecks(cx + p1.y, cy - p2.x, cy + p2.x);
                        DrawVerticalLineNoChecks(cx - p1.y, cy - p2.x, cy + p2.x);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 4 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        w = p1.y - p2.y + 1;
                        h = (p2.x * 2) + 1;

                        if (w > 1)
                        {
                            DrawRectFillNoChecks(cx - p1.y, cy - p2.x, cx - p2.y + 1, cy + p2.x + 1);
                            DrawRectFillNoChecks(cx + p2.y, cy - p2.x, cx + p1.y + 1, cy + p2.x + 1);
                        }
                        else
                        {
                            DrawVerticalLineNoChecks(cx - p1.y, cy - p2.x, cy - p2.x + h - 1);
                            DrawVerticalLineNoChecks(cx + p1.y, cy - p2.x, cy - p2.x + h - 1);
                        }
                    }
                }

                // Check flush
#if MESH_UPLOAD_2019_3
                if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 3 * 3)
#else
                if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 3 * 3)
#endif
                {
                    Flush(FlushReason.BATCH_FULL);
                }

                if (i <= pointCount - 2)
                {
                    p1 = mPoints[i];
                    p2 = mPoints[i + 1];

                    DrawRectFillNoChecks(cx - p1.y, cy - p2.x, cx + p1.y + 1, cy + p2.x + 1);
                }
            }

            TintColorSet(previousTintColor);
            AlphaSet(previousAlpha);
        }

        private void DrawEllipseLineListInverseFilled(int cx, int cy, int radiusX, int radiusY, int pointCount, Color32 color, int horizontalLastIndex, bool rotated)
        {
            if (pointCount < 2)
            {
                return;
            }

            if (pointCount < 2 || pointCount > mPoints.Length)
            {
                return;
            }

            Color32 previousTintColor = TintColorGet();
            TintColorSet(color);

            byte previousAlpha = AlphaGet();
            AlphaSet(color.a);

            Vector2i p1, p2;

            if (!rotated)
            {
                // Horizontal rect
                int i = 0;
                int w, h;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawHorizontalLineNoChecks(cx - radiusX, cx - p2.x - 1, cy - p1.y);
                        DrawHorizontalLineNoChecks(cx + p2.x + 1, cx + radiusX, cy - p1.y);

                        DrawHorizontalLineNoChecks(cx - radiusX, cx - p2.x - 1, cy + p1.y);
                        DrawHorizontalLineNoChecks(cx + p2.x + 1, cx + radiusX, cy + p1.y);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        w = (p1.x * 2) + 1;
                        h = p1.y - p2.y + 1;

                        if (h > 1)
                        {
                            DrawRectFillNoChecks(cx - radiusX, cy - p1.y, cx - p1.x, cy - p2.y + 1);
                            DrawRectFillNoChecks(cx + p1.x + 1, cy - p1.y, cx + radiusX + 1, cy - p2.y + 1);

                            DrawRectFillNoChecks(cx - radiusX, cy + p2.y, cx - p1.x, cy + p1.y + 1);
                            DrawRectFillNoChecks(cx + p1.x + 1, cy + p2.y, cx + radiusX + 1, cy + p1.y + 1);
                        }
                        else
                        {
                            DrawHorizontalLineNoChecks(cx - radiusX, cx - p1.x - 1, cy - p1.y);
                            DrawHorizontalLineNoChecks(cx - p1.x + w, cx + radiusX, cy - p1.y);

                            DrawHorizontalLineNoChecks(cx - radiusX, cx - p1.x - 1, cy + p1.y);
                            DrawHorizontalLineNoChecks(cx - p1.x + w, cx + radiusX, cy + p1.y);
                        }
                    }
                }
            }
            else
            {
                int i = 0;
                int w, h;
                int chunkEnd;
                int chunkSize = 128;

                while (i < horizontalLastIndex)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > horizontalLastIndex)
                    {
                        chunkEnd = horizontalLastIndex;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        DrawVerticalLineNoChecks(cx + p1.y, cy - radiusY, cy - p2.x - 1);
                        DrawVerticalLineNoChecks(cx - p1.y, cy + p2.x + 1, cy + radiusY);

                        DrawVerticalLineNoChecks(cx - p1.y, cy - radiusY, cy - p2.x - 1);
                        DrawVerticalLineNoChecks(cx + p1.y, cy + p2.x + 1, cy + radiusY);
                    }
                }

                while (i < pointCount - 2)
                {
                    chunkEnd = i + chunkSize;
                    if (chunkEnd > pointCount - 2)
                    {
                        chunkEnd = pointCount - 2;
                    }

#if MESH_UPLOAD_2019_3
                    if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#else
                    if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < ((chunkEnd - i + 1) * 8 * 3) / 2)
#endif
                    {
                        Flush(FlushReason.BATCH_FULL);
                    }

                    for (; i < chunkEnd; i += 2)
                    {
                        p1 = mPoints[i];
                        p2 = mPoints[i + 1];

                        w = p1.y - p2.y + 1;
                        h = (p2.x * 2) + 1;

                        if (w > 1)
                        {
                            DrawRectFillNoChecks(cx - p1.y, cy - radiusY, cx - p2.y + 1, cy - p2.x);
                            DrawRectFillNoChecks(cx - p1.y, cy + p2.x + 1, cx - p2.y + 1, cy + radiusY + 1);

                            DrawRectFillNoChecks(cx + p2.y, cy - radiusY, cx + p1.y + 1, cy - p2.x);
                            DrawRectFillNoChecks(cx + p2.y, cy + p2.x + 1, cx + p1.y + 1, cy + radiusY + 1);
                        }
                        else
                        {
                            DrawVerticalLineNoChecks(cx - p1.y, cy - radiusY, cy - p2.x - 1);
                            DrawVerticalLineNoChecks(cx - p1.y, cy - p2.x + h, cy + radiusY);

                            DrawVerticalLineNoChecks(cx + p1.y, cy - radiusY, cy - p2.x - 1);
                            DrawVerticalLineNoChecks(cx + p1.y, cy - p2.x + h, cy + radiusY);
                        }
                    }
                }
            }

            TintColorSet(previousTintColor);
            AlphaSet(previousAlpha);
        }

        private void DrawConvexPolygon(Vector2i center, Vector3[] points, int pointCount, Color32 color)
        {
            if (pointCount < 2 || pointCount > points.Length)
            {
                return;
            }

            // Fast color multiply
            color.r = (byte)((color.r * mCurrentColor.r) / 255);
            color.g = (byte)((color.g * mCurrentColor.g) / 255);
            color.b = (byte)((color.b * mCurrentColor.b) / 255);
            color.a = (byte)((color.a * mCurrentColor.a) / 255);

            // Check flush
#if MESH_UPLOAD_2019_3
            if (MAX_VERTEX_PER_MESH - mMeshStorage.CurrentVertex < 1 + (pointCount * 2))
#else
            if (mMeshStorage.MaxVertices - mMeshStorage.CurrentVertex < 1 + (pointCount * 2))
#endif
            {
                Flush(FlushReason.BATCH_FULL);
            }

            int i = mMeshStorage.CurrentVertex;
            int j = mMeshStorage.CurrentIndex;
            int ci = i;

#if MESH_UPLOAD_2019_3
            Vertex v;

            // Only have to set color once, will reuse for all vertices
            v.color_r = color.r;
            v.color_g = color.g;
            v.color_b = color.b;
            v.color_a = color.a;
            v.pos_z = 0;

            v.u = 0;
            v.v = 0;

            v.pos_x = center.x;
            v.pos_y = center.y;

            mMeshStorage.Verticies[i++] = v;

            for (int k = 0; k < pointCount - 1; k++)
            {
                float x1 = points[k].x;
                float y1 = points[k].y;
                float x2 = points[k + 1].x;
                float y2 = points[k + 1].y;

                v.pos_x = x1;
                v.pos_y = y1;

                mMeshStorage.Verticies[i++] = v;

                v.pos_x = x2;
                v.pos_y = y2;

                mMeshStorage.Verticies[i++] = v;

                mMeshStorage.Indices[j++] = ci;
                mMeshStorage.Indices[j++] = i - 1;
                mMeshStorage.Indices[j++] = i - 2;
            }
#else
            mMeshStorage.Verticies[i] = new Vector3(center.x, center.y, 0);
            mMeshStorage.Colors[i] = color;

            i++;

            for (int k = 0; k < pointCount - 1; k++)
            {
                float x1 = points[k].x;
                float y1 = points[k].y;
                float x2 = points[k + 1].x;
                float y2 = points[k + 1].y;

                mMeshStorage.Verticies[i + 0] = new Vector3(x1, y1, 0);
                mMeshStorage.Verticies[i + 1] = new Vector3(x2, y2, 0);

                mMeshStorage.Colors[i + 0] = color;
                mMeshStorage.Colors[i + 1] = color;

                i += 2;

                mMeshStorage.Indices[j++] = ci;
                mMeshStorage.Indices[j++] = i - 1;
                mMeshStorage.Indices[j++] = i - 2;
            }
#endif

            mMeshStorage.CurrentVertex = i;
            mMeshStorage.CurrentIndex = j;
        }

        private void SetCurrentMaterial(Material material)
        {
            if (material == mCurrentDrawMaterial)
            {
                return;
            }

            Flush(FlushReason.SET_MATERIAL);

            mCurrentDrawMaterial = material;
            SetShaderValues();
        }

        private void SetCurrentTexture(Texture texture, SpriteSheetAsset spriteSheet, bool force)
        {
            if (mPreviousTexture == texture && !force)
            {
                return;
            }

            mPreviousTexture = texture;

            if (texture == null)
            {
                return;
            }

            Flush(FlushReason.SET_TEXTURE);

            mCurrentDrawMaterial.SetTexture(mPropIDSpritesTexture, texture);
        }

        private void SetShaderValues()
        {
            if (mCurrentRenderTexture == null)
            {
                return;
            }

            int x = mClipRegion.x0;
            int y = mClipRegion.y0;
            int w = mClipRegion.x1 - mClipRegion.x0 + 1;
            int h = mClipRegion.y1 - mClipRegion.y0 + 1;

            int x0 = x;
            int y0 = mCurrentRenderTexture.height - (y + h);
            int x1 = x0 + w;
            int y1 = y0 + h;

            mCurrentDrawMaterial.SetVector(mPropIDClip, new Vector4(x0, y0, x1, y1));
            mCurrentDrawMaterial.SetVector(mPropIDDisplaySize, new Vector2(mCurrentRenderTexture.width, mCurrentRenderTexture.height));
            SetShaderGlobalTint(Color.white);
        }

        private void SetShaderGlobalTint(Color32 tint)
        {
            mCurrentDrawMaterial.SetColor(mPropIDGlobalTint, tint);
        }

        private void ResetMesh()
        {
            mMeshStorage.CurrentVertex = 0;
            mMeshStorage.CurrentIndex = 0;
        }

        /// <summary>
        /// Generate system texture
        /// </summary>
        /// <returns>True if successful</returns>
        private bool GenerateSystemTexture()
        {
            SystemTexture = new Texture2D(RBHardware.HW_SYSTEM_TEXTURE_WIDTH, RBHardware.HW_SYSTEM_TEXTURE_HEIGHT, TextureFormat.ARGB32, false);
            SystemTexture.filterMode = FilterMode.Point;
            SystemTexture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[RBHardware.HW_SYSTEM_TEXTURE_WIDTH * RBHardware.HW_SYSTEM_TEXTURE_HEIGHT];

            System.Array.Clear(pixels, 0, pixels.Length);

            // Fill the first row of pixels with solid white, we'll use this to render solid color geometry
            for (int i = 0; i < RBHardware.HW_SYSTEM_TEXTURE_WIDTH; i++)
            {
                pixels[i + (pixels.Length - RBHardware.HW_SYSTEM_TEXTURE_WIDTH)] = new Color32(255, 255, 255, 255);
            }

            var oldRandState = Random.state;
            Random.InitState(0xDEAD);

            // Create a noise texture that will be sampled for the noise retroness effect
            for (int x = RBHardware.HW_SYSTEM_TEXTURE_WIDTH - 128; x < RBHardware.HW_SYSTEM_TEXTURE_WIDTH; x++)
            {
                for (int y = RBHardware.HW_SYSTEM_TEXTURE_HEIGHT - 128; y < RBHardware.HW_SYSTEM_TEXTURE_HEIGHT; y++)
                {
                    int i = x + (y * RBHardware.HW_SYSTEM_TEXTURE_WIDTH);
                    byte c = (byte)Random.Range(0, 256);
                    pixels[i] = new Color32(c, c, c, 255);
                }
            }

            Random.state = oldRandState;

            // Create a gradient texture used for scanline effect
            int col = 0;
            for (int x = RBHardware.HW_SYSTEM_TEXTURE_WIDTH - 256; x < RBHardware.HW_SYSTEM_TEXTURE_WIDTH - 128; x++)
            {
                int scanHeight = col;

                int row = scanHeight - 1;
                for (int y = RBHardware.HW_SYSTEM_TEXTURE_HEIGHT - scanHeight; y < RBHardware.HW_SYSTEM_TEXTURE_HEIGHT; y++)
                {
                    int i = x + (y * RBHardware.HW_SYSTEM_TEXTURE_WIDTH);

                    byte c = 255;
                    if (scanHeight > 16)
                    {
                        if (row == 0)
                        {
                            c = 0;
                        }
                        else if (row == 1)
                        {
                            c = 64;
                        }
                        else if (row == 2)
                        {
                            c = 128;
                        }
                        else if (row == 3)
                        {
                            c = 196;
                        }
                    }
                    else if (scanHeight > 8)
                    {
                        if (row == 0)
                        {
                            c = 0;
                        }
                        else if (row == 1)
                        {
                            c = 85;
                        }
                        else if (row == 2)
                        {
                            c = 170;
                        }
                    }
                    else if (scanHeight > 3)
                    {
                        if (row == 0)
                        {
                            c = 0;
                        }
                        else if (row == 1)
                        {
                            c = 128;
                        }
                    }
                    else if (scanHeight > 2)
                    {
                        if (row == 0)
                        {
                            c = 0;
                        }
                    }
                    else if (scanHeight > 1)
                    {
                        if (row == 0)
                        {
                            c = 128;
                        }
                    }

                    pixels[i] = new Color32(c, c, c, 255);

                    row--;
                }

                col++;
            }

            SystemTexture.SetPixels32(pixels);
            SystemTexture.Apply();

            return true;
        }

#if MESH_UPLOAD_2019_3
        /// <summary>
        /// Vertex structure
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Vertex
        {
            /// <summary>
            /// Pos x
            /// </summary>
            public float pos_x;

            /// <summary>
            /// Pos y
            /// </summary>
            public float pos_y;

            /// <summary>
            /// Pos z
            /// </summary>
            public float pos_z;

            /// <summary>
            /// Color red
            /// </summary>
            public byte color_r;

            /// <summary>
            /// Color green
            /// </summary>
            public byte color_g;

            /// <summary>
            /// Color blue
            /// </summary>
            public byte color_b;

            /// <summary>
            /// Color alpha
            /// </summary>
            public byte color_a;

            /// <summary>
            /// U texture coordinate
            /// </summary>
            public ushort u;

            /// <summary>
            /// V texture coordinate
            /// </summary>
            public ushort v;
        }
#endif

        private struct FlushInfo
        {
            /// <summary>
            /// Reason for flushing
            /// </summary>
            public string Reason;

            /// <summary>
            /// Amount flushed
            /// </summary>
            public int Count;
        }

        private struct ClipRegion
        {
            public int x0, y0, x1, y1;
        }

        private struct DebugClipRegion
        {
            public Rect2i region;
            public Color32 color;
        }

        private struct DrawState
        {
            public RenderTexture CurrentRenderTexture;
            public Vector2i CameraPos;
            public byte Alpha;
            public Color32 TintColor;
            public Rect2i Clip;
            public Material CurrentMaterial;
        }

        /// <summary>
        /// Sprite pack
        /// </summary>
        public class SpritePack
        {
            /// <summary>
            /// Sprites dictionary
            /// </summary>
            public Dictionary<int, PackedSprite> sprites;
        }

        /// <summary>
        /// Wrapper for Unity Material, adding an extra API for tracking offscreen texture filters
        /// </summary>
        public class RetroBlitShader : Material
        {
            private List<FilterModeSetting> mShaderFilters = new List<FilterModeSetting>();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="shader">Shader</param>
            public RetroBlitShader(Shader shader) : base(shader)
            {
                mShaderFilters = new List<FilterModeSetting>();
            }

            /// <summary>
            /// Set texture filter
            /// </summary>
            /// <param name="asset">Offscreen sprite sheet</param>
            /// <param name="filterMode">Filter mode</param>
            public void SetSpriteSheetFilter(SpriteSheetAsset asset, FilterMode filterMode)
            {
                int i;
                for (i = 0; i < mShaderFilters.Count; i++)
                {
                    if (mShaderFilters[i].spriteSheet == asset)
                    {
                        // Point format, no need to store it, this is the default behaviour
                        if (filterMode == FilterMode.Point)
                        {
                            // Remove by swapping with last element
                            if (i < mShaderFilters.Count - 1)
                            {
                                mShaderFilters[i] = mShaderFilters[mShaderFilters.Count - 1];
                            }

                            mShaderFilters.RemoveAt(mShaderFilters.Count - 1);
                        }
                        else
                        {
                            var fm = new FilterModeSetting();
                            fm.filterMode = filterMode;
                            fm.spriteSheet = mShaderFilters[i].spriteSheet;
                            mShaderFilters[i] = fm;
                        }

                        break;
                    }
                }

                // Not found, new entry, or no entry if Point filter type
                if (i == mShaderFilters.Count && filterMode != FilterMode.Point)
                {
                    var fm = new FilterModeSetting();
                    fm.filterMode = filterMode;
                    fm.spriteSheet = asset;
                    mShaderFilters.Add(fm);
                }
            }

            /// <summary>
            /// Get all offscreen filters
            /// </summary>
            /// <returns>Filters</returns>
            public List<FilterModeSetting> GetOffscreenFilters()
            {
                return mShaderFilters;
            }

            /// <summary>
            /// Filter mode setting for particular sprite sheet index
            /// </summary>
            public struct FilterModeSetting
            {
                /// <summary>
                /// Sprite sheet index for this filter
                /// </summary>
                public SpriteSheetAsset spriteSheet;

                /// <summary>
                /// Filter mode
                /// </summary>
                public FilterMode filterMode;
            }
        }

        /// <summary>
        /// A collection of front buffers
        /// </summary>
        public class FrontBuffer
        {
            /// <summary>
            /// Size of front buffers, they are all the same size
            /// </summary>
            public Vector2i Size;

            private List<BufferState> mBuffers = new List<BufferState>();
            private int mCurrentBufferIndex = -1;

            /// <summary>
            /// Get current front buffer texture
            /// </summary>
            public RenderTexture Texture
            {
                get
                {
                    if (mCurrentBufferIndex < 0)
                    {
                        return null;
                    }

                    return mBuffers[mCurrentBufferIndex].tex;
                }
            }

            /// <summary>
            /// Resize all front buffers
            /// </summary>
            /// <param name="size">New size</param>
            /// <param name="api">Reference to RetroBlitAPI</param>
            /// <returns>True if successful</returns>
            public bool Resize(Vector2i size, RBAPI api)
            {
                if (size.x < 0 || size.y < 0)
                {
                    return false;
                }

                if (mBuffers.Count == 0)
                {
                    var tex = api.Renderer.RenderTextureCreate(size);
                    if (tex != null)
                    {
                        tex.name = "FrontBuffer_0";

                        mBuffers.Add(new BufferState(tex));
                        Size = size;
                        mCurrentBufferIndex = 0;
                        return true;
                    }
                }
                else
                {
                    // Same size, nothing to do
                    if (size == Size)
                    {
                        return true;
                    }

                    // Resize all existing buffers
                    for (int i = 0; i < mBuffers.Count; i++)
                    {
                        bool wasActive = false;
                        if (UnityEngine.RenderTexture.active == mBuffers[i].tex)
                        {
                            UnityEngine.RenderTexture.active = null;
                            wasActive = true;
                        }

                        bool wasCameraRenderTarget = false;
                        if (api.PixelCamera.GetRenderTarget() == mBuffers[i].tex)
                        {
                            api.PixelCamera.SetRenderTarget(null);
                            wasCameraRenderTarget = true;
                        }

                        if (size.x == 0 || size.y == 0)
                        {
                            if (mBuffers[i].tex != null)
                            {
                                mBuffers[i].tex.Release();
                                mBuffers[i].tex = null;
                            }
                        }

                        if (mBuffers[i].tex != null)
                        {
                            // Release existing texture
                            mBuffers[i].tex.Release();
                            mBuffers[i].tex = null;
                        }

                        RenderTexture tex = api.Renderer.RenderTextureCreate(size);
                        if (tex == null)
                        {
                            return false;
                        }

                        tex.name = "FrontBuffer_" + i;

                        mBuffers[i] = new BufferState(tex);

                        if (wasCameraRenderTarget)
                        {
                            api.PixelCamera.SetRenderTarget(mBuffers[i].tex);
                        }

                        if (wasActive)
                        {
                            UnityEngine.RenderTexture.active = mBuffers[i].tex;
                        }
                    }

                    Size = size;
                }

                return true;
            }

            /// <summary>
            /// Get next front buffer texture, one will be created if necessary
            /// </summary>
            /// <param name="api">Reference to the RetroBlitAPI</param>
            /// <returns>Next front buffer texture</returns>
            public bool NextBuffer(RBAPI api)
            {
                int index = mCurrentBufferIndex + 1;

                if (index >= mBuffers.Count)
                {
                    RenderTexture tex = api.Renderer.RenderTextureCreate(RB.DisplaySize);
                    if (tex == null)
                    {
                        return false;
                    }

                    tex.name = "FrontBuffer_" + index;

                    mBuffers.Add(new BufferState(tex));
                }

                if (mCurrentBufferIndex >= 0)
                {
                    api.Effects.CopyState(ref mBuffers[mCurrentBufferIndex].effectParams);
                }

                mCurrentBufferIndex = index;

                return true;
            }

            /// <summary>
            /// Notify that render frame has ended, this stores the effects state at the end of the frame
            /// for later post-process rendering stage
            /// </summary>
            /// <param name="api">Reference to the RetroBlitAPI</param>
            public void FrameEnd(RBAPI api)
            {
                if (mCurrentBufferIndex >= 0)
                {
                    api.Effects.CopyState(ref mBuffers[mCurrentBufferIndex].effectParams);
                }
            }

            /// <summary>
            /// Reset back to the first frame buffer
            /// </summary>
            public void Reset()
            {
                if (mBuffers.Count > 0)
                {
                    mCurrentBufferIndex = 0;
                }
                else
                {
                    mCurrentBufferIndex = -1;
                }
            }

            /// <summary>
            /// Get all the frame buffers, and the count of used ones
            /// </summary>
            /// <param name="usedBuffers">Count of currently used buffers in the last frame</param>
            /// <returns>List of all the front buffers</returns>
            public List<BufferState> GetBuffers(out int usedBuffers)
            {
                usedBuffers = mCurrentBufferIndex + 1;
                return mBuffers;
            }

            /// <summary>
            /// Checks if given texture is one of the front buffers
            /// </summary>
            /// <param name="tex">Texture</param>
            /// <returns>True if one of the front buffers</returns>
            public bool TextureIsFrontBuffer(Texture tex)
            {
                for (int i = 0; i < mBuffers.Count; i++)
                {
                    if (mBuffers[i].tex == tex)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Contains information about a single frame buffer
            /// </summary>
            public class BufferState
            {
                /// <summary>
                /// Texture
                /// </summary>
                public RenderTexture tex;

                /// <summary>
                /// Copy of all the effects to be applied to this frame buffer
                /// </summary>
                public RBEffects.EffectParams[] effectParams;

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="tex">Texture</param>
                public BufferState(RenderTexture tex)
                {
                    this.tex = tex;
                    this.effectParams = null;
                }
            }
        }

#if MESH_UPLOAD_2019_3
        /// <summary>
        /// Stores current mesh information to be uploaded. Vertices, uvs, colors, and indices.
        /// Uses NativeArray for buffers and new Mesh upload APIs
        /// </summary>
        public class MeshStorage
        {
            /// <summary>
            /// Current vertex to write to
            /// </summary>
            public int CurrentVertex = 0;

            /// <summary>
            /// Current index to write to
            /// </summary>
            public int CurrentIndex = 0;

#if USE_NATIVE_ARRAYS
            /// <summary>
            /// Vertices
            /// </summary>
            public NativeArray<Vertex> Verticies = new NativeArray<Vertex>(MAX_VERTEX_PER_MESH, Allocator.Persistent);
#else
            /// <summary>
            /// Vertices
            /// </summary>
            public Vertex[] Verticies = new Vertex[MAX_VERTEX_PER_MESH];
#endif

            /// <summary>
            /// Indices
            /// </summary>
#if USE_NATIVE_ARRAYS
            public NativeArray<int> Indices = new NativeArray<int>(MAX_INDICES_PER_MESH, Allocator.Persistent);
#else
            public int[] Indices = new int[MAX_INDICES_PER_MESH];
#endif

            /// <summary>
            /// Unity Meshes that will be uploaded to, these come in various sizes depending on how many
            /// vertices are to be uploaded
            /// </summary>
            private VertexMesh[] Meshes = new VertexMesh[8];

            /// <summary>
            /// Constructor
            /// </summary>
            public MeshStorage()
            {
                int maxVertices = MAX_VERTEX_PER_MESH;
                int maxIndecies = MAX_INDICES_PER_MESH;

                for (int i = 0; i < Meshes.Length; i++)
                {
                    var mesh = new VertexMesh();
                    mesh.maxVertices = maxVertices;
                    mesh.maxIndecies = maxIndecies;
                    mesh.mesh = new Mesh();

                    maxVertices /= 2;
                    maxIndecies /= 2;

                    Meshes[i] = mesh;
                }
            }

            /// <summary>
            /// Upload mesh, try to reduce its size if possible
            /// </summary>
            /// <returns>Uploaded mesh</returns>
            public Mesh UploadMesh()
            {
                var mesh = GetSmallestMesh(CurrentVertex, CurrentIndex);
                if (mesh == null)
                {
                    Debug.LogError("Could not find suitable mesh for flushing " + CurrentVertex + " vertices and " + CurrentIndex + " indecies!");
                    return null;
                }

                // Always upload maximum buffer size, so the underlying GPU buffer doesn't get resized
                mesh.mesh.SetVertexBufferParams(mesh.maxVertices, mVertexLayout);
                mesh.mesh.SetVertexBufferData<Vertex>(Verticies, 0, 0, mesh.maxVertices, 0, MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);

                mesh.mesh.SetIndexBufferParams(mesh.maxIndecies, IndexFormat.UInt32);
                mesh.mesh.SetIndexBufferData<int>(Indices, 0, 0, mesh.maxIndecies, MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);

                mesh.mesh.subMeshCount = 1;

                // Submesh specifies how many vertices to actually use out of the whole buffer
                mesh.mesh.SetSubMesh(
                    0,
                    new SubMeshDescriptor()
                    {
                        baseVertex = 0,
                        bounds = default,
                        indexStart = 0,
                        indexCount = CurrentIndex,
                        firstVertex = 0,
                        topology = MeshTopology.Triangles,
                        vertexCount = CurrentVertex
                    },
                    MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);

                return mesh.mesh;
            }

            /// <summary>
            /// Game shutting down, cleanup unmanaged resources
            /// </summary>
            public void CleanUp()
            {
#if USE_NATIVE_ARRAYS
                Verticies.Dispose();
                Indices.Dispose();
#endif
            }

            private VertexMesh GetSmallestMesh(int vertices, int indecies)
            {
                for (int i = Meshes.Length - 1; i >= 0; i--)
                {
                    var mesh = Meshes[i];
                    if (mesh.maxVertices >= vertices && mesh.maxIndecies >= indecies)
                    {
                        return mesh;
                    }
                }

                return null;
            }

            private class VertexMesh
            {
                public int maxVertices;
                public int maxIndecies;
                public Mesh mesh;
            }
        }
#else
        /// <summary>
        /// Stores current mesh information to be uploaded. Vertices, uvs, colors, and indices.
        /// </summary>
        public class MeshStorage
        {
            /// <summary>
            /// Current vertex to write to
            /// </summary>
            public int CurrentVertex = 0;

            /// <summary>
            /// Current index to write to
            /// </summary>
            public int CurrentIndex = 0;

            /// <summary>
            /// Maximum vertices
            /// </summary>
            public int MaxVertices = MAX_VERTEX_PER_MESH;

            /// <summary>
            /// Maximum indices
            /// </summary>
            public int MaxIndecies = MAX_INDICES_PER_MESH;

            /// <summary>
            /// Vertices
            /// </summary>
            public Vector3[] Verticies;

            /// <summary>
            /// UVs
            /// </summary>
            public Vector2[] Uvs;

            /// <summary>
            /// Colors
            /// </summary>
            public Color32[] Colors;

            /// <summary>
            /// Indices
            /// </summary>
            public int[] Indices;

            private List<MeshDef> MeshBucket = new List<MeshDef>();

            /// <summary>
            /// Constructor
            /// </summary>
            public MeshStorage()
            {
                int maxVerts = 4;
                while (true)
                {
                    MeshBucket.Add(new MeshDef(maxVerts));

                    if (maxVerts >= MAX_VERTEX_PER_MESH)
                    {
                        break;
                    }

                    maxVerts *= 2;

                    if (maxVerts > MAX_VERTEX_PER_MESH)
                    {
                        maxVerts = MAX_VERTEX_PER_MESH;
                    }
                }

                // Instead of allocating its own arrays the MeshStorage can share arrays with the biggest mesh
                var biggestMesh = MeshBucket[MeshBucket.Count - 1];
                biggestMesh.SharedArrays = true;

                Verticies = biggestMesh.Verticies;
                Uvs = biggestMesh.Uvs;
                Colors = biggestMesh.Colors;
                Indices = biggestMesh.Indices;

                MaxVertices = Verticies.Length;
                MaxIndecies = Indices.Length;
            }

            /// <summary>
            /// Upload mesh, try to reduce its size if possible
            /// </summary>
            /// <returns>Uploaded mesh</returns>
            public MeshDef ReduceAndUploadMesh()
            {
                MeshDef meshDef = null;

                for (int i = 0; i < MeshBucket.Count; i++)
                {
                    meshDef = MeshBucket[i];
                    if (meshDef.MaxVerts >= CurrentVertex)
                    {
                        break;
                    }
                }

                if (meshDef != null)
                {
                    if (!meshDef.SharedArrays)
                    {
                        System.Array.Copy(Verticies, meshDef.Verticies, CurrentVertex);
                        System.Array.Copy(Uvs, meshDef.Uvs, CurrentVertex);
                        System.Array.Copy(Colors, meshDef.Colors, CurrentVertex);

                        System.Buffer.BlockCopy(Indices, 0, meshDef.Indices, 0, CurrentIndex * sizeof(int));
                    }

                    System.Array.Clear(meshDef.Indices, CurrentIndex, meshDef.Indices.Length - CurrentIndex);

                    meshDef.LastIndicesLength = CurrentIndex;

                    meshDef.Upload();
                }

                CurrentVertex = 0;
                CurrentIndex = 0;

                return meshDef;
            }

            /// <summary>
            /// Game shutting down, cleanup unmanaged resources
            /// </summary>
            public void CleanUp()
            {
                // Do nothing
            }

            /// <summary>
            /// Stores mesh information to be uploaded. Vertices, uvs, colors, and indices.
            /// </summary>
            public class MeshDef
            {
                /// <summary>
                /// Unity Mesh that will be uploaded to
                /// </summary>
                public Mesh Mesh = new Mesh();

                /// <summary>
                /// Maximum vertices
                /// </summary>
                public int MaxVerts;

                /// <summary>
                /// Maximum indices
                /// </summary>
                public int MaxIndices;

                /// <summary>
                /// Vertices
                /// </summary>
                public Vector3[] Verticies;

                /// <summary>
                /// UVs
                /// </summary>
                public Vector2[] Uvs;

                /// <summary>
                /// Colors
                /// </summary>
                public Color32[] Colors;

                /// <summary>
                /// Indices
                /// </summary>
                public int[] Indices;

                /// <summary>
                /// Length of last indices array
                /// </summary>
                public int LastIndicesLength = 0;

                /// <summary>
                /// Are the arrays shared
                /// </summary>
                public bool SharedArrays = false;

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="maxVerts">Maximum vertices supported by this mesh</param>
                public MeshDef(int maxVerts)
                {
                    MaxVerts = maxVerts;
                    MaxIndices = maxVerts / 4 * 6;

                    Verticies = new Vector3[MaxVerts];
                    Uvs = new Vector2[MaxVerts];
                    Colors = new Color32[MaxVerts];
                    Indices = new int[MaxIndices];

                    Mesh.MarkDynamic();

                    // Pre-populate the mesh object at startup so there are no hiccups later
                    Mesh.vertices = Verticies;
                    Mesh.uv = Uvs;
                    Mesh.colors32 = Colors;
                    Mesh.SetIndices(Indices, MeshTopology.Triangles, 0, false);
                    Mesh.UploadMeshData(false);

                    LastIndicesLength = 0;
                }

                /// <summary>
                /// Upload mesh data
                /// </summary>
                public void Upload()
                {
                    // Upload meshes
                    Mesh.vertices = Verticies;
                    Mesh.uv = Uvs;
                    Mesh.colors32 = Colors;
                    Mesh.SetIndices(Indices, MeshTopology.Triangles, 0, false);
                    Mesh.UploadMeshData(false);
                }
            }
        }
#endif

        private class EmptySpriteSheetAsset : SpriteSheetAsset
        {
            public EmptySpriteSheetAsset()
            {
                this.internalState.texture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                this.internalState.rows = 1;
                this.internalState.columns = 1;
            }
        }
    }
}
