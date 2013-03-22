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
        private void InitializePlot()
        {
            chart1.Series.Clear();
            chart2.Series.Clear();
            chart3.Series.Clear();
            chart4.Series.Clear();
            chart5.Series.Clear();
            chart6.Series.Clear();

            chart1.ChartAreas[0].AxisX.Title = "position [m]";
            chart2.ChartAreas[0].AxisX.Title = "position [m]";
            chart3.ChartAreas[0].AxisX.Title = "position [m]";
            chart4.ChartAreas[0].AxisX.Title = "position [m]";
            chart5.ChartAreas[0].AxisX.Title = "position [m]";
            chart6.ChartAreas[0].AxisX.Title = "position [m]";
            chart1.ChartAreas[0].AxisY.Title = "chord [m]";
            chart2.ChartAreas[0].AxisY.Title = "phi [deg]";
            chart3.ChartAreas[0].AxisY.Title = "gamma";
            chart4.ChartAreas[0].AxisY.Title = "10^3 × Re";
            chart5.ChartAreas[0].AxisY.Title = "Cd";
            chart6.ChartAreas[0].AxisY.Title = "Cl";
        }

        private void ShowPlot2()
        {
            chart1.Series.Clear();
            chart2.Series.Clear();
            chart3.Series.Clear();
            chart4.Series.Clear();
            chart5.Series.Clear();
            chart6.Series.Clear();
            List<double> List_Radius = new List<double>();
            List_Radius.Add(0.0);
            List<double> List_ci = new List<double>();
            List_ci.Add(0.0);
            List<double> List_phi = new List<double>();
            List_phi.Add(0.0);
            List<double> List_gamma = new List<double>();
            List_gamma.Add(0.0);
            List<double> List_Re = new List<double>();
            List_Re.Add(0.0);
            List<double> List_Cl = new List<double>();
            List_Cl.Add(0.0);
            List<double> List_Cd = new List<double>();
            List_Cd.Add(0.0);

            List<string> legend = new List<string>();
            int index = 0;
            foreach (Prop prop in Props)
            {
                if (prop.Show == true)
                {
                    Series plotSeries1 = new Series();
                    Series plotSeries2 = new Series();
                    Series plotSeries3 = new Series();
                    Series plotSeries4 = new Series();
                    Series plotSeries5 = new Series();
                    Series plotSeries6 = new Series();
                    //plotSeries1.Add(new Series()); plotSeries2.Add(new Series());
                    //plotSeries3.Add(new Series()); plotSeries4.Add(new Series());
                    plotSeries1.ChartType = SeriesChartType.Spline;
                    plotSeries2.ChartType = SeriesChartType.Spline;
                    plotSeries3.ChartType = SeriesChartType.Spline;
                    plotSeries4.ChartType = SeriesChartType.Spline;
                    plotSeries5.ChartType = SeriesChartType.Spline;
                    plotSeries6.ChartType = SeriesChartType.Spline;
                    for (int x = 0; x < prop.n_number; x++)
                    {
                        plotSeries1.Points.AddXY(prop.R[x], prop.ci[x]);
                        plotSeries2.Points.AddXY(prop.R[x], prop.phi[x] * 180 / Math.PI);
                        plotSeries3.Points.AddXY(prop.R[x], prop.gamma[x]);
                        plotSeries4.Points.AddXY(prop.R[x], prop.Re[x] / 1000);   //1000で割ってグラフ見やすく
                        plotSeries5.Points.AddXY(prop.R[x], prop.CDs[x]);
                        plotSeries6.Points.AddXY(prop.R[x], prop.CLs[x]);
                        legend.Add(prop.Name);
                    }
                    chart1.Series.Add(plotSeries1);
                    chart2.Series.Add(plotSeries2);
                    chart3.Series.Add(plotSeries3);
                    chart4.Series.Add(plotSeries4);
                    chart5.Series.Add(plotSeries5);
                    chart6.Series.Add(plotSeries6);
                    chart1.Series[index].Name = prop.Name;
                    chart2.Series[index].Name = prop.Name;
                    chart3.Series[index].Name = prop.Name;
                    chart4.Series[index].Name = prop.Name;
                    chart5.Series[index].Name = prop.Name;
                    chart6.Series[index].Name = prop.Name;
                    index++;

                    List_Radius.Add(prop.Radius);
                    List_ci.Add(prop.ci.Max());
                    List_phi.Add(prop.phi.Max());
                    List_gamma.Add(prop.gamma.Max());
                    List_Re.Add(prop.Re.Max());
                    List_Cd.Add(prop.CDs.Max());
                    List_Cl.Add(prop.CLs.Max());
                }
            }
            chart1.Update(); chart2.Update();
            chart3.Update(); chart4.Update();
            chart5.Update(); chart6.Update();

            double plotmargin = 1.05;  //グラフの余白分
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart1.ChartAreas[0].AxisY.Maximum = List_ci.Max() * plotmargin;
            chart2.ChartAreas[0].AxisX.Minimum = 0;
            chart2.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart2.ChartAreas[0].AxisY.Maximum = List_phi.Max() * 180 / Math.PI * plotmargin;
            chart3.ChartAreas[0].AxisX.Minimum = 0;
            chart3.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart3.ChartAreas[0].AxisY.Maximum = List_gamma.Max() * plotmargin;
            chart4.ChartAreas[0].AxisX.Minimum = 0;
            chart4.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart4.ChartAreas[0].AxisY.Maximum = List_Re.Max() / 1000 * plotmargin;
            chart5.ChartAreas[0].AxisX.Minimum = 0;
            chart5.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart5.ChartAreas[0].AxisY.Maximum = List_Cd.Max() * plotmargin;
            chart6.ChartAreas[0].AxisX.Minimum = 0;
            chart6.ChartAreas[0].AxisX.Maximum = List_Radius.Max();
            chart6.ChartAreas[0].AxisY.Maximum = List_Cl.Max() * plotmargin;
        }

        /// <summary>
        /// PlotShowIndexSeriesにShowのtrueの番号を代入。消去してからListに追加
        /// Propsに入ってるPropｎ中からShowがTrueつまり、表示になっているものの番号をPlotShowIndexSeriesに代入
        /// </summary>
        List<int> PlotShowIndexSeries = new List<int>();
        private void PlotDetect()
        {
            int max_index = PlotShowIndexSeries.Count;
            for (int i = 0; i < max_index; i++)
            {
                PlotShowIndexSeries.RemoveAt(0);
            }
            foreach (Prop prop in Props)
            {
                if (prop.Show == true)
                {
                    PlotShowIndexSeries.Add(prop.dataGridView_index);
                }
            }
        }

    }
}
