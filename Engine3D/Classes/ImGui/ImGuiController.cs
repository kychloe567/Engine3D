﻿using Assimp;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

#pragma warning disable CS8602

namespace Engine3D
{
    public class FPS
    {
        public int fps;

        public int minFps = int.MaxValue;
        public int maxFps = 0;
        public double totalTime;

        private const int SAMPLE_SIZE = 30;
        private Queue<double> sampleTimes = new Queue<double>(SAMPLE_SIZE);

        private Stopwatch maxminStopwatch;

        private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS

        public const int fpsLength = 5;

        public FPS()
        {
            maxminStopwatch = new Stopwatch();
            maxminStopwatch.Start();
        }

        public void Update(float delta)
        {
            if (sampleTimes.Count >= SAMPLE_SIZE)
            {
                totalTime -= sampleTimes.Dequeue();
            }

            sampleTimes.Enqueue(delta);
            totalTime += delta;

            double averageDeltaTime = totalTime / sampleTimes.Count;
            double fps = 1.0 / averageDeltaTime;
            this.fps = (int)fps;

            if (!maxminStopwatch.IsRunning)
            {
                if (fps > maxFps)
                    maxFps = (int)fps;
                if (fps < minFps)
                    minFps = (int)fps;
            }
            else
            {
                if (maxminStopwatch.ElapsedMilliseconds > 3000)
                    maxminStopwatch.Stop();
            }
        }

        public string GetFpsString()
        {
            string fpsStr = fps.ToString();
            string maxFpsStr = maxFps.ToString();
            string minFpsStr = minFps.ToString();
            if(fpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - fpsStr.Length); i++)
                    fpsStr = " " + fpsStr;
            }
            if(maxFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - maxFpsStr.Length); i++)
                    maxFpsStr = " " + maxFpsStr;
            }
            if(minFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - minFpsStr.Length); i++)
                    minFpsStr = " " + minFpsStr;
            }

            string fpsFullStr = "FPS: " + fpsStr + "    |    MaxFPS: " + maxFpsStr + "    |    MinFPS: " + minFpsStr;

            return fpsFullStr;
        }
    }

    public enum GameState
    {
        Running,
        Stopped
    }

    public class EditorData
    {
        #region Data
        public List<Object> objects;
        public bool recalculateObjects = true;

        public FPS fps = new FPS();

        public object? selectedItem;
        public int instIndex = -1;

        public AssetStoreManager assetStoreManager;
        public AssetManager assetManager;
        public TextureManager textureManager;

        public int currentAssetTexture = 0;
        public List<Asset> AssetTextures;

        public GizmoManager gizmoManager;

        public GameWindowProperty gameWindow;

        public Vector2 gizmoWindowPos = Vector2.Zero;
        public Vector2 gizmoWindowSize = Vector2.Zero;

        public Physx physx;
        #endregion

        #region Properties 
        public bool windowResized = false;
        public MouseCursor mouseType = MouseCursor.Default;

        public bool manualCursor = false;
        public bool isGameFullscreen = false;
        public bool justSetGameState = false;
        public bool isPaused = false;
        public GameState prevGameState;
        private GameState _gameRunning;
        public GameState gameRunning
        {
            get
            {
                return _gameRunning;
            }
            set
            {
                prevGameState = _gameRunning;
                _gameRunning = value;
            }
        }
        #endregion

        public int animType = 0;
        public int animEndType = 0;
        public int matrixType = 5;

        public EditorData()
        {
            _gameRunning = GameState.Stopped;
        }
    }

    public class ImGuiController : BaseImGuiController
    {
        private EditorData editorData;

        private string currentBottomPanelTab = "Project";
        private Dictionary<string, byte[]> _inputBuffers = new Dictionary<string, byte[]>();

        private bool isResizingLeft = false;
        private bool isResizingRight = false;
        private bool isResizingBottom = false;
        private float seperatorSize = 5;
        private System.Numerics.Vector4 baseBGColor = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.0f);
        private System.Numerics.Vector4 seperatorColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
        public bool shouldOpenTreeNodeMeshes = false;
        private int isObjectHovered = -1;
        private bool justSelectedItem = false;

        private AssetFolder currentTextureAssetFolder;
        private AssetFolder currentModelAssetFolder;
        private AssetFolder currentAudioAssetFolder;

        List<Object> remainingObjects = new List<Object>();
        List<BaseMesh?> meshes = new List<BaseMesh?>();

        private bool showAddComponentWindow = false;
        private string searchQueryAddComponent = "";
        private List<ComponentType> availableComponents = new List<ComponentType>();
        private List<ComponentType> filteredComponents = new List<ComponentType>();
        private HashSet<string> exludedComponents = new HashSet<string>
        {
            "BaseMesh"
        };

        public ImGuiController(int width, int height, ref EditorData editorData) : base(width, height)
        {
            this.editorData = editorData;
            currentTextureAssetFolder = editorData.assetManager.assets.folders[FileType.Textures.ToString()];
            currentModelAssetFolder = editorData.assetManager.assets.folders[FileType.Models.ToString()];
            currentAudioAssetFolder = editorData.assetManager.assets.folders[FileType.Audio.ToString()];

            #region GetComponents
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (type.IsClass && typeof(IComponent).IsAssignableFrom(type) && !exludedComponents.Contains(type.Name))
                {
                    availableComponents.Add(new ComponentType(type.Name, type.BaseType.Name, type));
                }
            }

            filteredComponents = new List<ComponentType>(availableComponents);
            #endregion

            #region Input boxes
            _inputBuffers.Add("##name", new byte[100]);

            _inputBuffers.Add("##positionX", new byte[100]);
            _inputBuffers.Add("##positionY", new byte[100]);
            _inputBuffers.Add("##positionZ", new byte[100]);
            _inputBuffers.Add("##rotationX", new byte[100]);
            _inputBuffers.Add("##rotationY", new byte[100]);
            _inputBuffers.Add("##rotationZ", new byte[100]);
            _inputBuffers.Add("##scaleX", new byte[100]);
            _inputBuffers.Add("##scaleY", new byte[100]);
            _inputBuffers.Add("##scaleZ", new byte[100]);
            _inputBuffers.Add("##texturePath", new byte[300]);
            _inputBuffers.Add("##textureNormalPath", new byte[300]);
            _inputBuffers.Add("##textureHeightPath", new byte[300]);
            _inputBuffers.Add("##textureAOPath", new byte[300]);
            _inputBuffers.Add("##textureRoughPath", new byte[300]);
            _inputBuffers.Add("##textureMetalPath", new byte[300]);
            _inputBuffers.Add("##meshPath", new byte[300]);
            _inputBuffers.Add("##assetSearch", new byte[200]);

            #endregion
        }

        #region Buffer Methods
        private string GetStringFromBuffer(string bufferName)
        {
            string s = Encoding.UTF8.GetString(_inputBuffers[bufferName]).TrimEnd('\0');
            int nullIndex = s.IndexOf('\0');
            if (nullIndex >= 0)
            {
                return s.Substring(0, nullIndex);
            }
            return s;
        }

        private string GetStringFromByte(byte[] array)
        {
            string s = Encoding.UTF8.GetString(array).TrimEnd('\0');
            int nullIndex = s.IndexOf('\0');
            if (nullIndex >= 0)
            {
                return s.Substring(0, nullIndex);
            }
            return s;
        }

        private void CopyDataToBuffer(string buffer, byte[] data)
        {
            ClearBuffer(buffer);
            for (int i = 0; i < _inputBuffers[buffer].Length; i++)
            {
                if(i < data.Length)
                {
                    _inputBuffers[buffer][i] = data[i];
                }
                else
                {
                    if (_inputBuffers[buffer][i] == '\0')
                    {
                        break;
                    }
                    else
                    {
                        _inputBuffers[buffer][i] = 0;
                        break;
                    }
                }
            }
        }

        public void ClearBuffers()
        {
            foreach(var buffer in _inputBuffers)
            {
                int i = 0;
                while (buffer.Value.Length != i && buffer.Value[i] != '\0')
                {
                    buffer.Value[i] = 0;
                    i++;
                }
            }
            ;
        }

        public void ClearBuffer(string buffer)
        {
            int i = 0;
            while (_inputBuffers[buffer][i] != '\0' && _inputBuffers[buffer].Length != i)
            {
                _inputBuffers[buffer][i] = 0;
                i++;
            }
        }
        #endregion

        public void CalculateObjectList()
        {
            meshes.Clear();
            remainingObjects.Clear();
            List<Object> allObjects = new List<Object>(editorData.objects);
            Dictionary<string, int> meshNames = new Dictionary<string, int>();

            List<int> toRemoveMeshes = new List<int>();
            for(int i = 0; i < allObjects.Count; i++)
            {
                if (allObjects[i].Mesh != null)
                {
                    meshes.Add(allObjects[i].Mesh);
                    string name = allObjects[i].name == "" ? "Object " + i.ToString() : allObjects[i].name;

                    if (allObjects[i].Mesh is InstancedMesh)
                        name += " (Instanced)";

                    if (meshNames.ContainsKey(name))
                        meshNames[name] += 1;
                    else
                        meshNames[name] = 0;

                    if (meshNames[name] == 0)
                        allObjects[i].displayName = name;
                    else
                        allObjects[i].displayName = name + " (" + meshNames[name] + ")";

                    toRemoveMeshes.Insert(0,i);
                }
            }

            foreach (var i in toRemoveMeshes)
                allObjects.RemoveAt(i);


            remainingObjects.AddRange(allObjects);
        }

        #region Helpers
        private void CenteredText(string text)
        {
            var originalXPos = ImGui.GetCursorPosX();
            var windowSize = ImGui.GetWindowSize();
            var textSize = ImGui.CalcTextSize(text);
            float centeredXPos = (windowSize.X - textSize.X) / 2.0f;

            ImGui.SetCursorPosX(centeredXPos);
            ImGui.Text(text);

            ImGui.SetCursorPosX(originalXPos);
        }

        private void RightAlignedText(string text)
        {
            var originalXPos = ImGui.GetCursorPosX();
            float windowWidth = ImGui.GetWindowWidth();
            float textWidth = ImGui.CalcTextSize(text).X;
            float centeredXPos = windowWidth - textWidth - ImGui.GetStyle().WindowPadding.X;

            ImGui.SetCursorPosX(centeredXPos);
            ImGui.Text(text);

            ImGui.SetCursorPosX(originalXPos);
        }


        private float CenterCursor(float sizeXOfComp)
        {
            var originalXPos = ImGui.GetCursorPosX();
            var windowSize = ImGui.GetWindowSize();
            float centeredXPos = (windowSize.X - sizeXOfComp) / 2.0f;

            ImGui.SetCursorPosX(centeredXPos);

            return originalXPos;
        }
        private float RightAlignCursor(float sizeXOfComp)
        {
            var originalXPos = ImGui.GetCursorPosX();
            float windowWidth = ImGui.GetWindowWidth();
            float centeredXPos = windowWidth - sizeXOfComp - ImGui.GetStyle().WindowPadding.X;

            ImGui.SetCursorPosX(centeredXPos);

            return originalXPos;
        }
        #endregion

        public void SelectItem(object? selectedObject, EditorData editorData, int instIndex = -1)
        {
            if (editorData.selectedItem != null)
            {
                if (editorData.selectedItem is Object o)
                {
                    o.isSelected = false;
                }
                else if (editorData.selectedItem is ParticleSystem p)
                    p.isSelected = false;
                else if (editorData.selectedItem is PointLight pl)
                    pl.isSelected = false;
            }

            ClearBuffers();

            if (selectedObject == null)
            {
                editorData.selectedItem = null;
                editorData.instIndex = -1;
                return;
            }

            justSelectedItem = true;
            editorData.selectedItem = selectedObject;
            if (instIndex != -1)
                editorData.instIndex = instIndex;

            ((ISelectable)selectedObject).isSelected = true;

            if (selectedObject is Object castedO)
            {
                if (castedO.Mesh is BaseMesh mesh)
                {
                    mesh.recalculate = true;
                    mesh.RecalculateModelMatrix(new bool[] { true, true, true });
                }

                if(castedO.Physics is Physics physics)
                {
                    physics.UpdatePhysxPositionAndRotation(castedO.transformation);
                }
            }
            //TODO pointlight and particle system selection
        }

        public void EditorWindow(ref EditorData editorData, 
                                 KeyboardState keyboardState, MouseState mouseState, Engine engine)
        {
            GameWindowProperty gameWindow = editorData.gameWindow;
            if(editorData.recalculateObjects)
            {
                CalculateObjectList();
                editorData.recalculateObjects = false;
            }

            var io = ImGui.GetIO();
            int anyObjectHovered = -1;

            if (editorData.gameRunning == GameState.Running && !editorData.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            editorData.windowResized = false;
            bool[] mouseTypes = new bool[3];

            var style = ImGui.GetStyle();
            style.WindowBorderSize = 0.5f;
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0, 0, 0, 1.0f);
            style.Colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            style.Colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(1f, 1f, 1f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            style.Colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f); // RGBA
            style.WindowRounding = 5f;
            style.PopupRounding = 5f;

            #region Top panel with menubar
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, gameWindow.topPanelSize));
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            if (ImGui.Begin("TopPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar |
                                        ImGuiWindowFlags.NoScrollbar))
            {
                if(ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if(ImGui.MenuItem("Open", "Ctrl+O"))
                        {

                        }
                        ImGui.EndMenu();
                    }
                    if(ImGui.BeginMenu("Window"))
                    {
                        if(ImGui.MenuItem("Reset panels"))
                        {
                            gameWindow.leftPanelPercent = gameWindow.origLeftPanelPercent;
                            gameWindow.rightPanelPercent = gameWindow.origRightPanelPercent;
                            gameWindow.bottomPanelPercent = gameWindow.origBottomPanelPercent;
                            editorData.windowResized = true;
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

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
                    if (ImGui.ImageButton("play", (IntPtr)Engine.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)Engine.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorData.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)Engine.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.isPaused = !editorData.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)Engine.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isGameFullscreen = !editorData.isGameFullscreen;
                    editorData.windowResized = true;
                }


                style.Colors[(int)ImGuiCol.Button] = button;
            }
            ImGui.End();
            #endregion

            #region Game window frame
            float gwBorder = 5;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gameWindow.gameWindowSize.X, editorData.gameWindow.gameWindowSize.Y / 2));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(gameWindow.gameWindowPos.X + seperatorSize, gameWindow.topPanelSize + editorData.gameWindow.gameWindowSize.Y / 2), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

            if (ImGui.Begin("GameWindowFrame", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground/* | ImGuiWindowFlags.NoInputs*/))
            {
                ImGui.SetCursorPos(new System.Numerics.Vector2(gwBorder, gwBorder));
                ImGui.InvisibleButton("##invGameWindowFrame", ImGui.GetContentRegionAvail() - new System.Numerics.Vector2(gwBorder*2, gwBorder));
                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("MESH_NAME");
                    unsafe
                    {
                        if (payload.NativePtr != null)
                        {
                            byte[] pathBytes = new byte[payload.DataSize];
                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);

                            engine.AddMeshObject(GetStringFromByte(pathBytes));
                            shouldOpenTreeNodeMeshes = true;
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                ImGui.End();
            }

            ImGui.PopStyleVar(2);
            #endregion

            #region Manipulation gizmos menu
            if (editorData.selectedItem != null && editorData.gameRunning == GameState.Stopped)
            {
                bool isInst = false;
                float yHeight = 174.5f - 34.5f;

                if (editorData.selectedItem is Object o && o.Mesh is BaseMesh bm && bm.GetType() == typeof(InstancedMesh))
                {
                    isInst = true;
                    yHeight = 174.5f;
                }

                var button = style.Colors[(int)ImGuiCol.Button];
                style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.6f);
                style.WindowRounding = 0;
                var frameBorderSize = style.FrameBorderSize;
                var framePadding = style.FramePadding;
                style.FrameBorderSize = 0;
                style.FramePadding = new System.Numerics.Vector2(2.5f,2.5f);

                editorData.gizmoWindowSize = new Vector2(22.5f, yHeight);
                editorData.gizmoWindowPos = new Vector2(gameWindow.gameWindowPos.X + seperatorSize, gameWindow.topPanelSize);

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(editorData.gizmoWindowSize.X, editorData.gizmoWindowSize.Y));
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(editorData.gizmoWindowPos.X, editorData.gizmoWindowPos.Y), ImGuiCond.Always);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

                var origButton = style.Colors[(int)ImGuiCol.Button];
                var a = style.WindowPadding;
                if (ImGui.Begin("GizmosMenu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar))
                {
                    var availableWidth = ImGui.GetContentRegionAvail().X;
                    System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(32-5, 32-5);
                    var startXPos = (availableWidth - imageSize.X) * 0.5f;

                    #region Move gizmo button
                    ImGui.SetCursorPosX(0);
                    if (editorData.gizmoManager.gizmoType == GizmoType.Move)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo1", (IntPtr)Engine.textureManager.textures["ui_gizmo_move.png"].TextureId, imageSize))
                    {
                        if(editorData.gizmoManager.gizmoType != GizmoType.Move)
                            editorData.gizmoManager.gizmoType = GizmoType.Move;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Move)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Move tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Local/global move button
                    ImGui.SetCursorPosX(0);
                    if (editorData.gizmoManager.AbsoluteMoving)
                    {
                        if (ImGui.ImageButton("##gizmoRelativeMove", (IntPtr)Engine.textureManager.textures["ui_absolute.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.AbsoluteMoving = false;
                        }
                    }
                    else
                    {
                        if (ImGui.ImageButton("##gizmoAbsoluteMove", (IntPtr)Engine.textureManager.textures["ui_relative.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.AbsoluteMoving = true;
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        if(editorData.gizmoManager.AbsoluteMoving)
                            ImGui.Text("Set to relative moving");
                        else
                            ImGui.Text("Set to absolute moving");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Rotate gizmo button
                    ImGui.SetCursorPosX(0);
                    if (editorData.gizmoManager.gizmoType == GizmoType.Rotate)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo2", (IntPtr)Engine.textureManager.textures["ui_gizmo_rotate.png"].TextureId, imageSize))
                    {
                        if (editorData.gizmoManager.gizmoType != GizmoType.Rotate)
                            editorData.gizmoManager.gizmoType = GizmoType.Rotate;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Rotate)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Rotate tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Scale gizmo button
                    ImGui.SetCursorPosX(0);
                    if (editorData.gizmoManager.gizmoType == GizmoType.Scale)
                    {
                        style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);
                    }
                    if (ImGui.ImageButton("##gizmo3", (IntPtr)Engine.textureManager.textures["ui_gizmo_scale.png"].TextureId, imageSize))
                    {
                        if (editorData.gizmoManager.gizmoType != GizmoType.Scale)
                            editorData.gizmoManager.gizmoType = GizmoType.Scale;
                    }
                    if (editorData.gizmoManager.gizmoType == GizmoType.Scale)
                    {
                        style.Colors[(int)ImGuiCol.Button] = origButton;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                        ImGui.BeginTooltip();
                        ImGui.Text("Scale tool");
                        ImGui.EndTooltip();
                        ImGui.PopStyleVar(2);
                    }
                    #endregion

                    #region Per instance button
                    if (isInst)
                    {
                        var button2 = style.Colors[(int)ImGuiCol.Button];
                        if (editorData.gizmoManager.PerInstanceMove)
                            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 0.8f);

                        ImGui.SetCursorPosX(0);
                        if (ImGui.ImageButton("##gizmo4", (IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize))
                        {
                            editorData.gizmoManager.PerInstanceMove = !editorData.gizmoManager.PerInstanceMove;
                        }
                        if (editorData.gizmoManager.PerInstanceMove)
                            style.Colors[(int)ImGuiCol.Button] = button2;

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(5, 5));
                            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5);
                            ImGui.BeginTooltip();
                            ImGui.Text("Per instance manipulate");
                            ImGui.EndTooltip();
                            ImGui.PopStyleVar(2);
                        }
                    }
                    #endregion
                }
                ImGui.PopStyleVar(2);
                style.WindowRounding = 5f;
                style.Colors[(int)ImGuiCol.Button] = button;
                style.FrameBorderSize = frameBorderSize;
                style.FramePadding = framePadding;
            }
            #endregion

            #region Left panel

            if(keyboardState.IsKeyReleased(Keys.Delete) && editorData.selectedItem != null && editorData.selectedItem is Object objectDelete)
            {
                engine.RemoveObject(objectDelete);
                // Todo: particle and lights
            }

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent, 
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("LeftPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Objects"))
                    {
                        var windowPadding = style.WindowPadding;
                        var popupRounding = style.PopupRounding;
                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                        style.PopupRounding = 2f;
                        if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.BeginMenu("3D Object"))
                            {
                                if (ImGui.MenuItem("Cube"))
                                {
                                    engine.AddObject(ObjectType.Cube);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Sphere"))
                                {
                                    engine.AddObject(ObjectType.Sphere);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Capsule"))
                                {
                                    engine.AddObject(ObjectType.Capsule);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Plane"))
                                {
                                    engine.AddObject(ObjectType.Plane);
                                    shouldOpenTreeNodeMeshes = true;
                                }
                                if (ImGui.MenuItem("Mesh"))
                                {
                                    engine.AddObject(ObjectType.TriangleMesh);
                                    shouldOpenTreeNodeMeshes = true;
                                }

                                ImGui.EndMenu();
                            }
                            if (ImGui.MenuItem("Particle emitter"))
                            {
                                engine.AddObject(ObjectType.ParticleEmitter);
                                shouldOpenTreeNodeMeshes = true;
                            }
                            if (ImGui.MenuItem("Audio emitter"))
                            {
                                engine.AddObject(ObjectType.AudioEmitter);
                                shouldOpenTreeNodeMeshes = true;
                            }
                            if (ImGui.MenuItem("Light source"))
                            {
                                engine.AddObject(ObjectType.LightSource);
                                shouldOpenTreeNodeMeshes = true;
                            }

                            ImGui.EndPopup();
                        }
                        style.WindowPadding = windowPadding;
                        style.PopupRounding = popupRounding;

                        if (editorData.objects.Count > 0)
                        {

                            if (shouldOpenTreeNodeMeshes)
                            {
                                ImGui.SetNextItemOpen(true, ImGuiCond.Once); // Open the tree node once.
                                shouldOpenTreeNodeMeshes = false; // Reset the flag so it doesn't open again automatically.
                            }

                            if(remainingObjects.Count > 0 && ImGui.TreeNode("Objects"))
                            {
                                ImGui.Separator();
                                for (int i = 0; i < remainingObjects.Count; i++)
                                {
                                    Object ro = remainingObjects[i];
                                    string name = ro.name == "" ? "Object " + i.ToString() : ro.name;

                                    if (ImGui.Selectable(name))
                                    {
                                        SelectItem(ro, editorData);
                                    }
                                    if (ImGui.IsItemHovered())
                                        anyObjectHovered = ro.id;

                                    if (isObjectHovered != -1 && isObjectHovered == ro.id)
                                    {
                                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                                        style.PopupRounding = 2f;
                                        if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                                        {
                                            anyObjectHovered = ro.id;
                                            if (ImGui.MenuItem("Delete"))
                                            {
                                                engine.RemoveObject(ro);
                                            }

                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;
                                    }
                                }
                            }

                            if (meshes.Count > 0 && ImGui.TreeNode("Meshes"))
                            {
                                ImGui.Separator();
                                for (int i = 0; i < meshes.Count; i++)
                                {
                                    if (ImGui.Selectable(meshes[i].parentObject.displayName))
                                    {
                                        SelectItem(meshes[i].parentObject, editorData);
                                    }
                                    if (ImGui.IsItemHovered())
                                        anyObjectHovered = meshes[i].parentObject.id;

                                    if (isObjectHovered != -1 && isObjectHovered == meshes[i].parentObject.id)
                                    {
                                        style.WindowPadding = new System.Numerics.Vector2(style.WindowPadding.X, style.WindowPadding.X);
                                        style.PopupRounding = 2f;
                                        if (ImGui.BeginPopupContextWindow("objectManagingMenu", ImGuiPopupFlags.MouseButtonRight))
                                        {
                                            anyObjectHovered = meshes[i].parentObject.id;
                                            if (ImGui.MenuItem("Delete"))
                                            {
                                                engine.RemoveObject(meshes[i].parentObject);
                                            }

                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;
                                    }
                                }

                                ImGui.TreePop();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Left panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, 
                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * gameWindow.leftPanelPercent, gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("LeftSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##LeftSeparatorButton", new System.Numerics.Vector2(seperatorSize, _windowHeight));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingLeft = true;

                    mouseTypes[0] = true;
                }
                else
                    mouseTypes[0] = false;

                if (isResizingLeft)
                {
                    mouseTypes[0] = true;

                    float mouseX = ImGui.GetIO().MousePos.X;
                    gameWindow.leftPanelPercent = mouseX / _windowWidth;
                    if (gameWindow.leftPanelPercent + gameWindow.rightPanelPercent > 0.75)
                    {
                        gameWindow.leftPanelPercent = 1 - gameWindow.rightPanelPercent - 0.25f;
                    }
                    else
                    {
                        if (gameWindow.leftPanelPercent < 0.05f)
                            gameWindow.leftPanelPercent = 0.05f;
                        if (gameWindow.leftPanelPercent > 0.75f)
                            gameWindow.leftPanelPercent = 0.75f;
                    }

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingLeft = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Right panel

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth * gameWindow.rightPanelPercent - seperatorSize,
                                                                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - gameWindow.rightPanelPercent), gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("RightPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    if (ImGui.BeginTabItem("Inspector"))
                    {
                        if(editorData.selectedItem != null)
                        {
                            if(editorData.selectedItem is Object o)
                            {
                                if (ImGui.Checkbox("##isMeshEnabled", ref o.isEnabled))
                                {
                                    if(o.Mesh is BaseMesh enabledMesh)
                                        enabledMesh.recalculate = true;
                                }
                                ImGui.SameLine();

                                Encoding.UTF8.GetBytes(o.name, 0, o.name.Length, _inputBuffers["##name"], 0);
                                if (ImGui.InputText("##name", _inputBuffers["##name"], (uint)_inputBuffers["##name"].Length))
                                {
                                    o.name = GetStringFromBuffer("##name");
                                }

                                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                if (ImGui.TreeNode("Transform"))
                                {
                                    BaseMesh? baseMesh = o.Mesh;
                                    bool commit = false;
                                    bool reset = false;
                                    ImGui.PushItemWidth(50);

                                    ImGui.Separator();

                                    #region Position
                                    ImGui.Text("Position");

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Position.X.ToString(), 0, o.transformation.Position.X.ToString().Length, _inputBuffers["##positionX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionX", _inputBuffers["##positionX"], (uint)_inputBuffers["##positionX"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionX");
                                        float value = o.transformation.Position.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Position = new Vector3(value, o.transformation.Position.Y, o.transformation.Position.Z);
                                            else
                                            {
                                                ClearBuffer("##positionX");
                                                o.transformation.Position = new Vector3(0, o.transformation.Position.Y, o.transformation.Position.Z);
                                            }

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Position.Y.ToString(), 0, o.transformation.Position.Y.ToString().Length, _inputBuffers["##positionY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionY", _inputBuffers["##positionY"], (uint)_inputBuffers["##positionY"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionY");
                                        float value = o.transformation.Position.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Position = new Vector3(o.transformation.Position.X, value, o.transformation.Position.Z);   
                                            else
                                            {
                                                ClearBuffer("##positionY");
                                                o.transformation.Position = new Vector3(o.transformation.Position.X, 0, o.transformation.Position.Z);   
                                            }

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Position.Z.ToString(), 0, o.transformation.Position.Z.ToString().Length, _inputBuffers["##positionZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##positionZ", _inputBuffers["##positionZ"], (uint)_inputBuffers["##positionZ"].Length) && !justSelectedItem)
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##positionZ");
                                        float value = o.transformation.Position.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Position = new Vector3(o.transformation.Position.X, o.transformation.Position.Y, value);
                                            else
                                            {
                                                ClearBuffer("##positionZ");
                                                o.transformation.Position = new Vector3(o.transformation.Position.X, o.transformation.Position.Y, 0);
                                            }

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { true, false, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    #region Rotation
                                    ImGui.Text("Rotation");

                                    Vector3 rotation = Helper.EulerFromQuaternion(o.transformation.Rotation);

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.X.ToString(), 0, rotation.X.ToString().Length, _inputBuffers["##rotationX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationX", _inputBuffers["##rotationX"], (uint)_inputBuffers["##rotationX"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##rotationX");
                                        float value = rotation.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                rotation.X = value;
                                            else
                                            {
                                                ClearBuffer("##rotationX");
                                                rotation.X = 0;
                                            }
                                            o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.Y.ToString(), 0, rotation.Y.ToString().Length, _inputBuffers["##rotationY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationY", _inputBuffers["##rotationY"], (uint)_inputBuffers["##rotationY"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##rotationY");
                                        float value = rotation.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                rotation.Y = value;
                                            else
                                            {
                                                ClearBuffer("##rotationY");
                                                rotation.Y = 0;
                                            }
                                            o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);
                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(rotation.Z.ToString(), 0, rotation.Z.ToString().Length, _inputBuffers["##rotationZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##rotationZ", _inputBuffers["##rotationZ"], (uint)_inputBuffers["##rotationZ"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##rotationZ");
                                        float value = rotation.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                rotation.Z = value;
                                            else
                                            {
                                                ClearBuffer("##rotationZ");
                                                rotation.Z = 0;
                                            }
                                            o.transformation.Rotation = Helper.QuaternionFromEuler(rotation);
                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, true, false });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    #region Scale
                                    ImGui.Text("Scale");

                                    ImGui.Text("X");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.X.ToString(), 0, o.transformation.Scale.X.ToString().Length, _inputBuffers["##scaleX"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleX", _inputBuffers["##scaleX"], (uint)_inputBuffers["##scaleX"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleX");
                                        float value = o.transformation.Scale.X;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Scale.X = value;
                                            else
                                            {
                                                ClearBuffer("##scaleX");
                                                o.transformation.Scale.X = 1;
                                            }

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Y");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.Y.ToString(), 0, o.transformation.Scale.Y.ToString().Length, _inputBuffers["##scaleY"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleY", _inputBuffers["##scaleY"], (uint)_inputBuffers["##scaleY"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleY");
                                        float value = o.transformation.Scale.Y;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Scale.Y = value;
                                            else
                                            {
                                                ClearBuffer("##scaleY");
                                                o.transformation.Scale.Y = 1;
                                            }

                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    ImGui.SameLine();

                                    ImGui.Text("Z");
                                    ImGui.SameLine();
                                    Encoding.UTF8.GetBytes(o.transformation.Scale.Z.ToString(), 0, o.transformation.Scale.Z.ToString().Length, _inputBuffers["##scaleZ"], 0);
                                    commit = false;
                                    if (ImGui.InputText("##scaleZ", _inputBuffers["##scaleZ"], (uint)_inputBuffers["##scaleZ"].Length))
                                    {
                                        commit = true;
                                    }
                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        reset = true;
                                        commit = true;
                                    }
                                    if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                        commit = true;

                                    if (commit)
                                    {
                                        string valueStr = GetStringFromBuffer("##scaleZ");
                                        float value = o.transformation.Scale.Z;
                                        if (float.TryParse(valueStr, out value))
                                        {
                                            if (!reset)
                                                o.transformation.Scale.Z = value;
                                            else
                                            {
                                                ClearBuffer("##scaleZ");
                                                o.transformation.Scale.Z = 1;
                                            }
                                            
                                            if (baseMesh != null)
                                            {
                                                baseMesh.recalculate = true;
                                                baseMesh.RecalculateModelMatrix(new bool[] { false, false, true });
                                            }
                                            if (o.Physics is Physics p)
                                                p.UpdatePhysxPositionAndRotation(o.transformation);
                                        }
                                    }
                                    #endregion

                                    ImGui.Separator();

                                    ImGui.PopItemWidth();

                                    ImGui.TreePop();
                                }

                                //-----------------------------------------------------------

                                ImGui.PushFont(default20);
                                CenteredText("Components");
                                ImGui.PopFont();

                                List<IComponent> toRemoveComp = new List<IComponent>();
                                foreach (IComponent c in o.components)
                                {
                                    if(c is BaseMesh baseMesh)
                                    {
                                        ImGui.Separator();
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Mesh"))
                                        {
                                            //ImGui.SameLine();
                                            //var origDeleteX = RightAlignCursor(70);
                                            //ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                                            //ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                                            //if(ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                                            //{
                                            //    toRemoveComp.Add(c);
                                            //}
                                            //ImGui.PopStyleVar();
                                            //ImGui.PopStyleColor();
                                            //ImGui.SetCursorPosX(origDeleteX);
                                            //ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                                            #region Mesh
                                            if (baseMesh.GetType() == typeof(Mesh))
                                            {
                                                if (ImGui.Checkbox("##useBVH", ref o.useBVH))
                                                {
                                                    if (o.useBVH)
                                                    {
                                                        o.BuildBVH();
                                                    }
                                                    else
                                                    {
                                                        baseMesh.BVHStruct = null;
                                                    }
                                                }
                                                ImGui.SameLine();
                                                ImGui.Text("Use BVH for rendering");
                                            }

                                            ImGui.Checkbox("##useShading", ref baseMesh.useShading);
                                            ImGui.SameLine();
                                            ImGui.Text("Use shading");

                                            ImGui.Separator();

                                            Encoding.UTF8.GetBytes(baseMesh.modelName, 0, baseMesh.modelName.ToString().Length, _inputBuffers["##meshPath"], 0);
                                            ImGui.InputText("##meshPath", _inputBuffers["##meshPath"], (uint)_inputBuffers["##meshPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                            {
                                                baseMesh.modelName = "";
                                                _inputBuffers["##meshPath"][0] = 0;
                                            }

                                            if (ImGui.BeginDragDropTarget())
                                            {
                                                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("MESH_NAME");
                                                unsafe
                                                {
                                                    if (payload.NativePtr != null)
                                                    {
                                                        byte[] pathBytes = new byte[payload.DataSize];
                                                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                        baseMesh.modelPath = GetStringFromByte(pathBytes);
                                                        CopyDataToBuffer("##meshPath", Encoding.UTF8.GetBytes(baseMesh.modelPath));
                                                    }
                                                }
                                                ImGui.EndDragDropTarget();
                                            }
                                            #endregion

                                            ImGui.Separator();

                                            #region Textures
                                            ImGui.Text("Texture");
                                            Encoding.UTF8.GetBytes(baseMesh.textureName, 0, baseMesh.textureName.ToString().Length, _inputBuffers["##texturePath"], 0);
                                            ImGui.InputText("##texturePath", _inputBuffers["##texturePath"], (uint)_inputBuffers["##texturePath"].Length, ImGuiInputTextFlags.ReadOnly);
                                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureName != "")
                                            {
                                                baseMesh.textureName = "";
                                                ClearBuffer("##texturePath");
                                            }

                                            if (ImGui.BeginDragDropTarget())
                                            {
                                                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                unsafe
                                                {
                                                    if (payload.NativePtr != null)
                                                    {
                                                        byte[] pathBytes = new byte[payload.DataSize];
                                                        System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                        baseMesh.textureName = GetStringFromByte(pathBytes);
                                                        CopyDataToBuffer("##texturePath", Encoding.UTF8.GetBytes(baseMesh.textureName));
                                                    }
                                                }
                                                ImGui.EndDragDropTarget();
                                            }

                                            if (ImGui.TreeNode("Custom textures"))
                                            {
                                                ImGui.Text("Normal Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureNormalName, 0, baseMesh.textureNormalName.ToString().Length, _inputBuffers["##textureNormalPath"], 0);
                                                ImGui.InputText("##textureNormalPath", _inputBuffers["##textureNormalPath"], (uint)_inputBuffers["##textureNormalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureNormalName != "")
                                                {
                                                    baseMesh.textureNormalName = "";
                                                    ClearBuffer("##textureNormalPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureNormalName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureNormalPath", Encoding.UTF8.GetBytes(baseMesh.textureNormalName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Height Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureHeightName, 0, baseMesh.textureHeightName.ToString().Length, _inputBuffers["##textureHeightPath"], 0);
                                                ImGui.InputText("##textureHeightPath", _inputBuffers["##textureHeightPath"], (uint)_inputBuffers["##textureHeightPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureHeightName != "")
                                                {
                                                    baseMesh.textureHeightName = "";
                                                    ClearBuffer("##textureHeightPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureHeightName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureHeightPath", Encoding.UTF8.GetBytes(baseMesh.textureHeightName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("AO Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureAOName, 0, baseMesh.textureAOName.ToString().Length, _inputBuffers["##textureAOPath"], 0);
                                                ImGui.InputText("##textureAOPath", _inputBuffers["##textureAOPath"], (uint)_inputBuffers["##textureAOPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureAOName != "")
                                                {
                                                    baseMesh.textureAOName = "";
                                                    ClearBuffer("##textureAOPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureAOName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureAOPath", Encoding.UTF8.GetBytes(baseMesh.textureAOName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Rough Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureRoughName, 0, baseMesh.textureRoughName.ToString().Length, _inputBuffers["##textureRoughPath"], 0);
                                                ImGui.InputText("##textureRoughPath", _inputBuffers["##textureRoughPath"], (uint)_inputBuffers["##textureRoughPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureName != "")
                                                {
                                                    baseMesh.textureRoughName = "";
                                                    ClearBuffer("##textureRoughPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureRoughName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureRoughPath", Encoding.UTF8.GetBytes(baseMesh.textureRoughName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }
                                                ImGui.Text("Metal Texture");
                                                Encoding.UTF8.GetBytes(baseMesh.textureMetalName, 0, baseMesh.textureMetalName.ToString().Length, _inputBuffers["##textureMetalPath"], 0);
                                                ImGui.InputText("##textureMetalPath", _inputBuffers["##textureMetalPath"], (uint)_inputBuffers["##textureMetalPath"].Length, ImGuiInputTextFlags.ReadOnly);
                                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && baseMesh.textureMetalName != "")
                                                {
                                                    baseMesh.textureMetalName = "";
                                                    ClearBuffer("##textureMetalPath");
                                                }

                                                if (ImGui.BeginDragDropTarget())
                                                {
                                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("TEXTURE_NAME");
                                                    unsafe
                                                    {
                                                        if (payload.NativePtr != null)
                                                        {
                                                            byte[] pathBytes = new byte[payload.DataSize];
                                                            System.Runtime.InteropServices.Marshal.Copy(payload.Data, pathBytes, 0, payload.DataSize);
                                                            baseMesh.textureMetalName = GetStringFromByte(pathBytes);
                                                            CopyDataToBuffer("##textureMetalPath", Encoding.UTF8.GetBytes(baseMesh.textureMetalName));
                                                        }
                                                    }
                                                    ImGui.EndDragDropTarget();
                                                }

                                                ImGui.TreePop();
                                            }
                                            #endregion

                                            ImGui.TreePop();
                                        }
                                    }

                                    if(c is Physics physics)
                                    {
                                        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                                        if (ImGui.TreeNode("Physics"))
                                        {
                                            ImGui.SameLine();
                                            var origDeleteX = RightAlignCursor(70);
                                            ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.FrameBg]);
                                            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                                            if (ImGui.Button("Delete", new System.Numerics.Vector2(70, 20)))
                                            {
                                                toRemoveComp.Add(c);
                                            }
                                            ImGui.PopStyleVar();
                                            ImGui.PopStyleColor();
                                            ImGui.SetCursorPosX(origDeleteX);
                                            ImGui.Dummy(new System.Numerics.Vector2(0, 0));

                                            #region Physx
                                            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f);
                                            var buttonColor = style.Colors[(int)ImGuiCol.Button];
                                            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f);

                                            if (o.Physics is Physics p && p.HasCollider)
                                            {
                                                System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 40);
                                                if (ImGui.Button("Remove\n" + p.colliderStaticType + " " + p.colliderType + " Collider", buttonSize))
                                                {
                                                    p.RemoveCollider();
                                                }
                                            }
                                            else
                                            {
                                                BaseMesh? baseMeshPhysics = o.Mesh;

                                                System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20);
                                                string[] options = { "Static", "Dynamic" };
                                                for (int i = 0; i < options.Length; i++)
                                                {
                                                    if (ImGui.RadioButton(options[i], physics.selectedColliderOption == i))
                                                    {
                                                        physics.selectedColliderOption = i;
                                                    }

                                                    if (i != options.Length - 1)
                                                        ImGui.SameLine();
                                                }

                                                if (physics.selectedColliderOption == 0)
                                                {
                                                    if (ImGui.Button("Add Triangle Mesh Collider", buttonSize))
                                                    {
                                                        if (baseMeshPhysics == null)
                                                            throw new Exception("For a triangle mesh collider the object must have a mesh!");
                                                        physics.AddTriangleMeshCollider(o.transformation, baseMeshPhysics);
                                                    }
                                                    if (ImGui.Button("Add Cube Collider", buttonSize))
                                                    {
                                                        physics.AddCubeCollider(o.transformation, true);
                                                    }
                                                    if (ImGui.Button("Add Sphere Collider", buttonSize))
                                                    {
                                                        physics.AddSphereCollider(o.transformation, true);
                                                    }
                                                    if (ImGui.Button("Add Capsule Collider", buttonSize))
                                                    {
                                                        physics.AddCapsuleCollider(o.transformation, true);
                                                    }
                                                }
                                                else if (physics.selectedColliderOption == 1)
                                                {
                                                    if (ImGui.Button("Add Cube Collider", buttonSize))
                                                    {
                                                        physics.AddCubeCollider(o.transformation, false);
                                                    }
                                                    if (ImGui.Button("Add Sphere Collider", buttonSize))
                                                    {
                                                        physics.AddSphereCollider(o.transformation, false);
                                                    }
                                                    if (ImGui.Button("Add Capsule Collider", buttonSize))
                                                    {
                                                        physics.AddCapsuleCollider(o.transformation, false);
                                                    }
                                                }
                                            }
                                            style.Colors[(int)ImGuiCol.Button] = buttonColor;
                                            ImGui.PopStyleVar();
                                            #endregion

                                            ImGui.TreePop();
                                        }
                                    }

                                    ImGui.Separator();
                                }

                                foreach(IComponent c in toRemoveComp)
                                {
                                    o.DeleteComponent(c, ref editorData.textureManager);
                                    editorData.recalculateObjects = true;
                                }

                                #region Add Component
                                float addCompOrig = CenterCursor(100);
                                if (ImGui.Button("Add Component", new System.Numerics.Vector2(100,20)))
                                {
                                    showAddComponentWindow = true;
                                    var buttonPos = ImGui.GetItemRectMin();
                                    var buttonSize = ImGui.GetItemRectSize();
                                    ImGui.SetNextWindowPos(new System.Numerics.Vector2(seperatorSize + _windowWidth * (1 - gameWindow.rightPanelPercent), buttonPos.Y + buttonSize.Y));
                                    ImGui.SetNextWindowSize(new System.Numerics.Vector2((_windowWidth * gameWindow.rightPanelPercent - seperatorSize)-20, 
                                                                                        200));
                                }
                                ImGui.SetCursorPosX(addCompOrig);

                                if (showAddComponentWindow)
                                {
                                    System.Numerics.Vector2 componentWindowPos = ImGui.GetWindowPos();
                                    System.Numerics.Vector2 componentWindowSize = ImGui.GetWindowSize();
                                    if(ImGui.Begin("Component Selection", ref showAddComponentWindow, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
                                    {
                                        componentWindowPos = ImGui.GetWindowPos();
                                        componentWindowSize = ImGui.GetWindowSize();

                                        ImGui.BeginChild("ComponentList", new System.Numerics.Vector2(0, 150), true);
                                        ImGui.InputText("Search", ref searchQueryAddComponent, 256);

                                        HashSet<string> alreadyGotBase = new HashSet<string>();
                                        foreach (IComponent c in o.components)
                                        {
                                            var cType = c.GetType();
                                            if (cType.BaseType.Name == "BaseMesh")
                                            {
                                                alreadyGotBase.Add("BaseMesh");
                                            }
                                        }
                                        HashSet<string> alreadyGotClass = new HashSet<string>();
                                        foreach (IComponent c in o.components)
                                        {
                                            var cType = c.GetType();
                                            alreadyGotClass.Add(cType.Name);
                                        }

                                        filteredComponents = availableComponents
                                            .Where(component => component.name.ToLower().Contains(searchQueryAddComponent.ToLower()) &&
                                                                !alreadyGotBase.Contains(component.baseClass) &&
                                                                !alreadyGotClass.Contains(component.name))
                                            .ToList();

                                        foreach (var component in filteredComponents)
                                        {
                                            if (ImGui.Button(component.name))
                                            {
                                                if(component.name == "Physics")
                                                {
                                                    if (o.Mesh == null)
                                                    {
                                                        Engine.consoleManager.AddLog("Can't add physics to an object that doesn't have a mesh!", LogType.Warning);
                                                        showAddComponentWindow = false;
                                                        break;
                                                    }

                                                    object[] args = new object[]
                                                    {
                                                        editorData.physx
                                                    };
                                                    object? comp = Activator.CreateInstance(component.type, args);
                                                    if (comp == null)
                                                        continue;

                                                    o.components.Add((IComponent)comp);
                                                }


                                                showAddComponentWindow = false;
                                            }
                                        }
                                        ImGui.EndChild();

                                        

                                        ImGui.End();
                                    }

                                    System.Numerics.Vector2 mousePos = ImGui.GetMousePos();
                                    bool isMouseOutsideWindow = mousePos.X < componentWindowPos.X || mousePos.X > componentWindowPos.X + componentWindowSize.X ||
                                                                mousePos.Y < componentWindowPos.Y || mousePos.Y > componentWindowPos.Y + componentWindowSize.Y;

                                    if (ImGui.IsMouseClicked(0) && isMouseOutsideWindow)
                                    {
                                        showAddComponentWindow = false;
                                    }
                                }
                                #endregion

                                ImGui.Dummy(new System.Numerics.Vector2(0, 50));
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Right panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(seperatorSize, 
                _windowHeight - gameWindow.topPanelSize - gameWindow.bottomPanelSize - (_windowHeight * gameWindow.bottomPanelPercent)));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth * (1 - gameWindow.rightPanelPercent), gameWindow.topPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("RightSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##RightSeparatorButton", new System.Numerics.Vector2(seperatorSize, _windowHeight));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingRight = true;

                    mouseTypes[1] = true;
                }
                else
                    mouseTypes[1] = false;

                if (isResizingRight)
                {
                    mouseTypes[1] = true;

                    float mouseX = ImGui.GetIO().MousePos.X;
                    gameWindow.rightPanelPercent = 1 - mouseX / _windowWidth;
                    if (gameWindow.leftPanelPercent + gameWindow.rightPanelPercent > 0.75)
                    {
                        gameWindow.rightPanelPercent = 1 - gameWindow.leftPanelPercent - 0.25f;
                    }
                    else
                    {
                        if (gameWindow.rightPanelPercent < 0.05f)
                            gameWindow.rightPanelPercent = 0.05f;
                        if (gameWindow.rightPanelPercent > 0.75f)
                            gameWindow.rightPanelPercent = 0.75f;
                    }

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingRight = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Bottom asset panel seperator
            style.WindowMinSize = new System.Numerics.Vector2(seperatorSize, seperatorSize);
            style.Colors[(int)ImGuiCol.WindowBg] = seperatorColor;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, seperatorSize));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) - gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            if (ImGui.Begin("BottomAssetSeparator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
                                         ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.InvisibleButton("##BottomSeparatorButton", new System.Numerics.Vector2(_windowWidth, seperatorSize));

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        isResizingBottom = true;

                    mouseTypes[2] = true;
                }
                else
                    mouseTypes[2] = false;

                if (isResizingBottom)
                {
                    mouseTypes[2] = true;

                    float mouseY = ImGui.GetIO().MousePos.Y;
                    gameWindow.bottomPanelPercent = 1 - mouseY / (_windowHeight - gameWindow.bottomPanelSize-5);
                    if (gameWindow.bottomPanelPercent < 0.05f)
                        gameWindow.bottomPanelPercent = 0.05f;
                    if(gameWindow.bottomPanelPercent > 0.70f)
                        gameWindow.bottomPanelPercent = 0.70f;

                    editorData.windowResized = true;

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        isResizingBottom = false;
                    }
                }
            }
            ImGui.End();
            ImGui.PopStyleVar(); // Pop the style for padding
            style.WindowMinSize = new System.Numerics.Vector2(32, 32);
            style.Colors[(int)ImGuiCol.WindowBg] = baseBGColor;
            #endregion

            #region Bottom asset panel
            style.WindowRounding = 0f;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, _windowHeight * gameWindow.bottomPanelPercent - seperatorSize - 1));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight * (1 - gameWindow.bottomPanelPercent) + seperatorSize - gameWindow.bottomPanelSize), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomAssetPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if (ImGui.BeginTabBar("MyTabs"))
                {
                    float imageWidth = 100;
                    float imageHeight = 100;
                    System.Numerics.Vector2 imageSize = new System.Numerics.Vector2(imageWidth, imageHeight);

                    if (ImGui.BeginTabItem("Project"))
                    {
                        if (currentBottomPanelTab != "Project")
                            currentBottomPanelTab = "Project";

                        if (ImGui.BeginTabBar("Assets"))
                        {
                            if (ImGui.BeginTabItem("Textures"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                columns = Math.Max(1, columns);

                                float itemWidth = availableSpace.X / columns - spacing;

                                List<Asset> toRemove = new List<Asset>();
                                List<string> folderNames = currentTextureAssetFolder.folders.Keys.ToList();
                                AssetFolder? changeToFolder = null;

                                int columnI = 0;
                                int i = 0;

                                if(currentTextureAssetFolder.name != "Textures")
                                {
                                    ImGui.BeginGroup();
                                    ImGui.PushID("back");

                                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                    {
                                        changeToFolder = currentTextureAssetFolder.parentFolder;
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
                                int folderCount = currentTextureAssetFolder.folders.Count;

                                while (i < folderCount + currentTextureAssetFolder.assets.Count)
                                {
                                    if (columnI % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    if (i < currentTextureAssetFolder.folders.Count)
                                    {
                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentTextureAssetFolder.folders[folderNames[i]].name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                        {
                                            changeToFolder = currentTextureAssetFolder.folders[folderNames[i]];
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
                                                toRemove.AddRange(currentTextureAssetFolder.folders[folderNames[i]].assets);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentTextureAssetFolder.folders[folderNames[i]].name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    else if (Engine.textureManager.textures.ContainsKey("ui_" + currentTextureAssetFolder.assets[i-folderCount].Name))
                                    {
                                        if (columnI % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentTextureAssetFolder.assets[i - folderCount].Name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentTextureAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);

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
                                                toRemove.Add(currentTextureAssetFolder.assets[i - folderCount]);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        DragDropImageSourceUI(ref Engine.textureManager, "TEXTURE_NAME", currentTextureAssetFolder.assets[i - folderCount].Name,
                                                              currentTextureAssetFolder.assets[i - folderCount].Path, imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentTextureAssetFolder.assets[i - folderCount].Name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    i++;
                                }

                                editorData.assetManager.Remove(toRemove);

                                if(changeToFolder != null)
                                    currentTextureAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem("Models"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                                columns = Math.Max(1, columns);

                                float itemWidth = availableSpace.X / columns - spacing;

                                List<Asset> toRemove = new List<Asset>();
                                List<string> folderNames = currentModelAssetFolder.folders.Keys.ToList();
                                AssetFolder? changeToFolder = null;

                                int columnI = 0;
                                int i = 0;

                                // Back button
                                if (currentModelAssetFolder.name != "Models")
                                {
                                    ImGui.BeginGroup();
                                    ImGui.PushID("back");

                                    System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                    {
                                        changeToFolder = currentModelAssetFolder.parentFolder;
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

                                int folderCount = currentModelAssetFolder.folders.Count;
                                while (i < folderCount + currentModelAssetFolder.assets.Count)
                                {
                                    if (columnI % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    if (i < currentModelAssetFolder.folders.Count)
                                    {
                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentModelAssetFolder.folders[folderNames[i]].name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

                                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                        {
                                            changeToFolder = currentModelAssetFolder.folders[folderNames[i]];
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
                                                toRemove.AddRange(currentModelAssetFolder.folders[folderNames[i]].assets);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentModelAssetFolder.folders[folderNames[i]].name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    else if(currentTextureAssetFolder.assets.Count > i - folderCount)
                                    {
                                        if (columnI % columns != 0)
                                        {
                                            ImGui.SameLine();
                                        }

                                        ImGui.BeginGroup();
                                        ImGui.PushID(currentTextureAssetFolder.assets[i - folderCount].Name);

                                        System.Numerics.Vector2 cursorPos = ImGui.GetCursorPos();

                                        if(Engine.textureManager.textures.ContainsKey("ui_" + currentModelAssetFolder.assets[i - folderCount].Name))
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentModelAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);
                                        else
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize);

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
                                                toRemove.Add(currentModelAssetFolder.assets[i - folderCount]);
                                            }
                                            ImGui.EndPopup();
                                        }
                                        style.WindowPadding = windowPadding;
                                        style.PopupRounding = popupRounding;

                                        ImGui.SetCursorPos(cursorPos);

                                        ImGui.InvisibleButton("##invisible", imageSize);

                                        DragDropImageSourceUI(ref Engine.textureManager, "MESH_NAME", currentModelAssetFolder.assets[i - folderCount].Name,
                                                              currentModelAssetFolder.assets[i - folderCount].Path, imageSize);

                                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                        ImGui.TextWrapped(currentModelAssetFolder.assets[i - folderCount].Name);
                                        ImGui.PopTextWrapPos();

                                        ImGui.PopID();

                                        ImGui.EndGroup();

                                        columnI++;
                                    }
                                    i++;
                                }

                                editorData.assetManager.Remove(toRemove);

                                if (changeToFolder != null)
                                    currentModelAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }
                            if(ImGui.BeginTabItem("Audio"))
                            {
                                float padding = ImGui.GetStyle().WindowPadding.X;
                                float spacing = ImGui.GetStyle().ItemSpacing.X;

                                System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                                int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
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
                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_back.png"].TextureId, imageSize);

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
                                        ImGui.Image((IntPtr)Engine.textureManager.textures["ui_folder.png"].TextureId, imageSize);

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

                                        if (Engine.textureManager.textures.ContainsKey("ui_" + currentAudioAssetFolder.assets[i - folderCount].Name))
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + currentAudioAssetFolder.assets[i - folderCount].Name].TextureId, imageSize);
                                        else
                                            ImGui.Image((IntPtr)Engine.textureManager.textures["ui_missing.png"].TextureId, imageSize);

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

                                        DragDropImageSourceUI(ref Engine.textureManager, "AUDIO_NAME", currentAudioAssetFolder.assets[i - folderCount].Name,
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

                                editorData.assetManager.Remove(toRemove);

                                if (changeToFolder != null)
                                    currentAudioAssetFolder = changeToFolder;

                                ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                                ImGui.EndTabItem();
                            }
                            
                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Console"))
                    {
                        if (currentBottomPanelTab != "Console")
                            currentBottomPanelTab = "Console";

                        ImGui.PushFont(default18);
                        foreach(Log log in Engine.consoleManager.Logs)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Engine.consoleManager.LogColors[log.logType]);
                            ImGui.TextWrapped(log.message);
                            ImGui.PopStyleColor();
                        }
                        ImGui.PopFont();
                        ImGui.SetScrollHereY(1.0f);

                        ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20));

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Asset store"))
                    {
                        if (editorData.assetStoreManager.IsThereInternetConnection)
                        {
                            Encoding.UTF8.GetBytes(editorData.assetStoreManager.currentKeyword, 0, editorData.assetStoreManager.currentKeyword.Length, _inputBuffers["##assetSearch"], 0);

                            if (currentBottomPanelTab != "Asset store")
                            {
                                currentBottomPanelTab = "Asset store";
                                ImGui.SetKeyboardFocusHere();
                            }

                            bool commit = false;
                            if (ImGui.InputText("##assetSearch", _inputBuffers["##assetSearch"], (uint)_inputBuffers["##assetSearch"].Length, ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                commit = true;
                            }
                            if (ImGui.IsItemActive() && keyboardState.IsKeyReleased(Keys.KeyPadEnter))
                                commit = true;
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            {
                                ImGui.SetNextWindowFocus();
                                editorData.assetStoreManager.currentKeyword = "";
                                _inputBuffers["##assetSearch"][0] = 0;
                            }

                            if (commit)
                            {
                                editorData.assetStoreManager.currentKeyword = GetStringFromBuffer("##assetSearch");
                                editorData.assetStoreManager.currentPageNumber = 0;
                                editorData.assetStoreManager.GetOpenGameArtOrg();
                            }

                            ImGui.Separator();

                            float padding = ImGui.GetStyle().WindowPadding.X;
                            float spacing = ImGui.GetStyle().ItemSpacing.X;

                            System.Numerics.Vector2 availableSpace = ImGui.GetContentRegionAvail();
                            int columns = (int)((availableSpace.X + spacing) / (imageWidth + spacing));
                            columns = Math.Max(1, columns);
                            float itemWidth = availableSpace.X / columns - spacing;

                            for (int i = 0; i < editorData.assetStoreManager.assets.Count; i++)
                            {
                                if (Engine.textureManager.textures.ContainsKey("ui_" + editorData.assetStoreManager.assets[i].Path))
                                {
                                    if (i % columns != 0)
                                    {
                                        ImGui.SameLine();
                                    }

                                    ImGui.BeginGroup();
                                    ImGui.PushID(editorData.assetStoreManager.assets[i].Path);

                                    ImGui.Image((IntPtr)Engine.textureManager.textures["ui_" + editorData.assetStoreManager.assets[i].Path].TextureId, imageSize);

                                    var cursorPos = ImGui.GetCursorPos();

                                    if (ImGui.IsItemHovered())
                                    {
                                        // Get the current window's draw list
                                        ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                                        // Retrieve the bounding box of the last item (the image)
                                        System.Numerics.Vector2 min = ImGui.GetItemRectMin();
                                        System.Numerics.Vector2 max = ImGui.GetItemRectMax();

                                        // Define the overlay color (gray transparent)
                                        System.Numerics.Vector4 overlayColor = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.3f); // RGBA

                                        // Draw the overlay
                                        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(overlayColor));

                                        // Define the button position and size
                                        System.Numerics.Vector2 buttonSize = new System.Numerics.Vector2(100, 20);
                                        System.Numerics.Vector2 buttonPos = new System.Numerics.Vector2(
                                            (min.X + max.X) / 2 - buttonSize.X / 2,
                                            (min.Y + max.Y) / 2 - buttonSize.Y / 2
                                        );

                                        // Draw the button
                                        ImGui.SetCursorScreenPos(buttonPos);
                                        if (ImGui.Button("Download", buttonSize))
                                        {
                                        }

                                        if (ImGui.IsItemHovered() && mouseState.IsButtonReleased(MouseButton.Left))
                                        {
                                            editorData.assetStoreManager.DownloadAssetFull(editorData.assetStoreManager.assets[i]);
                                        }
                                    }

                                    ImGui.SetCursorPos(cursorPos);
                                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + itemWidth);
                                    ImGui.TextWrapped(editorData.assetStoreManager.assets[i].Name);
                                    ImGui.PopTextWrapPos();

                                    ImGui.PopID();

                                    ImGui.EndGroup();
                                }
                            }

                            ImGui.Dummy(new System.Numerics.Vector2(0.0f, 10f));

                            ImGui.PopStyleVar();
                        }
                        else
                        {
                            string noInternet = "There is no internet connection.";

                            System.Numerics.Vector2 textSize = ImGui.CalcTextSize(noInternet);

                            System.Numerics.Vector2 windowSize = ImGui.GetWindowSize();

                            System.Numerics.Vector2 textPos = new System.Numerics.Vector2(
                                (windowSize.X - textSize.X) * 0.5f, // X centered
                                (windowSize.Y - textSize.Y) * 0.5f  // Y centered (if you want it exactly in the middle)
                            );

                            ImGui.SetCursorPos(textPos);

                            ImGui.Text(noInternet);
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleVar();
            ImGui.End();
            #endregion

            #region Bottom Panel
            style.Colors[(int)ImGuiCol.WindowBg] = style.Colors[(int)ImGuiCol.MenuBarBg];
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_windowWidth, gameWindow.bottomPanelSize + 4));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, _windowHeight - gameWindow.bottomPanelSize - 4), ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            if (ImGui.Begin("BottomPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {

                string fpsStr = editorData.fps.GetFpsString();
                System.Numerics.Vector2 bottomPanelSize = ImGui.GetContentRegionAvail();
                System.Numerics.Vector2 textSize = ImGui.CalcTextSize(fpsStr);
                if (editorData.assetStoreManager.IsZipDownloadInProgress)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text(editorData.assetStoreManager.ZipDownloadProgress);
                }
                else if(editorData.assetStoreManager.tryingToDownload)
                {
                    ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                    ImGui.Text("Starting the download...");
                }
                ImGui.SetCursorPosY((bottomPanelSize.Y - textSize.Y) * 0.5f);
                ImGui.SetCursorPosX(bottomPanelSize.X - textSize.X);
                ImGui.Text(fpsStr);
            }
            #endregion

            #region Every frame variable updates
            if (isObjectHovered != anyObjectHovered)
                isObjectHovered = anyObjectHovered;

            if (justSelectedItem)
                justSelectedItem = false;

            if (mouseTypes[0] || mouseTypes[1])
                editorData.mouseType = MouseCursor.HResize;
            else if (mouseTypes[2])
                editorData.mouseType = MouseCursor.VResize;
            else
                editorData.mouseType = MouseCursor.Default;
            #endregion

            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 300));
            //if (ImGui.Begin("DebugPanel"))
            //{
            //    int sizeX = 10;
            //    int sizeY = 20;
            //    ImGui.SliderInt("AnimMatrix", ref editorData.animType, 0, 7);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<1", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if(editorData.animType > 0) editorData.animType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">1", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if(editorData.animType < 7) editorData.animType++;
            //    }
            //    ImGui.SliderInt("AnimEndMatrix", ref editorData.animEndType, 0, 1);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<2", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.animEndType == 1) editorData.animEndType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">2", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.animEndType == 0) editorData.animEndType++;
            //    }
            //    ImGui.SliderInt("Matrix", ref editorData.matrixType, 0, 7);
            //    ImGui.SameLine();
            //    if (ImGui.Button("<3", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.matrixType > 0) editorData.matrixType--;
            //    }
            //    ImGui.SameLine();
            //    if (ImGui.Button(">3", new System.Numerics.Vector2(sizeX, sizeY)))
            //    {
            //        if (editorData.matrixType < 7) editorData.matrixType++;
            //    }
            //}

        }

        private void DragDropImageSourceUI(ref TextureManager textureManager, string payloadName, string assetName, string payload, System.Numerics.Vector2 size)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(payload);
                    unsafe
                    {
                        fixed (byte* pPath = pathBytes)
                        {
                            ImGui.SetDragDropPayload(payloadName, (IntPtr)pPath, (uint)pathBytes.Length);
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(size.X, 5));

                    if(textureManager.textures.ContainsKey("ui_" + assetName))
                        ImGui.Image((IntPtr)textureManager.textures["ui_" + assetName].TextureId, size);
                    else
                        ImGui.Image((IntPtr)textureManager.textures["ui_missing.png"].TextureId, size);
                    ImGui.TextWrapped(assetName);

                    ImGui.Dummy(new System.Numerics.Vector2(size.X, 5));

                    ImGui.EndDragDropSource();
                }
            }
        }

        private void DragDropImageSource(ref TextureManager textureManager, string payloadName, string assetName, string textureName, System.Numerics.Vector2 size)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(assetName);
                    unsafe
                    {
                        fixed (byte* pPath = pathBytes)
                        {
                            ImGui.SetDragDropPayload(payloadName, (IntPtr)pPath, (uint)pathBytes.Length);
                        }
                    }
                    ImGui.Image((IntPtr)textureManager.textures[textureName].TextureId, size);
                    ImGui.TextWrapped(assetName);

                    ImGui.EndDragDropSource();
                }
            }
        }

        public void FullscreenWindow(ref EditorData editorData)
        {
            var io = ImGui.GetIO();

            if (editorData.gameRunning == GameState.Running && !editorData.manualCursor)
                io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
            else
                io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

            float totalWidth = 40;
            if (editorData.gameRunning == GameState.Running)
                totalWidth = 60;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(totalWidth*2, 40));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(_windowWidth/2 - totalWidth, 0), ImGuiCond.Always);
            if (ImGui.Begin("TopFullscreenPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                                  ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse ))
            {

                if (editorData.gameRunning == GameState.Stopped)
                {
                    if (ImGui.ImageButton("play", (IntPtr)Engine.textureManager.textures["ui_play.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Running;
                        editorData.justSetGameState = true;
                    }
                }
                else
                {
                    if (ImGui.ImageButton("stop", (IntPtr)Engine.textureManager.textures["ui_stop.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.gameRunning = GameState.Stopped;
                        editorData.justSetGameState = true;
                    }
                }

                ImGui.SameLine();
                if (editorData.gameRunning == GameState.Running)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)Engine.textureManager.textures["ui_pause.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                    {
                        editorData.isPaused = !editorData.isPaused;
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("screen", (IntPtr)Engine.textureManager.textures["ui_screen.png"].TextureId, new System.Numerics.Vector2(20, 20)))
                {
                    editorData.isGameFullscreen = !editorData.isGameFullscreen;
                }
            }
            ImGui.End();

            return;
        }
    }
}
