using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowingView
{
    public partial class Shader : IDisposable
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               画四边形例子
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly string VertexShaderSource_Quad = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        
        void main()
        {
            gl_Position = vec4(vPos, 1.0);
        }
        ";
        //layout (location = 0) in vec3 vPos;                //在布局位置为 0 的地方接收一个 vec3 类型的输入变量 vPos

        public static readonly string FragmentShaderSource_Quad = @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        }
        ";

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               画四边形例子    结束
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               纹理
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly string VertexShaderSource_Texture = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec2 vUv;
        
        out vec2 fUv;

        void main()
        {
            gl_Position = vec4(vPos, 1.0);
            fUv = vUv;
        }
        ";

        public static readonly string FragmentShaderSource_Texture = @"
        #version 330 core

        in vec2 fUv;
        out vec4 FragColor;

        uniform sampler2D uTexture;

        void main()
        {
            vec2 flippedUv = vec2(fUv.x, 1.0 - fUv.y);
            FragColor = texture(uTexture, flippedUv);
        }
        ";

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               纹理     结束
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               变换
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly string VertexShaderSource_Transformations = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec2 vUv;
        
        uniform mat4 uModel;

        out vec2 fUv;

        void main()
        {
            gl_Position =  uModel * vec4(vPos, 1.0);
            fUv = vUv;
        }
        ";

        public static readonly string FragmentShaderSource_Transformations = @"
        #version 330 core
        in vec2 fUv;
        out vec4 FragColor;
        uniform sampler2D uTexture;

        void main()
        {
            vec2 flippedUv = vec2(fUv.x, 1.0 - fUv.y);
            FragColor = texture(uTexture, flippedUv);
        }
        ";

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               变换     结束
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               坐标
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly string VertexShaderSource_Coordinate = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec2 vUv;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec2 fUv;

        void main()
        {
            gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
            fUv = vUv;
        }
        ";

        public static readonly string FragmentShaderSource_Coordinate = @"
        #version 330 core
        in vec2 fUv;
        out vec4 FragColor;
        uniform sampler2D uTexture;

        void main()
        {
            vec2 flippedUv = vec2(fUv.x, 1.0 - fUv.y);
            FragColor = texture(uTexture, flippedUv);
        }
        ";

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               坐标     结束
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               环境光
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly string VertexShaderSource_AmbientLighting = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        void main()
        {
            gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
        }
        ";

        public static readonly string FragmentShaderSource_AmbientLighting = @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f);
        }
        ";

        public static readonly string LightingShaderSource_AmbientLighting = @"
        #version 330 core
        out vec4 FragColor;

        uniform vec3 objectColor;
        uniform vec3 lightColor;

        void main()
        {
            float ambientStrength = 0.3;
            vec3 ambient = ambientStrength * lightColor;
            vec3 result = ambient * objectColor;

            FragColor = vec4(result, 1.0);
}
        ";

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                               环境光     结束
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






















    }
}
