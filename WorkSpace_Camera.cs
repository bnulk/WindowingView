using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Numerics;

namespace WindowingView
{
    internal class WorkSpace_Camera
    {
        IWindow _window;
        GL _gl;
        IKeyboard? primaryKeyboard;  // 主键盘输入设备（通常只有一个）

        private int width = 1024;
        private int height = 640;
        private string title = "WorkSpace";

        // BufferObject<T> 是示例自定义的泛型包装类，用于简化 VBO/EBO 创建与释放
        private static BufferObject<float>? vbo;              // 顶点缓冲区对象
        private static BufferObject<uint>? ebo;              // 索引缓冲区对象
        private static VertexArrayObject<float, uint>? vao;   // 顶点数组对象（记录属性布局）
        public static Texture_StbImageSharp? texture; // 纹理封装
        private static Shader? shader;  // 着色器封装

        private static Camera camera;   //摄像机对象，用于处理视角和投影

        private static Vector2 LastMousePosition;   // 上一次鼠标位置，用于计算移动增量

        public WorkSpace_Camera()
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

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                OnLoad：资源与输入初始化                                                   ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnLoad()
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                             输入设备初始化（键盘和鼠标）                                              ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // 创建输入上下文（IInputContext），用于访问键盘、鼠标、游戏手柄等输入设备
            IInputContext input = _window.CreateInput();

            // 获取主键盘（第一个键盘设备），通常电脑只有一个
            primaryKeyboard = input.Keyboards.Count > 0 ? input.Keyboards[0] : null;

            // 如果成功获取键盘，就绑定键盘按下事件
            if (primaryKeyboard != null)
            {
                // 绑定按键按下事件处理函数（KeyDown）
                primaryKeyboard.KeyDown += KeyDown;
            }

            // 遍历所有鼠标设备（通常只有一个）
            for (int i = 0; i < input.Mice.Count; i++)
            {
                // 设置鼠标模式为“原始输入”（Raw），用于精确控制鼠标移动（如摄像机控制）
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;

                // 绑定鼠标移动事件处理函数（OnMouseMove）
                input.Mice[i].MouseMove += OnMouseMove;

                // 绑定鼠标滚轮事件处理函数（OnMouseWheel）
                input.Mice[i].Scroll += OnMouseWheel;
            }

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

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 创建一个摄像机实例并初始化其位置、朝向和纵横比：
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // 创建一个新的 Camera 实例，参数如下：
            // 1. Camera 的初始位置：`Vector3.UnitZ * 6` 表示摄像机的位置在 Z 轴上为 6（即 6 个单位的距离），
            //    `Vector3.UnitZ` 是 (0, 0, 1) 单位向量，因此 `Vector3.UnitZ * 6` 就是 (0, 0, 6)。
            //    这意味着摄像机的位置位于 Z 轴的 6 单位处。

            // 2. Camera 的朝向：`Vector3.UnitZ * -1` 表示摄像机的观察目标位于 Z 轴的 -1 位置（即坐标 (0, 0, -1)）。
            //    摄像机的朝向向量指向这个位置（即从 (0, 0, 6) 指向 (0, 0, -1)）。

            // 3. Camera 的 "上" 向量：`Vector3.UnitY`，即 (0, 1, 0)，表示摄像机的“上”方向沿着 Y 轴。
            //    这确保了摄像机在旋转时保持正确的姿势，不会颠倒。

            // 4. Camera 的纵横比：`Width / Height`，计算屏幕的宽高比（宽度除以高度），
            //    用于创建投影矩阵，使得视图在不同尺寸的窗口中适配正确。

            camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, width / height);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///                                             准备工作结束                                                                 ///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                             OnLoad：初始化结束（输入 + 渲染资源）                                           ///
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        private unsafe void OnUpdate(double deltaTime)
        {
            //////////////////////////////////////////////////////////////
            ///
            /// OnUpdate 函数根据键盘的输入（W、A、S、D 键）来控制摄像机的移动。
            /// moveSpeed 会根据每帧的 deltaTime 来调整，以保证移动速度独立于帧率，确保跨平台一致性。
            /// 通过向量运算（叉积）和方向向量（Camera.Front 和 Camera.Up），来实现不同方向（前进、后退、左移、右移）的摄像机控制。
            ///
            ////////////////////////////////////////////////////////////////
            
            // 根据 deltaTime 计算相机的移动速度
            var moveSpeed = 2.5f * (float)deltaTime;

            // 判断是否按下了 Up 键，如果按下，移动相机前进
            if (primaryKeyboard.IsKeyPressed(Key.Up))
            {
                // 移动相机向前，沿着相机前方向量（camera.Front）前进
                camera.Position += moveSpeed * camera.Front;
            }

            // 判断是否按下了 Down 键，如果按下，移动相机后退
            if (primaryKeyboard.IsKeyPressed(Key.Down))
            {
                // 移动相机向后，沿着相机前方向量（Camera.Front）反方向移动
                camera.Position -= moveSpeed * camera.Front;
            }

            // 判断是否按下了 Left 键，如果按下，移动相机向左
            if (primaryKeyboard.IsKeyPressed(Key.Left))
            {
                // 移动相机向左，计算出相机前方向量（Camera.Front）与相机上方向量（Camera.Up）的叉积，得到右方向向量，然后规范化后乘以移动速度
                camera.Position -= Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * moveSpeed;
            }

            // 判断是否按下了 Right 键，如果按下，移动相机向右
            if (primaryKeyboard.IsKeyPressed(Key.Right))
            {
                // 移动相机向右，计算出相机前方向量（Camera.Front）与相机上方向量（Camera.Up）的叉积，得到右方向向量，然后规范化后乘以移动速度
                camera.Position += Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * moveSpeed;
            }
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

            var model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) *
                        Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
            var view = Matrix4x4.CreateLookAt(camera.Position, camera.Position+camera.Front, camera.Up);
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

        /// <summary>
        /// 这是一个回调函数，它在窗口大小发生变化时被触发。
        /// newSize 是一个 Vector2D<int> 类型的参数，表示新窗口的宽度和高度（以像素为单位）。newSize.X 代表新窗口的宽度，newSize.Y 代表新窗口的高度。
        /// </summary>
        /// <param name="newSize"></param>
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
            _gl.Viewport(newSize); // 设置 OpenGL 的视口大小为新窗口尺寸

            // 这一行代码更新了相机的宽高比（aspect ratio）。宽高比是指窗口的宽度与高度的比值，即 width / height
            // 相机的宽高比是非常重要的，因为它影响了视锥体（Frustum）的计算，从而决定了在不同尺寸的窗口中，图像如何被正确显示。如果宽高比不正确，渲染的内容可能会被拉伸或压缩。
            // 这里的 Camera.AspectRatio 是一个存储宽高比的变量，它通常用于调整投影矩阵，以确保图形渲染的正确性。
            // 通过 (float)newSize.X / newSize.Y 计算得到新的宽高比，并赋值给 Camera.AspectRatio，这样相机就能根据新窗口的尺寸更新其投影设置。
            camera.AspectRatio = (float)newSize.X / newSize.Y; // 更新相机的宽高比

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

        private static unsafe void OnMouseMove(IMouse mouse, Vector2 position)
        {
            var lookSensitivity = 0.1f; // 设置鼠标移动的灵敏度，控制视角旋转的速度

            // 如果是第一次调用该函数，初始化 LastMousePosition 为当前鼠标位置
            if (LastMousePosition == default)
            {
                LastMousePosition = position;
            }
            else
            {
                // 计算鼠标的偏移量（相对于上一次鼠标位置）
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity; // 水平偏移量
                var yOffset = (-1)*(position.Y - LastMousePosition.Y) * lookSensitivity; // 垂直偏移量

                // 更新 LastMousePosition 为当前鼠标位置，准备下一次比较
                LastMousePosition = position;

                // 调用 Camera 类中的 ModifyDirection 方法来更新相机的方向（俯仰角和偏航角）
                camera.ModifyDirection(xOffset, yOffset);
            }
        }

        private static unsafe void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            // 当鼠标滚轮滚动时，修改相机的缩放级别
            camera.ModifyZoom(scrollWheel.Y); // scrollWheel.Y 为滚动的纵向偏移量（通常是正值表示放大，负值表示缩小）
        }

    }
}
