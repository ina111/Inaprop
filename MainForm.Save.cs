using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Inaprop;

namespace Inaprop
{
    public partial class MainForm
    {
        /// <summary>
        /// PropのCSV出力
        /// # header
        /// Inaprop version
        /// 各種パラメータ
        /// # recorder
        /// 分布にそった値
        /// </summary>
        /// <param name="prop"></param>
        private void exportCSVprop(Prop prop, int partition, double start = 0)
        {
            if (partition < 2 || start < 0 || start > prop.Radius)
            {
                MessageBox.Show("Invalid value", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // File Open
            string stCurrentDir = System.IO.Directory.GetCurrentDirectory();
            string exportPath = "/";
            string exportName = prop.Name;
            System.Text.Encoding enc = System.Text.Encoding.GetEncoding("Shift_JIS");

            // 開く2つ目の引数は上書きか追加か
            System.IO.StreamWriter sr = new System.IO.StreamWriter(stCurrentDir + exportPath + exportName + ".csv", true, enc);

            // Header
            sr.WriteLine("Inaprop ver." + this.ProductVersion);
            sr.WriteLine("----");

            sr.WriteLine("No. blades" + "," + prop.Blade);
            sr.WriteLine("radius(m)" + "," + prop.Radius);
            sr.WriteLine("thrust(N)" + "," + prop.Thrust);
            sr.WriteLine("Power(W)" + "," + prop.Power);
            sr.WriteLine("Efficiency(%)" + "," + prop.Efficiency);
            sr.WriteLine("spped(m/s)" + "," + prop.Uinf);
            sr.WriteLine("rpm" + "," + prop.Rpm);
            sr.WriteLine("hub rad.(m)" + "," + prop.Radius_in);
            sr.WriteLine("rho(kg/m3)" + "," + prop.Rho);
            sr.WriteLine("nu(kg/m-s)" + "," + prop.Nu);
            sr.WriteLine("design METHOD" + "," + prop.Method);

            sr.WriteLine("====");

            // 出力用変数定義
            double[] radius = new double[partition];
            double[] chord = new double[partition];
            double[] phi_deg = new double[partition];
            double[] gamma = new double[partition];
            double[] reinolds = new double[partition];
            double[] Cl = new double[partition];
            double[] Cd = new double[partition];
            double[] thickness = new double[partition];

            //partitionが分割数、divisionが何個の分割の山があるか
            int division = partition - 1;


            //prop.Rがn+1の配列を持っているので他のと合わせる必要がある
            double[] radius_prop = new double[Prop.n];
            for (int i = 0; i < Prop.n; i++)
            {
                radius_prop[i] = prop.R[i];
            }
            //補間
            if (partition == Prop.n)
            {
                radius = radius_prop;    //参照コピー、値をコピーならprop.R.CopyTo(radius);
                chord = prop.ci;
                phi_deg = prop.phi;
                gamma = prop.gamma;
                reinolds = prop.Re;
                Cl = prop.CLs;
                Cd = prop.CDs;
                thickness = prop.thickness;
            }
            else
            {
                radius = Spline_Interpolate(radius_prop, radius_prop, partition, start);
                //radius = Spline_Interpolate(prop.R, prop.R, partition, start);
                chord = Spline_Interpolate(radius_prop, prop.ci, partition, start);
                phi_deg = Spline_Interpolate(radius_prop, prop.phi, partition, start);
                gamma = Spline_Interpolate(radius_prop, prop.gamma, partition, start);
                reinolds = Spline_Interpolate(radius_prop, prop.Re, partition, start);
                Cl = Spline_Interpolate(radius_prop, prop.CLs, partition, start);
                Cd = Spline_Interpolate(radius_prop, prop.CDs, partition, start);
                thickness = Spline_Interpolate(radius_prop, prop.thickness, partition, start);
            }
            
            // レコード
            string[] parameterName = { "Position(m)", "chord(m)", "phi(deg)", "gamma", "Re", "CL", "CD", "Thickness"};
            string s = string.Join(",", parameterName);
            sr.WriteLine(s);

            for (int i = 0; i < partition; i++)
            {
                string[] s1 = {radius[i].ToString("F3"), chord[i].ToString("F3"), phi_deg[i].ToString("F2"),
                                  gamma[i].ToString("F3"), reinolds[i].ToString("F1"),
                                  Cl[i].ToString("F2"), Cd[i].ToString("F2"),
                                  (thickness[i] * chord[i]).ToString("F3")};
                string s2 = string.Join(",", s1);
                sr.WriteLine(s2);
            }


            //フィイルクローズ
            sr.Close();
        }

        // DataTabke Header and Body
        DataTable dth = new DataTable();
        DataTable dtb = new DataTable();

        //DataTableにすべき
        //DataTableをそのまま保存すべき
        //翼厚を追加


        /// <summary>
        /// 補間のためのメソッド。開始点と終了点は補間前後で不変。
        /// </summary>
        /// <param name="x1">補間したい既知のx軸</param>
        /// <param name="y1">補間したい既知のy軸</param>
        /// <param name="count2">補間した後の個数</param>
        /// <param name ="init_value">補間した後のx軸のstart位置</param>
        /// <returns>補間された後のy軸</returns>
        private double[] Spline_Interpolate(double[] x1, double[] y1, int count2, double init_value)
        {
            double[] y2 = new double[count2];
            var y_known = new Dictionary<double, double>();
            for (int i = 0; i < y1.Count(); i++)
            {
                y_known.Add(x1[i], y1[i]);
            }
            var y_scaler = new SplineInterpolator(y_known);
            //double start = y_known.First().Key;
            double start = init_value;
            double end = y_known.Last().Key;
            double step = (end - start) / count2;
            int iter = 0;
            for (double x = start + 0.00001, i = 0; x <= end; x += step, i++)
            {
                iter = (int)i;
                y2[iter] = y_scaler.GetValue(x);
            }
            return y2;
        }
    }
}
