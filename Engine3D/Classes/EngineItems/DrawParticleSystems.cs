﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void DrawParticleSystems()
        {

            //GL.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.OneMinusSrcColor);
            foreach (ParticleSystem ps in particleSystems)
            {
                Object psO = ps.GetObject();
                psO.GetMesh().CalculateFrustumVisibility();

                InstancedMesh mesh = (InstancedMesh)psO.GetMesh();

                mesh.Draw(editorData.gameRunning, instancedShaderProgram, meshVbo, instancedMeshVbo, meshIbo);
            }
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
    }
}
