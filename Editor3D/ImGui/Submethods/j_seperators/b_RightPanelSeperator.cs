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
        public void RightPanelSeperator(ref ImGuiStylePtr style)
        {
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize,
                _windowHeight - editorData.gameWindow.topPanelSize - editorData.gameWindow.bottomPanelSize - (_windowHeight * editorData.gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * (1 - editorData.gameWindow.rightPanelPercent), editorData.gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("RightSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##RightSeparatorButton", new System.Numerics.Vector2(seperatorSize, _windowHeight));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingRight = true;

                    editorData.mouseTypes[1] = true;
                }
                else
                    editorData.mouseTypes[1] = false;

                if (isResizingRight)
                {
                    editorData.mouseTypes[1] = true;

                    float mouseX = ImGui.GetIO().MousePos.X;
                    editorData.gameWindow.rightPanelPercent = 1 - mouseX / _windowWidth;
                    if (editorData.gameWindow.leftPanelPercent + editorData.gameWindow.rightPanelPercent > 0.75)
                    {
                        editorData.gameWindow.rightPanelPercent = 1 - editorData.gameWindow.leftPanelPercent - 0.25f;
                    }
                    else
                    {
                        if (editorData.gameWindow.rightPanelPercent < 0.05f)
                            editorData.gameWindow.rightPanelPercent = 0.05f;
                        if (editorData.gameWindow.rightPanelPercent > 0.75f)
                            editorData.gameWindow.rightPanelPercent = 0.75f;
                    }

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingRight = false;
                    }
                }
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
        }
    }
}