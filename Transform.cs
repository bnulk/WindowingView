// 引入了 .NET 的数值库 System.Numerics，其中包括：Vector3（3D 向量）Quaternion（四元数）Matrix4x4（4×4 矩阵）这些类型常用于图形学中表示位置、旋转、缩放和变换。
using System.Numerics;


namespace WindowingView
{
    /// <summary>
    /// 表示 3D 空间中的变换信息，包括位置、缩放和旋转。
    /// 通常用于物体、摄像机、光源等的空间表示。
    /// </summary>
    public class Transform
    {
        // ==================== 属性：位置 Position ====================
        /// <summary>
        /// 物体在世界空间中的三维位置  
        /// 类型：Vector3（X, Y, Z）  
        /// 默认值：new Vector3(0, 0, 0)  
        /// </summary>
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);

        // ==================== 属性：缩放 Scale ====================
        /// <summary>
        /// 物体的统一缩放比例  
        /// 类型：float（所有轴相同缩放）  
        /// 默认值：1.0（不缩放）  
        /// </summary>
        public float Scale { get; set; } = 1f;

        // ==================== 属性：旋转 Rotation ====================
        /// <summary>
        /// 使用四元数表示的旋转角度  
        /// 类型：Quaternion（避免欧拉角万向锁）  
        /// 默认值：Quaternion.Identity（无旋转）  
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        // ==================== 属性：变换矩阵 ViewMatrix ====================
        /// <summary>
        /// 组合变换矩阵（先旋转，再缩放，最后平移）  
        /// 用于将本地坐标转换到世界坐标  
        /// 等价于：M = R * S * T  
        /// </summary>
        public Matrix4x4 ViewMatrix =>
            Matrix4x4.Identity
            * Matrix4x4.CreateFromQuaternion(Rotation)   // 旋转矩阵（四元数转）
            * Matrix4x4.CreateScale(Scale)               // 缩放矩阵（统一缩放）
            * Matrix4x4.CreateTranslation(Position);     // 平移矩阵
    }
}