﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8602

namespace Engine3D
{
    public class Particle
    {
        public InstancedMeshData meshData;

        private float lifetime;
        private float maxLifetime;
        private Vector3 position;
        private Vector3 velocity;

        private ParticleSystem emitter;

        // Properties
        private Vector3 dir;
        private float startSpeed;
        private float endSpeed;

        private Quaternion startRotation;
        private Quaternion endRotation;

        private Vector3 startScale;
        private Vector3 endScale;

        private Color4 startColor;
        private Color4 endColor;

        public Particle(Vector3 startPos, ParticleSystem emitter_, Vector3 dir,
            float startSpeed, float endSpeed,
            Quaternion startRotation, Quaternion endRotation, 
            Vector3 startScale, Vector3 endScale, 
            Color4 startColor, Color4 endColor)
        {
            position = startPos;
            emitter = emitter_;
            this.dir = dir;
            this.startSpeed = startSpeed;
            this.endSpeed = endSpeed;
            this.startRotation = startRotation;
            this.endRotation = endRotation;
            this.startScale = startScale;
            this.endScale = endScale;
            this.startColor = startColor;
            this.endColor = endColor;

            meshData = new InstancedMeshData();
            meshData.Position = startPos;
            meshData.Rotation = startRotation;
            meshData.Scale = startScale;
            meshData.Color = startColor;

            lifetime = emitter.lifetime;
            maxLifetime = emitter.lifetime;
        }

        public bool Update(float delta)
        {
            float normLifetime = 1 - lifetime/maxLifetime;

            lifetime -= delta;
            if (lifetime <= 0)
                return true;

            velocity = dir * Helper.Lerp(startSpeed, endSpeed, normLifetime);
            position += velocity * delta;
            meshData.Position = position;
            meshData.Rotation = Quaternion.Slerp(startRotation, endRotation, normLifetime);
            meshData.Scale = Vector3.Lerp(startScale, endScale, normLifetime);
            meshData.Color = Helper.LerpColor(startColor, endColor, normLifetime);


            return false;
        }
    }

    public class ParticleSystem : IComponent
    {
        public float duration = 5;
        public bool looping = true;
        public bool useGravity = false;

        public float startDelay = 0;

        public float emitTimeSec = 0.2f;
        private float time;

        public float lifetime = 5;
        public bool randomLifeTime = false;
        public float xLifeTime = 5;
        public float yLifeTime = 10;

        public Vector3 startPos = Vector3.Zero;
        public bool randomStartPos = false;
        public AABB xStartPos = new AABB(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));

        public Vector3 startDir = Vector3.UnitY;
        public bool randomDir = false;

        public float startSpeed = 5;
        public float endSpeed = 5;
        public bool randomSpeed = false;
        public float xStartSpeed = 5;
        public float yStartSpeed = 10;
        public float xEndSpeed = 5;
        public float yEndSpeed = 10;

        public Vector3 startScale = Vector3.One;
        public Vector3 endScale = Vector3.One;
        public bool randomScale = false;
        public AABB xStartScale = new AABB(new Vector3(1, 1, 1), new Vector3(1, 1, 1));
        public AABB xEndScale = new AABB(new Vector3(0, 0, 0), new Vector3(0, 0, 0));

        public Quaternion startRotation = Quaternion.Identity;
        public Quaternion endRotation = Quaternion.Identity;
        public bool randomRotation = false;

        public Color4 startColor = Color4.White;
        public Color4 endColor = Color4.White;
        public bool randomColor = false;

        private List<Particle> particles = new List<Particle>();

        private InstancedMesh mesh;
        private Object parentObject;

        public ParticleSystem(InstancedVAO instancedMeshVao, VBO instancedMeshVbo, int shaderProgramId, Vector2 windowSize, ref Camera camera, ref Object parentObject)
        {
            mesh = new InstancedMesh(instancedMeshVao, instancedMeshVbo, shaderProgramId, "cube", BaseMesh.GetUnitCube(), windowSize, ref camera, ref parentObject);

            this.parentObject = parentObject;
        }

        private void UpdateParticleAndRemove(ref List<Particle> toRemove, Particle p, float delta)
        {
            if (p == null)
                return;

            bool remove = p.Update(delta);
            if (remove)
            {
                lock (toRemove)
                {
                    toRemove.Add(p);
                }
            }
        }

        public void RemoveAllParticles()
        {
            particles.Clear();
        }

        public int GetParticleCount()
        {
            return particles.Count;
        }

        public void Update(float delta)
        {
            time += delta;
            if(time >= emitTimeSec)
            {
                time = 0;
                AddNewParticle();
            }

            List<Particle> toRemove = new List<Particle>();

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize }; // Adjust as needed
            Parallel.ForEach(particles, parallelOptions, particle =>
            {
                UpdateParticleAndRemove(ref toRemove, particle, delta);
            });

            Parallel.ForEach(toRemove, parallelOptions, r =>
            {
                particles.Remove(r);
            });
            //for (int i = 0; i < toRemove.Count(); i++)
            //{
            //    particles.Remove(toRemove[i]);
            //}

            toRemove.Clear();
        }

        private void AddNewParticle()
        {
            Vector3 pos = startPos;
            if(randomStartPos)
                pos = Helper.GetRandomVectorInAABB(xStartPos);
            pos = parentObject.transformation.Position + pos;

            Vector3 dir = startDir;
            if (randomDir)
                dir = Helper.GetRandomNormVector();
                //dir = Helper.GetForwardVectorFromQuaternion(xStartDir);

            float sSpeed = startSpeed;
            float eSpeed = endSpeed;
            if(randomSpeed)
            {
                sSpeed = Helper.GetRandomNumberBetween(xStartSpeed, yStartSpeed);
                eSpeed = Helper.GetRandomNumberBetween(xEndSpeed, yEndSpeed);
            }

            Quaternion sRot = startRotation;
            Quaternion eRot = endRotation;
            if(randomRotation)
            {
                sRot = Helper.GetRandomQuaternion();
                eRot = Helper.GetRandomQuaternion();
            }

            Vector3 sScale = startScale;
            Vector3 eScale = endScale;
            if(randomScale)
            {
                sScale = Helper.GetRandomScale(xStartScale);
                eScale = Helper.GetRandomScale(xEndScale);
            }

            Color4 sColor = startColor;
            Color4 eColor = endColor;
            if(randomColor)
            {
                sColor = Helper.GetRandomColor();
                eColor = Helper.GetRandomColor();
            }

            Particle p = new Particle(pos, this, dir,
                                      sSpeed, eSpeed,
                                      sRot, eRot,
                                      sScale, eScale,
                                      sColor, eColor);
            particles.Add(p);
        }

        public BaseMesh GetParentMesh()
        {
            List<InstancedMeshData> data = particles.Where(x => x != null).Select(x => x.meshData).ToList();
            //data.Sort((x,y) => x)

            mesh.SetInstancedData(data);

            return mesh;
        }

    }
}