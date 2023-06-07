using OpenTK.Mathematics;

namespace Project;

public class Camera
{
    private Vector3 front;
    private Vector3 up;
    private Vector3 right;
    private float pitch;
    private float yaw;
    public Vector3 position;
    public float aspect;

    public Camera(Vector3 position, float aspect)
    {
        this.position = position;
        this.aspect = aspect;
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(position, position + front, up);
    }

    public void RotateAround(Vector3 target, Vector2 rotation, float offset)
    {
        yaw = rotation.X + MathHelper.DegreesToRadians(90);
        pitch = rotation.Y;
        front.X = MathF.Cos(pitch) * MathF.Cos(yaw);
        front.Y = MathF.Sin(pitch);
        front.Z = MathF.Cos(pitch) * MathF.Sin(yaw);
        front = Vector3.Normalize(front);
        right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
        up = Vector3.Normalize(Vector3.Cross(right, front));
        position = target + front * offset;
    }
}