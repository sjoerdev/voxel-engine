using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public class Shader
{
    public int program;
    public int vao;

    public Shader(string vert, string frag)
    {
        CompileProgram(vert, frag);
        vao = CreateVertexArray();
    }

    public void RenderToFramebuffer(Vector2i resolution, float renderScale, Framebuffer framebuffer)
    {
        // resize
        int width = (int)(resolution.X * renderScale);
        int height = (int)(resolution.Y * renderScale);
        framebuffer.Resize(width, height);
        GL.Viewport(0, 0, width, height);

        // render
        GL.UseProgram(program);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.handle);
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public int CreateVertexArray()
    {
        // vertices
        float[] vertices =
        [
            -1f, 1f, 0f,
            1f, 1f, 0f,
            -1f, -1f, 0f,
            1f, 1f, 0f,
            1f, -1f, 0f,
            -1f, -1f, 0f,
        ];

        // create vbo
        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        // create vao
        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);

        // return vao
        return vao;
    }

    public void CompileProgram(string vertPath, string fragPath)
    {
        // read shaders
        string vertCode = File.ReadAllText(vertPath);
        string fragCode = File.ReadAllText(fragPath);

        // compile vert
        int vert = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vert, vertCode);
        GL.CompileShader(vert);
        GL.GetShader(vert, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus != 1) throw new Exception("Vertex shader failed to compile: " + GL.GetShaderInfoLog(vert));

        // compile frag
        int frag = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(frag, fragCode);
        GL.CompileShader(frag);
        GL.GetShader(frag, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus != 1) throw new Exception("Fragment shader failed to compile: " + GL.GetShaderInfoLog(frag));

        // create shader program
        program = GL.CreateProgram();
        GL.AttachShader(program, vert);
        GL.AttachShader(program, frag);
        GL.LinkProgram(program);
        GL.DetachShader(program, vert);
        GL.DetachShader(program, frag);

        // delete shaders
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
    }

    public void UseShader()
    {
        GL.UseProgram(program);
    }

    public void SetVoxelData(Voxels data, string name)
    {
        GL.Uniform1(GL.GetUniformLocation(program, name), 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, data.texture);
    }

    public void SetAmbientOcclusion(int tex, string name)
    {
        GL.Uniform1(GL.GetUniformLocation(program, name), 1);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, tex);
    }

    public void SetCamera(Camera camera, string viewMatrixName, string cameraPositionName)
    {
        GL.UniformMatrix4(GL.GetUniformLocation(program, viewMatrixName), true, ref camera.viewMatrix);
        GL.Uniform3(GL.GetUniformLocation(program, cameraPositionName), camera.position.X, camera.position.Y, camera.position.Z);
    }
    
    public void SetFloat(string name, float value)
    {
        GL.Uniform1(GL.GetUniformLocation(program, name), value);
    }

    public void SetInt(string name, int value)
    {
        GL.Uniform1(GL.GetUniformLocation(program, name), value);
    }

    public void SetBool(string name, bool value)
    {
        GL.Uniform1(GL.GetUniformLocation(program, name), value ? 1 : 0);
    }

    public void SetVector2(string name, Vector2 value)
    {
        GL.Uniform2(GL.GetUniformLocation(program, name), value.X, value.Y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3(GL.GetUniformLocation(program, name), value.X, value.Y, value.Z);
    }
}