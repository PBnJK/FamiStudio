﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
     public class Graphics : GraphicsBase
    {
        private bool supportsLineWidth = true;

        public Graphics(float mainScale, float fontScale) : base(mainScale, fontScale)
        {
            GL.StaticInitialize();

            dashedBitmap = CreateBitmapFromResource("Dash");
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.Repeat);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.Repeat);

            if (Platform.IsLinux)
            {
                var lineWidths = new float[2];
                GL.GetFloat(GL.LineWidthRange, lineWidths);
                supportsLineWidth = lineWidths[1] > 1.0f;
            }
        }

        public override void BeginDrawControl(Rectangle unflippedControlRect, int windowSizeY)
        {
            base.BeginDrawControl(unflippedControlRect, windowSizeY);

            GL.Viewport(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            GL.MatrixMode(GL.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, unflippedControlRect.Width, unflippedControlRect.Height, 0, -1, 1);
            GL.Disable(GL.CullFace);
            GL.MatrixMode(GL.Modelview);
            GL.LoadIdentity();
            GL.BlendFunc(GL.SrcAlpha, GL.OneMinusSrcAlpha);
            GL.Enable(GL.Blend);
            GL.Disable(GL.DepthTest);
            GL.Disable(GL.StencilTest);
            GL.Enable(GL.ScissorTest);
            GL.Scissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            GL.EnableClientState(GL.VertexArray);
        }

        private void SetScissorRect(int x0, int y0, int x1, int y1)
        {
            var scissor = new Rectangle(controlRect.X + x0, controlRect.Y + y0, x1 - x0, y1 - y0);
            scissor = FlipRectangleY(scissor);
            GL.Scissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        private void ClearScissorRect()
        {
            GL.Scissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
        }

        public void Clear(Color color)
        {
            GL.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            GL.Clear(GL.ColorBufferBit);
        }

        public void UpdateBitmap(Bitmap bmp, int x, int y, int width, int height, byte[] data)
        {
            GL.BindTexture(GL.Texture2D, bmp.Id);
            GL.TexSubImage2D(GL.Texture2D, 0, x, y, width, height, GL.Bgr, GL.UnsignedByte, data);
        }

        public void UpdateBitmap(Bitmap bmp, int x, int y, int width, int height, int[] data)
        {
            GL.BindTexture(GL.Texture2D, bmp.Id);
            GL.TexSubImage2D(GL.Texture2D, 0, x, y, width, height, GL.Rgba, GL.UnsignedByte, data);
        }

        protected override int CreateEmptyTexture(int width, int height, bool alpha, bool filter)
        {
            int id = GL.GenTexture();

            GL.BindTexture(GL.Texture2D, id);
            GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, filter ? GL.Linear : GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, filter ? GL.Linear : GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.ClampToEdge);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.ClampToEdge);
            GL.TexImage2D(GL.Texture2D, 0, alpha ? GL.Rgba8 : GL.Rgb8, width, height, 0, GL.Rgba, GL.UnsignedByte, new int[width * height]);

            return id;
        }

        protected unsafe override int CreateTexture(SimpleBitmap bmp, bool filter)
        {
            fixed (int* ptr = &bmp.Data[0])
            {
                var stride = sizeof(int) * bmp.Width;

                int id = GL.GenTexture();
                GL.BindTexture(GL.Texture2D, id);
                GL.TexImage2D(GL.Texture2D, 0, GL.Rgba8, bmp.Width, bmp.Height, 0, GL.Rgba, GL.UnsignedByte, new IntPtr(ptr));
                GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, GL.Nearest);
                GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, GL.Nearest);

                return id;
            }
        }

        public override void DeleteTexture(int id)
        {
            GL.DeleteTexture(id);
        }

        protected override string GetScaledFilename(string name, out bool needsScaling)
        {
            var assembly = Assembly.GetExecutingAssembly();

            if (windowScaling == 1.5f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@15x.tga") != null)
            {
                needsScaling = false;
                return $"FamiStudio.Resources.{name}@15x.tga";
            }
            else if (windowScaling > 1.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.tga") != null)
            {
                needsScaling = windowScaling != 2.0f;
                return $"FamiStudio.Resources.{name}@2x.tga";
            }
            else
            {
                needsScaling = false;
                return $"FamiStudio.Resources.{name}.tga";
            }
        }

        public Bitmap CreateBitmapFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new Bitmap(this, CreateTexture(bmp, false), bmp.Width, bmp.Height);
        }

        public Bitmap CreateBitmapFromOffscreenGraphics(OffscreenGraphics g)
        {
            return new Bitmap(this, g.Texture, g.SizeX, g.SizeY, false);
        }

        public unsafe override BitmapAtlas CreateBitmapAtlasFromResources(string[] names)
        {
            // Need to sort since we do binary searches on the names.
            Array.Sort(names);

            var bitmaps = new SimpleBitmap[names.Length];
            var elementSizeX = 0;
            var elementSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = LoadBitmapFromResourceWithScaling(names[i]);

                elementSizeX = Math.Max(elementSizeX, bmp.Width);
                elementSizeY = Math.Max(elementSizeY, bmp.Height);

                bitmaps[i] = bmp;
            }

            Debug.Assert(elementSizeX < MaxAtlasResolution);

            var elementsPerRow = MaxAtlasResolution / elementSizeX;
            var elementRects = new Rectangle[names.Length];
            var atlasSizeX = 0;
            var atlasSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];
                var row = i / elementsPerRow;
                var col = i % elementsPerRow;

                elementRects[i] = new Rectangle(
                    col * elementSizeX,
                    row * elementSizeY,
                    bmp.Width,
                    bmp.Height);

                atlasSizeX = Math.Max(atlasSizeX, elementRects[i].Right);
                atlasSizeY = Math.Max(atlasSizeY, elementRects[i].Bottom);
            }

            atlasSizeX = Utils.NextPowerOfTwo(atlasSizeX);
            atlasSizeY = Utils.NextPowerOfTwo(atlasSizeY);

            var textureId = CreateEmptyTexture(atlasSizeX, atlasSizeY, true, false);
            GL.BindTexture(GL.Texture2D, textureId);

            Debug.WriteLine($"Creating bitmap atlas of size {atlasSizeX}x{atlasSizeY} with {names.Length} images:");

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];

                Debug.WriteLine($"  - {names[i]} ({bmp.Width} x {bmp.Height}):");

                fixed (int* ptr = &bmp.Data[0])
                {
                    var stride = sizeof(int) * bmp.Width;
                    GL.TexSubImage2D(GL.Texture2D, 0, elementRects[i].X, elementRects[i].Y, bmp.Width, bmp.Height, GL.Rgba, GL.UnsignedByte, new IntPtr(ptr));
                }
            }

            return new BitmapAtlas(this, textureId, atlasSizeX, atlasSizeY, names, elementRects);
        }

        public override CommandList CreateCommandList()
        {
            return new CommandList(this, dashedBitmap.Size.Width, lineWidthBias, supportsLineWidth);
        }

        public unsafe override void DrawCommandList(CommandList list, Rectangle scissor)
        {
            if (list == null)
                return;

            if (list.HasAnything)
            {
                if (!scissor.IsEmpty)
                    SetScissorRect(scissor.Left, scissor.Top, scissor.Right, scissor.Bottom);

                if (list.HasAnyMeshes)
                {
                    var drawData = list.GetMeshDrawData();

                    GL.EnableClientState(GL.ColorArray);

                    foreach (var draw in drawData)
                    {
                        if (draw.smooth) GL.Enable(GL.PolygonSmooth);
                        GL.ColorPointer(4, GL.UnsignedByte, 0, draw.colArray);
                        GL.VertexPointer(2, GL.Float, 0, draw.vtxArray);
                        GL.DrawElements(GL.Triangles, draw.numIndices, GL.UnsignedShort, draw.idxArray);
                        if (draw.smooth) GL.Disable(GL.PolygonSmooth);
                    }

                    GL.DisableClientState(GL.ColorArray);
                }

                if (list.HasAnyLines)
                {
                    var drawData = list.GetLineDrawData();

                    GL.PushMatrix();
                    GL.Translate(0.5f, 0.5f, 0.0f);
                    GL.Enable(GL.Texture2D);
                    GL.BindTexture(GL.Texture2D, dashedBitmap.Id);
                    GL.EnableClientState(GL.ColorArray);
                    GL.EnableClientState(GL.TextureCoordArray);

                    foreach (var draw in drawData)
                    {
                        if (draw.smooth) GL.Enable(GL.LineSmooth);
                        GL.LineWidth(draw.lineWidth);
                        GL.TexCoordPointer(2, GL.Float, 0, draw.texArray);
                        GL.ColorPointer(4, GL.UnsignedByte, 0, draw.colArray);
                        GL.VertexPointer(2, GL.Float, 0, draw.vtxArray);
                        GL.DrawArrays(GL.Lines, 0, draw.numVertices);
                        if (draw.smooth) GL.Disable(GL.LineSmooth);
                    }

                    GL.DisableClientState(GL.ColorArray);
                    GL.DisableClientState(GL.TextureCoordArray);
                    GL.Disable(GL.Texture2D);
                    GL.PopMatrix();
                }

                if (Platform.IsLinux && list.HasAnyTickLineMeshes)
                {
                    var draw = list.GetThickLineAsPolygonDrawData();

                    /*if (draw.smooth)*/ GL.Enable(GL.PolygonSmooth);
                    GL.EnableClientState(GL.ColorArray);
                    GL.ColorPointer(4, GL.UnsignedByte, 0, draw.colArray);
                    GL.VertexPointer(2, GL.Float, 0, draw.vtxArray);
                    GL.DrawElements(GL.Triangles, draw.numIndices, GL.UnsignedShort, draw.idxArray);
                    GL.DisableClientState(GL.ColorArray);
                    /*if (draw.smooth)*/ GL.Disable(GL.PolygonSmooth);
                }

                if (list.HasAnyBitmaps)
                {
                    var drawData = list.GetBitmapDrawData(vtxArray, texArray, colArray, out _, out _, out _, out _);

                    GL.Enable(GL.Texture2D);
                    GL.EnableClientState(GL.ColorArray);
                    GL.EnableClientState(GL.TextureCoordArray);
                    GL.TexCoordPointer(2, GL.Float, 0, texArray);
                    GL.ColorPointer(4, GL.UnsignedByte, 0, colArray);
                    GL.VertexPointer(2, GL.Float, 0, vtxArray);

                    fixed (short* ptr = quadIdxArray)
                    {
                        foreach (var draw in drawData)
                        {
                            GL.BindTexture(GL.Texture2D, draw.textureId);
                            GL.DrawElements(GL.Triangles, draw.count, GL.UnsignedShort, new IntPtr(ptr + draw.start));
                        }
                    }

                    GL.DisableClientState(GL.ColorArray);
                    GL.DisableClientState(GL.TextureCoordArray);
                    GL.Disable(GL.Texture2D);
                }

                if (list.HasAnyTexts)
                {
                    var drawData = list.GetTextDrawData(vtxArray, texArray, colArray, out _, out _, out _, out _);

                    GL.Enable(GL.Texture2D);
                    GL.EnableClientState(GL.ColorArray);
                    GL.EnableClientState(GL.TextureCoordArray);
                    GL.TexCoordPointer(2, GL.Float, 0, texArray);
                    GL.ColorPointer(4, GL.UnsignedByte, 0, colArray);
                    GL.VertexPointer(2, GL.Float, 0, vtxArray);

                    fixed (short* ptr = quadIdxArray)
                    {
                        foreach (var draw in drawData)
                        {
                            GL.BindTexture(GL.Texture2D, draw.textureId);
                            GL.DrawElements(GL.Triangles, draw.count, GL.UnsignedShort, new IntPtr(ptr + draw.start));
                        }
                    }

                    GL.DisableClientState(GL.ColorArray);
                    GL.DisableClientState(GL.TextureCoordArray);
                    GL.Disable(GL.Texture2D);
                }

                if (!scissor.IsEmpty)
                    ClearScissorRect();
            }

            list.Release();
        }
    };

    public class OffscreenGraphics : Graphics
    {
        protected int fbo;
        protected int texture;
        protected int resX;
        protected int resY;

        public int Texture => texture;
        public int SizeX => resX;
        public int SizeY => resY;

        private OffscreenGraphics(int imageSizeX, int imageSizeY, bool allowReadback) : base(1.0f, 1.0f) 
        {
            resX = imageSizeX;
            resY = imageSizeY;

            texture = GL.GenTexture();
            GL.BindTexture(GL.Texture2D, texture);
            GL.TexImage2D(GL.Texture2D, 0, GL.Rgba, imageSizeX, imageSizeY, 0, GL.Rgba, GL.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.ClampToBorder);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.ClampToBorder);

            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(GL.Framebuffer, fbo);
            GL.FramebufferTexture2D(GL.Framebuffer, GL.ColorAttachment0, GL.Texture2D, texture, 0);
            GL.BindFramebuffer(GL.Framebuffer, 0);
        }

        public static OffscreenGraphics Create(int imageSizeX, int imageSizeY, bool allowReadback)
        {
            return new OffscreenGraphics(imageSizeX, imageSizeY, allowReadback);
        }

        public override void BeginDrawControl(Rectangle unflippedControlRect, int windowSizeY)
        {
            base.BeginDrawControl(unflippedControlRect, windowSizeY);

            GL.BindFramebuffer(GL.DrawFramebuffer, fbo);
            GL.DrawBuffer(GL.ColorAttachment0);
        }

        public override void EndDrawControl()
        {
            base.EndDrawControl();

            GL.BindFramebuffer(GL.DrawFramebuffer, 0);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            byte[] tmp = new byte[data.Length];

            GL.BindFramebuffer(GL.ReadFramebuffer, fbo);
            fixed (byte* tmpPtr = &tmp[0])
            {
                GL.ReadPixels(0, 0, resX, resY, GL.Rgba, GL.UnsignedByte, new IntPtr(tmpPtr));
                GL.BindFramebuffer(GL.ReadFramebuffer, 0);

                // Flip image vertically to match D3D. 
                for (int y = 0; y < resY; y++)
                {
                    int y0 = y;
                    int y1 = resY - y - 1;

                    y0 *= resX * 4;
                    y1 *= resX * 4;

                    // ABGR -> RGBA
                    byte* p = tmpPtr + y0;
                    for (int x = 0; x < resX * 4; x += 4)
                    {
                        data[y1 + x + 1] = *p++;
                        data[y1 + x + 2] = *p++;
                        data[y1 + x + 3] = *p++;
                        data[y1 + x + 0] = *p++;
                    }
                }
            }
        }

        public override void Dispose()
        {
            if (texture != 0) GL.DeleteTexture(texture);
            if (fbo != 0)     GL.DeleteFramebuffer(fbo);

            base.Dispose();
        }
    };

    public static class GL
    {
        private static bool initialized;

        public const int ColorBufferBit    = 0x4000;
        public const int Texture2D         = 0x0DE1;
        public const int Modelview         = 0x1700;
        public const int Projection        = 0x1701;
        public const int CullFace          = 0x0B44;
        public const int VertexArray       = 0x8074;
        public const int TextureWrapS      = 0x2802;
        public const int TextureWrapT      = 0x2803;
        public const int Clamp             = 0x2900;
        public const int Repeat            = 0x2901;
        public const int ClampToBorder     = 0x812D;
        public const int SrcAlpha          = 0x0302;
        public const int OneMinusSrcAlpha  = 0x0303;
        public const int Blend             = 0x0BE2;
        public const int DepthTest         = 0x0B71;
        public const int StencilTest       = 0x0B90;
        public const int ScissorTest       = 0x0C11;
        public const int Bgr               = 0x80E0;
        public const int Bgra              = 0x80E1;
        public const int Rgb               = 0x1907;
        public const int Rgba              = 0x1908;
        public const int UnsignedByte      = 0x1401;
        public const int TextureMagFilter  = 0x2800;
        public const int TextureMinFilter  = 0x2801;
        public const int Nearest           = 0x2600;
        public const int Linear            = 0x2601;
        public const int ClampToEdge       = 0x812F;
        public const int Rgb8              = 0x8051;
        public const int Rgba8             = 0x8058;
        public const int ColorArray        = 0x8076;
        public const int TextureCoordArray = 0x8078;
        public const int LineSmooth        = 0x0B20;
        public const int PolygonSmooth     = 0x0B41;
        public const int Float             = 0x1406;
        public const int Lines             = 0x0001;
        public const int Triangles         = 0x0004;
        public const int Quads             = 0x0007;
        public const int UnsignedShort     = 0x1403;
        public const int Framebuffer       = 0x8D40;
        public const int ColorAttachment0  = 0x8CE0;
        public const int DrawFramebuffer   = 0x8CA9;
        public const int ReadFramebuffer   = 0x8CA8;
        public const int LineWidthRange    = 0x0B22;

        public delegate void ClearDelegate(uint mask);
        public delegate void ClearColorDelegate(float red, float green, float blue, float alpha);
        public delegate void ViewportDelegate(int left, int top, int width, int height);
        public delegate void MatrixModeDelegate(int mode);
        public delegate void LoadIdentityDelegate();
        public delegate void OrthoDelegate(double left, double right, double bottom, double top, double near, double far);
        public delegate void EnableDelegate(int cap);
        public delegate void DisableDelegate(int cap);
        public delegate void BlendFuncDelegate(int src, int dst);
        public delegate void ScissorDelegate(int x, int y, int width, int height);
        public delegate void EnableClientStateDelegate(int cap);
        public delegate void DisableClientStateDelegate(int cap);
        public delegate void TexParameterDelegate(int target, int name, int val);
        public delegate void BindTextureDelegate(int target, int id);
        public delegate void TexImage2DDelegate(int target, int level, int internalformat, int width, int height, int border, int format, int type, IntPtr data);
        public delegate void TexSubImage2DDelegate(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr pixels);
        public delegate void GenTexturesDelegate(int n, IntPtr textures);
        public delegate void DeleteTexturesDelegate(int n, IntPtr textures);
        public delegate void ColorPointerDelegate(int size, int type, int stride, IntPtr pointer);
        public delegate void VertexPointerDelegate(int size, int type, int stride, IntPtr pointer);
        public delegate void TexCoordPointerDelegate(int size, int type, int stride, IntPtr pointer);
        public delegate void DrawArraysDelegate(int mode, int first, int count);
        public delegate void DrawElementsDelegate(int mode, int count, int type, IntPtr indices);
        public delegate void PushMatrixDelegate();
        public delegate void PopMatrixDelegate();
        public delegate void TranslateDelegate(float x, float y, float z);
        public delegate void LineWidthDelegate(float width);
        public delegate void GenFramebuffersDelegate(int n, IntPtr indices);
        public delegate void BindFramebufferDelegate(int target, int framebuffer);
        public delegate void DrawBufferDelegate(int buf);
        public delegate void FramebufferTexture2DDelegate(int target,int attachment, int textarget, int texture, int level);
        public delegate void ReadPixelsDelegate(int x, int y, int width, int height, int format, int type, IntPtr data);
        public delegate void DeleteFramebuffersDelegate(int n, IntPtr framebuffers);
        public delegate void GetFloatDelegate(int param, IntPtr floats);

        public static ClearDelegate                Clear;
        public static ClearColorDelegate           ClearColor;
        public static ViewportDelegate             Viewport;
        public static MatrixModeDelegate           MatrixMode;
        public static LoadIdentityDelegate         LoadIdentity;
        public static OrthoDelegate                Ortho;
        public static EnableDelegate               Enable;
        public static DisableDelegate              Disable;
        public static BlendFuncDelegate            BlendFunc;
        public static ScissorDelegate              Scissor;
        public static EnableClientStateDelegate    EnableClientState;
        public static DisableClientStateDelegate   DisableClientState;
        public static TexParameterDelegate         TexParameter;
        public static BindTextureDelegate          BindTexture;
        public static TexImage2DDelegate           TexImage2DRaw;
        public static TexSubImage2DDelegate        TexSubImage2DRaw;
        public static GenTexturesDelegate          GenTextures;
        public static DeleteTexturesDelegate       DeleteTextures;
        public static ColorPointerDelegate         ColorPointerRaw;
        public static VertexPointerDelegate        VertexPointerRaw;
        public static TexCoordPointerDelegate      TexCoordPointerRaw;
        public static DrawArraysDelegate           DrawArrays;
        public static DrawElementsDelegate         DrawElementsRaw;
        public static TranslateDelegate            Translate;
        public static PushMatrixDelegate           PushMatrix;
        public static PopMatrixDelegate            PopMatrix;
        public static LineWidthDelegate            LineWidth;
        public static GenFramebuffersDelegate      GenFramebuffers;
        public static BindFramebufferDelegate      BindFramebuffer;
        public static DrawBufferDelegate           DrawBuffer;
        public static FramebufferTexture2DDelegate FramebufferTexture2D;
        public static ReadPixelsDelegate           ReadPixels;
        public static DeleteFramebuffersDelegate   DeleteFramebuffers;
        public static GetFloatDelegate             GetFloatRaw;

        public static void StaticInitialize()
        {
            if (initialized)
                return;

            Clear                = Marshal.GetDelegateForFunctionPointer<ClearDelegate>(glfwGetProcAddress("glClear"));
            ClearColor           = Marshal.GetDelegateForFunctionPointer<ClearColorDelegate>(glfwGetProcAddress("glClearColor"));
            Viewport             = Marshal.GetDelegateForFunctionPointer<ViewportDelegate>(glfwGetProcAddress("glViewport"));
            MatrixMode           = Marshal.GetDelegateForFunctionPointer<MatrixModeDelegate>(glfwGetProcAddress("glMatrixMode"));
            LoadIdentity         = Marshal.GetDelegateForFunctionPointer<LoadIdentityDelegate>(glfwGetProcAddress("glLoadIdentity"));
            Ortho                = Marshal.GetDelegateForFunctionPointer<OrthoDelegate>(glfwGetProcAddress("glOrtho"));
            Enable               = Marshal.GetDelegateForFunctionPointer<EnableDelegate>(glfwGetProcAddress("glEnable"));
            Disable              = Marshal.GetDelegateForFunctionPointer<DisableDelegate>(glfwGetProcAddress("glDisable"));
            BlendFunc            = Marshal.GetDelegateForFunctionPointer<BlendFuncDelegate>(glfwGetProcAddress("glBlendFunc"));
            Scissor              = Marshal.GetDelegateForFunctionPointer<ScissorDelegate>(glfwGetProcAddress("glScissor"));
            EnableClientState    = Marshal.GetDelegateForFunctionPointer<EnableClientStateDelegate>(glfwGetProcAddress("glEnableClientState"));
            DisableClientState   = Marshal.GetDelegateForFunctionPointer<DisableClientStateDelegate>(glfwGetProcAddress("glDisableClientState"));
            TexParameter         = Marshal.GetDelegateForFunctionPointer<TexParameterDelegate>(glfwGetProcAddress("glTexParameteri"));
            BindTexture          = Marshal.GetDelegateForFunctionPointer<BindTextureDelegate>(glfwGetProcAddress("glBindTexture"));
            TexImage2DRaw        = Marshal.GetDelegateForFunctionPointer<TexImage2DDelegate>(glfwGetProcAddress("glTexImage2D"));
            TexSubImage2DRaw     = Marshal.GetDelegateForFunctionPointer<TexSubImage2DDelegate>(glfwGetProcAddress("glTexSubImage2D"));
            GenTextures          = Marshal.GetDelegateForFunctionPointer<GenTexturesDelegate>(glfwGetProcAddress("glGenTextures"));
            DeleteTextures       = Marshal.GetDelegateForFunctionPointer<DeleteTexturesDelegate>(glfwGetProcAddress("glDeleteTextures"));
            ColorPointerRaw      = Marshal.GetDelegateForFunctionPointer<ColorPointerDelegate>(glfwGetProcAddress("glColorPointer"));
            VertexPointerRaw     = Marshal.GetDelegateForFunctionPointer<VertexPointerDelegate>(glfwGetProcAddress("glVertexPointer"));
            TexCoordPointerRaw   = Marshal.GetDelegateForFunctionPointer<TexCoordPointerDelegate>(glfwGetProcAddress("glTexCoordPointer"));
            DrawArrays           = Marshal.GetDelegateForFunctionPointer<DrawArraysDelegate>(glfwGetProcAddress("glDrawArrays"));
            DrawElementsRaw      = Marshal.GetDelegateForFunctionPointer<DrawElementsDelegate>(glfwGetProcAddress("glDrawElements"));
            PushMatrix           = Marshal.GetDelegateForFunctionPointer<PushMatrixDelegate>(glfwGetProcAddress("glPushMatrix"));
            PopMatrix            = Marshal.GetDelegateForFunctionPointer<PopMatrixDelegate>(glfwGetProcAddress("glPopMatrix"));
            PopMatrix            = Marshal.GetDelegateForFunctionPointer<PopMatrixDelegate>(glfwGetProcAddress("glPopMatrix"));
            Translate            = Marshal.GetDelegateForFunctionPointer<TranslateDelegate>(glfwGetProcAddress("glTranslatef"));
            LineWidth            = Marshal.GetDelegateForFunctionPointer<LineWidthDelegate>(glfwGetProcAddress("glLineWidth"));
            DrawBuffer           = Marshal.GetDelegateForFunctionPointer<DrawBufferDelegate>(glfwGetProcAddress("glDrawBuffer"));
            ReadPixels           = Marshal.GetDelegateForFunctionPointer<ReadPixelsDelegate>(glfwGetProcAddress("glReadPixels"));
            GetFloatRaw          = Marshal.GetDelegateForFunctionPointer<GetFloatDelegate>(glfwGetProcAddress("glGetFloatv"));
            GenFramebuffers      = Marshal.GetDelegateForFunctionPointer<GenFramebuffersDelegate>(GetExtProcAddress("glGenFramebuffers"));
            BindFramebuffer      = Marshal.GetDelegateForFunctionPointer<BindFramebufferDelegate>(GetExtProcAddress("glBindFramebuffer"));
            FramebufferTexture2D = Marshal.GetDelegateForFunctionPointer<FramebufferTexture2DDelegate>(GetExtProcAddress("glFramebufferTexture2D"));
            DeleteFramebuffers   = Marshal.GetDelegateForFunctionPointer<DeleteFramebuffersDelegate>(GetExtProcAddress("glDeleteFramebuffers"));

            initialized = true;
        }

        private static IntPtr GetExtProcAddress(string f, string suffix = "EXT")
        {
            var addr = glfwGetProcAddress(f + suffix);
            return addr != IntPtr.Zero ? addr : glfwGetProcAddress(f);
        }

        public static unsafe void GetFloat(int param, float[] floats)
        {
            fixed (float* p = &floats[0])
                GetFloatRaw(param, (IntPtr)p);
        }

        public unsafe static int GenFramebuffer()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenFramebuffers(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteFramebuffer(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteFramebuffers(1, new IntPtr(p));
        }

        public unsafe static void ColorPointer(int size, int type, int stride, int[] data)
        {
            fixed (int* p = &data[0])
                ColorPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void VertexPointer(int size, int type, int stride, float[] data)
        {
            fixed (float* p = &data[0])
                VertexPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void TexCoordPointer(int size, int type, int stride, float[] data)
        {
            fixed (float* p = &data[0])
                TexCoordPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void DrawElements(int mode, int count, int type, IntPtr data)
        {
            DrawElementsRaw(mode, count, type, data);
        }

        public unsafe static void DrawElements(int mode, int count, int type, short[] data)
        {
            fixed (short* p = &data[0])
                DrawElementsRaw(mode, count, type, new IntPtr(p));
        }

        public unsafe static int GenTexture()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenTextures(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteTexture(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteTextures(1, new IntPtr(p));
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, IntPtr data)
        {
            TexImage2DRaw(target, level, internalformat, width, height, border, format, type, data);
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int[] data)
        {
            fixed (int* p = &data[0])
                TexImage2DRaw(target, level, internalformat, width, height, border, format, type, new IntPtr(p));
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, byte[] data)
        {
            fixed (byte* p = &data[0])
                TexImage2DRaw(target, level, internalformat, width, height, border, format, type, new IntPtr(p));
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data)
        {
            TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, data);
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int[] data)
        {
            fixed (int* p = &data[0])
                TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, new IntPtr(p));
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, byte[] data)
        {
            fixed (byte* p = &data[0])
                TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, new IntPtr(p));
        }
    }
}