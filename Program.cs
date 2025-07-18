namespace WindowingView
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //画四边形练习
            //WorkSpace_Quad quad = new WorkSpace_Quad();
            //quad.Run();

            //纹理练习
            //WorkSpace_Texture texture = new WorkSpace_Texture();
            //texture.Run();

            //变换练习
            //WorkSpace_Transformations transformations = new WorkSpace_Transformations();
            //transformations.Run();

            //画d轨道
            //WorkSpace_dOrbital dOrbital = new WorkSpace_dOrbital();
            //dOrbital.Run();

            //画三个点
            //WorkSpace_Points points = new WorkSpace_Points();
            //points.Run();

            //给世界建立坐标
            //WorkSpace_Coordinate coordinate = new WorkSpace_Coordinate();
            //coordinate.Run();

            //画d轨道，给世界建立坐标
            //WorkSpace_dOrbital_Coordinate dOrbitalCoordinate = new WorkSpace_dOrbital_Coordinate();
            //dOrbitalCoordinate.Run();

            //摄像机
            WorkSpace_Camera camera = new WorkSpace_Camera();
            camera.Run();
        }
    }
}
