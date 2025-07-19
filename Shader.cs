using System;
using System.IO;
using Silk.NET.OpenGL;
using System.Numerics;

namespace WindowingView
{
    public partial class Shader : IDisposable
    {
        // 私有字段：_handle 是 Shader Program 的 ID，_gl 是当前 OpenGL 上下文对象
        // 这两个变量都是私有的，因为我们只在内部使用它们。
        private uint _handle;
        private GL _gl;

        // 构造函数：接受 GL 上下文和顶点 / 片段着色器文件路径
        public Shader(GL gl, string VertexShaderSource, string FragmentShaderSource)
        {
            _gl = gl;

            // -------------------- 顶点着色器编译 --------------------

            // 创建一个顶点着色器对象
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, VertexShaderSource); // 设置着色器源码
            _gl.CompileShader(vertexShader);                    // 编译着色器

            // 获取并检查编译日志（如果有错误则输出）
            string infoLog = _gl.GetShaderInfoLog(vertexShader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling vertex shader {infoLog}");
            }
            
            // -------------------- 片段着色器编译 --------------------

            // 创建一个片段着色器对象
            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, FragmentShaderSource); // 设置着色器源码
            _gl.CompileShader(fragmentShader);                      // 编译着色器

            // 获取并检查编译日志（如果有错误则输出）
            infoLog = _gl.GetShaderInfoLog(fragmentShader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling fragment shader {infoLog}");
            }

            // -------------------- 着色器程序链接 --------------------

            // 创建一个着色器程序并将两个着色器附加到程序上
            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);   // 附加顶点着色器
            _gl.AttachShader(_handle, fragmentShader); // 附加片段着色器
            _gl.LinkProgram(_handle);                  // 链接程序，组合为完整的 GPU 渲染管线

            // 检查程序链接是否成功
            _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {_gl.GetProgramInfoLog(_handle)}");
            }

            // -------------------- 清理着色器对象 --------------------

            // 着色器链接完成后，不再需要单独的着色器对象，卸载和删除它们
            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        // 调用该方法来在渲染时启用 shader program
        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        // 设置 uniform 变量（整数类型）
        public void SetUniform(string name, int value)
        {
            // 获取 uniform 变量的位置
            int location = _gl.GetUniformLocation(_handle, name);

            // 如果找不到 uniform 变量，说明该变量在 shader 中未使用或拼写错误
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }

            // 设置该 uniform 的值
            _gl.Uniform1(location, value);
        }

        // 设置 uniform 变量（浮点数类型）
        public void SetUniform(string name, float value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }

            _gl.Uniform1(location, value);
        }

        // 设置 uniform 变量（vector3类型）
        public void SetUniform(string name, Vector3 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        // 设置 uniform 变量（Matrix4x4类型）
        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.UniformMatrix4(location, 1, false, (float*)&value);
        }

        // 实现 IDisposable 接口：释放 OpenGL 占用的资源
        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }

        // 加载和编译单个着色器的私有方法
        private uint LoadShader(ShaderType type, string path)
        {
            // 步骤说明：
            // 1. 从文件读取 shader 源代码
            // 2. 创建 shader 对象
            // 3. 将源码传给 GPU
            // 4. 编译 shader
            // 5. 检查编译是否成功

            // 1) 读取 shader 源代码（.vert 或 .frag）
            string src = File.ReadAllText(path);

            // 2) 创建 shader 对象，返回 OpenGL 分配的句柄 ID
            uint handle = _gl.CreateShader(type);

            // 3) 上传 shader 源代码给 GPU
            _gl.ShaderSource(handle, src);

            // 4) 编译 shader
            _gl.CompileShader(handle);

            // 5) 获取编译日志并检查是否出错
            string infoLog = _gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            // 返回该 shader 的 handle
            return handle;
        }
    }
}
