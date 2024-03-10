using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public class Voxels
{
    public float[,,] array;
    public int texture;
    public Vector3i size;

    public Voxels()
    {
        LoadVox("vox/dragon.vox");
    }

    public void Save()
    {
        Serialization.SerializeVoxels("voxeldata", array, size);
    }

    public void Load()
    {
        array = Serialization.DeserializeVoxels("voxeldata", size);
        GenTexture();
        Ambient.Init(this);
    }

    public void LoadVox(string path)
    {
        array = Vox.ReadVox(path);
        size = GetSize(array);
        GenTexture();
        Ambient.Init(this);
    }

    public void LoadSphere(int size)
    {
        this.size = Vector3i.One * size;
        float[,,] sphere = new float[size, size, size];

        Parallel.For(0, size, x =>
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    var filled = Vector3.Distance(new Vector3(x, y, z), Vector3.One * size / 2) < 100;
                    if (filled) sphere[x, y, z] = 0.6f;
                }
            }
        });

        array = sphere;
        GenTexture();
        Ambient.Init(this);
    }

    public void LoadOcclusionTest(int size)
    {
        this.size = Vector3i.One * size;
        float[,,] voxels = new float[size, size, size];

        Parallel.For(0, size, x =>
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    int sphereSize = 100;
                    int floorHeight = size / 2 - 40;
                    bool inSphere = Vector3.Distance(new Vector3(x, y, z), Vector3.One * size / 2) < sphereSize / 2;
                    bool inFloor = y < floorHeight;
                    if (inSphere || inFloor) voxels[x, y, z] = 0.6f;
                    else voxels[x, y, z] = 0;
                }
            }
        });

        array = voxels;
        GenTexture();
        Ambient.Init(this);
    }

    public void LoadNoise(int size)
    {
        this.size = Vector3i.One * size;
        float[,,] noise = new float[size, size, size];
        Random random = new Random();
        Noise.Seed = random.Next(100, 10000);
        Parallel.For(0, size, x =>
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    var filled = Noise.CalcPixel3D(x, y, z, 0.0075f) / 255 > 0.5f;
                    if (filled) noise[x, y, z] = 0.6f;
                }
            }
        });

        array = noise;
        GenTexture();
        Ambient.Init(this);
    }

    public void GenTexture()
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
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
    }

    // voxel sculpting, if value <= 0, instead of adding voxels you will remove them
    public void SculptVoxelData(Vector3i position, int radius, float value)
    {
        Vector3i corner = position - Vector3i.One * radius / 2;
        float[,,] newSubData = new float[radius, radius, radius];

        // clamp subdata corner inside voxel data
        Vector3i delta = new Vector3i();
        if (corner.X < 0) delta.X = corner.X;
        if (corner.Y < 0) delta.Y = corner.Y;
        if (corner.Z < 0) delta.Z = corner.Z;
        if (corner.X > size.X - radius) delta.X = radius - (size.X - corner.X);
        if (corner.Y > size.Y - radius) delta.Y = radius - (size.Y - corner.Y);
        if (corner.Z > size.Z - radius) delta.Z = radius - (size.Z - corner.Z);
        corner -= delta;

        // calculate chich voxels to change
        List<Vector3i> voxelsToChange = new List<Vector3i>();
        for (int x = 0; x < radius; x++)
        {
            for (int y = 0; y < radius; y++)
            {
                for (int z = 0; z < radius; z++)
                {
                    Vector3i localCoord = new Vector3i(x, y, z);
                    Vector3i worldCoord = corner + localCoord;

                    // set subdata voxel to current voxel before changing it to anything else
                    float currentWorldSpaceValue = array[worldCoord.X, worldCoord.Y, worldCoord.Z];
                    newSubData[localCoord.Z, localCoord.Y, localCoord.X] = currentWorldSpaceValue;

                    // flag surface voxels for change depending on if we are removing or adding voxels
                    bool isInRadius = Vector3.Distance(localCoord - delta, new Vector3(radius, radius , radius) / 2) < radius / 2;
                    bool shouldAdd = value > 0 && currentWorldSpaceValue <= 0 && isInRadius && IsVoxelOnSurface(worldCoord);
                    bool shouldRemove = value <= 0 && currentWorldSpaceValue > 0 && isInRadius && IsVoxelSurface(worldCoord);
                    if (shouldAdd || shouldRemove) voxelsToChange.Add(localCoord);
                }
            }
        }
        
        // change voxels that need changing
        for (int i = 0; i < voxelsToChange.Count; i++)
        {
            Vector3i localCoord = voxelsToChange[i];
            Vector3i worldCoord = corner + localCoord;
            newSubData[localCoord.Z, localCoord.Y, localCoord.X] = value;
            array[worldCoord.X, worldCoord.Y, worldCoord.Z] = value;
        }

        // update texture
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexSubImage3D(TextureTarget.Texture3D, 0, corner.X, corner.Y, corner.Z, radius, radius, radius, PixelFormat.Red, PixelType.Float, newSubData);

        // update vvao
        Ambient.CalcChanged(this, voxelsToChange, corner);
    }

    public Vector3i VoxelTrace(Vector3 pos, Vector3 dir, int maxSteps)
    {
        Vector3 tdelta;
        float tx, ty, tz;

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

        Vector3i coord = (Vector3i)pos;
        float steps = 0;
        Vector3i result;

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

    public bool IsVoxelOnSurface(Vector3i pos)
    {
        bool isOnSurface = 
        TryGetVoxelValue(new Vector3i(pos.X + 1, pos.Y, pos.Z)) > 0 ||
        TryGetVoxelValue(new Vector3i(pos.X - 1, pos.Y, pos.Z)) > 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y + 1, pos.Z)) > 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y - 1, pos.Z)) > 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y, pos.Z + 1)) > 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y, pos.Z - 1)) > 0;
        return isOnSurface;
    }

    public bool IsVoxelSurface(Vector3i pos)
    {
        bool isSurface = 
        TryGetVoxelValue(new Vector3i(pos.X + 1, pos.Y, pos.Z)) <= 0 || 
        TryGetVoxelValue(new Vector3i(pos.X - 1, pos.Y, pos.Z)) <= 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y + 1, pos.Z)) <= 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y - 1, pos.Z)) <= 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y, pos.Z + 1)) <= 0 ||
        TryGetVoxelValue(new Vector3i(pos.X, pos.Y, pos.Z - 1)) <= 0;
        return isSurface;
    }

    public float TryGetVoxelValue(Vector3i pos)
    {
        if (!IsInBounds(pos, array)) return 0;
        else return array[pos.X, pos.Y, pos.Z];
    }

    private static Vector3i GetSize(float[,,] array)
    {
        return new Vector3i
        (
            array.GetUpperBound(0) + 1,
            array.GetUpperBound(1) + 1,
            array.GetUpperBound(2) + 1
        );
    }
}