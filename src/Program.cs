using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace Project
{
    class Program
    {
        static void Main(string[] args)
        {
            Window window = new Window();
            window.Run();
        }
    }

    public class Window : GameWindow
    {
        ImGuiHelper imgui;
        private float timePassed;
        
        private bool firstMouseMovement = true;
        private Vector2 lastMousePos;

        private Camera camera;
        private Shader shader;
        private VoxelData voxelData;

        int voxelTraceSteps = 1024;
        bool normalAsAlbedo = false;
        float hue = 0.5f;
        Vector3i dataSize = new Vector3i(256, 256, 256); // this value should not change between serializing and deserializing

        float sculptTick = 0;
        float sculptTickSpeed = 60;

        public Window() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(1280, 720));
            Title = "Sjoerd's Voxel Engine";
        }

        protected override void OnResize(ResizeEventArgs args)
        {
            base.OnResize(args);
            if (shader != null) shader.SetViewport(Size);
            if (camera != null) camera.AspectRatio = Size.X / Size.Y;
            if (imgui != null) imgui.WindowResized(Size.X, Size.Y);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // setup shader
            shader = new Shader("res/shader.vert", "res/shader.frag");

            // setup camera
            var pos = new Vector3(dataSize.X / 2, dataSize.Y / 2, dataSize.Z * 2);
            camera = new Camera(pos, Size.X / Size.Y);
            camera.Yaw = 90;

            // set initial cursor state
            CursorState = CursorState.Normal;

            // setup imgui
            imgui = new ImGuiHelper(Size.X, Size.Y);

            // create voxel data
            voxelData = new VoxelData(dataSize);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            shader.Destroy();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            timePassed += (float)args.Time;
            var mouse = MouseState;
            var input = KeyboardState;
            if (!IsFocused) return;

            // set mouse mode
            if (input.IsKeyPressed(Keys.LeftControl))
            {
                if (CursorState == CursorState.Grabbed) CursorState = CursorState.Normal;
                else if (CursorState == CursorState.Normal)
                {
                    CursorState = CursorState.Grabbed;
                    firstMouseMovement = true;
                    return;
                }
            }
            if (CursorState == CursorState.Normal) return;
            
            // voxel sculpting
            if (timePassed > sculptTick)
            {
                var position = voxelData.VoxelTrace(camera.Position, -camera.Front, voxelTraceSteps);
                if(mouse.IsButtonDown(0)) voxelData.SculptVoxelData(((Vector3i)position), 32, hue);
                sculptTick += (1 / sculptTickSpeed);
            }

            // camera movement
            float cameraSpeed = 100f;
            float sensitivity = 0.1f;
            if (input.IsKeyDown(Keys.W)) camera.Position -= camera.Front * cameraSpeed * (float)args.Time;
            if (input.IsKeyDown(Keys.S)) camera.Position += camera.Front * cameraSpeed * (float)args.Time;
            if (input.IsKeyDown(Keys.A)) camera.Position -= camera.Right * cameraSpeed * (float)args.Time;
            if (input.IsKeyDown(Keys.D)) camera.Position += camera.Right * cameraSpeed * (float)args.Time;
            if (input.IsKeyDown(Keys.Space)) camera.Position += camera.Up * cameraSpeed * (float)args.Time;
            if (input.IsKeyDown(Keys.LeftShift)) camera.Position -= camera.Up * cameraSpeed * (float)args.Time;
            if (firstMouseMovement)
            {
                lastMousePos = new Vector2(mouse.X, mouse.Y);
                firstMouseMovement = false;
            }
            else
            {
                var deltaX = mouse.X - lastMousePos.X;
                var deltaY = mouse.Y - lastMousePos.Y;
                lastMousePos = new Vector2(mouse.X, mouse.Y);
                camera.Yaw -= deltaX * sensitivity;
                camera.Pitch += deltaY * sensitivity;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            imgui.Update(this, (float)args.Time);
            shader.Use();

            // setup imgui
            ImGui.SetWindowPos(new System.Numerics.Vector2(16, 16));
            ImGui.SetWindowSize(new System.Numerics.Vector2(256, 256));
            if (CursorState == CursorState.Normal)
            {
                // metrics
                ImGui.Text("fps: " + ImGui.GetIO().Framerate.ToString("#"));
                ImGui.Text("frametime: " + args.Time.ToString("0.00#") + " ms");

                // brush color
                System.Numerics.Vector4 brushColor = new System.Numerics.Vector4();
                ImGui.ColorConvertHSVtoRGB(hue, 1, 1, out brushColor.X, out brushColor.Y, out brushColor.Z);
                ImGui.ColorButton("current brush color", brushColor);
                ImGui.SliderFloat("hue", ref hue, 0.001f, 1);

                // other
                ImGui.Checkbox("use normal as albedo", ref normalAsAlbedo);
                ImGui.SetNextItemWidth(100); ImGui.SliderInt("voxel trace steps", ref voxelTraceSteps, 10, 1000);

                // serialization
                if (ImGui.Button("save", new System.Numerics.Vector2(128, 0))) voxelData.Save();
                if (ImGui.Button("load", new System.Numerics.Vector2(128, 0))) voxelData.Load();
                if (ImGui.Button("clear", new System.Numerics.Vector2(128, 0))) voxelData.LoadSphere();
            }

            // pass data to shader
            shader.SetVector2("resolution", ((Vector2)Size));
            shader.SetFloat("iTime", timePassed);
            shader.SetBool("normalAsAlbedo", normalAsAlbedo);
            shader.SetInt("voxelTraceSteps", voxelTraceSteps);
            shader.SetVector3("dataSize", ((Vector3)dataSize));
            shader.SetCamera(camera, "view", "camPos");
            shader.SetVoxelData(voxelData, "data");

            // render
            shader.Render();
            imgui.Render();
            ImGuiHelper.CheckGLError("End of frame");
            this.Context.SwapBuffers();
        }

        protected override void OnTextInput(TextInputEventArgs eventArgs)
        {
            base.OnTextInput(eventArgs);
            imgui.PressChar((char)eventArgs.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs eventArgs)
        {
            base.OnMouseWheel(eventArgs);
            imgui.MouseScroll(eventArgs.Offset);
        }
    }
}