﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class ImGuiController : BaseImGuiController
    {
        public void Audio(ref ImGuiStylePtr style, ref Vector2 imageSize)
        {
            if (ImGui.BeginTabItem("Audio"))
            {
                float padding = ImGui.GetStyle().WindowPadding.X;
                float spacing = ImGui.GetStyle().ItemSpacing.X;

                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                int columns = (int)((availableSpace.X + spacing) / (imageSize.X + spacing));
                columns = Math.Max(1, columns);

                float itemWidth = availableSpace.X / columns - spacing;

                List<Asset> toRemove = new List<Asset>();
                List<string> folderNames = currentAudioAssetFolder.folders.Keys.ToList();
                AssetFolder? changeToFolder = null;

                int columnI = 0;
                int i = 0;

                // Back button
                if (currentAudioAssetFolder.name != "Audio")
                {
                    ImGui.BeginGroup();
                    ImGui.PushID("back");

                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                    ImGui.Image((IntPtr)engineData.textureManager.textures["ui_back.png"].TextureId, imageSize);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        changeToFolder = currentAudioAssetFolder.parentFolder;
                    }

                    ImGui.SetCursorPos(cursorPos);

                    ImGui.InvisibleButton("##invisible", imageSize);

                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                    ImGui.TextWrapped("Back");
                    ImGui.PopTextWrapPos();

                    ImGui.PopID();

                    ImGui.EndGroup();


                    columnI++;
                }

                int folderCount = currentAudioAssetFolder.folders.Count;
                while (i < folderCount + currentAudioAssetFolder.assets.Count)
                {
                    if (columnI % columns != 0)
                    {
                        ImGui.SameLine();
                    }

                    if (i < currentAudioAssetFolder.folders.Count)
                    {
                        ImGui.BeginGroup();
                        ImGui.PushID(currentAudioAssetFolder.folders[folderNames[i]].name);

                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                        ImGui.Image((IntPtr)engineData.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            changeToFolder = currentAudioAssetFolder.folders[folderNames[i]];
                        }
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup("AssetFolderContextMenu");
                        }

                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        if (ImGui.BeginPopup("AssetFolderContextMenu"))
                        {
                            if (ImGui.MenuItem("Delete folder"))
                            {
                                toRemove.AddRange(currentAudioAssetFolder.folders[folderNames[i]].assets);
                            }
                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        ImGui.SetCursorPos(cursorPos);

                        ImGui.InvisibleButton("##invisible", imageSize);

                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                        ImGui.TextWrapped(currentAudioAssetFolder.folders[folderNames[i]].name);
                        ImGui.PopTextWrapPos();

                        ImGui.PopID();

                        ImGui.EndGroup();

                        columnI++;
                    }
                    else
                    {
                        if (columnI % columns != 0)
                        {
                            ImGui.SameLine();
                        }

                        ImGui.BeginGroup();
                        ImGui.PushID(currentAudioAssetFolder.assets[i - folderCount].Name);

                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();

                        if (engineData.textureManager.textures.ContainsKey("ui_" + currentAudioAssetFolder.assets[i - folderCount].Name))
                            ImGui.Image((IntPtr)engineData.textureManager.textures["ui_" + currentAudioAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);
                        else
                            ImGui.Image((IntPtr)engineData.textureManager.textures["ui_missing.png"].TextureId, imageSize);

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup("AssetContextMenu");
                        }

                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        if (ImGui.BeginPopup("AssetContextMenu"))
                        {
                            if (ImGui.MenuItem("Delete"))
                            {
                                toRemove.Add(currentAudioAssetFolder.assets[i - folderCount]);
                            }
                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        ImGui.SetCursorPos(cursorPos);

                        ImGui.InvisibleButton("##invisible", imageSize);

                        DragDropImageSourceUI(ref engineData.textureManager, "AUDIO_NAME", currentAudioAssetFolder.assets[i - folderCount].Name,
                                              currentAudioAssetFolder.assets[i - folderCount].Path, imageSize);

                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                        ImGui.TextWrapped(currentAudioAssetFolder.assets[i - folderCount].Name);
                        ImGui.PopTextWrapPos();

                        ImGui.PopID();

                        ImGui.EndGroup();

                        columnI++;
                    }
                    i++;
                }

                engineData.assetManager.Remove(toRemove);

                if (changeToFolder != null)
                    currentAudioAssetFolder = changeToFolder;

                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                ImGui.EndTabItem();
            }
        }
    }
}