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
    public class PrismModel:GeoModel
    {
        int n;
        Mesh[] surface;
        GeoModel geoModel;

        /// <summary>
        /// 空构造函数，生成单位正方体
        /// </summary>
        public PrismModel()
        {
            Point3Ds Bottom = new Point3Ds();
            Point3Ds Top = new Point3Ds();
            for (int i = 0; i < 4; i++)
			{
                Bottom.Add(new Point3D(i%2,i/2,0));
                Top.Add(new Point3D(i%2,i/2,1));
			}
            InitModel(Bottom, Top);
        }

        /// <summary>
        /// 构造一个棱柱
        /// </summary>
        /// <param name="Bottom">棱柱底面外围坐标序列</param>
        /// <param name="h">棱柱高</param>
        public PrismModel(Point3Ds Bottom,double h)
        {
            Point3Ds Top = new Point3Ds();
            for (int i = 0; i < Bottom.Count; i++)
            {
                Top.Add(new Point3D(Bottom[i].X, Bottom[i].Y, Bottom[i].Z + h));
            }
            InitModel(Bottom, Top);
        }

        /// <summary>
        /// 构造一个斜棱柱
        /// </summary>
        /// <param name="Bottom">底面</param>
        /// <param name="Top">顶面</param>
        public PrismModel(Point3Ds Bottom, Point3Ds Top)
        {
            InitModel(Bottom, Top);
        }

        protected void InitModel(Point3Ds Bottom, Point3Ds Top)
        {
            geoModel = new GeoModel();
            n = Math.Min(Bottom.Count, Top.Count);
            surface = new Mesh[n + 2];

            surface[n] = geoModel.CreateMesh(Bottom);
            surface[n + 1] = geoModel.CreateMesh(Top);

            for (int i = 0; i < n; i++)
            {
                Point3Ds p3ds = new Point3Ds();
                p3ds.Add(Bottom[i]);
                p3ds.Add(Bottom[(i + 1) % n]);
                p3ds.Add(Top[(i + 1) % n]);
                p3ds.Add(Top[i]);
                surface[i] = geoModel.CreateMesh(p3ds);
            }

            if (this.Meshes.Count > 0)
            {
                this.Meshes.Clear();
            }
            for (int i = 0; i < surface.Length; i++)
            {
                this.Meshes.Add(surface[i]);
            }
        }
    }
}
