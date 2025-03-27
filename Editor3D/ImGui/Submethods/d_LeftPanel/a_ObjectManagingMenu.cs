using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        private bool wasPopupOpen = false;

        public void ObjectManagingMenu()
        {
            if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
            {
                wasPopupOpen = true;
                engine.editorMenuOpen = true;
                if (ImGui.MenuItem("Empty Object"))
                {
                    engine.AddObject(ObjectType.Empty);
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                    engine.editorMenuOpen = false;
                }
                if (ImGui.BeginMenu("3D Object"))
                {
                    if (ImGui.MenuItem("Cube"))
                    {
                        engine.AddObject(ObjectType.Cube);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }
                    if (ImGui.MenuItem("Sphere"))
                    {
                        engine.AddObject(ObjectType.Sphere);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }
                    if (ImGui.MenuItem("Capsule"))
                    {
                        engine.AddObject(ObjectType.Capsule);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }
                    if (ImGui.MenuItem("Plane"))
                    {
                        engine.AddObject(ObjectType.Plane);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }
                    if (ImGui.MenuItem("Mesh"))
                    {
                        engine.AddObject(ObjectType.TriangleMesh);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Particle system"))
                {
                    engine.AddParticleSystem();
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                    engine.editorMenuOpen = false;
                }
                if (ImGui.MenuItem("Audio emitter"))
                {
                    engine.AddObject(ObjectType.AudioEmitter);
                    shouldOpenTreeNodeMeshes = true;
                    editorData.recalculateObjects = true;
                    engine.editorMenuOpen = false;
                }
                if (ImGui.BeginMenu("Lighting"))
                {
                    if (ImGui.MenuItem("Point Light"))
                    {
                        engine.AddLight(Light.LightType.PointLight);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }
                    if (ImGui.MenuItem("Directional Light"))
                    {
                        engine.AddLight(Light.LightType.DirectionalLight);
                        shouldOpenTreeNodeMeshes = true;
                        editorData.recalculateObjects = true;
                        engine.editorMenuOpen = false;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndPopup();
            }
            else
            {
                if (wasPopupOpen)
                {
                    engine.editorMenuOpen = false;
                }
                wasPopupOpen = false;
            }
        }
    }
}
