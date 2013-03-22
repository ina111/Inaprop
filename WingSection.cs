using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Inaprop;

namespace Inaprop
{
    public class WingSection : Airfoil
    {
        public WingSection(double position = 0.1)
        {
            this.Position = position;
        }
    }

    public class Airfoils
    {
        /// <summary>
        /// 粗く定義されるOldのListとPropと同じ間隔のNewのListを引数にしてPropを出力
        /// </summary>
        /// <param name="Old">粗く定義されている断面</param>
        /// <param name="New">n個で定義されている断面Propに代入</param>
        /// <returns>Propのコンストラクタをreturn</returns>
        public void Transration(ref Prop prop, List<WingSection> Old)
        {
            //中間出力用のList<WingSection>,Propの分割数だけ配列作成
            List<WingSection> New = new List<WingSection>();
            for (int i = 0; i < Prop.n; i++)
            {
                New.Add(new WingSection());
            }

            var ZeroLiftAlpha_deg_known = new Dictionary<double, double>();   //ゼロ揚力角[deg]
            var DCl_dAlpha_known = new Dictionary<double, double>();          //揚力傾斜
            var MaxCl_known = new Dictionary<double, double>();               //最大Cl
            var MinCl_known = new Dictionary<double, double>();               //最小Cl
            var MinCd_known = new Dictionary<double, double>();               //最小Cd
            var ClatMinCd_known = new Dictionary<double, double>();           //最小Cd時のCl
            var DCd_dCl2_known = new Dictionary<double, double>();            //二階微分
            var Reref_known = new Dictionary<double, double>();               //Reの参照部
            var Reexp_known = new Dictionary<double, double>();               //Reの指数部
            var Cm_known = new Dictionary<double, double>();                  //翼型Cm
            var Mcrit_known = new Dictionary<double, double>();

            var CL_known = new Dictionary<double, double>();                  //CLこれを補間したい。後はオマケ
            var Thickness_known = new Dictionary<double, double>();

            for (int i = 0; i < Old.Count; i++)
            {
                ZeroLiftAlpha_deg_known.Add(Old[i].Position, Old[i].ZeroLiftAlpha_deg);
                DCl_dAlpha_known.Add(Old[i].Position, Old[i].DCl_dAlpha);
                MaxCl_known.Add(Old[i].Position, Old[i].MaxCl);
                MinCl_known.Add(Old[i].Position, Old[i].MinCl);
                MinCd_known.Add(Old[i].Position, Old[i].MinCd);
                ClatMinCd_known.Add(Old[i].Position, Old[i].ClatMinCd);
                DCd_dCl2_known.Add(Old[i].Position, Old[i].DCd_dCl2);
                Reref_known.Add(Old[i].Position, Old[i].Reref);
                Reexp_known.Add(Old[i].Position, Old[i].Reexp);
                Mcrit_known.Add(Old[i].Position, Old[i].Mcrit);

                CL_known.Add(Old[i].Position, Old[i].Cl);
                Thickness_known.Add(Old[i].Position, Old[i].Thickness);
            }
            var ZeroLiftAlpha_deg_scaler = new SplineInterpolator(ZeroLiftAlpha_deg_known);
            var DCl_dAlpha_scaler = new SplineInterpolator(DCl_dAlpha_known);
            var MaxCl_scaler = new SplineInterpolator(MaxCl_known);
            var MinCl_scaler = new SplineInterpolator(MinCl_known);
            var MinCd_scaler = new SplineInterpolator(MinCd_known);
            var ClatMinCd_scaler = new SplineInterpolator(ClatMinCd_known);
            var DCd_dCl2_scaler = new SplineInterpolator(DCd_dCl2_known);
            var Reref_scaler = new SplineInterpolator(Reref_known);
            var Reexp_scaler = new SplineInterpolator(Reexp_known);
            var Mcrit_scaler = new SplineInterpolator(Mcrit_known);

            var CL_scaler = new SplineInterpolator(CL_known);
            var Thickness_scaler = new SplineInterpolator(Thickness_known);

            double start = MaxCl_known.First().Key;
            double end = MaxCl_known.Last().Key;
            double step = (end - start) / prop.n_number;
            int iter = 0;

            for (double x = start + 0.00000000001, i = 0; x <= end; x += step, i++)
            {
                iter = (int)i;
                New[iter].Position = (double)iter / (double)(prop.n_number - 1)
                    * (prop.Radius - prop.Radius_in) + prop.Radius_in;

                //New[iter].ZeroLiftAlpha_deg = ZeroLiftAlpha_deg_scaler.GetValue(x);
                //New[iter].DCl_dAlpha = DCl_dAlpha_scaler.GetValue(x);
                //New[iter].MaxCl = MaxCl_scaler.GetValue(x);
                //New[iter].MinCl = MinCl_scaler.GetValue(x);
                //New[iter].MinCd = MinCd_scaler.GetValue(x);
                //New[iter].ClatMinCd = ClatMinCd_scaler.GetValue(x);
                //New[iter].DCd_dCl2 = DCd_dCl2_scaler.GetValue(x);
                //New[iter].Reref = Reref_scaler.GetValue(x);
                //New[iter].Reexp = Reexp_scaler.GetValue(x);
                //New[iter].Mcrit = Mcrit_scaler.GetValue(x);

                New[iter].ZeroLiftAlpha_deg = ZeroLiftAlpha_deg_scaler.GetLinearValue(x);
                New[iter].DCl_dAlpha = DCl_dAlpha_scaler.GetLinearValue(x);
                New[iter].MaxCl = MaxCl_scaler.GetLinearValue(x);
                New[iter].MinCl = MinCl_scaler.GetLinearValue(x);
                New[iter].MinCd = MinCd_scaler.GetLinearValue(x);
                New[iter].ClatMinCd = ClatMinCd_scaler.GetLinearValue(x);
                New[iter].DCd_dCl2 = DCd_dCl2_scaler.GetLinearValue(x);
                New[iter].Reref = Reref_scaler.GetLinearValue(x);
                New[iter].Reexp = Reexp_scaler.GetLinearValue(x);
                New[iter].Mcrit = Mcrit_scaler.GetLinearValue(x);

                New[iter].Cl = CL_scaler.GetLinearValue(x);
                New[iter].Thickness = Thickness_scaler.GetLinearValue(x);

                prop.CLs[iter] = New[iter].Cl;
                prop.CDs[iter] = New[iter].Get_Cd();
                prop.AOAs[iter] = New[iter].Get_Alpha(); //順番が大事。CLの後にAlpha
                //レイノルズ数は未考慮
                prop.thickness[iter] = New[iter].Thickness;
            }
            //return prop;

        }

        /// <summary>
        /// WingSentionのスプライン補間メソッド。粗い間隔からProp用Prop.n個に補間。型合わず未使用
        /// </summary>
        /// <param name="x1">補間前x軸</param>
        /// <param name="y1">補間前y軸</param>
        /// <returns>補間後y軸</returns>
        private double[] Spline_Interpolate(double[] x1, double[] y1)
        {
            double[] y2 = new double[Prop.n];
            var y_known = new Dictionary<double, double>();
            for (int i = 0; i < x1.Count(); i++)
            {
                y_known.Add(x1[i], y1[i]);
            }
            var y_scaler = new SplineInterpolator(y_known);
            double start = y_known.First().Key;
            double end = y_known.Last().Key;
            double step = (end - start) / Prop.n;
            int iter = 0;
            for (double x = start + 0.00000001, i = 0; x <= end; x += step, i++)
            {
                iter = (int)i;
                y2[iter] = y_scaler.GetValue(x);
            }
            return y2;
        }
    }
}
