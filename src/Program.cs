using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace Project;

static class Program
{
    static GameWindow window;
    static ImGuiHelper imguiHelper;
    static Camera camera;
    static VoxelData voxeldata;
    static FullscreenShader rt_fullscreenshader;
    static FullscreenShader fb_fullscreenshader;
    static Framebuffer framebuffer;

    static bool firstMouseMovement = true;
    static Vector2 lastMousePos;
    static Vector2 camOrbitRotation;
    static float cameraDistance = 600;
    static float timePassed;
    static float sculptTick = 0;
    static bool showSettings = true;

    static List<float> frametimes = [];
    static int maxsteps = 1000;
    static bool canvasCheck = true;
    static bool showDebugView = false;
    static int debugView = 0;
    static int currentBrushType = 0;
    static int currentDataSetType = 0;
    static int brushSize = 24;
    static float brushSpeed = 30;
    static bool vsync = false;
    static bool fullscreen;
    static float shadowBias = 2.8f;
    static bool shadows = true;
    static bool vvao = true;
    static float renderScale = 1.0f;
    static Vector3 color = new Vector3(1, 0.4f, 0);

    static void Main()
    {
        var nativesettings = NativeWindowSettings.Default;
        nativesettings.Size = new Vector2i(1280, 720);
        nativesettings.Title = "Sjoerd's Voxel Engine";
        window = new GameWindow(GameWindowSettings.Default, nativesettings);
        window.Load += WindowLoad;
        window.RenderFrame += WindowRender;
        window.Resize += WindowResize;
        window.TextInput += WindowTextInput;
        window.MouseWheel += WindowMouseWheelInput;
        window.Run();
        window.Dispose();
    }

    static void WindowLoad()
    {
        rt_fullscreenshader = new FullscreenShader("res/shaders/rt-vert.glsl", "res/shaders/rt-frag.glsl");
        fb_fullscreenshader = new FullscreenShader("res/shaders/fb-vert.glsl", "res/shaders/fb-frag.glsl");
        framebuffer = new Framebuffer();
        voxeldata = new VoxelData();
        camera = new Camera();
        imguiHelper = new ImGuiHelper(window.Size.X, window.Size.Y);
        AmbientOcclusion.Init(voxeldata);
    }

    static void WindowRender(FrameEventArgs args)
    {
        float delta = (float)args.Time;
        window.VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        imguiHelper.Update(window, delta);
        timePassed += delta;
        frametimes.Add(delta);

        ApplyInput();
        if (showSettings) SettingsWindow();

        framebuffer.Clear();
        rtshaderUniforms();
        rt_fullscreenshader.RenderToFramebuffer(window.Size, renderScale, framebuffer);
        framebuffer.Show(window.Size.X, window.Size.Y, fb_fullscreenshader);

        imguiHelper.Render();
        window.Context.SwapBuffers();
    }

    static void ApplyInput()
    {
        // start input
        var mouse = window.MouseState;
        var input = window.KeyboardState;
        if (!window.IsFocused) return;

        // toggle settings
        if (input.IsKeyPressed(Keys.F1)) showSettings = !showSettings;

        // voxel sculpting
        if (timePassed > sculptTick)
        {
            var ndc = (mouse.Position / window.Size) - new Vector2(0.5f, 0.5f);
            float aspect = (float)window.Size.X / (float)window.Size.Y;
            var uv = ndc * new Vector2(aspect, 1);
            Vector3 dir = (camera.viewMatrix * new Vector4(uv.X, -uv.Y, 1, 1)).Xyz;
            var position = voxeldata.VoxelTrace(camera.position, dir, 10000);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 0) voxeldata.SculptVoxelData(position, brushSize, color);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 1) voxeldata.SculptVoxelData(position, brushSize, Vector3.Zero);
            sculptTick += 1 / brushSpeed;
        }

        // camera orbit movement
        if (firstMouseMovement) firstMouseMovement = false;
        else if (mouse.IsButtonDown(MouseButton.Right))
        {
            Vector2 mouseDelta = new Vector2(-(mouse.X - lastMousePos.X), mouse.Y - lastMousePos.Y);
            camOrbitRotation += mouseDelta / 500;
            if (camOrbitRotation.Y > MathHelper.DegreesToRadians(89)) camOrbitRotation.Y = MathHelper.DegreesToRadians(89);
            if (camOrbitRotation.Y < MathHelper.DegreesToRadians(-89)) camOrbitRotation.Y = MathHelper.DegreesToRadians(-89);
        }
        lastMousePos = new Vector2(mouse.X, mouse.Y);
        cameraDistance -= mouse.ScrollDelta.Y * 10;
        camera.RotateAround(voxeldata.size / 2, camOrbitRotation, cameraDistance);
    }

    static void rtshaderUniforms()
    {
        rt_fullscreenshader.UseShader();
        
        rt_fullscreenshader.SetMatrix4("view", camera.viewMatrix);

        rt_fullscreenshader.SetTexture3("data", voxeldata.texture, 0);
        rt_fullscreenshader.SetTexture3("ambientOcclusionData", AmbientOcclusion.texture, 1);
        
        rt_fullscreenshader.SetFloat("time", timePassed);
        rt_fullscreenshader.SetFloat("shadowBias", shadowBias);

        rt_fullscreenshader.SetInt("debugView", debugView);
        rt_fullscreenshader.SetInt("maxsteps", maxsteps);
        rt_fullscreenshader.SetInt("aoDis", AmbientOcclusion.distance);
        
        rt_fullscreenshader.SetBool("showDebugView", showDebugView);
        rt_fullscreenshader.SetBool("canvasCheck", canvasCheck);
        rt_fullscreenshader.SetBool("shadows", shadows);
        rt_fullscreenshader.SetBool("vvao", vvao);

        rt_fullscreenshader.SetVector2("resolution", (Vector2)window.Size);

        rt_fullscreenshader.SetVector3("camPos", camera.position);
        rt_fullscreenshader.SetVector3("dataSize", (Vector3)voxeldata.size);
        rt_fullscreenshader.SetVector3("ambientOcclusionDataSize", (Vector3)AmbientOcclusion.size);
        
    }

    static void SettingsWindow()
    {
        // imgui start
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(8, 8), ImGuiCond.Once);
        ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(256, 32), new System.Numerics.Vector2(720, 720));
        ImGui.Begin("settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize);
        int itemsWidth = 140;

        // imgui metrics
        if (ImGui.CollapsingHeader("metrics"))
        {
            int amount = 128;
            if (frametimes.Count > amount)
            {
                int start = frametimes.Count - amount;
                var overlay = "fps: " + ImGui.GetIO().Framerate.ToString("#");
                var size = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 64);
                ImGui.PlotLines("", ref frametimes.ToArray()[start], amount, 0, overlay, 0, 1 / 10f, size);
            }
            else
            {
                int percentage = (int)(frametimes.Count / (float)amount * 100);
                ImGui.Text("loading: " + percentage + "%%");
            }
        }

        // imgui brush
        if (ImGui.CollapsingHeader("brush"))
        {
            string[] items = ["sculpt add", "sculpt remove"];
            ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("type", ref currentBrushType, items, items.Length);
            ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderInt("size", ref brushSize, 8, 32);
            ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("speed", ref brushSpeed, 10, 30);
            
            ImGui.SetNextItemWidth(itemsWidth);
            var colorvec = new System.Numerics.Vector3(color.X, color.Y, color.Z);
            ImGui.ColorPicker3("color", ref colorvec, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoSidePreview);
            color = new Vector3(colorvec.X, colorvec.Y, colorvec.Z);
        }
        
        // imgui display settings
        if (ImGui.CollapsingHeader("display"))
        {
            ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("render scale", ref renderScale, 0.1f, 1);
            ImGui.Checkbox("vsync", ref vsync);
            if (ImGui.Checkbox("fullscreen", ref fullscreen))
            {
                if (fullscreen) window.WindowState = WindowState.Fullscreen;
                else
                {
                    window.WindowState = WindowState.Normal;
                    window.Size = new Vector2i(1280, 720);
                    window.CenterWindow();
                }
            }
        }

        // imgui rendering settings
        if (ImGui.CollapsingHeader("rendering"))
        {
            ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderInt("ray steps", ref maxsteps, 10, 2000);
            ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("shadow bias", ref shadowBias, 0.1f, 4);
            ImGui.Checkbox("shadows", ref shadows);
            ImGui.Checkbox("vvao", ref vvao);
            ImGui.Checkbox("canvas check", ref canvasCheck);
        }

        // imgui debugging
        if (ImGui.CollapsingHeader("debug"))
        {
            string[] view = ["normals", "steps", "vvao"];
            ImGui.Checkbox("debug view", ref showDebugView);
            ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("view", ref debugView, view, view.Length);
        }

        // imgui serialization
        if (ImGui.CollapsingHeader("serialize"))
        {
            if (ImGui.Button("save", new System.Numerics.Vector2(itemsWidth, 0))) voxeldata.Save();
            if (ImGui.Button("load", new System.Numerics.Vector2(itemsWidth, 0))) voxeldata.Load();
        }

        // imgui voxeldata
        if (ImGui.CollapsingHeader("voxeldata"))
        {
            string[] dataSetType = ["sphere", "simplex noise", "occlusion test", "dragon", "nymphe"];
            ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("dataset", ref currentDataSetType, dataSetType, dataSetType.Length);
            if (ImGui.Button("generate", new System.Numerics.Vector2(itemsWidth, 0)))
            {
                if (currentDataSetType == 0) voxeldata.LoadSphere(256);
                if (currentDataSetType == 1) voxeldata.LoadNoise(256);
                if (currentDataSetType == 2) voxeldata.LoadOcclusionTest(256);
                if (currentDataSetType == 3) voxeldata.LoadVox("res/models/dragon.vox");
                if (currentDataSetType == 4) voxeldata.LoadVox("res/models/nymphe.vox");
            }
        }

        // imgui end
        ImGui.End();
    }

    static void WindowResize(ResizeEventArgs arg) => imguiHelper.WindowResized(window.Size.X, window.Size.Y);
    static void WindowTextInput(TextInputEventArgs arg) => imguiHelper.PressChar((char)arg.Unicode);
    static void WindowMouseWheelInput(MouseWheelEventArgs arg) => imguiHelper.MouseScroll(arg.Offset);
}