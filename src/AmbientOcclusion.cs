using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public static class AmbientOcclusion
{
    public static float[,,] array;
    public static int texture;
    public static int distance = 32;
    public static Vector3i size;

    public static void Scale(VoxelData voxeldata)
    {
        size = voxeldata.size / distance;
        array = new float[size.X, size.Y, size.Z];
    }

    public static void Init(VoxelData voxeldata)
    {
        Scale(voxeldata);
        CalcAll(voxeldata);
        GenTexture();
    }

    public static void CalcChanged(VoxelData voxeldata, List<Vector3i> changedVoxels, Vector3i corner)
    {
        List<Vector3i> changedBoxes = new List<Vector3i>();
        foreach (var voxel in changedVoxels) if (!changedBoxes.Contains((voxel + corner) / distance)) changedBoxes.Add((voxel + corner) / distance);
        foreach (var box in changedBoxes) CalcBox(box, voxeldata);
        UpdateTexture(voxeldata);
    }

    public static void CalcBox(Vector3i box, VoxelData voxeldata)
    {
        float total = distance * distance * distance;
        float filled = 0;

        for (int vx = 0; vx < distance; vx++)
        {
            for (int vy = 0; vy < distance; vy++)
            {
                for (int vz = 0; vz < distance; vz++)
                {
                    var coord = new Vector3i(box.X * distance + vx, box.Y * distance + vy, box.Z * distance + vz);
                    if (IsInBounds(coord, voxeldata.array) && voxeldata.array[coord.X, coord.Y, coord.Z] != Vector3.Zero) filled++;
                }
            }
        }

        float ao = filled / total;
        if (IsInBounds(box, array)) array[box.X, box.Y, box.Z] = ao;
    }

    public static void CalcAll(VoxelData voxeldata)
    {
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    CalcBox(new Vector3i(x, y, z), voxeldata);
                }
            }
        });
    }

    public static void GenTexture()
    {
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

    public static void UpdateTexture(VoxelData voxeldata)
    {
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

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
    }

    public static bool IsInBounds(Vector3 coord, float[,,] data)
    {
        bool xIs = coord.X >= 0 && coord.X <= data.GetUpperBound(0);
        bool yIs = coord.Y >= 0 && coord.Y <= data.GetUpperBound(1);
        bool zIs = coord.Z >= 0 && coord.Z <= data.GetUpperBound(2);
        return xIs && yIs && zIs;
    }

    public static bool IsInBounds(Vector3 coord, Vector3[,,] data)
    {
        bool xIs = coord.X >= 0 && coord.X <= data.GetUpperBound(0);
        bool yIs = coord.Y >= 0 && coord.Y <= data.GetUpperBound(1);
        bool zIs = coord.Z >= 0 && coord.Z <= data.GetUpperBound(2);
        return xIs && yIs && zIs;
    }
}