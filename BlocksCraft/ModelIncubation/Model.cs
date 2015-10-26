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
            }

            //设置顶点的法向量方向，设置法向量可以突出Mesh的阴影效果，更加逼真。
            for (int i = 0; i < mesh.Vertices.Length; i += 3)
            {
                Normals[i] = 0;
                Normals[i + 1] = 1;
                Normals[i + 2] = 0;
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
