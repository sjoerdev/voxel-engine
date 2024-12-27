using OpenTK.Mathematics;

namespace Project;

public class Vox
{
    public static Vector3[,,] ReadVox(string path)
    {
        var models = ExtractModels(path);

        Vector3i aMax = Vector3i.Zero;
        Vector3i aMin = Vector3i.Zero;
        foreach (var model in models)
        {
            Vector3i mMax = model.position + model.size - Vector3i.One * 3;
            Vector3i mMin = model.position;
            if (mMax.X > aMax.X) aMax.X = mMax.X;
            if (mMax.Y > aMax.Y) aMax.Y = mMax.Y;
            if (mMax.Z > aMax.Z) aMax.Z = mMax.Z;
            if (mMin.X < aMin.X) aMin.X = mMin.X;
            if (mMin.Y < aMin.Y) aMin.Y = mMin.Y;
            if (mMin.Z < aMin.Z) aMin.Z = mMin.Z;
        }
        Vector3i totalSize = aMax - aMin;
        Vector3i offset = -aMin;

        Vector3[,,] fullArray = new Vector3[totalSize.X, totalSize.Z, totalSize.Y];
        foreach (var model in models) foreach (var voxel in model.voxels)
        {
            byte index = (byte)voxel.W;
            Vector3 color = model.palette[index - 1];
            Vector3i worldPos = offset + model.position + voxel.Xyz - Vector3i.One * 3;
            if (Inside(worldPos, totalSize)) fullArray[worldPos.X, worldPos.Z, worldPos.Y] = color;
        }

        return fullArray;
    }

    private static List<Model> ExtractModels(string filePath)
    {
        var models_v = new List<List<Vector4i>>();
        var models_s = new List<Vector3i>();
        var models_p = new List<Vector3i>();
        var models_c = new Vector3[256];

        FileStream fileStream = new FileStream(filePath, FileMode.Open);
        BinaryReader reader = new BinaryReader(fileStream);
        string header = new string(reader.ReadChars(8));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            string chunkIdString = new string(reader.ReadChars(4));
            int contentSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();

            if (chunkIdString == "MAIN")
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // see what subchunk it is
                    string subChunk = new string(reader.ReadChars(4));

                    // size of subchunk
                    int subChunkSize = reader.ReadInt32();
                    int subChunkChildrenSize = reader.ReadInt32();

                    if (subChunk == "SIZE") // model size
                    {
                        Vector3i modelSize = new Vector3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        models_s.Add(modelSize);
                    }
                    else if (subChunk == "XYZI") // model voxels
                    {
                        List<Vector4i> voxels = new List<Vector4i>();
                        int numVoxels = reader.ReadInt32();
                        for (int i = 0; i < numVoxels; i++)
                        {
                            byte x = reader.ReadByte();
                            byte y = reader.ReadByte();
                            byte z = reader.ReadByte();
                            byte index = reader.ReadByte();
                            voxels.Add(new Vector4i(x, y, z, index));
                        }
                        models_v.Add(voxels);
                    }
                    else if (subChunk == "nTRN") // model position
                    {
                        Vector3i modelPosition = Vector3i.Zero;

                        int nodeId = reader.ReadInt32();
                        bool readAttributes = true;
                        if (readAttributes)
                        {
                            int pairAmount = reader.ReadInt32();
                            for (int i = 0; i < pairAmount; i++)
                            {
                                string key = ReadString(reader);
                                string value = ReadString(reader);
                            }
                        }
                        int childNodeId = reader.ReadInt32();
                        int reservedId = reader.ReadInt32();
                        int layerId = reader.ReadInt32();
                        int numFrames = reader.ReadInt32();

                        // read transformation dict
                        for (int f = 0; f < numFrames; f++)
                        {
                            int pairAmount = reader.ReadInt32();
                            for (int p = 0; p < pairAmount; p++)
                            {
                                string key = ReadString(reader);
                                string value = ReadString(reader);
                                if (key == "_t")
                                {
                                    modelPosition = PositionFromString(value);
                                    models_p.Add(modelPosition);
                                }
                            }
                        }
                    }
                    else if (subChunk == "RGBA")
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            byte r = reader.ReadByte();
                            byte g = reader.ReadByte();
                            byte b = reader.ReadByte();
                            byte a = reader.ReadByte();
                            models_c[i] = new Vector3(r / 255f, g / 255f, b / 255f);
                        }
                    }
                    else
                    {
                        reader.ReadBytes(subChunkSize);
                        reader.ReadBytes(subChunkChildrenSize);
                    }
                }
            }
            else
            {
                reader.ReadBytes(contentSize);
                reader.ReadBytes(childrenSize);
            }
        }

        // add up all information
        List<Model> models = new List<Model>();
        for (int i = 0; i < models_s.Count; i++)
        {
            Model model = new Model
            {
                size = models_s[i],
                voxels = models_v[i],
                position = models_p[i],
                palette = models_c
            };
            models.Add(model);
        }

        return models;
    }

    static Vector3i PositionFromString(string str)
    {
        string[] components = str.Split(' ');
        Vector3i vector = new Vector3i(int.Parse(components[0]), int.Parse(components[1]), int.Parse(components[2]));
        return vector;
    }

    private static string ReadString(BinaryReader reader)
    {
        int bytesAmount = reader.ReadInt32();
        List<byte> bytes = new List<byte>();
        for (int i = 0; i < bytesAmount; i++)
        {
            byte currentByte = reader.ReadByte();
            if (currentByte != 0) bytes.Add(currentByte);
        }
        string str = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
        return str;
    }

    private static bool Inside(Vector3i position, Vector3i size)
    {
        bool x = position.X >= 0 && position.X < size.X;
        bool y = position.Y >= 0 && position.Y < size.X;
        bool z = position.Z >= 0 && position.Z < size.X;
        return x && y && z;
    }
    
    private struct Model
    {
        public Vector3i size;
        public List<Vector4i> voxels;
        public Vector3i position;
        public Vector3[] palette;

        public Model()
        {
            size = Vector3i.Zero;
            voxels = new List<Vector4i>();
            position = Vector3i.Zero;
            palette = new Vector3[256];
        }
    }
}