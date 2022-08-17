using OpenTK.Mathematics;
using System.Text.Json;
using System.Text;

namespace Project
{
    public static class Serialization
    {
        public static void SerializeVoxels(string fileName, float[,,] voxelData, Vector3i size)
        {
            float[] flat = Flatten(voxelData, size);
            string path = Environment.CurrentDirectory + "/" + fileName + ".json";
            string json = JsonSerializer.Serialize(flat);
            File.WriteAllText(path, json);
        }

        public static float[,,] DeserializeVoxels(string fileName, Vector3i size)
        {
            string path = Environment.CurrentDirectory + "/" + fileName + ".json";
            string json = File.ReadAllText(path);
            float[] flat = JsonSerializer.Deserialize<float[]>(json);
            return Expand(flat, size);
        }

        public static void SerializeVoxelsBinary(string fileName, float[,,] voxelData, Vector3i size)
        {
            string path = Environment.CurrentDirectory + "/" + fileName + ".bin";
            float[] flat = Flatten(voxelData, size);
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);  
            var writer = new BinaryWriter(stream, Encoding.UTF8, false);
            for (int i = 0; i < flat.Length; i++) writer.Write(flat[i]);
            stream.Close();
        }

        public static float[,,] DeserializeVoxelsBinary(string fileName, Vector3i size)
        {
            string path = Environment.CurrentDirectory + "/" + fileName + ".bin";
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            float[] flat = new float[size.X * size.Y * size.Z];
            var reader = new BinaryReader(stream, Encoding.UTF8, false);
            for (int i = 0; i < flat.Length; i++) flat[i] = reader.ReadSingle();
            stream.Close(); 
            return Expand(flat, size);
        }

        // formula used: flat[ x * height * depth + y * depth + z ] = raw[x, y, z];
        private static float[] Flatten(float[,,] raw, Vector3i size)
        {
            int width = size.X;
            int height = size.Y;
            int depth = size.Z;

            float[] flat = new float[width * height * depth];

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

        private static float[,,] Expand(float[] flat, Vector3i size)
        {
            int width = size.X;
            int height = size.Y;
            int depth = size.Z;

            float[,,] raw = new float[width, height, depth];

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
}