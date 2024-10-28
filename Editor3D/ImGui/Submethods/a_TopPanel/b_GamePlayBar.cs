﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void GamePlayBar(ref ImGuiStylePtr style)
        {
            var button = style.Colors[(int)ImGuiCol.Button];
            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            float totalWidth = 40;
            if (editorData.gameRunning == GameState.Running)
                totalWidth = 60;

            float startX = (ImGui.GetWindowSize().X - totalWidth) * 0.5f;
            float startY = (ImGui.GetWindowSize().Y - 10) * 0.5f;
            ImGui.SetCursorPos(new System.Numerics.Vector2(startX, startY));

            if (editorData.gameRunning == GameState.Stopped)
            {
                if (ImGui.ImageButton("play", (IntPtr)engineData.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.gameRunning = GameState.Running;
                    editorData.justSetGameState = true;
                    engine.SetGameState(editorData.gameRunning);
                }
            }
            else
            {
                if (ImGui.ImageButton("stop", (IntPtr)engineData.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.gameRunning = GameState.Stopped;
                    editorData.justSetGameState = true;
                    engine.SetGameState(editorData.gameRunning);
                }
            }

            ImGui.SameLine();
            if (editorData.gameRunning == GameState.Running)
            {
                if (ImGui.ImageButton("pause", (IntPtr)engineData.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isPaused = !editorData.isPaused;
                }
            }

            ImGui.SameLine();
            if (ImGui.ImageButton("screen", (IntPtr)engineData.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
            {
                editorData.isGameFullscreen = !editorData.isGameFullscreen;
                editorData.windowResized = true;
            }

            style.Colors[(int)ImGuiCol.Button] = button;
        }
    }
}
