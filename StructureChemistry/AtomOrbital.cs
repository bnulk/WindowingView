using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WindowingView.StructureChemistry
{
    public class AtomOrbital
    {
        // 氢原子 3d_z² 轨道的电子密度函数（非归一化）
        public static double Rho_3dz2(double r, double theta)
        {
            double R = r * r * Math.Exp(-r / 3.0);
            double Y = 3 * Math.Cos(theta) * Math.Cos(theta) - 1;
            double psi = R * Y;
            return psi * psi;
        }

        // 氢原子 3dₓᵧ 轨道的电子密度函数（非归一化）
        public static double Rho_3dxy(double r, double theta, double phi)
        {
            // Radial part (n=3, l=2)：略作简化，忽略归一化系数
            double R = r * r * Math.Exp(-r / 3.0);

            // 角部分：Y₂² = sin²θ * sin(2φ)
            double Y = Math.Sin(theta) * Math.Sin(theta) * Math.Sin(2 * phi);

            // ψ² = |R × Y|²
            double psi = R * Y;
            return psi * psi;
        }

        public static float[] GetVertice()
        {
            int totalSamples = 100_000_000;
            double rMax = 1.0;
            Random random = new Random();
            List<Vector3> acceptedPoints = new();

            for (int i = 0; i < totalSamples; i++)
            {
                // 随机采样球坐标
                double r = random.NextDouble() * rMax;
                double theta = random.NextDouble() * Math.PI;
                double phi = random.NextDouble() * 2 * Math.PI;

                double density = Rho_3dxy(r, theta,phi);

                // 随机接受：按密度函数过滤
                if (random.NextDouble() < density * 0.08)
                {
                    // 球坐标 → 笛卡尔坐标
                    float x = (float)(r * Math.Sin(theta) * Math.Cos(phi));
                    float y = (float)(r * Math.Sin(theta) * Math.Sin(phi));
                    float z = (float)(r * Math.Cos(theta));
                    acceptedPoints.Add(new Vector3(x, y, z));
                }
            }

            return acceptedPoints
                .Select(p => new float[] { p.X, p.Y, p.Z })
                .SelectMany(arr => arr)
                .ToArray();
        }
    }
}
