using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel.Design;
using static System.Net.WebRequestMethods;
using System.Security.Cryptography;

#pragma warning disable CS0649

namespace Mario64
{

    public class Engine : GameWindow
    {
        #region Wireframe drawing
        private void DrawPixel(double x, double y, Color4 color, bool scissorTest = true)
        {
            if (scissorTest)
                GL.Enable(EnableCap.ScissorTest);

            GL.Scissor((int)x, (int)y, 1, 1);
            GL.ClearColor(color);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (scissorTest)
                GL.Disable(EnableCap.ScissorTest);
        }
        private void DrawLine(double x1, double y1, double x2, double y2, Color4 color)
        {
            double x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
            dx = x2 - x1; dy = y2 - y1;
            dx1 = Math.Abs(dx); dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
            if (dy1 <= dx1)
            {
                if (dx >= 0)
                { x = x1; y = y1; xe = x2; }
                else
                { x = x2; y = y2; xe = x1; }

                //DrawPixel(x, y, c, col);
                DrawPixel(x, y, color);

                for (i = 0; x < xe; i++)
                {
                    x = x + 1;
                    if (px < 0)
                        px = px + 2 * dy1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) y = y + 1; else y = y - 1;
                        px = px + 2 * (dy1 - dx1);
                    }
                    DrawPixel(x, y, color);
                }
            }
            else
            {
                if (dy >= 0)
                { x = x1; y = y1; ye = y2; }
                else
                { x = x2; y = y2; ye = y1; }

                DrawPixel(x, y, color);

                for (i = 0; y < ye; i++)
                {
                    y = y + 1;
                    if (py <= 0)
                        py = py + 2 * dx1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) x = x + 1; else x = x - 1;
                        py = py + 2 * (dx1 - dy1);
                    }
                    DrawPixel(x, y, color);
                }
            }
        }
        private void DrawTriangle(triangle tri, Color4 color)
        {
            int x1 = (int)tri.p[0].X;
            int y1 = (int)tri.p[0].Y;
            int x2 = (int)tri.p[1].X;
            int y2 = (int)tri.p[1].Y;
            int x3 = (int)tri.p[2].X;
            int y3 = (int)tri.p[2].Y;

            DrawLine(x1, y1, x2, y2, color);
            DrawLine(x2, y2, x3, y3, color);
            DrawLine(x3, y3, x1, y1, color);
        }
        #endregion

        // OPENGL
        private int vao;
        private int textVao;
        private Shader shaderProgram;
        private Shader textShaderProgram;
        private Shader depthShaderProgram;
        private int textureCount = 0;

        private const int SHADOW_WIDTH = 1024;
        private const int SHADOW_HEIGHT = 1024;
        private int depthMapFBO;
        private int depthMap;

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private int screenWidth;
        private int screenHeight;
        private int frameCount;
        private double totalTime;

        private int vertex2Size;

        // Engine variables
        private List<Mesh> meshes;
        private List<Text> textMeshes;

        private Camera camera = new Camera();
        private Frustum frustum;
        private List<PointLight> pointLights;
        private TextGenerator textGenerator;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix, lightSpaceMatrix;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            screenWidth = width;
            screenHeight = height;
            this.CenterWindow(new Vector2i(screenWidth, screenHeight));
            meshes = new List<Mesh>();
            textMeshes = new List<Text>();
            frustum = new Frustum();
            shaderProgram = new Shader();
            textShaderProgram = new Shader();
            pointLights = new List<PointLight>();
        }

        private double DrawFps(double deltaTime)
        {
            frameCount += 1;
            totalTime += deltaTime;

            double fps = (double)frameCount / totalTime;
            Title = "Mario 64    |    FPS: " + Math.Round(fps, 4).ToString();

            if (frameCount > 1000)
            {
                frameCount = 0;
                totalTime = 0;
            }

            return fps;
        }

        private void SendUniforms()
        {
            int windowSizeLocation = GL.GetUniformLocation(shaderProgram.id, "windowSize");
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram.id, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgram.id, "viewMatrix");
            int lightSpaceMatrixLocation = GL.GetUniformLocation(shaderProgram.id, "lightSpaceMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgram.id, "projectionMatrix");
            int cameraPositionLocation = GL.GetUniformLocation(shaderProgram.id, "cameraPosition");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.GetProjectionMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
            GL.UniformMatrix4(lightSpaceMatrixLocation, false, ref lightSpaceMatrix);
            GL.Uniform2(windowSizeLocation, new Vector2(screenWidth, screenHeight));
            GL.Uniform3(cameraPositionLocation, camera.position);
        }

        private void SendTextUniforms()
        {
            int windowSizeLocation = GL.GetUniformLocation(shaderProgram.id, "windowSize");
            GL.Uniform2(windowSizeLocation, new Vector2(screenWidth, screenHeight));
        }

        public void GenerateShadowMap()
        {
            GL.GenFramebuffers(1, out depthMapFBO);

            GL.GenTextures(1, out depthMap);
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, SHADOW_WIDTH, SHADOW_HEIGHT, 0, PixelFormat.DepthComponent,
                PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D,
                depthMap, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        Vertex2[] quadVertices =
        {
            new Vertex2(new Vector2(-1.0f,  1.0f), new Vector2(0.0f, 1.0f)),
            new Vertex2(new Vector2(-1.0f, -1.0f), new Vector2(0.0f, 0.0f)),
            new Vertex2(new Vector2(1.0f, -1.0f), new Vector2(1.0f, 0.0f)),
            new Vertex2(new Vector2(-1.0f,  1.0f), new Vector2( 0.0f, 1.0f)),
            new Vertex2(new Vector2(1.0f, -1.0f), new Vector2(1.0f, 0.0f)),
            new Vertex2(new Vector2(1.0f,  1.0f), new Vector2(1.0f, 1.0f)),
        };

        int quadVAO, quadVBO;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            double fps = DrawFps(args.Time);


            viewMatrix = camera.GetViewMatrix();
            frustum = camera.GetFrustum();

            GL.Enable(EnableCap.DepthTest);

            // shadow map creating
            depthShaderProgram.Use();
            GL.UniformMatrix4(GL.GetUniformLocation(depthShaderProgram.id, "lightSpaceMatrix"), false, ref lightSpaceMatrix);
            GL.Viewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // The model drawing
            foreach (Mesh mesh in meshes)
            {
                mesh.Draw();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            float[] depthValues = new float[SHADOW_WIDTH * SHADOW_HEIGHT];
            GL.ReadPixels(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT, PixelFormat.DepthComponent, PixelType.Float, depthValues);

            // Set up viewport
            GL.Viewport(0, 0, screenWidth, screenHeight);

            // Use the debug shader
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Disable(EnableCap.DepthTest);
            // Draw the quad with the shader map texture
            shaderProgram.Use();
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BindVertexArray(quadVAO);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * vertex2Size, quadVertices, BufferUsageHint.DynamicDraw);

            int shadowMapLocation = GL.GetUniformLocation(shaderProgram.id, "shadowMap");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            GL.Uniform1(shadowMapLocation, 0);  // 0 corresponds to TextureUnit.Texture0
            //Bind the shadow map

            GL.DrawArrays(PrimitiveType.Triangles, 0, quadVertices.Length);


            //GL.DisableVertexAttribArray(0);
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            //CursorState = CursorState.Grabbed;

            textGenerator = new TextGenerator();

            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.FramebufferSrgb);
            //GL.Disable(EnableCap.CullFace);
            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // OPENGL init
            vao = GL.GenVertexArray();
            GenerateShadowMap();
            lightSpaceMatrix = PointLight.GetDirLightSpaceMatrix();

            shaderProgram = new Shader("Default.vert", "Default.frag");
            depthShaderProgram = new Shader("depth.vert", "depth.frag");

            shaderProgram.Use();
            // Generate the VAO
            quadVAO = GL.GenVertexArray();
            quadVBO = GL.GenBuffer();

            vertex2Size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex2));

            // Bind the VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BindVertexArray(quadVAO);

            // Bind and set vertex buffer

            // Position attribute
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, vertex2Size, 0);
            GL.EnableVertexArrayAttrib(quadVAO, 0);
            // TexCoord attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vertex2Size, 2 * sizeof(float));
            GL.EnableVertexArrayAttrib(quadVAO, 1);

            //GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.DynamicDraw);
            // Unbind VAO
            //GL.BindVertexArray(0);




            // create the shader program

            //Camera
            camera = new Camera(new Vector2(screenWidth, screenHeight));
            camera.UpdateVectors();


            meshes.Add(new Mesh(vao, shaderProgram.id, "spiro.obj", "High.png", ref textureCount));

            frustum = camera.GetFrustum();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(vao);
            foreach(Mesh mesh in meshes)
                GL.DeleteBuffer(mesh.vbo);
            foreach(Text mesh in textMeshes)
                GL.DeleteBuffer(mesh.vbo);
            shaderProgram.Unload();
            textShaderProgram.Unload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            screenWidth = e.Width;
            screenHeight = e.Height;
        }
    }
}
