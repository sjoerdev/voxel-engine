using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public static class Ambient
{
    public static float[,,] array;
    public static int texture;
    public static int distance = 32;

    public static void CalcValues(Voxels voxels)
    {
        var size = voxels.size / distance;
        array = new float[size.X, size.Y, size.Z];

        // loop every ao box
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    float total = distance * distance * distance;
                    float filled = 0;

                    // count non empty voxels in ao box
                    for (int a = 0; a < distance; a++)
                    {
                        for (int b = 0; b < distance; b++)
                        {
                            for (int c = 0; c < distance; c++)
                            {
                                var coord = new Vector3i(x * distance + a, y * distance + b, z * distance + c);
                                float value = voxels.array[coord.X, coord.Y, coord.Z];

                                bool Xinside = coord.X > 0 && coord.X < voxels.size.X - 1;
                                bool Yinside = coord.Y > 0 && coord.Y < voxels.size.Y - 1;
                                bool Zinside = coord.Z > 0 && coord.Z < voxels.size.Z - 1;
                                bool inside = Xinside && Yinside && Zinside;

                                if (value != 0 && inside) filled++;
                            }
                        }
                    }

                    float box = filled / total;
                    array[x, y, z] = box;
                }
            }
        });
    }

    public static void GenTexture(Voxels voxels)
    {
        var size = voxels.size / distance;

        // rotate data (dont know why this is needed, but whatever, it works)
        float[,,] rotated = new float[size.Z, size.Y, size.X];
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    rotated[z, y, x] = array[x, y, z];
                }
            }
        });

        GL.DeleteTexture(texture);
        texture = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
    }
}