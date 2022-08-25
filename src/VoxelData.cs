using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project
{
    public class VoxelData
    {
        public float[,,] rawData;
        public int voxelTextureHandle;
        public Vector3i dataSize;

        public VoxelData(Vector3i dataSize)
        {
            this.dataSize = dataSize;
            LoadSphere();
        }

        public void Save()
        {
            Serialization.SerializeVoxels("voxeldata", rawData, dataSize);
            //Serialization.SerializeVoxelsBinary("voxeldata", rawData, dataSize);
        }

        public void Load()
        {
            rawData = Serialization.DeserializeVoxels("voxeldata", dataSize);
            //rawData = Serialization.DeserializeVoxelsBinary("voxeldata", dataSize);
            GenTexture();
        }

        public void LoadSphere()
        {
            float[,,] sphere = new float[dataSize.X, dataSize.Y, dataSize.Z];
            for (int x = 0; x < dataSize.X; x++)
            {
                for (int y = 0; y < dataSize.Y; y++)
                {
                    for (int z = 0; z < dataSize.Z; z++)
                    {
                        if (Vector3.Distance(new Vector3(x, y, z), (new Vector3(dataSize.X, dataSize.Y, dataSize.Z) / 2)) < 64) sphere[x, y, z] = 0.6f;
                        else sphere[x, y, z] = 0;
                    }
                }
            }
            rawData = sphere;
            GenTexture();
        }

        public void GenTexture()
        {
            // rotate data (dont know why this is needed, but whatever, it works)
            float[,,] rotated = new float[dataSize.Z, dataSize.Y, dataSize.X];
            for (int x = 0; x < dataSize.X; x++)
            {
                for (int y = 0; y < dataSize.Y; y++)
                {
                    for (int z = 0; z < dataSize.Z; z++)
                    {
                        rotated[z, y, x] = rawData[x, y, z];
                    }
                }
            }

            voxelTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture3D, voxelTextureHandle);
            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, dataSize.X, dataSize.Y, dataSize.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
        }

        // voxel sculpting, if value == 0, instead of adding voxels you will remove them
        public void SculptVoxelData(Vector3i position, int radius, float value)
        {
            GL.BindTexture(TextureTarget.Texture3D, voxelTextureHandle);

            float[,,] newSubVoxelData = new float[radius, radius, radius];

            position = position - Vector3i.One * radius / 2;

            Vector3i delta = new Vector3i();
            if (position.X < 0) delta.X = position.X;
            if (position.Y < 0) delta.Y = position.Y;
            if (position.Z < 0) delta.Z = position.Z;
            if (position.X > dataSize.X - radius) delta.X = radius - (dataSize.X - position.X);
            if (position.Y > dataSize.Y - radius) delta.Y = radius - (dataSize.Y - position.Y);
            if (position.Z > dataSize.Z - radius) delta.Z = radius - (dataSize.Z - position.Z);
            position -= delta;

            List<Vector3i> voxelsToChange = new List<Vector3i>();

            for (int x = 0; x < radius; x++)
            {
                for (int y = 0; y < radius; y++)
                {
                    for (int z = 0; z < radius; z++)
                    {
                        bool isInBounds = 
                        IsInBounds(position + new Vector3(x, y, z), rawData) &&
                        IsInBounds(position + new Vector3(x + 1, y, z), rawData) &&
                        IsInBounds(position + new Vector3(x - 1, y, z), rawData) &&
                        IsInBounds(position + new Vector3(x, y + 1, z), rawData) &&
                        IsInBounds(position + new Vector3(x, y - 1, z), rawData) &&
                        IsInBounds(position + new Vector3(x, y, z + 1), rawData) &&
                        IsInBounds(position + new Vector3(x, y, z - 1), rawData);

                        if (isInBounds)
                        {
                            Vector3i localCoord = new Vector3i(x, y, z);
                            Vector3i worldCoord = position + localCoord;
                            
                            bool isInRadius = Vector3.Distance(localCoord - delta, new Vector3(radius, radius , radius) / 2) < radius / 2;

                            float currentWorldSpaceValue = rawData[worldCoord.X, worldCoord.Y, worldCoord.Z];

                            if (value > 0)
                            {
                                bool isOnSurface = 
                                rawData[worldCoord.X + 1, worldCoord.Y, worldCoord.Z] > 0 || 
                                rawData[worldCoord.X - 1, worldCoord.Y, worldCoord.Z] > 0 ||
                                rawData[worldCoord.X, worldCoord.Y + 1, worldCoord.Z] > 0 ||
                                rawData[worldCoord.X, worldCoord.Y - 1, worldCoord.Z] > 0 ||
                                rawData[worldCoord.X, worldCoord.Y, worldCoord.Z + 1] > 0 ||
                                rawData[worldCoord.X, worldCoord.Y, worldCoord.Z - 1] > 0;

                                if (currentWorldSpaceValue == 0 && isInRadius && isOnSurface) voxelsToChange.Add(localCoord);
                            }
                            else
                            {
                                bool isSurface = 
                                rawData[worldCoord.X + 1, worldCoord.Y, worldCoord.Z] == 0 || 
                                rawData[worldCoord.X - 1, worldCoord.Y, worldCoord.Z] == 0 ||
                                rawData[worldCoord.X, worldCoord.Y + 1, worldCoord.Z] == 0 ||
                                rawData[worldCoord.X, worldCoord.Y - 1, worldCoord.Z] == 0 ||
                                rawData[worldCoord.X, worldCoord.Y, worldCoord.Z + 1] == 0 ||
                                rawData[worldCoord.X, worldCoord.Y, worldCoord.Z - 1] == 0;

                                if (currentWorldSpaceValue > 0 && isInRadius && isSurface) voxelsToChange.Add(localCoord);
                            }

                            newSubVoxelData[localCoord.Z, localCoord.Y, localCoord.X] = currentWorldSpaceValue;
                            rawData[worldCoord.X, worldCoord.Y, worldCoord.Z] = currentWorldSpaceValue;
                        }
                    }
                }
            }
            
            for (int i = 0; i < voxelsToChange.Count; i++)
            {
                Vector3i localCoord = voxelsToChange[i];
                Vector3i worldCoord = position + localCoord;

                newSubVoxelData[localCoord.Z, localCoord.Y, localCoord.X] = value;
                rawData[worldCoord.X, worldCoord.Y, worldCoord.Z] = value;
            }

            GL.TexSubImage3D(TextureTarget.Texture3D, 0, position.X, position.Y, position.Z, radius, radius, radius, PixelFormat.Red, PixelType.Float, newSubVoxelData);
        }

        public bool IsInBounds(Vector3 coord, float[,,] data)
        {
            bool xIsInBounds = coord.X >= data.GetLowerBound(0) && coord.X <= data.GetUpperBound(0);
            bool yIsInBounds = coord.Y >= data.GetLowerBound(1) && coord.Y <= data.GetUpperBound(1);
            bool zIsInBounds = coord.Z >= data.GetLowerBound(2) && coord.Z <= data.GetUpperBound(2);
            return xIsInBounds && yIsInBounds && zIsInBounds;
        }

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
    }
}