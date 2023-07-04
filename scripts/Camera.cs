using OpenTK.Mathematics;

namespace Project;

public class Camera
{
    public Vector3 position;
    private Vector3 front;
    private Vector3 up;
    private Vector3 right;
    private float pitch;
    private float yaw;

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(position, position + front, up);
    }

    public void RotateAround(Vector3 target, Vector2 rotation, float offset)
    {
        pitch = rotation.Y;
        yaw = rotation.X + MathHelper.DegreesToRadians(90);
        front = new Vector3(MathF.Cos(pitch) * MathF.Cos(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Sin(yaw)).Normalized();
        right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
        up = Vector3.Normalize(Vector3.Cross(right, front));
        position = target + front * offset;
    }
}