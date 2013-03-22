using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Inaprop;

namespace Inaprop
{
    /// <summary>
    /// SelectedPropの値を読み込んで、各種値をSelectedPropのものに変更
    /// 差分の値を読み込んでAnalysisPropを
    /// </summary>
    public partial class AnalysisForm : Form
    {
        private Prop SelectedProp = new Prop();
        private Prop AnalysisProp = new Prop();

        public AnalysisForm(Prop prop)
        {
            SelectedProp = prop;
            InitializeComponent();
            InitializeLabel();
            InitializePlot();
        }

        private void InitializeLabel()
        {
            label7.Text = SelectedProp.Rpm.ToString();
            label8.Text = SelectedProp.Uinf.ToString("F2");

            numericUpDown4.Value = (decimal)SelectedProp.Rho;
            numericUpDown5.Value = (decimal)SelectedProp.Nu;
        }

        private void InitializePlot()
        {
            chart1.Series.Clear();
            chart2.Series.Clear();

            chart1.ChartAreas[0].AxisX.Title = "position [m]";
            chart2.ChartAreas[0].AxisX.Title = "position [m]";
            chart1.ChartAreas[0].AxisY.Title = "Cl";
            chart2.ChartAreas[0].AxisY.Title = "Gamma";

            //基準となる元の特性を一つ目として入れておく
            ShowStandardPlot();
        }


        /// <summary>
        /// Analysisボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //増減分を読み込み
            //AnalysisProp = SelectedProp.Clone();
            CloneProp(SelectedProp);
            //AnalysisProp = new Prop(SelectedProp);
            //AnalysisProp = (Prop)SelectedProp.DeepCopy();
            for (int i = 0; i < Prop.n; i++)
            {
                AnalysisProp.theta[i] = SelectedProp.theta[i] - (double)numericUpDown1.Value;
            }
            AnalysisProp.Rpm = SelectedProp.Rpm + (double)numericUpDown2.Value;
            AnalysisProp.Uinf = SelectedProp.Uinf + (double)numericUpDown3.Value;
            AnalysisProp.Rho = (double)numericUpDown4.Value;
            AnalysisProp.Nu = (double)numericUpDown5.Value;
            AnalysisProp.PaformanceAnalysis();
            //Plot
            ShowPlot();
            label14.Text = AnalysisProp.Power.ToString("F1");
            label15.Text = AnalysisProp.Thrust.ToString("F1");
        }

        private void ShowStandardPlot()
        {
            Series plotSeries1 = new Series();
            Series plotSeries2 = new Series();
            plotSeries1.ChartType = SeriesChartType.Spline;
            plotSeries2.ChartType = SeriesChartType.Spline;
            for (int x = 0; x < Prop.n; x++)
            {
                plotSeries1.Points.AddXY(SelectedProp.R[x], SelectedProp.CLs[x]);
                plotSeries2.Points.AddXY(SelectedProp.R[x], SelectedProp.gamma[x]);
            }
            chart1.Series.Add(plotSeries1);
            chart2.Series.Add(plotSeries2);
            chart1.Series[0].Name = "Standard";
            chart2.Series[0].Name = "Standard";

            chart1.Update(); chart2.Update();

            double plotmargin = 1.05;  //グラフの余白分
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = SelectedProp.R.Max();
            chart1.ChartAreas[0].AxisY.Maximum = SelectedProp.CLs.Max() * plotmargin;
            chart2.ChartAreas[0].AxisX.Minimum = 0;
            chart2.ChartAreas[0].AxisX.Maximum = SelectedProp.R.Max();
            chart2.ChartAreas[0].AxisY.Maximum = SelectedProp.gamma.Max() * plotmargin;
        }

        private void ShowPlot()
        {
            chart1.Series.Clear();
            chart2.Series.Clear();
            ShowStandardPlot();

            Series plotSeries3 = new Series();
            Series plotSeries4 = new Series();
            plotSeries3.ChartType = SeriesChartType.Spline;
            plotSeries4.ChartType = SeriesChartType.Spline;
            for (int x = 0; x < Prop.n; x++)
            {
                plotSeries3.Points.AddXY(AnalysisProp.R[x], AnalysisProp.CLs[x]);
                plotSeries4.Points.AddXY(AnalysisProp.R[x], AnalysisProp.gamma[x]);
            }
            chart1.Series.Add(plotSeries3);
            chart2.Series.Add(plotSeries4);
            chart1.Series[1].Name = "Analysis";
            chart2.Series[1].Name = "Analysis";

            chart1.Update(); chart2.Update();
        }

        //SelectedpropからAnalysispropにコピー
        private void CloneProp(Prop prop)
        {
            AnalysisProp.Blade = prop.Blade;
            AnalysisProp.Power = prop.Power;
            AnalysisProp.Thrust = prop.Thrust;
            AnalysisProp.Efficiency = prop.Efficiency;
            AnalysisProp.Rpm = prop.Rpm;
            AnalysisProp.Uinf = prop.Uinf;
            AnalysisProp.Radius = prop.Radius;
            AnalysisProp.Radius_in = prop.Radius_in;
            AnalysisProp.Rho = prop.Rho;
            AnalysisProp.Nu = prop.Nu;
            AnalysisProp.Method = prop.Method;

            //AnalysisProp.ci = prop.ci;では参照コピー
            prop.ci.CopyTo(AnalysisProp.ci, 0);
            prop.theta.CopyTo(AnalysisProp.theta, 0);
            prop.phi.CopyTo(AnalysisProp.phi, 0);
            prop.gamma.CopyTo(AnalysisProp.gamma, 0);
            prop.Re.CopyTo(AnalysisProp.Re, 0);

            prop.CDs.CopyTo(AnalysisProp.CDs, 0);
            prop.CLs.CopyTo(AnalysisProp.CLs, 0);
            prop.AOAs.CopyTo(AnalysisProp.AOAs, 0);
            prop.thickness.CopyTo(AnalysisProp.thickness, 0);
        }
    }
}
