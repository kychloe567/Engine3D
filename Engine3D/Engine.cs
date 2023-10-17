﻿using System;
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
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;

#pragma warning disable CS0649
#pragma warning disable CS8618

namespace Engine3D
{

    public class Engine : GameWindow
    {

        // OPENGL
        private VAO meshVao;
        private VBO meshVbo;

        private VAO testVao;
        private VBO testVbo;

        private VAO textVao;
        private VBO textVbo;

        private VAO noTexVao;
        private VBO noTexVbo;

        private VAO uiTexVao;
        private VBO uiTexVbo;

        private VAO wireVao;
        private VBO wireVbo;

        private Shader shaderProgram;
        private Shader posTexShader;
        private Shader noTextureShaderProgram;
        private int textureCount = 0;

        // Program variables
        private Random rnd = new Random((int)DateTime.Now.Ticks);
        private Vector2 windowSize;
        private int frameCount;
        private double totalTime;
        private int temp = -1;
        private bool haveText = false;

        // Engine variables

        private List<Object> objects;

        private Character character;

        private Frustum frustum;
        private List<PointLight> pointLights;
        private TextGenerator textGenerator;
        private Physx physx;

        // Frame limiting
        private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS
        private Stopwatch stopwatch;

        public Engine(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            

            windowSize = new Vector2(width, height);
            CenterWindow(new Vector2i(width, height));
            frustum = new Frustum();
            shaderProgram = new Shader();
            posTexShader = new Shader();
            pointLights = new List<PointLight>();

            objects = new List<Object>();
        }

        private double DrawFps(double deltaTime)
        {
            frameCount += 1;
            totalTime += deltaTime;

            double fps = (double)frameCount / totalTime;
            Title = "3D Engine    |    FPS: " + Math.Round(fps, 4).ToString();

            if (frameCount > 1000)
            {
                //frameCount = 0;
                //totalTime = 0;
            }

            return fps;
        }

        private void AddObject(Object obj)
        {
            if (obj.GetObjectType() == ObjectType.TextMesh)
                haveText = true;
            objects.Add(obj);
            objects.Sort();
        }     

        private bool DrawCorrectMesh(ref List<float> vertices, Type prevMeshType, Type currentMeshType)
        {
            if (prevMeshType == null || currentMeshType == null)
                return false;

            if(prevMeshType == typeof(Mesh) && prevMeshType != currentMeshType)
            {
                meshVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(TestMesh) && prevMeshType != currentMeshType)
            {
                testVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(NoTextureMesh) && prevMeshType != currentMeshType)
            {
                noTexVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(WireframeMesh) && prevMeshType != currentMeshType)
            {
                wireVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(UITextureMesh) && prevMeshType != currentMeshType)
            {
                uiTexVbo.Buffer(vertices);
            }
            else if(prevMeshType == typeof(TextMesh) && prevMeshType != currentMeshType)
            {
                textVbo.Buffer(vertices);
            }
            else
                return false;

            if(prevMeshType == typeof(WireframeMesh))
                GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            else
                GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
            return true;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            double fps = DrawFps(args.Time);

            frustum = character.camera.GetFrustum();

            GL.Enable(EnableCap.DepthTest);
            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            Type? currentMesh = null;
            List<float> vertices = new List<float>();
            foreach(Object o in objects)
            {
                ObjectType objectType = o.GetObjectType();
                if(objectType == ObjectType.Cube ||
                   objectType == ObjectType.Sphere ||
                   objectType == ObjectType.Capsule ||
                   objectType == ObjectType.TriangleMesh)
                {
                    Mesh mesh = (Mesh)o.GetMesh();

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(Mesh)))
                    //    vertices = new List<float>();

                    if(currentMesh == null || currentMesh != mesh.GetType())
                    {
                        shaderProgram.Use();
                    }

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(Mesh);

                    meshVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
                else if(objectType == ObjectType.TestMesh)
                {
                    TestMesh mesh = (TestMesh)o.GetMesh();

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(TestMesh)))
                    //    vertices = new List<float>();

                    if (currentMesh == null || currentMesh != mesh.GetType())
                    {
                        shaderProgram.Use();
                    }

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(TestMesh);

                    testVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
                else if(objectType == ObjectType.NoTexture)
                {
                    NoTextureMesh mesh = (NoTextureMesh)o.GetMesh();

                    if (DrawCorrectMesh(ref vertices, currentMesh == null ? typeof(NoTextureMesh) : currentMesh, typeof(NoTextureMesh)))
                        vertices = new List<float>();

                    if (currentMesh == null || currentMesh != mesh.GetType())
                    {
                        noTextureShaderProgram.Use();
                    }

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(NoTextureMesh);
                }
                else if(objectType == ObjectType.Wireframe)
                {
                    WireframeMesh mesh = (WireframeMesh)o.GetMesh();

                    if (DrawCorrectMesh(ref vertices, currentMesh == null ? typeof(WireframeMesh) : currentMesh, typeof(WireframeMesh)))
                        vertices = new List<float>();

                    if (currentMesh == null || currentMesh != mesh.GetType())
                    {
                        noTextureShaderProgram.Use();
                    }

                    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(WireframeMesh);
                }
                else if(objectType == ObjectType.UIMesh)
                {
                    UITextureMesh mesh = (UITextureMesh)o.GetMesh();

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(UITextureMesh)))
                    //    vertices = new List<float>();

                    if (currentMesh == null || currentMesh != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(UITextureMesh);

                    uiTexVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
                else if(objectType == ObjectType.TextMesh)
                {
                    TextMesh mesh = (TextMesh)o.GetMesh();

                    if (currentMesh == null || currentMesh != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    if (DrawCorrectMesh(ref vertices, currentMesh == null ? typeof(TextMesh) : currentMesh, typeof(TextMesh)))
                        vertices = new List<float>();

                    vertices.AddRange(mesh.Draw());
                    currentMesh = typeof(TextMesh);
                }
            }


            if (DrawCorrectMesh(ref vertices, currentMesh == null ? typeof(int) : currentMesh, typeof(int)))
                vertices = new List<float>();

            //Drawing character wireframe
            WireframeMesh characterWiremesh = character.mesh;
            noTextureShaderProgram.Use();
            characterWiremesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            vertices.AddRange(characterWiremesh.Draw());
            wireVbo.Buffer(vertices);
            GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            vertices = new List<float>();

            //foreach (Mesh mesh in meshes)
            //{
            //    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //    vertices.AddRange(mesh.Draw());
            //}
            //meshVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            ////-------------------------------TestMesh---------------------------------
            ////testMeshes[0].tris = character.GetTrianglesColliding(ref meshes[0].Octree);
            ////testMeshes[0].tris = meshes[0].Octree.GetNearTriangles(character.Position);
            //vertices = new List<float>();
            //foreach (TestMesh mesh in testMeshes)
            //{
            //    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //    vertices.AddRange(mesh.Draw());
            //}
            //testVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
            ////------------------------------------------------------------------------

            //noTextureShaderProgram.Use();
            //vertices = new List<float>();
            //foreach (PointLight pl in pointLights)
            //{
            //    pl.mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //    vertices.AddRange(pl.mesh.Draw());
            //}
            //noTexVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            //wireMeshes[0].lines = character.GetBoundLines();
            //vertices = new List<float>();
            //foreach (WireframeMesh mesh in wireMeshes)
            //{
            //    mesh.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //    vertices.AddRange(mesh.Draw());
            //}
            //foreach (Mesh mesh in meshes)
            //{
            //    if(mesh.drawNormals)
            //    {
            //        WireframeMesh m = mesh.normalMesh;
            //        m.UpdateFrustumAndCamera(ref frustum, ref character.camera);
            //        vertices.AddRange(m.Draw());
            //    }
            //}
            //wireVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);

            //// Text rendering
            //GL.Disable(EnableCap.DepthTest);

            //posTexShader.Use();
            //vertices = new List<float>();
            //foreach (UITextureMesh mesh in uiTexMeshes)
            //{
            //    vertices.AddRange(mesh.Draw());
            //}
            //uiTexVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            ((TextMesh)objects.Where(x => x.GetMesh().GetType() == typeof(TextMesh) && ((TextMesh)x.GetMesh()).currentText.Contains("Position")).First().GetMesh())
                        .ChangeText("Position = (" + character.PStr + ")");
            ((TextMesh)objects.Where(x => x.GetMesh().GetType() == typeof(TextMesh) && ((TextMesh)x.GetMesh()).currentText.Contains("Velocity")).First().GetMesh())
                        .ChangeText("Velocity = (" + character.VStr + ")");
            ((TextMesh)objects.Where(x => x.GetMesh().GetType() == typeof(TextMesh) && ((TextMesh)x.GetMesh()).currentText.Contains("IsOnGround")).First().GetMesh())
                        .ChangeText("IsOnGround = " + character.isOnGround.ToString());
            //textMeshes[2].ChangeText("GroundY = (" + character.groundYStr + ")");
            //textMeshes[3].ChangeText("DistToGround = (" + character.distToGroundStr + ")");
            //textMeshes[4].ChangeText("IsOnGround = (" + character.isOnGroundStr + ")");
            //textMeshes[5].ChangeText("AngleToGround = (" + character.angleOfGround.ToString() + ")");
            //textMeshes[6].ChangeText("ApplyGravity = (" + character.applyGravity.ToString() + ")");
            //vertices = new List<float>();
            //foreach (TextMesh textMesh in textMeshes)
            //{
            //    vertices.AddRange(textMesh.Draw());
            //}
            //textVbo.Buffer(vertices);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            //GL.DisableVertexAttribArray(0);

            Context.SwapBuffers();

            base.OnRenderFrame(args);

            if (limitFps)
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                if (elapsed < TargetDeltaTime)
                {
                    double sleepTime = TargetDeltaTime - elapsed;
                    Thread.Sleep((int)(sleepTime * 1000));
                }
                stopwatch.Restart();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
                Close();

            character.CalculateVelocity(KeyboardState, MouseState, args);
            character.UpdatePosition(KeyboardState, MouseState, args);
            character.AfterUpdate(MouseState, args);

            //if (temp != Math.Round(totalTime) || temp == -1)
            //{
            //    Object obj = new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitSphere(), "red.png", windowSize, ref frustum, ref character.camera, ref textureCount), ObjectType.Sphere, ref physx);
            //    obj.SetPosition(new Vector3(rnd.Next(-20, 20), 50, rnd.Next(-20, 20)));
            //    obj.SetSize(2);
            //    obj.AddSphereCollider(false);
            //    temp += 1;
            //    AddObject(obj);
            //}

            if (totalTime > 0)
            {
                physx.Simulate((float)args.Time);
                foreach (Object o in objects)
                {
                    o.CollisionResponse();
                }
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            CursorState = CursorState.Grabbed;

            if(limitFps)
                stopwatch = Stopwatch.StartNew();

            textGenerator = new TextGenerator();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);


            // OPENGL init
            meshVbo = new VBO();
            meshVao = new VAO(Mesh.floatCount);
            meshVao.LinkToVAO(0, 4, 0, meshVbo);
            meshVao.LinkToVAO(1, 3, 4, meshVbo);
            meshVao.LinkToVAO(2, 2, 7, meshVbo);

            testVbo = new VBO();
            testVao = new VAO(Mesh.floatCount);
            testVao.LinkToVAO(0, 4, 0, testVbo);
            testVao.LinkToVAO(1, 3, 4, testVbo);
            testVao.LinkToVAO(2, 2, 7, testVbo);

            textVbo = new VBO();
            textVao = new VAO(TextMesh.floatCount);
            textVao.LinkToVAO(0, 4, 0, textVbo);
            textVao.LinkToVAO(1, 4, 4, textVbo);
            textVao.LinkToVAO(2, 2, 8, textVbo);

            noTexVbo = new VBO();
            noTexVao = new VAO(NoTextureMesh.floatCount);
            noTexVao.LinkToVAO(0, 4, 0, noTexVbo);
            noTexVao.LinkToVAO(1, 4, 4, noTexVbo);

            uiTexVbo = new VBO();
            uiTexVao = new VAO(UITextureMesh.floatCount);
            uiTexVao.LinkToVAO(0, 4, 0, uiTexVbo);
            uiTexVao.LinkToVAO(1, 4, 4, uiTexVbo);
            uiTexVao.LinkToVAO(2, 2, 8, uiTexVbo);

            wireVbo = new VBO();
            wireVao = new VAO(WireframeMesh.floatCount);
            wireVao.LinkToVAO(0, 4, 0, wireVbo);
            wireVao.LinkToVAO(1, 4, 4, wireVbo);

            // Create the shader program
            shaderProgram = new Shader("Default.vert", "Default.frag");
            posTexShader = new Shader("postex.vert", "postex.frag");
            noTextureShaderProgram = new Shader("noTexture.vert", "noTexture.frag");

            // Create Physx context
            physx = new Physx(true);

            //Camera
            Camera camera = new Camera(windowSize);
            camera.UpdateVectors();

            //TEMP---------------------------------------
            //camera.position.X = -6.97959471f;
            //camera.position.Z = -7.161373f;
            //camera.yaw = 45.73648f;
            //camera.pitch = -18.75002f;
            //-------------------------------------------

            noTextureShaderProgram.Use();
            Vector3 characterPos = new Vector3(0, 20, 0);
            character = new Character(new WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera, Color4.White), ref physx, characterPos, camera);
            frustum = character.camera.GetFrustum();

            //Point Lights
            //pointLights.Add(new PointLight(new Vector3(0, 5000, 0), Color4.White, meshVao.id, shaderProgram.id, ref frustum, ref camera, noTexVao, noTexVbo, noTextureShaderProgram.id, pointLights.Count));

            shaderProgram.Use();
            PointLight.SendToGPU(ref pointLights, shaderProgram.id);

            // Projection matrix and mesh loading
            //meshCube.OnlyCube();
            //meshCube.OnlyTriangle();
            //meshCube.ProcessObj("spiro.obj");
            //meshes.Add(new Mesh(meshVao, meshVbo, shaderProgram.id, "spiro.obj", "High.png", 7, windowSize, ref frustum, ref camera, ref textureCount));
            //meshes.Last().CalculateNormalWireframe(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera);
            //testMeshes.Add(new TestMesh(testVao, testVbo, shaderProgram.id, "red.png", windowSize, ref frustum, ref camera, ref textureCount));

            objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, "spiro.obj", "High.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.TriangleMesh, ref physx));
            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitCube(), "red.png", windowSize, ref frustum, ref camera, ref textureCount), ObjectType.Cube, ref physx));
            //objects.Last().SetSize(new Vector3(10, 2, 10));
            //objects.Last().AddCubeCollider(true);

            //objects.Add(new Object(new Mesh(meshVao, meshVbo, shaderProgram.id, Object.GetUnitSphere(), "red.png", -1, windowSize, ref frustum, ref character.camera, ref textureCount), ObjectType.Sphere, ref physx));
            //objects.Last().SetPosition(new Vector3(0, 20, 0));
            //objects.Last().SetSize(2);
            //objects.Last().AddSphereCollider(false);

            posTexShader.Use();

            Object textObj1 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj1.GetMesh()).ChangeText("Position = (" + character.PStr + ")");
            ((TextMesh)textObj1.GetMesh()).Position = new Vector2(10, windowSize.Y - 35);
            ((TextMesh)textObj1.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddObject(textObj1);

            Object textObj2 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj2.GetMesh()).ChangeText("Velocity = (" + character.VStr + ")");
            ((TextMesh)textObj2.GetMesh()).Position = new Vector2(10, windowSize.Y - 65);
            ((TextMesh)textObj2.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddObject(textObj2);

            Object textObj3 = new Object(new TextMesh(textVao, textVbo, posTexShader.id, "font.png", windowSize, ref textGenerator, ref textureCount), ObjectType.TextMesh, ref physx);
            ((TextMesh)textObj3.GetMesh()).ChangeText("IsOnGround = " + character.isOnGround.ToString());
            ((TextMesh)textObj3.GetMesh()).Position = new Vector2(10, windowSize.Y - 95);
            ((TextMesh)textObj3.GetMesh()).Scale = new Vector2(1.5f, 1.5f);
            AddObject(textObj3);

            //uiTexMeshes.Add(new UITextureMesh(uiTexVao, uiTexVbo, posTexShader.id, "bmp_24.bmp", new Vector2(10, 10), new Vector2(100, 100), windowSize, ref textureCount));

            // We have text on screen
            if (haveText)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }

            objects.Sort();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(meshVao.id);
            GL.DeleteVertexArray(testVao.id);
            GL.DeleteVertexArray(textVao.id);
            GL.DeleteVertexArray(noTexVao.id);
            GL.DeleteVertexArray(uiTexVao.id);
            GL.DeleteVertexArray(wireVao.id);

            foreach(Object obj in objects)
            {
                BaseMesh mesh = obj.GetMesh();
                GL.DeleteBuffer(mesh.vbo);               
            }

            shaderProgram.Unload();
            noTextureShaderProgram.Unload();
            posTexShader.Unload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            windowSize.X = e.Width;
            windowSize.Y = e.Height;
        }
    }
}
