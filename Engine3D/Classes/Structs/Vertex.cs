﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public struct Vertex
    {
        public Vector3 p;
        public Vector3 n;
        public Color4 c;
        public Vec2d t;
        public int pi;
        public Vector3 tan;
        public Vector3 bitan;

        public bool gotNormal;

        public Vertex()
        {
            p = Vector3.Zero;
            n = Vector3.Zero;
            c = Color4.White;
            t = Vec2d.Zero;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos)
        {
            p = pos;
            n = Vector3.Zero;
            c = Color4.White;
            t = Vec2d.Zero;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos, Vec2d tex)
        {
            p = pos;
            n = Vector3.Zero;
            c = Color4.White;
            t = tex;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos, Vector3 normal, Vec2d tex)
        {
            p = pos;
            n = normal;
            c = Color4.White;
            t = tex;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = true;
        }

        public float[] GetData()
        {
            return new float[]
            {
                p.X, p.Y, p.Z,
                n.X, n.Y, n.Z,
                t.u, t.v,
                c.R, c.G, c.B, c.A,
                tan.X, tan.Y, tan.Z
            };
        }

        public float[] GetDataOnlyPos()
        {
            return new float[]
            {
                p.X, p.Y, p.Z
            };
        }

        public float[] GetDataOnlyPosAndNormal()
        {
            return new float[]
            {
                p.X, p.Y, p.Z,
                n.X, n.Y, n.Z
            };
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(p);
            hashCode.Add(n);
            hashCode.Add(t);
            return hashCode.ToHashCode();
        }
    }
}