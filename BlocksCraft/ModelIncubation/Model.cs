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
            //顶点去重
            #region
            List<int> wellBeRm = new List<int>();
            for (int i = ps.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (i == 0) break;
                    if (ps[i].X == ps[j].X && ps[i].Y == ps[j].Y && ps[i].Z == ps[j].Z)
                        if (!wellBeRm.Contains(i)) wellBeRm.Add(i);
                }
            }
            foreach (int i in wellBeRm)
            {
                ps.Remove(i);
            }
            #endregion

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
        /// <param name="Tolerance">容差，小数点后位数。设置后所有点坐标的小数点后只保留该长度</param>
        public static void MergeMeshs(this GeoModel geoModel, int Tolerance=2)
        {
            if (geoModel.Meshes.Count > 0)
            {
                Mesh mesh = new Mesh();

                Dictionary<int, Vertice> vPDic;
                List<Index> vPIndex;

                geoModel.structureData(Tolerance,out vPDic,out vPIndex);
                //归一化法线
                foreach (KeyValuePair<int, Vertice> vP in vPDic)
                {
                    double distence = Math.Sqrt(Math.Pow(vP.Value.Normal.X, 2) + Math.Pow(vP.Value.Normal.Y, 2) + Math.Pow(vP.Value.Normal.Z, 2));
                    if (distence>0)
                    {
                        vP.Value.Normal.X /= distence;
                        vP.Value.Normal.Y /= distence;
                        vP.Value.Normal.Z /= distence;
                    }
                }
                geoModel.MakeMesh(vPDic, vPIndex);
            }
        }
       public  class Vertice
        {
           public int HashCode { get { return GetHashCode(); } }

            public double X, Y, Z;
            public Normal Normal = new Normal();

            public Vertice(double X, double Y,double Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
            /// <summary>
            /// 重写GetHashCode，精确到厘米
            /// </summary>
            /// <returns>相同坐标点具有相同的hashcode</returns>
            public override int GetHashCode()
            {
                string s = Convert.ToString(X) + Convert.ToString(Y) + Convert.ToString(Z);
                return s.GetHashCode();
            }
        };
       public class Index
        {
            public Int32 P1, P2, P3;
        }
       public class Normal
        {
            public double X=0, Y=0, Z=0;
        }

        /// <summary>
        /// 根据结构化的数据制作mesh,三角网构造算法还没写好。此函数构造的面连线不正确
        /// </summary>
        /// <param name="geoModel"></param>
        /// <param name="vPDic">顶点集合</param>
        /// <param name="vPIndex">索引列表</param>
        public static void MakeMesh(this GeoModel geoModel, Dictionary<int, Vertice> vPDic, List<Index> vPIndex)
       {
           #region 输出结构化数据到顺序数组
           //double[] Vertices = new double[vPDic.Count*3];
           double[] Vertices = new double[vPIndex.Count * 3 * 3];//由于顶点被按照索引展开，这里需要额外的空间。
           Int32[] Indexes = new Int32[vPIndex.Count * 3];
           double[] Normals = new double[Vertices.Length];

           int j = 0;
           for (int i = 0; i < Indexes.Length; i++)
           {
               Indexes[i] = i;//顺序写入索引，之后的点序列按照这个索引展开
           }
           foreach (Index index in vPIndex)
           {
               //这里又产生了重复数据，我不知道要怎么再不产生重复的基础上写入数组
               #region 依赖索引list写入顶点数组，此过程多次索引的顶点被展开了
               Vertices[j] = vPDic[index.P1].X; Vertices[j + 1] = vPDic[index.P1].Y; Vertices[j + 2] = vPDic[index.P1].Z;
               Vertices[j + 3] = vPDic[index.P2].X; Vertices[j + 4] = vPDic[index.P2].Y; Vertices[j + 5] = vPDic[index.P2].Z;
               Vertices[j + 6] = vPDic[index.P3].X; Vertices[j + 7] = vPDic[index.P3].Y; Vertices[j + 8] = vPDic[index.P3].Z;
               #endregion

               #region 计算法向量
               geoModel.CalculateNormals(ref vPDic, vPIndex);
               #endregion

               #region 写入法向量
               Normals[j / 9] = vPDic[index.P1].Normal.X;
               Normals[(j / 9) + 1] = vPDic[index.P1].Normal.Y;
               Normals[(j / 9) + 2] = vPDic[index.P1].Normal.Z;

               Normals[(j / 9) + 3] = vPDic[index.P2].Normal.X;
               Normals[(j / 9) + 4] = vPDic[index.P2].Normal.Y;
               Normals[(j / 9) + 5] = vPDic[index.P2].Normal.Z;

               Normals[(j / 9) + 6] = vPDic[index.P3].Normal.X;
               Normals[(j / 9) + 7] = vPDic[index.P3].Normal.Y;
               Normals[(j / 9) + 8] = vPDic[index.P3].Normal.Z;

               #endregion

               j += 9;
           }
           #endregion
           Mesh mesh = new Mesh();
           mesh.Vertices = Vertices;
           mesh.Indexes = Indexes;
           mesh.Normals = Normals;

           //geoModel.Meshes.Clear();
           geoModel.Meshes.Add(mesh);
       }


        /// <summary>
        /// 每三点都连成一个三角片
        /// </summary>
        /// <param name="geoModel"></param>
        /// <param name="vPDic">顶点集合</param>
        /// <param name="vPIndex">索引列表</param>
        public static void MekeMeshAll(this GeoModel geoModel, Dictionary<int, Vertice> vPDic, ref List<Index> vPIndex)
        {
            double[] Vertices = new double[vPIndex.Count * 3 * 3];//由于顶点被按照索引展开，这里需要额外的空间。
            vPIndex = new List<Index>();
            foreach (KeyValuePair<int,Vertice> vP1 in vPDic)
            {
                foreach (KeyValuePair<int, Vertice> vP2 in vPDic)
                {
                    foreach (KeyValuePair<int, Vertice> vP3 in vPDic)
                    {
                        if (vP1.Value.HashCode == vP1.Value.HashCode && vP1.Value.HashCode == vP2.Value.HashCode && vP2.Value.HashCode == vP3.Value.HashCode)
                            continue;
                        Index index = new Index(){
                            P1=vP1.Key,
                            P2=vP1.Key,
                            P3=vP1.Key,
                        };
                        vPIndex.Add(index);
                    }
                    
                }
            }
            Mesh mesh = new Mesh();
            Int32[] Indexes = new Int32[vPIndex.Count * 3];
            double[] Normals = new double[Vertices.Length];

            

            mesh.Vertices = Vertices;
            mesh.Indexes = Indexes;
            mesh.Normals = Normals;
        }
        /// <summary>
        /// 单参数有返回值递归方法生成器。
        /// </summary>
        /// <typeparam name="T">单参数方法参数类型。</typeparam>
        /// <typeparam name="TResult">方法返回值类型。</typeparam>
        /// <param name="f">递归运算描述方法。</param>
        /// <returns>生成器生成递归方法。</returns>
        static Func<T, TResult> RFunc<T, TResult>(Func<Func<T, TResult>, T, TResult> f)
        {
            return x => f(RFunc(f), x);
        }
        /// <summary>
        /// 阶乘方法实现。
        /// </summary>
        static Func<int, int> factorial = RFunc<int, int>((f, n) => n == 1 ? 1 : n * f(n - 1));

        /// <summary>
        /// 提取结构化的数据
        /// </summary>
        /// <param name="geoModel"></param>
        /// <param name="Tolerance"></param>
        /// <param name="vPDic"></param>
        /// <param name="vPIndex"></param>
        public static void structureData(this GeoModel geoModel,int Tolerance, out Dictionary<int, Vertice> vPDic, out  List<Index> vPIndex)
        {
            vPDic = new Dictionary<int, Vertice>();
            vPIndex = new List<Index>();

            #region 提取数据并结构化
            foreach (Mesh m in geoModel.Meshes)
            {
                //插入一组mesh并索引三角面
                int inLen = m.Indexes.Length;
                for (int i = 0; i < inLen; i += 3)
                {
                    #region 结构化被索引的三个点对象，并依据hash插入dictionary
                    //相同坐标的点具有相同的hash
                    Normal nor = new Normal();
                    nor.X = m.Normals[3 * (m.Indexes[i])]; nor.Y = m.Normals[3 * (m.Indexes[i]) + 1]; nor.Z = m.Normals[3 * (m.Indexes[i]) + 2];
                    Vertice vP1 = new Vertice(Convert.ToInt32(m.Vertices[3 * (m.Indexes[i])] * Math.Pow(10, Tolerance)) / Convert.ToDouble((Math.Pow(10, Tolerance))),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i]) + 1] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i]) + 2] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)))
                    {
                        Normal = nor
                    };
                    nor.X = m.Normals[3 * (m.Indexes[i + 1])]; nor.Y = m.Normals[3 * (m.Indexes[i + 1]) + 1]; nor.Z = m.Normals[3 * (m.Indexes[i + 1]) + 2];
                    Vertice vP2 = new Vertice(Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 1])] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 1]) + 1] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 1]) + 2] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)))
                    {
                        Normal = nor
                    };
                    nor.X = m.Normals[3 * (m.Indexes[i + 2])]; nor.Y = m.Normals[3 * (m.Indexes[i + 2]) + 1]; nor.Z = m.Normals[3 * (m.Indexes[i + 2]) + 2];
                    Vertice vP3 = new Vertice(Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 2])] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 2]) + 1] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)),
                                                Convert.ToInt32(m.Vertices[3 * (m.Indexes[i + 2]) + 2] * Math.Pow(10, Tolerance)) / (Math.Pow(10, Tolerance)))
                    {
                        Normal = nor
                    };

                    //插入点并计算该点法向量，如果已存在点则累加法向量
                    if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                    vPDic[vP1.HashCode].Normal.X += vP1.Normal.X;
                    vPDic[vP1.HashCode].Normal.Y += vP1.Normal.Y;
                    vPDic[vP1.HashCode].Normal.Z += vP1.Normal.Z;

                    if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                    vPDic[vP2.HashCode].Normal.X += vP2.Normal.X;
                    vPDic[vP2.HashCode].Normal.Y += vP2.Normal.Y;
                    vPDic[vP2.HashCode].Normal.Z += vP2.Normal.Z;

                    if (!vPDic.ContainsKey(vP3.HashCode)) vPDic.Add(vP3.HashCode, vP3);
                    vPDic[vP3.HashCode].Normal.X += vP3.Normal.X;
                    vPDic[vP3.HashCode].Normal.Y += vP3.Normal.Y;
                    vPDic[vP3.HashCode].Normal.Z += vP3.Normal.Z;

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
        }

        /// <summary>
        /// 按平面切割模型
        /// </summary>
        /// <param name="geoModel"></param>
        /// <param name="surface">切割平面</param>
        public static void ClipModel(this GeoModel geoModel, Surface surface)
        {
            #region 提取顶点
            Dictionary<int, Vertice> vPDic;
            List<Index> vPIndex;

            geoModel.structureData(2, out vPDic, out vPIndex);
            #endregion

            List<Index> wellBeRm = new List<Index>();
            List<Index> wellBeAdd = new List<Index>();
            foreach (Index index in vPIndex)
            {
                #region 找到至少具有一点高于切割平面的三角面
                if (!surface.isBelowMe(vPDic[index.P1]) || !surface.isBelowMe(vPDic[index.P2]) || !surface.isBelowMe(vPDic[index.P3]))
                {
                    wellBeRm.Add(index);
                    //第一步去掉都三个顶点都在切割面上方的三角面
                    if (!surface.isBelowMe(vPDic[index.P1]) && !surface.isBelowMe(vPDic[index.P2]) && !surface.isBelowMe(vPDic[index.P3]))
                    {
                        //vPDic.Remove(index.P1); vPDic.Remove(index.P2); vPDic.Remove(index.P3);
                        //只需要删掉索引！不要删掉点，这个点还可能被其它三角面索引到，而其它三角面可能与切割平面相交
                        //这里不能直接在循环体内删除。应该记录列表，待循环完毕后再统一删，否则会打乱集合。
                        //vPIndex.Remove(index);
                        //这里逻辑应该是都会删掉的，移到判断外去执行了
                        //wellBeRm.Add(index);
                    }
                    else
                    {
                        //剩下的就是和切割平面相交的三角面。这里开始计算三角形边与平面的交点
                        #region 计算交点的函数
                        //P1在下
                        if (surface.isBelowMe(vPDic[index.P1]))
                        {
                            #region
                            //P1,P2在下，P3在上
                            if (surface.isBelowMe(vPDic[index.P2]))
                            {
                                #region
                                Vertice vP1 = surface.Intersect(vPDic[index.P1], vPDic[index.P3]);//第一个交点
                                Vertice vP2 = surface.Intersect(vPDic[index.P2], vPDic[index.P3]);//第二个交点

                                //插入新生成的两个点，并添加这两点和剩下两点构成三角面的索引。
                                if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                                if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                                //连接P1和vP2,构成两个三角形
                                Index vIn1 = new Index()
                                {
                                    P1 = vP1.HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P1].HashCode
                                };
                                Index vIn2 = new Index()
                                {
                                    P1 = vPDic[index.P1].HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P2].HashCode
                                };
                                //同理这里不能直接编辑当前循环的list
                                //vPIndex.Add(vIn1);
                                //vPIndex.Add(vIn2);
                                wellBeAdd.Add(vIn1);
                                wellBeAdd.Add(vIn2);
                                #endregion
                            }
                            //P1,P3在下，P2在上
                            else if (surface.isBelowMe(vPDic[index.P3]))
                            {
                                #region
                                Vertice vP1 = surface.Intersect(vPDic[index.P1], vPDic[index.P2]);//第一个交点
                                Vertice vP2 = surface.Intersect(vPDic[index.P3], vPDic[index.P2]);//第二个交点

                                //插入新生成的两个点，并添加这两点和剩下两点构成三角面的索引。
                                if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                                if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                                //连接P1和vP2,构成两个三角形
                                Index vIn1 = new Index()
                                {
                                    P1 = vP1.HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P1].HashCode
                                };
                                Index vIn2 = new Index()
                                {
                                    P1 = vPDic[index.P1].HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P3].HashCode
                                };
                                //vPIndex.Add(vIn1);
                                //vPIndex.Add(vIn2);
                                wellBeAdd.Add(vIn1);
                                wellBeAdd.Add(vIn2);
                                #endregion
                            }
                            #endregion
                        }
                        else
                        //P1在上
                        {
                            #region
                            //P1,P2在上，P3在下
                            if (!surface.isBelowMe(vPDic[index.P2]))
                            {
                                #region
                                Vertice vP1 = surface.Intersect(vPDic[index.P3], vPDic[index.P2]);//第一个交点
                                Vertice vP2 = surface.Intersect(vPDic[index.P3], vPDic[index.P1]);//第二个交点

                                //插入新生成的两个点，并添加这两点和剩下一点构成三角面的索引
                                if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                                if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                                Index vIn = new Index()
                                {
                                    P1 = vP1.HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P3].HashCode
                                };
                                //vPIndex.Add(vIn);
                                wellBeAdd.Add(vIn);
                                #endregion
                            }
                            //P1,P3在上，P2在下
                            else if (!surface.isBelowMe(vPDic[index.P3]))
                            {
                                #region
                                Vertice vP1 = surface.Intersect(vPDic[index.P2], vPDic[index.P3]);//第一个交点
                                Vertice vP2 = surface.Intersect(vPDic[index.P2], vPDic[index.P1]);//第二个交点

                                //插入新生成的两个点，并添加这两点和剩下一点构成三角面的索引
                                if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                                if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                                Index vIn = new Index()
                                {
                                    P1 = vP1.HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P2].HashCode
                                };
                                //vPIndex.Add(vIn);
                                wellBeAdd.Add(vIn);
                                #endregion
                            }
                            //P1在上，P2,P3在下
                            else
                            {
                                #region
                                Vertice vP1 = surface.Intersect(vPDic[index.P2], vPDic[index.P1]);//第一个交点
                                Vertice vP2 = surface.Intersect(vPDic[index.P3], vPDic[index.P1]);//第二个交点

                                //插入新生成的两个点，并添加这两点和剩下两点构成三角面的索引。
                                if (!vPDic.ContainsKey(vP1.HashCode)) vPDic.Add(vP1.HashCode, vP1);
                                if (!vPDic.ContainsKey(vP2.HashCode)) vPDic.Add(vP2.HashCode, vP2);
                                //连接P1和vP2,构成两个三角形
                                Index vIn1 = new Index()
                                {
                                    P1 = vP1.HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P2].HashCode
                                };
                                Index vIn2 = new Index()
                                {
                                    P1 = vPDic[index.P2].HashCode,
                                    P2 = vP2.HashCode,
                                    P3 = vPDic[index.P3].HashCode
                                };
                                //vPIndex.Add(vIn1);
                                //vPIndex.Add(vIn2);
                                wellBeAdd.Add(vIn1);
                                wellBeAdd.Add(vIn2);
                                #endregion
                            }
                            #endregion
                        }

                        //surface.Intersect()
                        #endregion
                    }
                }
                #endregion
            }
            //统一删除刚刚记录的三角面
            foreach (Index index in wellBeRm) { 
                vPIndex.Remove(index);}
            //统一添加
            foreach (Index index in wellBeAdd)
            {
                Console.WriteLine("=添加点=");
                Console.WriteLine(string.Format("{0},{1},{2}", vPDic[index.P1].X, vPDic[index.P1].Y, vPDic[index.P1].Z));
                Console.WriteLine(string.Format("{0},{1},{2}", vPDic[index.P2].X, vPDic[index.P1].Y, vPDic[index.P2].Z));
                Console.WriteLine(string.Format("{0},{1},{2}", vPDic[index.P3].X, vPDic[index.P1].Y, vPDic[index.P3].Z));
                Console.WriteLine("========");

                vPIndex.Add(index);
            }

            Point3Ds ps = new Point3Ds();
            ps.ImportVPList(vPDic, wellBeAdd);
            Mesh m = geoModel.CreateMesh(ps);
            geoModel.Meshes.Clear();
            geoModel.Meshes.Add(m);
            geoModel.MakeMesh(vPDic, vPIndex);

            geoModel.CalculateNormals(ref vPDic, vPIndex);
        }

        /// <summary>
        /// 计算法向量
        /// </summary>
        /// <param name="geoModel"></param>
        public static void CalculateNormals(this GeoModel geoModel, ref Dictionary<int, Vertice> vPDic, List<Index> vPIndex)
        {
            Normal normal1,normal2,normal3;
            foreach (Index index in vPIndex)
            {
                normal1 = vPDic[index.P1].Normal;
                normal2 = vPDic[index.P2].Normal;
                normal3 = vPDic[index.P3].Normal;

                double a1 = vPDic[index.P2].X - vPDic[index.P1].X,
                       a2 = vPDic[index.P2].Y - vPDic[index.P1].Y,
                       a3 = vPDic[index.P2].Z - vPDic[index.P1].Z,

                       b1 = vPDic[index.P3].X - vPDic[index.P1].X,
                       b2 = vPDic[index.P3].Y - vPDic[index.P1].Y,
                       b3 = vPDic[index.P3].Z - vPDic[index.P1].Z;

                double aXb1 = a2 * b3 - a3 * b2,
                       aXb2 = a3 * b1 - a1 * b3,
                       aXb3 = a1 * b2 - a2 * b1;
                #region 法线正向
                //法线投影到XY平面，(aXb1,aXb2)+(Px,Py)长度大于(Px,Py)，则为正
                if ((Math.Pow(vPDic[index.P1].X + aXb1, 2) + Math.Pow(vPDic[index.P1].Y + aXb2, 2)) < (Math.Pow(vPDic[index.P1].X, 2) + Math.Pow(vPDic[index.P1].Y, 2)))
                {
                    aXb1 *= -1;
                    aXb2 *= -1;
                    aXb3 *= -1;
                }

                #endregion

                normal1.X += aXb1; normal1.Y = aXb2; normal1.Z = aXb3;
                normal2.X += aXb1; normal2.Y = aXb2; normal2.Z = aXb3;
                normal3.X += aXb1; normal3.Y = aXb2; normal3.Z = aXb3;

                //归一化法线
                double d1 = Math.Sqrt(Math.Pow(normal1.X, 2) + Math.Pow(normal1.Y, 2) + Math.Pow(normal1.Z, 2));
                double d2 = Math.Sqrt(Math.Pow(normal2.X, 2) + Math.Pow(normal2.Y, 2) + Math.Pow(normal2.Z, 2));
                double d3 = Math.Sqrt(Math.Pow(normal3.X, 2) + Math.Pow(normal3.Y, 2) + Math.Pow(normal3.Z, 2));
                if (d1 > 0) { normal1.X /= d1; normal1.Y /= d1; normal1.Z /= d1; }
                if (d2 > 0) { normal2.X /= d2; normal2.Y /= d2; normal2.Z /= d2; }
                if (d3 > 0) { normal3.X /= d3; normal3.Y /= d3; normal3.Z /= d3; }

                vPDic[index.P1].Normal = normal1;
                vPDic[index.P2].Normal = normal2;
                vPDic[index.P3].Normal = normal3;
            }
        }


        /// <summary>
        /// 平面Ax+By+Cz+D=0
        /// 保留在平面正法线方向小的点
        /// </summary>
        public class Surface
        {
            public double A, B, C, D;

            public Surface(double A = 1, double B = 1, double C = 1, double D = -1)
            {
                this.A = A;
                this.B = B;
                this.C = C;
                this.D = D;
            }

            public bool isBelowMe(Vertice vP)
            {
                if (A * vP.X + B * vP.Y + C * vP.Z + D <= 0)
                    return true;
                else
                    return false;
            }

            public Vertice Intersect(Vertice p1, Vertice p2)
            {
                Vertice p = new Vertice(0, 0, 0);
                double vp1, vp2, vp3, n1, n2, n3, v1, v2, v3, m1, m2, m3, t, vpt;
                vp1 = this.A; vp2 = this.B; vp3 = this.C;
                n1 = 0; n2 = 0; n3 = -1 * D / C;
                v1 = p2.X - p1.X; v2 = p2.Y - p1.Y; v3 = p2.Z - p1.Z;
                m1 = p1.X; m2 = p1.Y; m3 = p1.Z;
                vpt = v1 * vp1 + v2 * vp2 + v3 * vp3;
                //首先判断直线是否与平面平行
                if (vpt == 0)
                {
                    p = null;
                }
                else
                {
                    t = ((n1 - m1) * vp1 + (n2 - m2) * vp2 + (n3 - m3) * vp3) / vpt;
                    p.X = m1 + v1 * t;
                    p.Y = m2 + v2 * t;
                    p.Z = m3 + v3 * t;

                    Console.WriteLine("==交点==");
                    Console.WriteLine(string.Format("{0},{1},{2}  ---  {3},{4},{5}", p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z));
                    Console.WriteLine(string.Format("{0},{1},{2}",p.X,p.Y,p.Z));
                    Console.WriteLine("========");
                }

                return p;
            }
        }

        /// <summary>
        /// 从vPDic和vPIndex导入顶点到Point3Ds
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="vPDic"></param>
        /// <param name="vPIndex"></param>
        public static void ImportVPList(this Point3Ds ps,Dictionary<int, Vertice> vPDic, List<Index> vPIndex)
        {
            foreach (Index index in vPIndex)
            {
                ps.Add(new Point3D(vPDic[index.P1].X, vPDic[index.P1].Y, vPDic[index.P1].Z));
                ps.Add(new Point3D(vPDic[index.P2].X, vPDic[index.P2].Y, vPDic[index.P2].Z));
                ps.Add(new Point3D(vPDic[index.P3].X, vPDic[index.P3].Y, vPDic[index.P3].Z));
            }
            //foreach (KeyValuePair<int, Vertice> vP in vPDic)
            //{
            //    ps.Add(new Point3D(vP.Value.X, vP.Value.Y, vP.Value.Z));
            //}
        }

        /// <summary>
        /// 导出Point3Ds数据到vPDic和vPIndex（未完成，需要一个三角网构成算法，在这儿调用。同时替代以前构成mesh时候用的临时算法)
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="vPDic"></param>
        /// <param name="vPIndex"></param>
        public static void ExportVPList(this Point3Ds ps, ref Dictionary<int, Vertice> vPDic, ref List<Index> vPIndex)
        {
            foreach (Point3D p3d in ps)
            {
                Vertice vp = new Vertice(p3d.X, p3d.Y, p3d.Z);
                if (!vPDic.ContainsKey(vp.HashCode))
                {
                    vPDic.Add(vp.HashCode, vp);
                }

            }

            #region 调用一个三角网构建算法

            #endregion
        }
    }
}
