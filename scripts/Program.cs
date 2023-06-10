using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace Project;

class Program
{
    static void Main(string[] args)
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
    int voxelTraceSteps = 600;
    bool canvasAABBcheck = true;
    bool normalAsAlbedo = false;
    bool visualizeSteps = false;
    int currentBrushType = 0;
    int currentDataSetType = 0;
    int brushSize = 24;
    float hue = 0.25f;
    float sculptTick = 0;
    float brushSpeed = 30;

    public Window() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        this.CenterWindow(new Vector2i(1280, 720));
        Title = "Sjoerd's Voxel Engine";
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        if (shader != null) shader.SetViewport(Size);
        if (camera != null) camera.aspect = Size.X / Size.Y;
        if (imguiHelper != null) imguiHelper.WindowResized(Size.X, Size.Y);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // setup shader
        shader = new Shader("shaders/vert.glsl", "shaders/frag.glsl");

        // create voxel data
        voxels = new Voxels(new Vector3i(256, 256, 256));

        // setup camera
        var pos = new Vector3(voxels.size.X / 2, voxels.size.Y / 2, voxels.size.Z * 2);
        camera = new Camera(pos, Size.X / Size.Y);

        // setup imgui
        imguiHelper = new ImGuiHelper(Size.X, Size.Y);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        shader.Destroy();
        File.Delete("imgui.ini");
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        timePassed += (float)args.Time;
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
            var position = voxels.VoxelTrace(camera.position, dir, 9999);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 0) voxels.SculptVoxelData(position, brushSize, hue);
            if(mouse.IsButtonDown(MouseButton.Left) && currentBrushType == 1) voxels.SculptVoxelData(position, brushSize, 0);
            sculptTick += (1 / brushSpeed);
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
        imguiHelper.Update(this, (float)args.Time);
        shader.Use();
        frametimes.Add(((float)args.Time));

        // imgui start
        ImGui.Begin("window");
        int itemsWidth = 180;

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

        // imgui rendering settings
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "rendering settings:");
        ImGui.Checkbox("use normal as albedo", ref normalAsAlbedo);
        ImGui.Checkbox("visualize steps", ref visualizeSteps);
        ImGui.Checkbox("canvas aabb check", ref canvasAABBcheck);
        ImGui.SetNextItemWidth(itemsWidth); ImGui.SliderInt("ray steps", ref voxelTraceSteps, 10, 1000);

        // imgui serialization
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "serialization:");
        if (ImGui.Button("save", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Save();
        if (ImGui.Button("load", new System.Numerics.Vector2(itemsWidth, 0))) voxels.Load();

        // imgui dataset generation
        for (int i = 0; i < 2; i++) ImGui.Spacing();
        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.8f, 1), "dataset generation:");
        string[] dataSetType = new string[3]{"sphere", "simplex noise", "jawbreaker"};
        ImGui.SetNextItemWidth(itemsWidth); ImGui.Combo("dataset type", ref currentDataSetType, dataSetType, dataSetType.Length);
        if (ImGui.Button("generate", new System.Numerics.Vector2(itemsWidth, 0)))
        {
            if (currentDataSetType == 0) voxels.LoadSphere();
            if (currentDataSetType == 1) voxels.LoadNoise();
            if (currentDataSetType == 2) voxels.LoadJawBreaker();
        }

        // imgui end
        ImGui.End();

        // pass data to shader
        shader.SetVector2("resolution", ((Vector2)Size));
        shader.SetFloat("iTime", timePassed);
        shader.SetBool("normalAsAlbedo", normalAsAlbedo);
        shader.SetBool("visualizeSteps", visualizeSteps);
        shader.SetBool("canvasAABBcheck", canvasAABBcheck);
        shader.SetInt("voxelTraceSteps", voxelTraceSteps);
        shader.SetVector3("dataSize", ((Vector3)voxels.size));
        shader.SetCamera(camera, "view", "camPos");
        shader.SetVoxelData(voxels, "data");

        // render
        shader.Render();
        imguiHelper.Render();
        Context.SwapBuffers();
    }

    protected override void OnTextInput(TextInputEventArgs eventArgs)
    {
        base.OnTextInput(eventArgs);
        imguiHelper.PressChar((char)eventArgs.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs eventArgs)
    {
        base.OnMouseWheel(eventArgs);
        imguiHelper.MouseScroll(eventArgs.Offset);
    }
}