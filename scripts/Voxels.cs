using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public class Voxels
{
    public float[,,] array;
    public int texture;
    public Vector3i size;

    public Voxels(Vector3i size)
    {
        this.size = size;
        LoadSphere();
    }

    public void Save()
    {
        Serialization.SerializeVoxels("voxeldata", array, size);
    }

    public void Load()
    {
        array = Serialization.DeserializeVoxels("voxeldata", size);
        GenTexture();
    }

    public void LoadSphere()
    {
        float[,,] sphere = new float[size.X, size.Y, size.Z];
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    if (Vector3.Distance(new Vector3(x, y, z), (new Vector3(size.X, size.Y, size.Z) / 2)) < 64) sphere[x, y, z] = 0.6f;
                    else sphere[x, y, z] = 0;
                }
            }
        }
        array = sphere;
        GenTexture();
    }

    public void GenTexture()
    {
        // rotate data (dont know why this is needed, but whatever, it works)
        float[,,] rotated = new float[size.Z, size.Y, size.X];
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    rotated[z, y, x] = array[x, y, z];
                }
            }
        }

        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
    }

    // voxel sculpting, if value == 0, instead of adding voxels you will remove them
    public void SculptVoxelData(Vector3i position, int radius, float value)
    {
        GL.BindTexture(TextureTarget.Texture3D, texture);

        float[,,] newSubData = new float[radius, radius, radius];

        position = position - Vector3i.One * radius / 2;

        Vector3i delta = new Vector3i();
        if (position.X < 0) delta.X = position.X;
        if (position.Y < 0) delta.Y = position.Y;
        if (position.Z < 0) delta.Z = position.Z;
        if (position.X > size.X - radius) delta.X = radius - (size.X - position.X);
        if (position.Y > size.Y - radius) delta.Y = radius - (size.Y - position.Y);
        if (position.Z > size.Z - radius) delta.Z = radius - (size.Z - position.Z);
        position -= delta;

        List<Vector3i> voxelsToChange = new List<Vector3i>();

        for (int x = 0; x < radius; x++)
        {
            for (int y = 0; y < radius; y++)
            {
                for (int z = 0; z < radius; z++)
                {
                    bool isInBounds = 
                    IsInBounds(position + new Vector3(x, y, z), array) &&
                    IsInBounds(position + new Vector3(x + 1, y, z), array) &&
                    IsInBounds(position + new Vector3(x - 1, y, z), array) &&
                    IsInBounds(position + new Vector3(x, y + 1, z), array) &&
                    IsInBounds(position + new Vector3(x, y - 1, z), array) &&
                    IsInBounds(position + new Vector3(x, y, z + 1), array) &&
                    IsInBounds(position + new Vector3(x, y, z - 1), array);

                    if (isInBounds)
                    {
                        Vector3i localCoord = new Vector3i(x, y, z);
                        Vector3i worldCoord = position + localCoord;
                        
                        bool isInRadius = Vector3.Distance(localCoord - delta, new Vector3(radius, radius , radius) / 2) < radius / 2;

                        float currentWorldSpaceValue = array[worldCoord.X, worldCoord.Y, worldCoord.Z];

                        if (value > 0)
                        {
                            bool isOnSurface = 
                            array[worldCoord.X + 1, worldCoord.Y, worldCoord.Z] > 0 || 
                            array[worldCoord.X - 1, worldCoord.Y, worldCoord.Z] > 0 ||
                            array[worldCoord.X, worldCoord.Y + 1, worldCoord.Z] > 0 ||
                            array[worldCoord.X, worldCoord.Y - 1, worldCoord.Z] > 0 ||
                            array[worldCoord.X, worldCoord.Y, worldCoord.Z + 1] > 0 ||
                            array[worldCoord.X, worldCoord.Y, worldCoord.Z - 1] > 0;

                            if (currentWorldSpaceValue == 0 && isInRadius && isOnSurface) voxelsToChange.Add(localCoord);
                        }
                        else
                        {
                            bool isSurface = 
                            array[worldCoord.X + 1, worldCoord.Y, worldCoord.Z] == 0 || 
                            array[worldCoord.X - 1, worldCoord.Y, worldCoord.Z] == 0 ||
                            array[worldCoord.X, worldCoord.Y + 1, worldCoord.Z] == 0 ||
                            array[worldCoord.X, worldCoord.Y - 1, worldCoord.Z] == 0 ||
                            array[worldCoord.X, worldCoord.Y, worldCoord.Z + 1] == 0 ||
                            array[worldCoord.X, worldCoord.Y, worldCoord.Z - 1] == 0;

                            if (currentWorldSpaceValue > 0 && isInRadius && isSurface) voxelsToChange.Add(localCoord);
                        }

                        newSubData[localCoord.Z, localCoord.Y, localCoord.X] = currentWorldSpaceValue;
                        array[worldCoord.X, worldCoord.Y, worldCoord.Z] = currentWorldSpaceValue;
                    }
                }
            }
        }
        
        for (int i = 0; i < voxelsToChange.Count; i++)
        {
            Vector3i localCoord = voxelsToChange[i];
            Vector3i worldCoord = position + localCoord;

            newSubData[localCoord.Z, localCoord.Y, localCoord.X] = value;
            array[worldCoord.X, worldCoord.Y, worldCoord.Z] = value;
        }

        GL.TexSubImage3D(TextureTarget.Texture3D, 0, position.X, position.Y, position.Z, radius, radius, radius, PixelFormat.Red, PixelType.Float, newSubData);
    }

    public Vector3i VoxelTrace(Vector3 pos, Vector3 dir, int maxSteps)
    {
        Vector3i coord = new Vector3i(((int)MathF.Floor(pos.X)), ((int)MathF.Floor(pos.Y)), ((int)MathF.Floor(pos.Z)));
        Vector3 tdelta;
        float tx, ty, tz;
        float steps = 0;
        Vector3i result;

        if (dir.X < 0)
        {
            tdelta.X = -1 / dir.X;
            tx = (MathF.Floor(pos.X / 1) * 1 - pos.X) / dir.X;
        }
        else 
        {
            tdelta.X = 1 / dir.X;
            tx = ((MathF.Floor(pos.X / 1) + 1) * 1 - pos.X) / dir.X;
        }
        if (dir.Y < 0)
        {
            tdelta.Y = -1 / dir.Y;
            ty = (MathF.Floor(pos.Y / 1) * 1 - pos.Y) / dir.Y;
        }
        else 
        {
            tdelta.Y = 1 / dir.Y;
            ty = ((MathF.Floor(pos.Y / 1) + 1) * 1 - pos.Y) / dir.Y;
        }
        if (dir.Z < 0)
        {
            tdelta.Z = -1 / dir.Z;
            tz = (MathF.Floor(pos.Z / 1) * 1 - pos.Z) / dir.Z;
        }
        else
        {
            tdelta.Z = 1 / dir.Z;
            tz = ((MathF.Floor(pos.Z / 1) + 1) * 1 - pos.Z) / dir.Z;
        }

        // tracing through the grid
        while (true)
        {
            // voxel is hit
            if (IsInBounds(coord, array) && array[coord.X, coord.Y, coord.Z] > 0)
            {
                result = coord;
                break;
            }

            // no voxel was hit
            if (steps > maxSteps)
            {
                result = new Vector3i();
                break;
            }

            // increment step
            if (tx < ty)
            {
                if (tx < tz)
                {
                    tx += tdelta.X;
                    if (dir.X < 0) coord.X -= 1;
                    else coord.X += 1;
                }
                else
                {
                    tz += tdelta.Z;
                    if (dir.Z < 0) coord.Z -= 1;
                    else coord.Z += 1;
                }
            }
            else
            {
                if (ty < tz)
                {
                    ty += tdelta.Y;
                    if (dir.Y < 0) coord.Y -= 1;
                    else coord.Y += 1;
                }
                else
                {
                    tz += tdelta.Z;
                    if (dir.Z < 0) coord.Z -= 1;
                    else coord.Z += 1;
                }
            }
            steps++;
        }

        return result;
    }

    public bool IsInBounds(Vector3 coord, float[,,] data)
    {
        bool xIs = coord.X >= data.GetLowerBound(0) && coord.X <= data.GetUpperBound(0);
        bool yIs = coord.Y >= data.GetLowerBound(1) && coord.Y <= data.GetUpperBound(1);
        bool zIs = coord.Z >= data.GetLowerBound(2) && coord.Z <= data.GetUpperBound(2);
        return xIs && yIs && zIs;
    }
}