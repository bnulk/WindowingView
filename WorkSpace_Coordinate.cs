using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Numerics;

namespace WindowingView
{
    internal class WorkSpace_Coordinate
    {
        IWindow _window;
        GL _gl;
        private int width = 1024;
        private int height = 640;
        private string title = "WorkSpace";

        // BufferObject<T> 是示例自定义的泛型包装类，用于简化 VBO/EBO 创建与释放
        private static BufferObject<float>? vbo;              // 顶点缓冲区对象
        private static BufferObject<uint>? ebo;              // 索引缓冲区对象
        private static VertexArrayObject<float, uint>? vao;   // 顶点数组对象（记录属性布局）
        public static Texture_StbImageSharp? texture; // 纹理封装
        private static Shader? shader;  // 着色器封装


        // 设置相机的位置，目标以及相机的右、上方向
        private static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 3.0f); // 相机位置
        private static Vector3 CameraTarget = Vector3.Zero; // 相机目标位置
        private static Vector3 CameraDirection = Vector3.Normalize(CameraPosition - CameraTarget); // 相机方向向量
        private static Vector3 CameraRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, CameraDirection)); // 计算相机的右向量
        private static Vector3 CameraUp = Vector3.Cross(CameraDirection, CameraRight); // 计算相机的上向量

        public WorkSpace_Coordinate()
        {
            // 创建窗口选项（使用默认配置）  
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(width, height);    // 设置窗口大小  
            options.Title = title;                              // 设置窗口标题  

            // 创建窗口对象  
            _window = Silk.NET.Windowing.Window.Create(options);

            // 分配窗口事件（生命周期事件处理函数）  
            _window.Load += OnLoad;                                             // 窗口加载时执行 OnLoad（初始化 GL、资源等）  
            _window.Update += OnUpdate;                                         // 每帧更新前执行 OnUpdate（逻辑更新，如输入）  
            _window.Render += OnRender;                                         // 每帧绘制时执行 OnRender（渲染调用）  
            _window.FramebufferResize += OnFramebufferResize;                   // 窗口大小变化时执行 OnFramebufferResize（更新视口等）  
            _window.Closing += OnClose;                                         // 当用户关闭窗口或程序退出时，触发 OnClose 方法：  

        }

        public void Run()
        {
            // 启动窗口主循环（开始运行）  
            _window.Run();                                                      // 这是一个“阻塞方法”，直到窗口关闭才返回  

            // 注意：Run() 是阻塞的，窗口关闭之前不会继续执行下面的代码  
            // 所以下面这句 Dispose() 会在窗口关闭后执行，用于释放资源  
            _window.Dispose();                         // 手动释放窗口资源  
        }

        public void OnLoad()
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                               交互操作                                                                  ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////   
            // window.CreateInput()：告诉 Silk.NET 框架我们要开始接收输入设备（键盘、鼠标、游戏手柄等）的输入事件。
            // 它返回一个 IInputContext 对象，代表所有输入设备的集合。
            IInputContext input = _window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                              交互操作结束                                                                ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////   



            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                              准备工作                                                                   ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                              
            // 获取 GL API
            _gl = GL.GetApi(_window);

            // ---------- 定义顶点数据和索引数据 ----------
            float[] vertices = GetVertice.GetVertices_Coordinate();
            uint[] indices = GetVertice.GetIndices_Texture();

            // ---------- 创建并填充 VBO/EBO ----------
            vbo = new BufferObject<float>(_gl, vertices, BufferTargetARB.ArrayBuffer);
            ebo = new BufferObject<uint>(_gl, indices, BufferTargetARB.ElementArrayBuffer);

            // ---------- 创建 VAO 并配置顶点属性 ---------- 
            vao = new VertexArrayObject<float, uint>(_gl, vbo, ebo);
            vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0); // 位置属性
            vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3); // 纹理坐标属性

            // -------------------- 着色器类中的四部曲：1、顶点着色器编译；2、片段着色器编译；3、着色器程序链接；4、清理着色器对象 --------------------
            shader = new Shader(_gl, Shader.VertexShaderSource_Coordinate, Shader.FragmentShaderSource_Coordinate);

            // -------------------- 纹理类中的四部曲：1、创建纹理；2、绑定纹理到gl；3、上传纹理图片；4、设置纹理参数 --------------------
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "data\\silk.png");
            texture = new Texture_StbImageSharp(_gl, fullPath);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                             准备工作结束                                                                 ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        }

        public void OnUpdate(double obj)
        {
        }

        public void OnRender(double obj)
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                                绘制                                                                     ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // ------------------- 清除屏幕 -------------------
            // 清除颜色缓冲区（即上一帧的颜色），准备绘制新的一帧
            // ClearBufferMask.ColorBufferBit 表示“颜色缓冲位”
            _gl.Enable(EnableCap.DepthTest);
            _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            shader?.Use();   // 激活着色器
            texture?.Bind(TextureUnit.Texture0);
            shader?.SetUniform("uTexture", 0);


            // 使用经过的时间（window.Time）来计算旋转角度，将其转换为弧度使得立方体能够随着时间旋转
            var difference = (float)(_window.Time * 100);  // 乘以100是为了加速旋转（可以调整旋转速度）

            // 获取当前窗口的帧缓冲区大小（宽度和高度）
            var size = _window.FramebufferSize;

            // 创建模型矩阵：首先绕Y轴旋转，再绕X轴旋转，合成一个旋转矩阵（旋转速度由difference控制）
            var model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) *
                        Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));

            // 创建视图矩阵：相机从CameraPosition朝向CameraTarget，并且上方向量是CameraUp
            var view = Matrix4x4.CreateLookAt(CameraPosition, CameraTarget, CameraUp);

            // 创建投影矩阵：透视投影，视野为45度，宽高比由窗口大小决定，近裁剪面为0.1，远裁剪面为100
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f),
                                                                    (float)size.X / size.Y,
                                                                    0.1f, 100.0f);

            // 将模型矩阵传递到着色器中的"uModel" uniform
            shader?.SetUniform("uModel", model);

            // 将视图矩阵传递到着色器中的"uView" uniform
            shader?.SetUniform("uView", view);

            // 将投影矩阵传递到着色器中的"uProjection" uniform
            shader?.SetUniform("uProjection", projection);

            // 绘制立方体，使用顶点数据而不使用索引数据。绘制时将使用36个顶点来构建六面体（立方体）的每个面
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);


            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                                绘制结束                                                                 ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        public void OnFramebufferResize(Vector2D<int> newSize)
        {
            // 当窗口大小（帧缓冲区大小）发生变化时调用此方法。
            // 在这里可以根据新的窗口尺寸更新相关内容，比如：
            // - 更新宽高比（aspect ratio）
            // - 重新设置视口（viewport）
            // - 调整投影矩阵
            // - 更新剪裁区域（clipping region）等图形参数



            // 设置 OpenGL 的视口（Viewport）大小，即告诉 OpenGL 应该在哪个区域绘制图形。
            // Gl.Viewport 会将 normalized device coordinates（范围从 -1 到 1）映射到屏幕像素坐标。
            // 这里我们使用新的窗口大小，确保图形能正确显示在整个窗口中。
            _gl.Viewport(newSize);
        }

        public void OnClose()
        {
            Console.WriteLine("Window Closing");

            vbo?.Dispose();
            ebo?.Dispose();
            vao?.Dispose();
            shader?.Dispose();
            texture?.Dispose();
        }

        public void Dispose()
        {
            _window.Dispose();
        }

        private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            // 当键盘按键按下时触发此方法。
            // 如果按下的是 ESC 键，就关闭窗口。
            if (arg2 == Key.Escape)
            {
                _window.Close();
            }
        }
    }
}
