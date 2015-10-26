using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SuperMap.Data;
using SuperMap.Realspace;
using SuperMap.UI;

namespace BlocksCraft.ModelIncubation
{
    public static class GeoModelEx
    {

        /// <summary>
        /// 给定坐标序列，构造凸多边形
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static Mesh CreateMesh(this GeoModel geoMoel, Point3Ds ps)
        {
            Mesh mesh = new Mesh();

            Double[] Vertices = new Double[ps.Count * 3];
            Int32[] Indexes = new Int32[3 * (ps.Count - 2)];
            Double[] Normals = new Double[ps.Count * 3];

            for (int i = 0; i < ps.Count; i++)
            {
                //读入坐标
                Vertices[3 * i] = ps[i].X;
                Vertices[3 * i + 1] = ps[i].Y;
                Vertices[3 * i + 2] = ps[i].Z;
            }

            for (int i = 0; i < ps.Count - 2; i++)
            {
                //生成索引
                Indexes[3 * i] = 0;
                Indexes[3 * i + 1] = i + 1;
                Indexes[3 * i + 2] = i + 2;

                //构造三角面的时候累计计算顶点法向量。顶点法向量为该点为顶点所有面的法向量之和
                //给定直角坐标系的单位向量i，j，k满足下列等式：
                //i×j=k ；
                //j×k=i ；
                //k×i=j ；
                //通过这些规则，两个向量的叉积的坐标可以方便地计算出来，不需要考虑任何角度：设
                //a= [a1, a2, a3] =a1i+ a2j+ a3k
                //b= [b1,b2,b3]=b1i+ b2j+ b3k ;
                //则
                //a × b= [a2b3-a3b2,a3b1-a1b3, a1b2-a2b1]
                double a1 = Vertices[3 * (Indexes[3 * i + 1])] - Vertices[3 * (Indexes[3 * i])],
                       a2 = Vertices[3 * (Indexes[3 * i + 1]) + 1] - Vertices[3 * (Indexes[3 * i]) + 1],
                       a3 = Vertices[3 * (Indexes[3 * i + 1]) + 2] - Vertices[3 * (Indexes[3 * i]) + 2],

                       b1 = Vertices[3 * (Indexes[3 * i + 2])] - Vertices[3 * (Indexes[3 * i])],
                       b2 = Vertices[3 * (Indexes[3 * i + 2]) + 1] - Vertices[3 * (Indexes[3 * i]) + 1],
                       b3 = Vertices[3 * (Indexes[3 * i + 2]) + 2] - Vertices[3 * (Indexes[3 * i]) + 2];

                double aXb1 = a2 * b3 - a3 * b2,
                       aXb2 = a3 * b1 - a1 * b3,
                       aXb3 = a1 * b2 - a2 * b1;

                //更新法向量
                //但是这算法未完善，由于现在model的各个侧面是独立的mesh，法向量累加实际上无效，依然是各个面遵循自己的方向
                for (int j = 0; j < 3; j++)
                {
                    Normals[3 * (Indexes[3 * i + 1])] += aXb1;
                    Normals[3 * (Indexes[3 * i + 1]) + 1] += aXb2;
                    Normals[3 * (Indexes[3 * i + 1]) + 2] += aXb3;
                }
            }

            mesh.Vertices = Vertices;
            mesh.Indexes = Indexes;
            mesh.Normals = Normals;
            return mesh;
        }

        public static void OffsetModel(this GeoModel geoModel, Point3D p)
        {
            foreach (Mesh m in geoModel.Meshes)
            {
                int length = m.Vertices.Length;
                double[] Vertices = m.Vertices;
                for (int i = 0; i < length-2; i+=3)
                {
                    Vertices[i] += p.X;
                    Vertices[i+1] += p.Y;
                    Vertices[i+2] += p.Z;
                }
                m.Vertices = Vertices;
            }
        }
    }
}
