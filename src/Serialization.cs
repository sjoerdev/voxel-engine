using OpenTK.Mathematics;
using System.Text;

namespace Project;

public static class Serialization
{
    public static void SerializeVoxelsBinary(string fileName, Vector3[,,] voxelData, Vector3i size)
    {
        string path = Environment.CurrentDirectory + "/" + fileName + ".bin";
        Vector3[] flat = Flatten(voxelData, size);
        Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);  
        var writer = new BinaryWriter(stream, Encoding.UTF8, false);
        for (int i = 0; i < flat.Length; i++)
        {
            writer.Write(flat[i].X);
            writer.Write(flat[i].Y);
            writer.Write(flat[i].Z);
        }
        stream.Close();
    }

    public static Vector3[,,] DeserializeVoxelsBinary(string fileName, Vector3i size)
    {
        string path = Environment.CurrentDirectory + "/" + fileName + ".bin";
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Vector3[] flat = new Vector3[size.X * size.Y * size.Z];
        var reader = new BinaryReader(stream, Encoding.UTF8, false);
        for (int i = 0; i < flat.Length; i++)
        {
            flat[i].X = reader.ReadSingle();
            flat[i].Y = reader.ReadSingle();
            flat[i].Z = reader.ReadSingle();
        }
        stream.Close(); 
        return Expand(flat, size);
    }

    // formula used: flat[ x * height * depth + y * depth + z ] = raw[x, y, z];
    private static Vector3[] Flatten(Vector3[,,] raw, Vector3i size)
    {
        int width = size.X;
        int height = size.Y;
        int depth = size.Z;

        Vector3[] flat = new Vector3[width * height * depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    flat[ x * height * depth + y * depth + z ] = raw[x, y, z];
                }
            }
        }
        return flat;
    }

    private static Vector3[,,] Expand(Vector3[] flat, Vector3i size)
    {
        int width = size.X;
        int height = size.Y;
        int depth = size.Z;

        Vector3[,,] raw = new Vector3[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    raw[x, y, z] = flat[ x * height * depth + y * depth + z ];
                }
            }
        }
        return raw;
    }
}