using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Inaprop
{
    /// <summary>
    /// 翼型のComboboxのためのデータソースクラス
    /// </summary>
    public class Airfoil
    {
        //C#自動プロパティを利用
        public string Name { get; set; }                //翼型名前
        public double ZeroLiftAlpha_deg { get; set; }   //ゼロ揚力角[deg]
        public double DCl_dAlpha { get; set; }          //揚力傾斜
        public double MaxCl { get; set; }               //最大Cl
        public double MinCl { get; set; }               //最小Cl
        public double MinCd { get; set; }               //最小Cd
        public double ClatMinCd { get; set; }           //最小Cd時のCl
        public double DCd_dCl2 { get; set; }            //二階微分
        public double Reref { get; set; }               //Reの参照部
        public double Reexp { get; set; }               //Reの指数部
        public double Cm { get; set; }                  //翼型Cm
        public double Mcrit { get; set; }               //マッハ数部

        public double Position { get; set; }            //Prop中の位置
        public double Cl { get; set; }                  //揚力係数Cl
        public double Cd { get; set; }                  //抗力係数Cd
        public double Re { get; set; }                  //レイノルズ数
        public double Alpha { get; set; }               //迎え角

        public List<float> origin_x = new List<float>();       //元の翼型のx座標
        public List<float> origin_y = new List<float>();       //元の翼型のy座標
        public List<float> set_x = new List<float>();       //取り付け角入りの翼型のx座標
        public List<float> set_y = new List<float>();       //取り付け角入りの翼型のx座標
        public int point_number;                            //翼型の点の数

        public double Thickness { get; set; }           //翼厚

        public Airfoil(double posi = 0.1)
        {
            Name = "DAE51";
            ZeroLiftAlpha_deg = 0.0;
            DCl_dAlpha = 6.28;
            MaxCl = 2.0;
            MinCl = -1.5;
            MinCd = 0.014;  //0.007
            ClatMinCd = 0.5;
            DCd_dCl2 = 0.004;   //0.004
            Reref = 100000;
            Reexp = -0.4;
            Cm = -0.1;
            Mcrit = 0.62;

            Cl = 0.6;
            Cd = 0.01;
            Re = 100000;

            Position = posi;

            Thickness = 0;

        }

        /// <summary>
        /// this.AlphaからCL取得
        /// </summary>
        /// <returns>揚力係数Cl</returns>
        public double Get_CL()  //(double Alpha)
        {
            double max_alpha = MaxCl / DCl_dAlpha + ZeroLiftAlpha_deg;
            double min_alpha = MinCl / DCl_dAlpha + ZeroLiftAlpha_deg;

            if (Alpha < min_alpha)
            {
                Cl = MaxCl;
            }
            else if (Alpha >= min_alpha && Alpha <= max_alpha)
            {
                Cl = DCl_dAlpha * (Alpha - ZeroLiftAlpha_deg);
            }
            else// (alpha > max_alpha)
            {
                Cl = MinCl;
            }
            return Cl;
        }
        /// <summary>
        /// this.ClからAlpha取得
        /// </summary>
        /// <returns>迎え角Alpha</returns>
        public double Get_Alpha()   //(double Cl)
        {
            //double Max_Cl;
            //double Min_Cl;
            if (Cl < MinCl)
            {
                Alpha = MinCl / DCl_dAlpha + ZeroLiftAlpha_deg;
            }
            else if (Cl >= MinCl && Cl <= MaxCl)
            {
                Alpha = Cl / DCl_dAlpha + ZeroLiftAlpha_deg;
            }
            else
            {
                Alpha = MaxCl / DCl_dAlpha + ZeroLiftAlpha_deg;
            }
            return Alpha;
        }

        /// <summary>
        /// this.Alpha, this.ReからCd取得
        /// </summary>
        /// <returns>抗力係数Cd</returns>
        public double Get_Cd()//(double Cl, double Re)
        {
            if (Cl < MinCl)
            {
                Cd = 10000000;
            }
            else if (Cl >= MinCl && Cl <= MaxCl)
            {
                Cd = (DCd_dCl2 * (Cl - ClatMinCd) * (Cl - ClatMinCd) + MinCd) * Math.Pow(Re / Reref, Reexp);
            }
            else //(CL > MaxCl)
            {
                Cd = 100000000;
            }
            return Cd;
        }

        /// <summary>
        /// origin_x, origin_yをdegreeだけ回転してset_x, set_yに代入
        /// </summary>
        /// <param name="degree">回転角度</param>
        public void Rotate(double degree)
        {
            set_x = new List<float>(origin_x);  //origin_xをset_xにコピー
            set_y = new List<float>(origin_y);  //origin_yをset_yにコピー
            float radian = (float)(-degree * Math.PI / 180.0);
            for (int i = 0; i < point_number; i++)
            {
                set_x[i] = (float)(origin_x[i] * Math.Cos(radian)
                    - origin_y[i] * Math.Sin(radian));
                set_y[i] = (float)(origin_x[i] * Math.Sin(radian)
                    + origin_y[i] * Math.Cos(radian));
            }
        }

        /// <summary>
        /// datファイルから翼型の座標を読み込み、origin_xに代入
        /// </summary>
        public void ReadDatFile()
        {
            origin_x.Clear(); origin_y.Clear(); 
            set_x.Clear(); set_y.Clear();
            OpenFileDialog ofd1 = new OpenFileDialog();
            ofd1.Filter = "翼型datファイル|*.dat";
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(ofd1.FileName, System.Text.Encoding.Default);
                Name = sr.ReadLine();
                string buffer = sr.ReadToEnd();
                string[] buffer_line = buffer.Split('\n');
                string[] buffer_x = new string[buffer_line.Length];
                string[] buffer_y = new string[buffer_line.Length];

                for (int i = 0; i < buffer_line.Length; i++)        //datファイルの行の数だけ反復
                {
                    string[] buffer_xy = buffer_line[i].Split(' ');  //スペースで分割
                    bool xy_flag = true;                            //x, yのどちらに代入するかのフラグ
                    for (int j = 0; j < buffer_xy.Length; j++)      //スペースも含めたxyの配列だけ反復
                    {
                        if (buffer_xy[j] != "" && xy_flag == false) //buffer_xyからスペースは排除
                        {
                            buffer_y[i] = buffer_xy[j];
                            origin_y.Add(float.Parse(buffer_y[i]));        //List<> yにyの値を追加
                        }
                        if (buffer_xy[j] != "" && xy_flag == true)
                        {
                            buffer_x[i] = buffer_xy[j];
                            origin_x.Add(float.Parse(buffer_x[i]));
                            xy_flag = false;                        //フラグ反転
                        }
                    }
                }
                point_number = origin_x.Count();
                sr.Close();
            }
        }

        /// <summary>
        /// 翼型を読み込んでいればその厚みを計算
        /// </summary>
        /// <param name="position">位置position,0~1</param>
        public void setThickness(double position)
        {
            double upper_y = 0;
            double lower_y = 0;
            bool upper_flag = false;
            bool lower_flag = false;
            for (int i = 0; i > origin_x.Count; i++)
            {

                if (position < origin_x[i] && upper_flag == false)
                {
                    upper_y = origin_y[i];
                    upper_flag = true;
                }
                if (position > origin_x[i] && upper_flag == true && lower_flag == false)
                {
                    lower_y = origin_y[i];
                    lower_flag = true;
                }
            }
            Thickness = upper_y - lower_y;
        }
    }
}
