using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public class Shader
{
    public int mainProgramHandle;
    public int postProgramHandle;
    public int vbo;
    public int vao;
    public int fbo;
    public int fbtex;

    public Shader(string vertexShaderPath, string fragmentShaderPath, string postShaderPath)
    {
        // read shaders
        string vertCode = File.ReadAllText(vertexShaderPath);
        string fragCode = File.ReadAllText(fragmentShaderPath);
        string postCode = File.ReadAllText(postShaderPath);

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

        // compile post
        int post = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(post, postCode);
        GL.CompileShader(post);
        GL.GetShader(post, ShaderParameter.CompileStatus, out int pStatus);
        if (pStatus != 1) throw new Exception("Post shader failed to compile: " + GL.GetShaderInfoLog(post));

        // create main shader program
        mainProgramHandle = GL.CreateProgram();
        GL.AttachShader(mainProgramHandle, vert);
        GL.AttachShader(mainProgramHandle, frag);
        GL.LinkProgram(mainProgramHandle);
        GL.DetachShader(mainProgramHandle, vert);
        GL.DetachShader(mainProgramHandle, frag);

        // create post shader program
        postProgramHandle = GL.CreateProgram();
        GL.AttachShader(postProgramHandle, vert);
        GL.AttachShader(postProgramHandle, post);
        GL.LinkProgram(postProgramHandle);
        GL.DetachShader(postProgramHandle, vert);
        GL.DetachShader(postProgramHandle, post);

        // delete shaders
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
        GL.DeleteShader(post);

        // define vertices
        float[] vertices = new float[]
        {
            -1f, 1f, 0f,
            1f, 1f, 0f,
            -1f, -1f, 0f,
            1f, 1f, 0f,
            1f, -1f, 0f,
            -1f, -1f, 0f,
        };

        // create vbo
        vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        // create vao
        vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);

        // create framebuffer
        fbo = GL.GenFramebuffer();

        // create framebuffer texture
        fbtex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, fbtex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 1280, 720, 0, PixelFormat.Rgb, PixelType.Float, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fbtex, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void RenderMain(int width, int height)
    {
        // resize framebuffer texture
        GL.BindTexture(TextureTarget.Texture2D, fbtex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.Float, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        // render main shader program
        GL.Viewport(0, 0, width, height);
        GL.UseProgram(mainProgramHandle);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void RenderPost(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        GL.UseProgram(postProgramHandle);
        GL.Uniform1(GL.GetUniformLocation(postProgramHandle, "fbtex"), 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, fbtex);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void UseMainProgram()
    {
        GL.UseProgram(mainProgramHandle);
    }

    public void SetVoxelData(Voxels data, string name)
    {
        GL.Uniform1(GL.GetUniformLocation(mainProgramHandle, name), 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, data.texture);
    }

    public void SetAmbientOcclusion(int tex, string name)
    {
        GL.Uniform1(GL.GetUniformLocation(mainProgramHandle, name), 1);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, tex);
    }

    public void SetCamera(Camera camera, string viewMatrixName, string cameraPositionName)
    {
        GL.UniformMatrix4(GL.GetUniformLocation(mainProgramHandle, viewMatrixName), true, ref camera.viewMatrix);
        GL.Uniform3(GL.GetUniformLocation(mainProgramHandle, cameraPositionName), camera.position.X, camera.position.Y, camera.position.Z);
    }
    
    public void SetFloat(string name, float value)
    {
        GL.Uniform1(GL.GetUniformLocation(mainProgramHandle, name), value);
    }

    public void SetInt(string name, int value)
    {
        GL.Uniform1(GL.GetUniformLocation(mainProgramHandle, name), value);
    }

    public void SetBool(string name, bool value)
    {
        GL.Uniform1(GL.GetUniformLocation(mainProgramHandle, name), value ? 1 : 0);
    }

    public void SetVector2(string name, Vector2 value)
    {
        GL.Uniform2(GL.GetUniformLocation(mainProgramHandle, name), value.X, value.Y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3(GL.GetUniformLocation(mainProgramHandle, name), value.X, value.Y, value.Z);
    }
}