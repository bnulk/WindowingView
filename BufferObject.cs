using Silk.NET.OpenGL;


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// 该类封装了一个 GPU 缓冲区对象（VBO/EBO/UBO 等），负责：
/// 1. 生成缓冲区并把数据一次性上传到显存；
/// 2. 在需要时绑定到当前 OpenGL 上下文；
/// 3. 释放时删除缓冲区，避免显存泄漏。
/// 
/// 天津师范大学化学学院 刘鲲 2025-07-04
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace WindowingView
{
    public unsafe class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged            // 泛型约束：仅允许不可托管类型，以便固定指针
    {
        private readonly GL _gl;               // Silk.NET OpenGL 上下文句柄
        private readonly BufferTargetARB _bufferType; // 此缓冲区的目标类型（ArrayBuffer、ElementArrayBuffer 等）
        private readonly uint _handle;         // GPU 端缓冲区对象的句柄


        // ==================== 构造：生成缓冲区并上传数据 ====================
        /// <summary>
        /// 构造函数：创建缓冲区对象并把 data 写入显存  
        /// gl         —— Silk.NET.OpenGL.GL 实例，由调用者传入  
        /// data       —— 要上传到 GPU 的数据切片（Span）  
        /// bufferType —— 指定缓冲区用途：ArrayBuffer / ElementArrayBuffer / UniformBuffer…
        /// </summary>
        public BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            _gl = gl;
            _bufferType = bufferType;

            /////     1/3：创建     /////
            _handle = _gl.GenBuffer();      // 调用 glGenBuffers 创建缓冲区对象，返回句柄
            /////     2/3：绑定     /////
            Bind();
            /////     3/3：上传数据     /////
            // fixed 语句块：告诉 GC “这段内存我暂时不想你移动”，并获取其原始指针。
            // 在 unsafe 上下文中才能使用 pointer，以便直接把托管内存的数据地址交给 OpenGL。
            fixed (void* ptr = data)
            {
                // 调用 glBufferData 把数据一次性复制到显存
                _gl.BufferData(
                    bufferType,                                   // 绑定点：之前记下的 ArrayBuffer / ElementArrayBuffer …
                    (nuint)(data.Length * sizeof(TDataType)),     // 数据总字节数：元素个数 × 单个元素字节
                    ptr,                                          // 指向托管数组首元素的裸指针
                    BufferUsageARB.StaticDraw);                   // Usage Hint：告诉驱动这块数据不会再改动（静态绘制）
            }
        }


        // ==================== 绑定缓冲区 ====================
        /// <summary>
        /// 将本缓冲区绑定到当前 OpenGL 上下文的指定目标。
        /// 后续绘制命令才能正确读取或写入这块显存。
        /// </summary>
        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle); // 相当于 glBindBuffer(bufferType, handle);
        }


        // ==================== 释放资源 ====================
        /// <summary>
        /// 实现 IDisposable：在托管对象被释放或 using 块结束时，
        /// 主动通知 OpenGL 删除 GPU 缓冲区，释放显存。
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);      // glDeleteBuffers(1, &handle);
        }
    }
}
