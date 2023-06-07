using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public class Shader
{
    public int handle;
    public int vbo;
    public int vao;

    public Shader(string vertexShaderPath, string fragmentShaderPath)
    {
        string vertCode = File.ReadAllText(vertexShaderPath);
        string fragCode = File.ReadAllText(fragmentShaderPath);

        int vert = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vert, vertCode);
        GL.CompileShader(vert);

        GL.GetShader(vert, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus != 1) throw new Exception("Vertex shader failed to compile: " + GL.GetShaderInfoLog(vert));

        int frag = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(frag, fragCode);
        GL.CompileShader(frag);

        GL.GetShader(frag, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus != 1) throw new Exception("Fragment shader failed to compile: " + GL.GetShaderInfoLog(frag));

        handle = GL.CreateProgram();
        GL.AttachShader(handle, vert);
        GL.AttachShader(handle, frag);
        GL.LinkProgram(handle);

        GL.DetachShader(handle, vert);
        GL.DetachShader(handle, frag);

        GL.DeleteShader(vbo);
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

        vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
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

    public void SetVector2(string name, Vector2 value)
    {
        GL.Uniform2(GL.GetUniformLocation(handle, name), value.X, value.Y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3(GL.GetUniformLocation(handle, name), value.X, value.Y, value.Z);
    }

    public void SetCamera(Camera camera, string viewMatrixName, string cameraPositionName)
    {
        var viewMatrix = camera.GetViewMatrix();
        GL.UniformMatrix4(GL.GetUniformLocation(handle, viewMatrixName), true, ref viewMatrix);
        GL.Uniform3(GL.GetUniformLocation(handle, cameraPositionName), camera.position.X, camera.position.Y, camera.position.Z);
    }

    public void SetVoxelData(Voxels data, string name)
    {
        GL.Uniform1(GL.GetUniformLocation(handle, name), 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, data.texture);
    }

    public void Use()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(handle);
    }

    public void Render()
    {
        GL.UseProgram(handle);
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Destroy()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.DeleteBuffer(vbo);
        GL.UseProgram(0);
        GL.DeleteProgram(vao);
    }
}