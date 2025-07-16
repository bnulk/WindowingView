using System;
using System.IO;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// 主要实现了以下功能：
/// 1、加载磁盘图像文件到 GPU；
/// 2、支持程序生成图像数据（如噪声、着色器输出）；
/// 3、设置必要的纹理参数；
/// 4、自动生成 mipmap；
/// 5、绑定到指定纹理单元；
/// 6、正确释放 OpenGL 资源避免内存泄漏。
///
/// 天津师范大学化学学院 刘鲲 2025-07-04
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace WindowingView
{
    public class Texture
    {
        private uint _handle;  // 纹理对象句柄
        private GL _gl;        // Silk.NET OpenGL 实例

        // ==================== 构造：从文件加载纹理 ====================
        /// <summary>
        /// 从图片文件创建 2D 纹理对象，并上传到显存
        /// gl   —— Silk.NET.OpenGL.GL 实例，由调用者传入  
        /// path —— 图片路径，支持任意常见格式（.png, .jpg 等）
        /// </summary>
        public unsafe Texture(GL gl, string path)
        {
            _gl = gl;

            /////     1/4：创建     /////
            _handle = _gl.GenTexture();     // glGenTextures，生成纹理对象句柄
            /////     2/4：绑定     /////
            Bind();                         // 绑定到当前纹理单元
                                            /////     3/4：上传数据     /////
                                            // 从文件读取图片，并解码为 RGBA 格式像素数据
            using (var img = Image.Load<Rgba32>(path))
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        fixed (void* data = accessor.GetRowSpan(y))
                        {
                            gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                    }
                });
            }

            /////     4/4：设置参数     /////
            SetParameters();   // 设置纹理参数
        }

        // ==================== 构造：从数据流创建纹理 ====================
        /// <summary>
        /// 从原始像素数据创建纹理（例如程序生成或 FBO 输出）
        /// gl     —— Silk.NET.OpenGL.GL 实例  
        /// data   —— RGBA 像素数据（Span<byte>，长度 = 宽 × 高 × 4）  
        /// width  —— 纹理宽度（像素）  
        /// height —— 纹理高度（像素）  
        /// </summary>
        public unsafe Texture(GL gl, Span<byte> data, uint width, uint height)
        {
            _gl = gl;

            /////     1/4：创建     /////
            _handle = _gl.GenTexture();     // 生成纹理对象
            /////     2/4：绑定     /////
            Bind();                         // 绑定到当前上下文
            /////     3/4：上传数据     /////
            fixed (void* ptr = &data[0])
            {
                _gl.TexImage2D(TextureTarget.Texture2D,         // 目标类型：2D 纹理
                               0,                                // mipmap level 0
                               (int)InternalFormat.Rgba,         // GPU 内部格式
                               width, height,                    // 宽高
                               0,                                // 边框宽度
                               PixelFormat.Rgba,                 // 输入像素格式
                               PixelType.UnsignedByte,           // 输入像素类型
                               ptr);                             // 像素数据指针
            }

            /////     4/4：设置参数     /////
            SetParameters();   // 设置行为参数并生成 mipmap
        }

        // ==================== 设置纹理参数 ====================
        /// <summary>
        /// 设置纹理行为参数（环绕方式、过滤方式、mipmap 等）
        /// 包括平滑插值、ClampToEdge、防止越界采样等
        /// </summary>
        private void SetParameters()
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);        // S 方向边缘采样方式
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);        // T 方向边缘采样方式

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear); // 缩小时使用 mipmap + 线性插值
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);             // 放大时线性插值

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);   // 设置 mipmap 最小级别为 0
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);    // 设置最大 mipmap 级别

            _gl.GenerateMipmap(TextureTarget.Texture2D);  // 自动生成多级纹理贴图
        }

        // ==================== 绑定纹理 ====================
        /// <summary>
        /// 绑定本纹理对象到指定的纹理单元（默认是 Texture0）
        /// 必须绑定后，shader 才能使用本纹理进行采样
        /// </summary>
        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(textureSlot);                        // 激活指定纹理槽
            _gl.BindTexture(TextureTarget.Texture2D, _handle);     // 将本对象绑定为当前 2D 纹理
        }

        // ==================== 释放资源 ====================
        /// <summary>
        /// 实现 IDisposable：主动释放 OpenGL 纹理资源，避免显存泄漏
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);  // 删除纹理对象，释放 GPU 资源
        }
    }
}
