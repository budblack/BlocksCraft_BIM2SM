﻿using System;
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


        /// <summary>
        /// 该方法可将geomodel中的mesh集合合并为一个mesh对象
        /// </summary>
        /// <param name="geoModel"></param>
        /// <param name="Tolerance">容差，小数点后位数。设置后所有点坐标的小数点后只保留该长度（这个功能还没想好，暂时无视掉←，←）</param>
        public static void MergeMeshs(this GeoModel geoModel, int Tolerance=0)
        {
            if (geoModel.Meshes.Count > 0)
            {
                Mesh mesh = new Mesh();

                Dictionary<int, Vertice> vPDic = new Dictionary<int, Vertice>();
                List<Index> vPIndex = new List<Index>();

                #region 提取数据并结构化
                foreach (Mesh m in geoModel.Meshes)
                {
                    //插入一组mesh并索引三角面
                    int inLen = m.Indexes.Length;
                    for (int i = 0; i < inLen; i += 3)
                    {
                        #region 结构化被索引的三个点对象，并依据hash插入dictionary
                        //相同坐标的点具有相同的hash
                        Vertice vP1 = new Vertice()
                        {
                            X = m.Vertices[m.Indexes[i]],
                            Y = m.Vertices[m.Indexes[i] + 1],
                            Z = m.Vertices[m.Indexes[i] + 2]
                        };
                        Vertice vP2 = new Vertice()
                        {
                            X = m.Vertices[m.Indexes[i + 1]],
                            Y = m.Vertices[m.Indexes[i + 1] + 1],
                            Z = m.Vertices[m.Indexes[i + 1] + 2]
                        };
                        Vertice vP3 = new Vertice()
                        {
                            X = m.Vertices[m.Indexes[i + 2]],
                            Y = m.Vertices[m.Indexes[i + 2] + 1],
                            Z = m.Vertices[m.Indexes[i + 2] + 2]
                        };

                        //插入点并计算该点法向量，如果已存在点则累加法向量
                        if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                        if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                        if (!vPDic.ContainsKey(vP3.HashCode)) vPDic.Add(vP3.HashCode, vP3);

                        #endregion

                        Index Index = new Index()
                        {
                            P1 = vP1.HashCode,
                            P2 = vP2.HashCode,
                            P3 = vP3.HashCode
                        };

                        vPIndex.Add(Index);
                    }
                }
                #endregion

                #region 计算顶点法向量


                #endregion

                double[] Vertices = new double[vPDic.Count];
                Int32[] Indexes = new Int32[vPIndex.Count];
                double[] Normals = new double[vPDic.Count];

                int j = 0;
                foreach (Index index in vPIndex)
                {
                    //这里又产生了重复数据，我不知道要怎么再不产生重复的基础上写入数组
                    #region 依赖索引list写入顶点数组，此过程多次索引的顶点被展开了
                    Vertices[j] = vPDic[index.P1].X; Vertices[j + 1] = vPDic[index.P1].Y; Vertices[j + 2] = vPDic[index.P1].Z;
                    Vertices[j + 3] = vPDic[index.P2].X; Vertices[j + 4] = vPDic[index.P2].Y; Vertices[j + 5] = vPDic[index.P2].Z;
                    Vertices[j + 6] = vPDic[index.P3].X; Vertices[j + 7] = vPDic[index.P3].Y; Vertices[j + 8] = vPDic[index.P3].Z;
                    #endregion
                    #region 紧接着写入索引
                    Indexes[j / 3] = j / 3;
                    Indexes[(j / 3) + 1] = j / 3;
                    Indexes[(j / 3) + 2] = j / 3;

                    j += 9;
                    #endregion
                }
                
                geoModel.Meshes.Clear();
                geoModel.Meshes.Add(mesh);
            }
        }
        class Vertice
        {
            public int HashCode;

            public double X, Y, Z;
            public Normal Normal;

            public Vertice()
            {
                this.HashCode = GetHashCode();
            }
            /// <summary>
            /// 重写GetHashCode，精确到厘米
            /// </summary>
            /// <returns>相同坐标点具有相同的hashcode</returns>
            public override int GetHashCode()
            {
                return (((int)(X * 100)) ^ ((int)(Y * 100)) ^ ((int)(Z * 100))).GetHashCode();
            }
        };
        class Index
        {
            public Int32 P1, P2, P3;
        }
        class Normal
        {
            public double X=0, Y=0, Z=0;
        }
    }
}
