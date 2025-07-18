using System;
using System.Numerics;

namespace WindowingView
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///                                                    摄像机类                                                              ///
    ///                                用于控制视角方向、位置、视野变换等核心功能                                                 ///
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Camera
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                             摄像机属性字段                                                             ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Vector3 Position { get; set; }   // 摄像机在世界空间中的位置
        public Vector3 Front { get; set; }      // 摄像机当前朝向的向量（通常为单位向量）

        public Vector3 Up { get; private set; } // 摄像机的“上”方向，通常为 (0,1,0)，用于构建视图矩阵

        public float Yaw { get; set; } = -90f;  // 摄像机的偏航角（绕 Y 轴），初始向 -Z 看
        public float Pitch { get; set; }        // 摄像机的俯仰角（绕 X 轴）

        public float AspectRatio { get; set; }  // 屏幕宽高比，用于计算投影矩阵

        private float Zoom = 45f;               // 摄像机的视野角度（FOV），用于投影矩阵计算

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                             构造函数                                                                  ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Camera(Vector3 position, Vector3 front, Vector3 up, float aspectRatio)
        {
            Position = position;       // 初始化位置
            AspectRatio = aspectRatio; // 初始化宽高比
            Front = front;             // 初始化前向量
            Up = up;                   // 初始化上方向
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                     修改摄像机视野（缩放）                                                            ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ModifyZoom(float zoomAmount)
        {
            // 调整缩放（FOV）：减去滚轮的 zoomAmount，然后限制在 1° 到 45° 之间，避免太广或太窄的视角
            Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 45f);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                     修改摄像机方向（鼠标移动）                                                        ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ModifyDirection(float xOffset, float yOffset)
        {
            Yaw += xOffset;     // 水平偏航（绕 Y 轴）
            Pitch -= yOffset;   // 垂直俯仰（绕 X 轴，Y 为上方向）

            // 限制 Pitch 的范围在 [-89°, 89°]，防止视角翻转
            Pitch = Math.Clamp(Pitch, -89f, 89f);

            // 根据俯仰角和偏航角重新计算朝向向量（前向量 Front）
            var cameraDirection = Vector3.Zero;

            cameraDirection.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            cameraDirection.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            cameraDirection.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));

            // 归一化方向向量，赋值给 Front，确保方向是单位向量
            Front = Vector3.Normalize(cameraDirection);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                     获取视图矩阵（LookAt）                                                             ///
        ///      通常用于构造 MVP 矩阵中的 View，用于观察世界场景。                                                              ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Matrix4x4 GetViewMatrix()
        {
            // 使用 LookAt 矩阵：从摄像机的位置向 Position + Front 看，并以 Up 为上方向
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                     获取投影矩阵（Perspective）                                                       ///
        ///      通常用于构造 MVP 矩阵中的 Projection，实现远近透视。                                                             ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Matrix4x4 GetProjectionMatrix()
        {
            // 使用透视投影矩阵（FOV, 宽高比, 近平面, 远平面）
            return Matrix4x4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(Zoom), // 将缩放角度转换为弧度
                AspectRatio,
                0.1f,   // 近平面
                100.0f  // 远平面
            );
        }
    }
}
