# Voxel Engine
<img width="632" alt="OJYno4" src="https://user-images.githubusercontent.com/59654421/188631933-4fae6c0a-b264-4192-b201-6c7c5f9e9588.png">

## What is this, and how was it made?
This is a voxel engine. You can use it to make voxel art, or you can just play around with it. In 3D computer graphics, a voxel represents a value on a regular grid in three-dimensional space. As with pixels in a 2D. So basically a voxel is a 3D pixel. I made this project with no experience of how graphics API's worked, i had to learn everything from scratch and i learned a ton along the way. The project is made with C# and OpenGL.

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

## Tracing a ray through a volume of voxels:
If i want to render the voxels using ray tracing, i have to first find a way to trace a ray through a volume of voxels, after some research i found the 3d dda algorithm, which can be implemented in C# like this:

```csharp
public Vector3i VoxelTrace(Vector3 eye, Vector3 marchingDirection, int voxelTraceSteps)
{
    Vector3 origin = eye;
    Vector3 direction = marchingDirection;
    Vector3 tdelta;
    float tx, ty, tz;

    // initialize t
    if (direction.X < 0)
    {
        tdelta.X = -1 / direction.X;
        tx = (MathF.Floor(origin.X / 1) * 1 - origin.X) / direction.X;
    }
    else 
    {
        tdelta.X = 1 / direction.X;
        tx = ((MathF.Floor(origin.X / 1) + 1) * 1 - origin.X) / direction.X;
    }
    if (direction.Y < 0)
    {
        tdelta.Y = -1 / direction.Y;
        ty = (MathF.Floor(origin.Y / 1) * 1 - origin.Y) / direction.Y;
    }
    else 
    {
        tdelta.Y = 1 / direction.Y;
        ty = ((MathF.Floor(origin.Y / 1) + 1) * 1 - origin.Y) / direction.Y;
    }
    if (direction.Z < 0)
    {
        tdelta.Z = -1 / direction.Z;
        tz = (MathF.Floor(origin.Z / 1) * 1 - origin.Z) / direction.Z;
    }
    else
    {
        tdelta.Z = 1 / direction.Z;
        tz = ((MathF.Floor(origin.Z / 1) + 1) * 1 - origin.Z) / direction.Z;
    }

    // initializing some variables
    float t = 0;
    float steps = 0;
    Vector3i coord = new Vector3i(((int)MathF.Floor(origin.X)), ((int)MathF.Floor(origin.Y)), ((int)MathF.Floor(origin.Z)));
    Vector3i result;

    // tracing through the grid
    while (true)
    {
        // if voxel is hit
        if (IsInBounds(coord, rawData) && rawData[coord.X, coord.Y, coord.Z] > 0)
        {
            result = coord;
            break;
        }

        // if no voxel was hit
        if (steps > voxelTraceSteps)
        {
            result = new Vector3i();
            break;
        }

        // increment step
        if (tx < ty)
        {
            if (tx < tz)
            {
                t = tx;
                tx += tdelta.X;
                if (direction.X < 0) coord.X -= 1;
                else coord.X += 1;
            }
            else
            {
                t = tz;
                tz += tdelta.Z;
                if (direction.Z < 0) coord.Z -= 1;
                else coord.Z += 1;
            }
        }
        else
        {
            if (ty < tz)
            {
                t = ty;
                ty += tdelta.Y;
                if (direction.Y < 0) coord.Y -= 1;
                else coord.Y += 1;
            }
            else
            {
                t = tz;
                tz += tdelta.Z;
                if (direction.Z < 0) coord.Z -= 1;
                else coord.Z += 1;
            }
        }
        steps++;
    }

    return result;
}
```

This code can ealitly be replicated in the GLSL shader language, and a glsl implementation can be found in this repo inside the main fragment shader.

## Calculating normals for voxels and implementing the phong shading model:
To calculate shadows, reflections and other lighting effects, i first have to calculate the normal of the voxel, this is done much differently to how a normal is calculated for polygons. This is the technique i decided to use:

```glsl
// this calculated a normal for a given voxel, it is calculated by sampling neighboring voxels
vec3 VoxelNormal(vec3 coord)
{
    vec3 normal = vec3(0, 0, 0);
    int samplesize = 5;
    float t = samplesize / 2;
    
    for (int x = 0; x < samplesize; x++)
    {
        for (int y = 0; y < samplesize; y++)
        {
            for (int z = 0; z < samplesize; z++)
            {
                float a = x - t;
                float b = y - t;
                float c = z - t;
                if (Sample(vec3(coord.x + a, coord.y + b, coord.z + c)) > 0) 
                {
                    normal += vec3(a, b, c);
                }
            }
        }
    }

    return -normalize(normal);
}
```

Using the phong shading model, diffuse and specular lighting can then be claculated using the normal like this:

```glsl
// calc diffuse
float diffuse = max(0.3, dot(lightpos, normal));

// calc specular
vec3 specularcolor = vec3(0.3, 0.3, 0.3);
vec3 specular = pow(clamp(dot(lightpos, normal), 0.0, 1.0), 64.0) * specularcolor;
```
