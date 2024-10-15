﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Engine3D
{

    public class UITextureMesh : BaseMesh
    {
        public static int floatCount = 10;

        private Vector2 windowSize;

        private List<float> vertices = new List<float>();

        private Vector3 position;
        public Vector2 Position
        {
            get { return new Vector2(position.X, position.Y); }
            set { position = new Vector3(value.X, value.Y, 0); }
        }
        public Vector3 Size;
        public float Rotation
        {
            set
            {
                rotation = new Vector3(0, 0, value);
            }
            get
            {
                return rotation.Z;
            }
        }
        private Vector3 rotation;

        private VAO Vao;
        private VBO Vbo;

        public UITextureMesh(VAO vao, VBO vbo, int shaderProgramId, string texturePath, Vector2 position, Vector2 size, Vector2 windowSize) : base(vao.id, vbo.id, shaderProgramId)
        {
            throw new NotImplementedException();

            this.windowSize = windowSize;
            Position = new Vector2(position.X, position.Y);
            Size = new Vector3(size.X, size.Y, 0);
            rotation = Vector3.Zero;

            bool success = false;
            texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            OnlyQuad();

            GetUniformLocations();
            SendUniforms(null);
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
        }

        protected override void SendUniforms(Vector3? lightDir)
        {
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            if(texture != null)
                GL.Uniform1(uniformLocations["textureSampler"], texture.TextureUnit);
            else
                GL.Uniform1(uniformLocations["textureSampler"], -1);
        }

        //private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        //{
        //    Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

        //    float x = (2.0f * v.X / windowSize.X) - 1.0f;
        //    float y = (2.0f * v.Y / windowSize.Y) - 1.0f;

        //    List<float> result = new List<float>()
        //    {
        //        x, y, -1.0f, 1.0f,
        //        tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A,
        //        tri.t[index].u, tri.t[index].v
        //    };

        //    return result;
        //}

        public List<float> Draw(GameState gameRunning)
        {
            Vao.Bind();

            if (gameRunning == GameState.Stopped && vertices.Count > 0)
            {
                SendUniforms(null);

                if (texture != null)
                {
                    texture.Bind();
                }

                return vertices;
            }

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Size);
            Matrix4 t = Matrix4.CreateTranslation(position);
            Matrix4 transformMatrix = s * t;

            if (rotation != Vector3.Zero)
            {
                Matrix4 toOrigin = Matrix4.CreateTranslation(-Size.X / 2, -Size.Y / 2, 0);
                Matrix4 fromOrigin = Matrix4.CreateTranslation(Size.X / 2, Size.Y / 2, 0);
                Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation.Z));
                transformMatrix = s * toOrigin * rZ * fromOrigin * t;
            }

            //foreach (triangle tri in tris)
            //{
            //    vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
            //    vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
            //    vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
            //}

            SendUniforms(null);

            if(texture != null)
                texture.Bind();

            return vertices;
        }

        private void OnlyQuad()
        {
            throw new NotImplementedException();
            //tris = new List<triangle>
            //{
            //    new triangle(new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0) },
            //                      new Vec2d[] { new Vec2d(0, 0), new Vec2d(0, 1), new Vec2d(1, 0) }),
            //    new triangle(new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
            //                      new Vec2d[] { new Vec2d(1, 0), new Vec2d(0, 1), new Vec2d(1, 1) })
            //};
        }
    }
}
