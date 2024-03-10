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
    Shader shader;
    Voxels voxels;

    float timePassed;
    List<float> frametimes = new List<float>();

    bool firstMouseMovement = true;
    Vector2 lastMousePos;
    Vector2 camOrbitRotation;
    float cameraDistance = 600;

    int voxelTraceSteps = 1000;
    bool canvasAABBcheck = true;

    bool showDebugView = false;
    int debugView = 0;

    int currentBrushType = 0;
    int currentDataSetType = 0;
    int brushSize = 24;
    float hue = 0.72f;
    float sculptTick = 0;
    float brushSpeed = 30;
    bool vsync = true;
    bool fullscreen;
    float shadowBias = 2;
    bool shadows = true;
    bool vvao = true;
    float renderScale = 0.5f;

    static NativeWindowSettings windowSettings = new NativeWindowSettings()
    {
        Title = "Sjoerd's Voxel Engine",
        APIVersion = new Version(3, 3),
        Size = new Vector2i(1280, 720)
    };

    public Window() : base(GameWindowSettings.Default, windowSettings) { }

    protected override void OnLoad()
    {
        base.OnLoad();

        // setup shader
        shader = new Shader("shaders/vert.glsl", "shaders/frag.glsl", "shaders/post.glsl");

        // create voxel data
        voxels = new Voxels();

        // setup camera
        camera = new Camera();

        // setup imgui
        imguiHelper = new ImGuiHelper(Size.X, Size.Y);

        // initialize vvao
        Ambient.Init(voxels);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        File.Delete("imgui.ini");
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // general stuff
        VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        timePassed += (float)args.Time;
        frametimes.Add((float)args.Time);
        imguiHelper.Update(this, (float)args.Time);

        // start input
        var mouse = MouseState;
        var input = KeyboardState;
        if (!IsFocused) return;

        // voxel sculpting
        if (timePassed > sculptTick)
        {
            var ndc = (mouse.Position / Size) - new Vector2(0.5f, 0.5f);
            float aspect = (float)Size.X / (float)Size.Y;
            var uv = ndc * new Vector2(aspect, 1);
            Vector3 dir = (camera.GetViewMatrix() * new Vector4(uv.X, -uv.Y, 1, 1)).Xyz;
            var position = voxels.VoxelTrace(camera.position, dir, 10000);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 0) voxels.SculptVoxelData(position, brushSize, hue);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 1) voxels.SculptVoxelData(position, brushSize, 0);
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

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // imgui start
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(8, 8), ImGuiCond.Once);
        ImGui.Begin("settings", ImGuiWindowFlags.NoResize);
        int itemsWidth = 160;

        // imgui metrics
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "metrics:");
        int amount = 256;
        float maxFramerate = 165;
        float minFramerate = 20;
        int start = frametimes.Count < amount ? 0 : frametimes.Count - amount;
        int length = frametimes.Count < amount ? frametimes.Count : amount;
        ImGui.PlotLines("", ref frametimes.ToArray()[start], length, 0, "fps: " + ImGui.GetIO().Framerate.ToString("#"), 1f / maxFramerate, 1f / minFramerate, new System.Numerics.Vector2(0, 40));

        // imgui brush
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "brush settings:");
        string[] items = new string[2]{"sculpt add", "sculpt remove"};
        ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("brush type", ref currentBrushType, items, items.Length);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderInt("brush size", ref brushSize, 8, 32);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("brush speed", ref brushSpeed, 10, 30);
        System.Numerics.Vector4 hueSliderColor = new System.Numerics.Vector4();
        ImGui.ColorConvertHSVtoRGB(hue, 1, 0.5f, out hueSliderColor.X, out hueSliderColor.Y, out hueSliderColor.Z);
        hueSliderColor.W = 1;
        ImGui.PushStyleColor(ImGuiCol.FrameBg, hueSliderColor);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, hueSliderColor);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, hueSliderColor);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("brush hue", ref hue, 0.001f, 1);
        ImGui.StyleColorsDark();

        // imgui display settings
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "display settings:");
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("resolution scale", ref renderScale, 0.1f, 1);
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

        // imgui rendering settings
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "rendering settings:");
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderInt("ray steps", ref voxelTraceSteps, 10, 2000);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderFloat("shadow bias", ref shadowBias, 0.1f, 4);
        ImGui.Checkbox("shadows", ref shadows);
        ImGui.Checkbox("vvao", ref vvao);
        ImGui.Checkbox("canvas aabb check", ref canvasAABBcheck);

        // imgui debugging
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "debugging:");
        string[] view = new string[3]{"normals", "steps", "vvao"};
        ImGui.Checkbox("debug view", ref showDebugView);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("view", ref debugView, view, view.Length);

        // imgui serialization
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "serialization:");
        if (ImGui.Button("save", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Save();
        if (ImGui.Button("load", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Load();

        // imgui dataset generation
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "dataset generation:");
        string[] dataSetType = new string[5]{"sphere", "simplex noise", "occlusion test", "dragon", "nymphe"};
        ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("dataset type", ref currentDataSetType, dataSetType, dataSetType.Length);
        if (ImGui.Button("generate", new System.Numerics.Vector2(itemsWidth, 0)))
        {
            if (currentDataSetType == 0) voxels.LoadSphere(256);
            if (currentDataSetType == 1) voxels.LoadNoise(256);
            if (currentDataSetType == 2) voxels.LoadOcclusionTest(256);
            if (currentDataSetType == 3) voxels.LoadVox("vox/dragon.vox");
            if (currentDataSetType == 4) voxels.LoadVox("vox/nymphe.vox");
        }

        // imgui end
        ImGui.End();

        // set shader uniforms
        shader.UseMainProgram();
        shader.SetVector2("resolution", ((Vector2)Size));
        shader.SetFloat("iTime", timePassed);
        shader.SetBool("showDebugView", showDebugView);
        shader.SetInt("debugView", debugView);
        shader.SetBool("canvasAABBcheck", canvasAABBcheck);
        shader.SetBool("shadows", shadows);
        shader.SetBool("vvao", vvao);
        shader.SetFloat("shadowBias", shadowBias);
        shader.SetInt("voxelTraceSteps", voxelTraceSteps);
        shader.SetVector3("dataSize", (Vector3)voxels.size);
        shader.SetCamera(camera, "view", "camPos");
        shader.SetVoxelData(voxels, "data");
        shader.SetAmbientOcclusion(Ambient.texture, "ambientOcclusionData");
        shader.SetVector3("ambientOcclusionDataSize", (Vector3)Ambient.size);
        shader.SetInt("aoDis", Ambient.distance);
        
        // render
        shader.RenderMain((int)(Size.X * renderScale), (int)(Size.Y * renderScale));
        shader.RenderPost(Size.X, Size.Y);
        imguiHelper.Render();
        Context.SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs arg) { base.OnResize(arg); imguiHelper.WindowResized(Size.X, Size.Y); }
    protected override void OnTextInput(TextInputEventArgs arg) { base.OnTextInput(arg); imguiHelper.PressChar((char)arg.Unicode); }
    protected override void OnMouseWheel(MouseWheelEventArgs arg) { base.OnMouseWheel(arg); imguiHelper.MouseScroll(arg.Offset); }
}