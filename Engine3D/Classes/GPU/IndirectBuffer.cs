﻿using Engine3D.Classes.GPU;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class IndirectBuffer
    {
        public int id;

        public IndirectBuffer()
        {
            id = GL.GenBuffer();

            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
            {
                count = 0, // vertexStride is the number of floats per vertex
                instanceCount = 0, // Use 1 if you're not instancing
                first = 0,
                baseInstance = 0
            };

            Buffer(cmd);
        }

        public void Buffer(DrawArraysIndirectCommand cmd)
        {
            Bind();
            GL.BufferData(BufferTarget.DrawIndirectBuffer, (IntPtr)Marshal.SizeOf(typeof(DrawArraysIndirectCommand)), ref cmd, BufferUsageHint.DynamicDraw);
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, id);
        }

        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);
        }

        public void Delete()
        {
            GL.DeleteBuffer(id);
        }
    }
}