using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project
{
    public class Shader
    {
        public int handle;
        public int VBO;
        public int VAO;

        public Shader(string vertexShaderPath, string fragmentShaderPath)
        {
            string vertCode = File.ReadAllText(vertexShaderPath);
            string fragCode = File.ReadAllText(fragmentShaderPath);

            int vert = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vert, vertCode);
            GL.CompileShader(vert);

            int frag = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(frag, fragCode);
            GL.CompileShader(frag);

            handle = GL.CreateProgram();
            GL.AttachShader(handle, vert);
            GL.AttachShader(handle, frag);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, vert);
            GL.DetachShader(handle, frag);

            GL.DeleteShader(VBO);
            GL.DeleteShader(frag);

            float[] vertices = new float[]
            {
                -1f, 1f, 0f,
                1f, 1f, 0f,
                -1f, -1f, 0f,
                1f, 1f, 0f,
                1f, -1f, 0f,
                -1f, -1f, 0f,
            };

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        }

        private static void CheckForGLError()
        {
            var error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
            {
                Console.WriteLine(error);
                throw new InvalidOperationException();
            }
        }

        public void SetViewport(Vector2i resolution)
        {
            GL.Viewport(0, 0, resolution.X, resolution.Y);
        }
        
        public void SetFloat(string name, float value)
        {
            GL.Uniform1(GL.GetUniformLocation(handle, name), value);
        }

        public void SetInt(string name, int value)
        {
            GL.Uniform1(GL.GetUniformLocation(handle, name), value);
        }

        public void SetBool(string name, bool value)
        {
            GL.Uniform1(GL.GetUniformLocation(handle, name), value ? 1 : 0);
        }

        public void SetVector2i(string name, Vector2i value)
        {
            GL.Uniform2(GL.GetUniformLocation(handle, name), (float)value.X, (float)value.Y);
        }

        public void SetCamera(Camera camera, string viewMatrixName, string cameraPositionName)
        {
            var viewMatrix = camera.GetViewMatrix();
            GL.UniformMatrix4(GL.GetUniformLocation(handle, viewMatrixName), true, ref viewMatrix);
            GL.Uniform3(GL.GetUniformLocation(handle, cameraPositionName), camera.Position.X, camera.Position.Y, camera.Position.Z);
        }

        public void SetVoxelData(VoxelData data, string name)
        {
            GL.Uniform1(GL.GetUniformLocation(handle, name), 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture3D, data.voxelTextureHandle);
        }

        public void Use()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.UseProgram(handle);
        }

        public void Render()
        {
            GL.UseProgram(handle);
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        public void Destroy()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VBO);
            GL.UseProgram(0);
            GL.DeleteProgram(VAO);
        }
    }
}