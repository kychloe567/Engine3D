﻿using MagicPhysX;
using OpenTK.Mathematics;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Engine3D
{
    public class triangle
    {
        public Vertex[] v;
        public uint[] vi;
        public bool gotPointNormals;

        public bool visibile = false;

        public triangle()
        {
            gotPointNormals = false;
            v = new Vertex[3] { new Vertex(), new Vertex(), new Vertex() };
            vi = new uint[3];
        }

        public triangle(triangle tri)
        {
            gotPointNormals = tri.gotPointNormals;

            v = new Vertex[3] { tri.v[0], tri.v[1], tri.v[2] };
            vi = new uint[3] { tri.vi[0], tri.vi[1], tri.vi[2] };
        }

        public triangle(Vector3[] p)
        {
            gotPointNormals = false;

            v = new Vertex[] { new Vertex(), new Vertex(), new Vertex() };
            v[0].p = p[0];
            v[1].p = p[1];
            v[2].p = p[2];

            vi = new uint[3];
        }

        public triangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            gotPointNormals = false;

            v = new Vertex[] { new Vertex(), new Vertex(), new Vertex() };
            v[0].p = p1;
            v[1].p = p2;
            v[2].p = p3;

            vi = new uint[3];
        }

        public triangle(Vector3[] p, Vec2d[] t)
        {
            gotPointNormals = false;

            v = new Vertex[] { new Vertex(), new Vertex(), new Vertex() };
            v[0].p = p[0];
            v[1].p = p[1];
            v[2].p = p[2];

            v[0].t = t[0];
            v[1].t = t[1];
            v[2].t = t[2];

            vi = new uint[3];
        }

        public triangle(Vector3[] p, Vec2d[] t, Color4[] color)
        {
            gotPointNormals = false;

            v = new Vertex[] { new Vertex(), new Vertex(), new Vertex() };
            v[0].p = p[0];
            v[1].p = p[1];
            v[2].p = p[2];

            v[0].t = t[0];
            v[1].t = t[1];
            v[2].t = t[2];

            v[0].c = color[0];
            v[1].c = color[1];
            v[2].c = color[2];

            vi = new uint[3];
        }

        public triangle(Vector3[] p, Vector3[] n, Vec2d[] t)
        {
            gotPointNormals = true;

            v = new Vertex[] { new Vertex(), new Vertex(), new Vertex() };
            v[0].p = p[0];
            v[1].p = p[1];
            v[2].p = p[2];

            v[0].n = n[0];
            v[1].n = n[1];
            v[2].n = n[2];

            v[0].t = t[0];
            v[1].t = t[1];
            v[2].t = t[2];

            vi = new uint[3];
        }

        public Vector3 GetCenter()
        {
            return (v[0].p + v[1].p + v[2].p) / 3;
        }

        public void TransformPosition(Vector3 transform)
        {
            v[0].p += transform;
            v[1].p += transform;
            v[2].p += transform;
        }

        public float DistanceToCamera(Vector3 cameraPosition)
        {
            return (GetCenter() - cameraPosition).Length;
        }

        public Plane GetPlane()
        {
            return new Plane(v[0].p, v[1].p, v[2].p);
        }

        public bool RayIntersects(Vector3 rayOrigin, Vector3 rayDir, out Vector3 intersection)
        {
            intersection = new Vector3();

            Vector3 e1 = v[1].p - v[0].p;
            Vector3 e2 = v[2].p - v[0].p;
            Vector3 h = Vector3.Cross(rayDir, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -float.Epsilon && a < float.Epsilon)
                return false;

            float f = 1.0f / a;
            Vector3 s = rayOrigin - v[0].p;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, e1);
            float vdot = f * Vector3.Dot(rayDir, q);

            if (vdot < 0.0f || u + vdot > 1.0f)
                return false;

            float t = f * Vector3.Dot(e2, q);

            if (t > float.Epsilon)
            {
                intersection = rayOrigin + rayDir * t;
                return true;
            }

            return false;
        }

        public Vector3 GetPointAboveXZ(Vector3 point)
        {
            Vector3 rayDir = new Vector3(0, -1, 0);

            if (RayIntersects(point, rayDir, out Vector3 intersection))
            {
                return intersection;
            }
            return Vector3.NegativeInfinity;
        }

        public void SetColor(Color4 c)
        {
            for (int i = 0; i < v.Length; i++)
            {
                v[i].c = c;
            }
        }

        public void SetColor(triangle t)
        {
            for (int i = 0; i < v.Length; i++)
            {
                v[i].c = t.v[i].c;
            }
        }

        public string GetPointsStr()
        {
            return v[0].p.ToString() + v[1].p.ToString() + v[2].p.ToString();
        }

        public Vector3 ComputeTriangleNormal()
        {
            Vector3 normal, line1, line2;
            line1 = v[1].p - v[0].p;
            line2 = v[2].p - v[0].p;

            normal = Vector3.Cross(line1, line2);
            normal.Normalize();
            return normal;
        }

        public void ComputeTriangleNormal(ref Matrix4 transformMatrix)
        {
            Vector3 p0 = Vector3.TransformPosition(v[0].p, transformMatrix);
            Vector3 p1 = Vector3.TransformPosition(v[1].p, transformMatrix);
            Vector3 p2 = Vector3.TransformPosition(v[2].p, transformMatrix);

            Vector3 normal, line1, line2;
            line1 = p1 - p0;
            line2 = p2 - p0;

            normal = Vector3.Cross(line1, line2);
            normal.Normalize();
            v[0].n = normal;
            v[1].n = normal;
            v[2].n = normal;
        }

        public float GetAngleToNormal(Vector3 dir)
        {
            Vector3 n = ComputeTriangleNormal();
            float dotProduct = Vector3.Dot(n, dir);
            float magnitudeA = n.Length;
            float magnitudeB = dir.Length;

            float angle = MathHelper.RadiansToDegrees((float)Math.Acos(dotProduct / (magnitudeA * magnitudeB)));

            if (angle <= 180)
            {
                return 180 - angle;
            }
            else
            {
                return angle - 180;
            }
        }

        public triangle GetCopy()
        {
            triangle tri = new triangle();
            tri.v[0] = v[0];
            tri.v[1] = v[1];
            tri.v[2] = v[2];

            return tri;
        }

        public int CompareTo(triangle tri)
        {
            double z1 = (v[0].p.Z + v[1].p.Z + v[2].p.Z) / 3.0f;
            double z2 = (tri.v[0].p.Z + tri.v[1].p.Z + tri.v[2].p.Z) / 3.0f;

            if (z1 < z2)
                return 1;
            else if (z1 > z2)
                return -1;
            else
                return 0;
        }

        public bool IsPointInTriangle(Vector3 p, out float distance)
        {
            Vector3 a = this.v[0].p;
            Vector3 b = this.v[1].p;
            Vector3 c = this.v[2].p;

            Vector3 normal = Vector3.Cross(b - a, c - a).Normalized();
            distance = Vector3.Dot(normal, p - a);

            Vector3 u = b - a;
            Vector3 v = c - a;
            Vector3 w = p - a;

            float uu = Vector3.Dot(u, u);
            float uv = Vector3.Dot(u, v);
            float vv = Vector3.Dot(v, v);
            float wu = Vector3.Dot(w, u);
            float wv = Vector3.Dot(w, v);
            float denom = 1.0f / (uv * uv - uu * vv);

            float s = (uv * wv - vv * wu) * denom;
            float t = (uv * wu - uu * wv) * denom;

            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        public bool IsLineInTriangle(Line line)
        {
            Vector3 normal = Vector3.Cross(v[1].p - v[0].p, v[2].p - v[0].p);

            // Find intersection of line and triangle's plane
            float dotNumerator = Vector3.Dot(v[0].p - line.Start, normal);
            float dotDenominator = Vector3.Dot(line.End - line.Start, normal);

            // Check if line and plane are parallel
            if (Math.Abs(dotDenominator) < 0.000001f)
            {
                return false;  // They are parallel so they don't intersect!
            }

            float t = dotNumerator / dotDenominator;
            Vector3 intersectionPoint = line.Start + t * (line.End - line.Start);

            // Check if the intersection point lies within the triangle
            return PointInTriangle(intersectionPoint, v[0].p, v[1].p, v[2].p);
        }

        private bool PointInTriangle(Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 d1 = pt - v1;
            Vector3 d2 = pt - v2;
            Vector3 d3 = pt - v3;

            float area = Vector3.Dot(Vector3.Cross(v1 - v2, v1 - v3).Normalized(), Vector3.Cross(v2 - v3, v2 - v1).Normalized());
            float area1 = Vector3.Dot(Vector3.Cross(d1, d2).Normalized(), Vector3.Cross(d1, d3).Normalized());

            return (Math.Abs(area - area1) < 0.000001f);
        }

        public bool IsRectInTriangle(List<Vector3> rectangleVertices)
        {
            List<Vector3> potentialSeparatingAxes = GetPotentialSeparatingAxes(rectangleVertices);

            foreach (Vector3 axis in potentialSeparatingAxes)
            {
                List<Vector3> vertices = v.Select(x => x.p).ToList();
                if (!IsOverlapping(Projection(rectangleVertices, axis), Projection(vertices, axis)))
                    return false; // Separating axis found
            }

            return true;  // All projections overlap
        }

        private List<Vector3> GetPotentialSeparatingAxes(List<Vector3> rectangleVertices)
        {
            List<Vector3> axes = new List<Vector3>();

            // Triangle normal
            Vector3 triangleNormal = Vector3.Cross(v[1].p - v[0].p, v[2].p - v[0].p);
            axes.Add(triangleNormal.Normalized());

            // Rectangle normals (parallelogram)
            Vector3 edge1 = rectangleVertices[1] - rectangleVertices[0];
            Vector3 edge2 = rectangleVertices[2] - rectangleVertices[0];
            axes.Add(Vector3.Cross(edge1, edge2).Normalized());

            // Cross products of edges
            Vector3 triangleEdge1 = v[1].p - v[0].p;
            Vector3 triangleEdge2 = v[2].p - v[1].p;
            Vector3 triangleEdge3 = v[0].p - v[2].p;
            Vector3[] triangleEdges = { triangleEdge1, triangleEdge2, triangleEdge3 };

            for (int i = 0; i < triangleEdges.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Vector3 rectangleEdge = rectangleVertices[(j + 1) % 4] - rectangleVertices[j];
                    Vector3 axis = Vector3.Cross(triangleEdges[i], rectangleEdge).Normalized();

                    if (axis.LengthSquared > 0.0001f)  // Avoid nearly-zero vectors
                        axes.Add(axis);
                }
            }

            return axes;
        }

        private (float, float) Projection(List<Vector3> vertices, Vector3 axis)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (Vector3 vertex in vertices)
            {
                float dot = Vector3.Dot(vertex, axis);
                if (dot < min) min = dot;
                if (dot > max) max = dot;
            }

            return (min, max);
        }

        private bool IsOverlapping((float, float) a, (float, float) b)
        {
            return a.Item1 <= b.Item2 && b.Item1 <= a.Item2;
        }

        public bool IsSphereInTriangle(Sphere sphere, out Vector3 penetration_normal, out float penetration_depth)
        {
            penetration_normal = new Vector3();
            penetration_depth = 0.0f;

            Vector3 normal = ComputeTriangleNormal();
            float d = Vector3.Dot(normal, v[0].p);

            // Project point onto triangle plane
            float t = (Vector3.Dot(normal, sphere.Position) - d) / Vector3.Dot(normal, normal);
            Vector3 projectedPoint = sphere.Position - t * normal;

            float temp;
            // If projected point is inside triangle, it's the closest point
            if (IsPointInTriangle(projectedPoint, out temp))
            {
                float l1 = (projectedPoint - sphere.Position).Length;
                if (l1 < sphere.Radius)
                {
                    penetration_normal = (sphere.Position - projectedPoint).Normalized();
                    if (float.IsNaN(penetration_normal.X) || float.IsNaN(penetration_normal.Y) || float.IsNaN(penetration_normal.Z))
                        penetration_normal = Vector3.Zero;

                    penetration_depth = sphere.Radius - l1;

                    return true;
                }
                else
                    return false;
            }

            // Otherwise, find the closest point on each edge
            Vector3 closestPoint = Vector3.Zero;
            float minDistance = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                Vector3 segmentStart = v[i].p;
                Vector3 segmentEnd = v[(i + 1) % 3].p;
                Vector3 closestOnSegment = ClosestPointOnSegment(segmentStart, segmentEnd, sphere.Position);
                float distance = (closestOnSegment - sphere.Position).LengthSquared;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = closestOnSegment;
                }
            }

            float l2 = (closestPoint - sphere.Position).Length;
            if (l2 < sphere.Radius)
            {
                penetration_normal = (sphere.Position - closestPoint).Normalized();
                if (float.IsNaN(penetration_normal.X) || float.IsNaN(penetration_normal.Y) || float.IsNaN(penetration_normal.Z))
                    penetration_normal = Vector3.Zero;

                penetration_depth = sphere.Radius - l2;

                return true;
            }
            else
                return false;

            //return closestPoint;
        }

        private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
            t = Math.Clamp(t, 0f, 1f);
            return a + t * ab;
        }

        public bool IsCapsuleInTriangle(Capsule capsule, out Vector3 penetration_normal, out float penetration_depth)
        {
            Vector3 aTip = capsule.Position + new Vector3(0, capsule.Height, 0);
            Vector3 aBase = capsule.Position;

            // Compute capsule line endpoints A, B like before in capsule-capsule case:
            Vector3 CapsuleNormal = Vector3.Normalize(aTip - aBase);
            Vector3 LineEndOffset = CapsuleNormal * capsule.Radius;
            Vector3 A = aBase + LineEndOffset;
            Vector3 B = aTip - LineEndOffset;

            Vector3 N = Vector3.Normalize(Vector3.Cross(v[1].p - v[0].p, v[2].p - v[0].p)); // plane normal

            // Then for each triangle, ray-plane intersection:
            //  N is the triangle plane normal (it was computed in sphere – triangle intersection case)
            float t = Vector3.Dot(N, (v[0].p - aBase) / Math.Abs(Vector3.Dot(N, CapsuleNormal)));
            Vector3 line_plane_intersection = aBase + CapsuleNormal * t;

            Vector3 reference_point = new Vector3();

            // Determine whether line_plane_intersection is inside all triangle edges: 
            Vector3 c0 = Vector3.Cross(line_plane_intersection - v[0].p, v[1].p - v[0].p);
            Vector3 c1 = Vector3.Cross(line_plane_intersection - v[1].p, v[2].p - v[1].p);
            Vector3 c2 = Vector3.Cross(line_plane_intersection - v[2].p, v[0].p - v[2].p);
            bool inside = Vector3.Dot(c0, N) <= 0 && Vector3.Dot(c1, N) <= 0 && Vector3.Dot(c2, N) <= 0;

            if (inside)
            {
                reference_point = line_plane_intersection;
            }
            else
            {
                // Edge 1:
                Vector3 point1 = Line.ClosestPointOnLineSegment(v[0].p, v[1].p, line_plane_intersection);
                Vector3 v1 = line_plane_intersection - point1;
                float distsq = Vector3.Dot(v1, v1);
                float best_dist = distsq;
                reference_point = point1;

                // Edge 2:
                Vector3 point2 = Line.ClosestPointOnLineSegment(v[1].p, v[2].p, line_plane_intersection);
                Vector3 v2 = line_plane_intersection - point2;
                distsq = Vector3.Dot(v2, v2);
                if (distsq < best_dist)
                {
                    reference_point = point2;
                    best_dist = distsq;
                }

                // Edge 3:
                Vector3 point3 = Line.ClosestPointOnLineSegment(v[2].p, v[0].p, line_plane_intersection);
                Vector3 v3 = line_plane_intersection - point3;
                distsq = Vector3.Dot(v3, v3);
                if (distsq < best_dist)
                {
                    reference_point = point3;
                    best_dist = distsq;
                }
            }

            // The center of the best sphere candidate:
            Vector3 center = Line.ClosestPointOnLineSegment(A, B, reference_point);

            bool intersects = IsSphereInTriangle(new Sphere(center, capsule.Radius), out penetration_normal, out penetration_depth);

            return intersects;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            triangle other = (triangle)obj;
            return v[0].p == other.v[0].p && v[1].p == other.v[1].p && v[2].p == other.v[2].p;
        }

        public override int GetHashCode()
        {
            // Here's a simple way to get a hash code, but you might want to refine this for better distribution.
            return v[0].p.GetHashCode() ^ v[1].p.GetHashCode() ^ v[2].p.GetHashCode();
        }

        public static bool operator ==(triangle t1, triangle t2)
        {
            // If both are null, or both are the same instance, return true.
            if (ReferenceEquals(t1, t2))
                return true;

            // If one is null, but not both, return false.
            if (t1 is null || t2 is null)
                return false;

            // Use the Equals() method for the actual comparison.
            return t1.Equals(t2);
        }

        public static bool operator !=(triangle t1, triangle t2)
        {
            return !(t1 == t2);
        }
    }
}