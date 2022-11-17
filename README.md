## What is this?
This is a voxel engine. You can use it to make voxel art, or you can just play around with it. In 3D computer graphics, a voxel represents a value on a regular grid in three-dimensional space. As with pixels in a 2D. So basically a voxel is a 3D pixel. I made this project with no experience of how graphics API's worked, i had to learn everything from scratch and i learned a ton along the way. The project is made with C# and OpenGL.

<img width="632" alt="OJYno4" src="https://user-images.githubusercontent.com/59654421/188631933-4fae6c0a-b264-4192-b201-6c7c5f9e9588.png">

## Storage of voxel data in ram, vram and on disk:

On the cpu side the voxel data is stored in a 3d array of floats, this data can then be manipulated on the cpu:

```csharp
public float[,,] data;
```

To render this data, the data must be send to the shader (a shader is a program that runs on the gpu). In de shader the data is stored as a 3d texture, which in OpenGL can be created like this: 

```csharp
int handle = GL.GenTexture();
GL.BindTexture(TextureTarget.Texture3D, handle);
GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, 128, 128, 128, 0, PixelFormat.Red, PixelType.Float, data);
```

If i want to store the voxel data on disk, i have to flatten the 3d array of floats on the cpu and then serialize them to a binary or json file format, which i did like so:

```csharp
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

// This serialized a flat array of floats to a binary file format
public static void SerializeVoxelsBinary(string fileName, float[,,] voxelData, Vector3i size)
{
    string path = Environment.CurrentDirectory + "/" + fileName + ".bin";
    float[] flat = Flatten(voxelData, size);
    Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);  
    var writer = new BinaryWriter(stream, Encoding.UTF8, false);
    for (int i = 0; i < flat.Length; i++) writer.Write(flat[i]);
    stream.Close();
}
```

## Tracing a ray through voxel data using the 3d dda algorithm on the cpu and on the gpu:
todo

## Calculating normals for voxels and implementing the phong shading model:
todo
