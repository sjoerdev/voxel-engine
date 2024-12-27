using OpenTK.Graphics.OpenGL;

namespace Project;

public class Framebuffer
{
    public int handle;
    public int texture;

    public Framebuffer()
    {
        handle = GL.GenFramebuffer();
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 1280, 720, 0, PixelFormat.Rgb, PixelType.Float, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Show(int width, int height, Shader shader)
    {
        GL.Viewport(0, 0, width, height);
        GL.UseProgram(shader.program);

        GL.Uniform1(GL.GetUniformLocation(shader.program, "fbtex"), 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindVertexArray(shader.vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Resize(int width, int height)
    {
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.Float, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Clear()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }
}