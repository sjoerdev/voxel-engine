using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace Project;

class Program
{
    static void Main()
    {
        Window window = new Window();
        window.Run();
    }
}

class Window : GameWindow
{
    ImGuiHelper imguiHelper;
    Camera camera;
    Voxels voxels;
    Shader rtshader;
    Shader fbshader;
    Framebuffer framebuffer;

    bool firstMouseMovement = true;
    Vector2 lastMousePos;
    Vector2 camOrbitRotation;
    float cameraDistance = 600;
    float timePassed;
    float sculptTick = 0;
    bool showSettings = true;

    List<float> frametimes = new List<float>();
    int maxsteps = 1000;
    bool canvasCheck = true;
    bool showDebugView = false;
    int debugView = 0;
    int currentBrushType = 0;
    int currentDataSetType = 0;
    int brushSize = 24;
    float brushSpeed = 30;
    bool vsync = false;
    bool fullscreen;
    float shadowBias = 2.8f;
    bool shadows = true;
    bool vvao = true;
    float renderScale = 0.5f;
    Vector3 color = new Vector3(1, 0.4f, 0);
    
    static NativeWindowSettings nativeSettings = new NativeWindowSettings()
    {
        Title = "Sjoerd's Voxel Engine",
        APIVersion = new Version(3, 3),
        Size = new Vector2i(1280, 720)
    };

    public Window() : base(GameWindowSettings.Default, nativeSettings){}

    protected override void OnLoad()
    {
        base.OnLoad();
        rtshader = new Shader("shaders/rt-vert.glsl", "shaders/rt-frag.glsl");
        fbshader = new Shader("shaders/fb-vert.glsl", "shaders/fb-frag.glsl");
        framebuffer = new Framebuffer();
        voxels = new Voxels();
        camera = new Camera();
        imguiHelper = new ImGuiHelper(Size.X, Size.Y);
        Ambient.Init(voxels);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        float delta = (float)args.Time;
        VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        imguiHelper.Update(this, delta);
        timePassed += delta;
        frametimes.Add(delta);

        ApplyInput();
        if (showSettings) SettingsWindow();

        framebuffer.Clear();
        rtshaderUniforms();
        rtshader.RenderToFramebuffer(Size, renderScale, framebuffer);
        framebuffer.Show(Size.X, Size.Y, fbshader);

        imguiHelper.Render();
        Context.SwapBuffers();
    }

    private void ApplyInput()
    {
        // start input
        var mouse = MouseState;
        var input = KeyboardState;
        if (!IsFocused) return;

        // toggle settings
        if (input.IsKeyPressed(Keys.F1)) showSettings = !showSettings;

        // voxel sculpting
        if (timePassed > sculptTick)
        {
            var ndc = (mouse.Position / Size) - new Vector2(0.5f, 0.5f);
            float aspect = (float)Size.X / (float)Size.Y;
            var uv = ndc * new Vector2(aspect, 1);
            Vector3 dir = (camera.viewMatrix * new Vector4(uv.X, -uv.Y, 1, 1)).Xyz;
            var position = voxels.VoxelTrace(camera.position, dir, 10000);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 0) voxels.SculptVoxelData(position, brushSize, color);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 1) voxels.SculptVoxelData(position, brushSize, Vector3.Zero);
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
        camera.RotateAround(voxels.size / 2, camOrbitRotation, cameraDistance);
    }

    private void rtshaderUniforms()
    {
        rtshader.UseShader();
        
        rtshader.SetMatrix4("view", camera.viewMatrix);

        rtshader.SetTexture3("data", voxels.texture, 0);
        rtshader.SetTexture3("ambientOcclusionData", Ambient.texture, 1);
        
        rtshader.SetFloat("time", timePassed);
        rtshader.SetFloat("shadowBias", shadowBias);

        rtshader.SetInt("debugView", debugView);
        rtshader.SetInt("maxsteps", maxsteps);
        rtshader.SetInt("aoDis", Ambient.distance);
        
        rtshader.SetBool("showDebugView", showDebugView);
        rtshader.SetBool("canvasCheck", canvasCheck);
        rtshader.SetBool("shadows", shadows);
        rtshader.SetBool("vvao", vvao);

        rtshader.SetVector2("resolution", (Vector2)Size);

        rtshader.SetVector3("camPos", camera.position);
        rtshader.SetVector3("dataSize", (Vector3)voxels.size);
        rtshader.SetVector3("ambientOcclusionDataSize", (Vector3)Ambient.size);
        
    }

    private void SettingsWindow()
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
                if (fullscreen) WindowState = WindowState.Fullscreen;
                else
                {
                    WindowState = WindowState.Normal;
                    Size = new Vector2i(1280, 720);
                    CenterWindow();
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
            if (ImGui.Button("save", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Save();
            if (ImGui.Button("load", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Load();
        }

        // imgui voxeldata
        if (ImGui.CollapsingHeader("voxeldata"))
        {
            string[] dataSetType = ["sphere", "simplex noise", "occlusion test", "dragon", "nymphe"];
            ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("dataset", ref currentDataSetType, dataSetType, dataSetType.Length);
            if (ImGui.Button("generate", new System.Numerics.Vector2(itemsWidth, 0)))
            {
                if (currentDataSetType == 0) voxels.LoadSphere(256);
                if (currentDataSetType == 1) voxels.LoadNoise(256);
                if (currentDataSetType == 2) voxels.LoadOcclusionTest(256);
                if (currentDataSetType == 3) voxels.LoadVox("vox/dragon.vox");
                if (currentDataSetType == 4) voxels.LoadVox("vox/nymphe.vox");
            }
        }

        // imgui end
        ImGui.End();
    }

    protected override void OnResize(ResizeEventArgs arg) { base.OnResize(arg); imguiHelper.WindowResized(Size.X, Size.Y); }
    protected override void OnTextInput(TextInputEventArgs arg) { base.OnTextInput(arg); imguiHelper.PressChar((char)arg.Unicode); }
    protected override void OnMouseWheel(MouseWheelEventArgs arg) { base.OnMouseWheel(arg); imguiHelper.MouseScroll(arg.Offset); }
}