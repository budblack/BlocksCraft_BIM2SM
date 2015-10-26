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

    public class CuboidModel : PrismModel
    {
        
        /// <summary>
        /// 正方体
        /// </summary>
        public CuboidModel(double a=1,double b=1 ,double c=1)
        {
            Point3Ds Bottom = new Point3Ds();
            Point3Ds Top = new Point3Ds();
            for (int i = 0; i < 4; i++)
			{
                Bottom.Add(new Point3D(((i + 1)/ 2) % 2 * a, i / 2 * b, 0));
                Top.Add(new Point3D(((i + 1) / 2) % 2 * a, i / 2 * b, c));
			}
            InitModel(Bottom, Top);
        }
    }
}
