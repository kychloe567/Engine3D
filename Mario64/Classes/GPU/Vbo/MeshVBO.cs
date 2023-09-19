﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public class MeshVBO : BaseVBO
    {
        int vertexSize;
        public MeshVBO(List<Vertex> data)
        {
            vertexSize = Marshal.SizeOf(typeof(Vertex));
            id = GL.GenBuffer();
            Bind();

            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }

        public void Buffer(List<Vertex> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }
    }
}
