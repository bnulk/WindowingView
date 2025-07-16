using Silk.NET.OpenGL;  // 引入 Silk.NET 的 OpenGL API 封装


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// 该类用于封装 Vertex Array Object（顶点数组对象，简称 VAO）的行为。
/// 它将顶点数据（VBO）和索引数据（EBO）绑定到一个 VAO 上，实现顶点格式的统一配置。
/// 
/// 天津师范大学化学学院 刘鲲 2025-07-05
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


namespace WindowingView
{
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged    // 限定：只能用于非托管类型（原始数据类型，如 float, int）
        where TIndexType : unmanaged     // 同上，确保数据可以直接上传至 GPU
    {
        // 顶点数组对象的句柄，由 OpenGL 分配，用于后续绑定
        private uint _handle;

        // Silk.NET 提供的 GL 实例接口，用于调用各种 OpenGL 函数
        private GL _gl;

        /// <summary>
        /// 构造函数：创建 VAO，并将传入的 VBO 与之绑定
        /// </summary>
        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo)
        {
            _gl = gl; // 记录 GL 实例，后续用于操作 OpenGL API

            _handle = _gl.GenVertexArray(); // 生成一个新的 VAO 对象（OpenGL 分配句柄）
            Bind();                         // 绑定这个 VAO，使得后续的配置操作作用于它

            vbo.Bind();                     // 绑定传入的顶点缓冲对象（VBO）
        }

        /// <summary>
        /// 构造函数：创建 VAO，并将传入的 VBO 和 EBO 与之绑定
        /// </summary>
        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
        {
            _gl = gl; // 记录 GL 实例，后续用于操作 OpenGL API

            _handle = _gl.GenVertexArray(); // 生成一个新的 VAO 对象（OpenGL 分配句柄）
            Bind();                         // 绑定这个 VAO，使得后续的配置操作作用于它

            vbo.Bind();                     // 绑定传入的顶点缓冲对象（VBO）
            ebo.Bind();                     // 绑定传入的索引缓冲对象（EBO）
        }

        /// <summary>
        /// 设置顶点属性指针：告诉 GPU 如何解释每个顶点结构中的某个字段
        /// </summary>
        /// <param name="index">index：属性位置（layout(location = x)）</param>
        /// <param name="count">每个属性由多少个分量构成（如 vec3 是 3）</param>
        /// <param name="type">数据类型（如 Float、Int）</param>
        /// <param name="vertexSize">每个点完整的长度</param>
        /// <param name="offSet">本属性在点中的偏移量</param>
        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            // index：属性位置（layout(location = x)）
            // count：每个属性由多少个分量构成（如 vec3 是 3）
            // type：数据类型（如 Float、Int）
            // normalized：是否归一化；此处 false，表示保留原始值
            // stride：每个顶点总大小（以字节计）
            // pointer：偏移（以字节计）= offSet * sizeof(TVertexType)

            _gl.VertexAttribPointer(
                index,
                count,
                type,
                normalized: false,
                vertexSize * (uint)sizeof(TVertexType),
                pointer: (void*)(offSet * sizeof(TVertexType))
            );

            // 启用该顶点属性
            _gl.EnableVertexAttribArray(index);
        }

        /// <summary>
        /// 显式绑定当前 VAO，使得后续对 VBO/EBO 的设置应用到此对象
        /// </summary>
        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        /// <summary>
        /// 清理资源：删除 GPU 中的 VAO 对象
        /// 注意：**不会删除 VBO/EBO**，因为它们可能被多个 VAO 共享
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}
