using ImGuiNET;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void BottomInfoPanel(ref ImGuiStylePtr style)
        {
            var windowBg = style.Colors[(int)ImGuiCol.WindowBg];
            style.Colors[(int)ImGuiCol.WindowBg] = style.Colors[(int)ImGuiCol.MenuBarBg];
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, editorData.gameWindow.bottomPanelSize + 4));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight - editorData.gameWindow.bottomPanelSize - 4), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomInfoPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                string fpsStr = engine.GetFpsString();
                System.Numerics.Vector2 bottomPanelSize = ImGui.GetContentRegionAvail();
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(fpsStr);
                if (editorData.assetStoreManager.IsZipDownloadInProgress)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text(editorData.assetStoreManager.ZipDownloadProgress);
                }
                else if (editorData.assetStoreManager.tryingToDownload)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text("Starting the download...");
                }
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.SetCursorPosX(bottomPanelSize.X - textSize.X);
                ImGui.Text(fpsStr);

                string errorCount = "Errors: " + Engine.consoleManager.errorCount.ToString();
                ImGui.SetCursorPosX(10);
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.PushStyleColor(ImGuiCol.Text, Engine.consoleManager.LogColors[LogType.Error]);
                ImGui.Text(errorCount);
                ImGui.PopStyleColor();

                string logSeperator = "    |    ";
                System.Numerics.Vector2 errorTextSize = ImGui.CalcTextSize(errorCount);
                ImGui.SetCursorPosX(10 + errorTextSize.X);
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1,1,1,1));
                ImGui.Text(logSeperator);
                ImGui.PopStyleColor();

                string warningCount = "Warnings: " + Engine.consoleManager.warningCount.ToString();
                System.Numerics.Vector2 errorTextSize2 = ImGui.CalcTextSize(errorCount + logSeperator);
                ImGui.SetCursorPosX(10 + errorTextSize2.X);
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.PushStyleColor(ImGuiCol.Text, Engine.consoleManager.LogColors[LogType.Warning]);
                ImGui.Text(warningCount);
                ImGui.PopStyleColor();
            }
            if (ImGui.IsWindowHovered())
                editorData.uiHasMouse = true;
            style.Colors[(int)ImGuiCol.WindowBg] = windowBg;
        }
    }
}
