using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/* ---------------------------- *
*        Input  uinf,rpm radius, rho, nu, lambda, power,
* static Input  t1, t2, power0, w1, e, factor, gamma, ci
* parameter     alpha, CL, CD_0, AOA
* static param  n, t_length, 
*        Output P_, I_, T_, gamma, ci,       
* ---------------------------- */
namespace Inaprop
{
    public class Prop
    {
        //表示用の変数
        public int dataGridView_index;
        //----Constants----
        private double uinf, rpm, radius, rho, nu;
        private double radius_in;
        public static int n = 50; //40
        public int n_number = n;
        public static int t_length = 100;
        private double t1, t2, lambda, power0;
        private double db;
        private double power, w1, error, alpha, CL, CD_0, AOA, factor;
        public double P_;

        //翼性能を配列にする
        public double[] CLs = new double[n];
        public double[] CDs = new double[n];
        public double[] AOAs = new double[n];
        public double[] thickness = new double[n];
        
        public double[] gamma = new double[n];
        public double[] ci = new double[n];
        public double[] Re = new double[n];
        public double[] phi = new double[n];
        public double[] theta = new double[n];

        //----Calcuration of time----
        private double[] t = new double[t_length + 1];
        private double[] t_step = new double[t_length + 1];
        private double[] time = new double[t_length + 1];

        //----Definition of CP DP----
        private double omega;
        public double[] R = new double[n + 1];
        private double[] Rplus = new double[n + 1];
        private double[] psi = new double[t_length + 1];
        private double[] CPx = new double[n + 1];
        public double[] CPy = new double[n + 1];
        private double[] CPz = new double[n + 1];
        private double[,] DPx1 = new double[n + 1, t_length + 1];
        private double[,] DPy1 = new double[n + 1, t_length + 1];
        private double[,] DPz1 = new double[n + 1, t_length + 1];
        private double[,] DPx2 = new double[n + 1, t_length + 1];
        private double[,] DPy2 = new double[n + 1, t_length + 1];
        private double[,] DPz2 = new double[n + 1, t_length + 1];

        //----Calculation of K_x K_z----
        private double[,] K_x = new double[n, n];
        private double[,] K_y = new double[n, n];
        private double[,] K_z = new double[n, n];

        private double[] V_f = new double[3];

        //----収束計算----
        private double d_gamma;
        private double I;
        private double[] GG = new double[n];
        private int[] i_r = new int[n];
        private int counter;
        private int[] swap_i = new int[2];
        private int i1, i2;

        //----Cal_T_P_I----
        public double T_, I_; //P_は定義済み

        //----I_plus,I_minus----
        private double I_p, I_n;

        //blade
        public double blade = 2;

        //----Constructor----
        public Prop()
        {
            Name = "Prop" + dataGridView_index;
            Method = "Not calc.";
            uinf = 8;
            rpm = 120;
            radius = 1.6;
            rho = 1.225;
            nu = 1.46 * Math.Pow(10, -5);
            t1 = 1.0 / 2000;
            t2 = 1.0 / 20;
            lambda = 40;  //タイムステップの指数部の定数
            power = 250;
            w1 = 0.1;
            error = Math.Pow(10, -6);   //渦法の収束限界
            alpha = 0.5; //Conserge param
            CL = 0.6;
            CD_0 = 0.014;
            AOA = 1.0 / 180 * Math.PI;
            double thickness0 = 0;
            factor = 0;
            omega = rpm / 60 * 2 * Math.PI;

            this.Show = false;

            //CLs,CDs,AOAsを読み込まない場合初期化で全部CL,CD_0で代用
            for (int i = 0; i < Prop.n; i++)
            {
                CLs[i] = CL;
                CDs[i] = CD_0;
                AOAs[i] = AOA;
                thickness[i] = thickness0;
            }
        }

        public void CalcHarada(BackgroundWorker worker, DoWorkEventArgs e)
        {
            Method = "Harada";
            power0 = power;
            //----Initilization----
            P_ = 0;
            db = (radius - radius_in) / (n - 1);
            for (int i = 0; i < n; i++)
            {
                gamma[i] = 0.2;
                ci[i] = 1;
            }

            //-----Calcuration and definition of time, CP, DP, Kx and Kz-----
            Calcuration_of_time();
            Definition_of_CP_DP();
            Calcuration_of_Kx_Kz();
                        
            while ((power - P_) * (power - P_) > 0.5)
            {
                d_gamma = 0.02;
                //----Initial Lift and Drag----
                //GG = gamma;
                gamma.CopyTo(GG, 0);
                Cal_T_P_I();
                I = I_;
                //i_r = [1:n];
                for (int i = 0; i < n; i++)
                {
                    i_r[i] = i;
                }
                //----Main----
                counter = 0;
                while (d_gamma > error)
                {
                    //---- Random order----
                    for (int i = 0; i < n; i++)
                    {
                        Random rnd = new Random();
                        swap_i[0] = (int)(rnd.Next(n));
                        swap_i[1] = (int)(rnd.Next(n));
                        i1 = i_r[swap_i[0]];
                        i2 = i_r[swap_i[1]];
                        i_r[swap_i[1]] = i1;
                        i_r[swap_i[0]] = i2;
                    }
                    for (int i = 0; i < n; i++)
                    {
                        //----Calculate I_plus----
                        //GG = gamma;
                        gamma.CopyTo(GG, 0);
                        GG[i_r[i]] = GG[i_r[i]] + d_gamma;
                        Cal_T_P_I();
                        I_p = I_;
                        //----Calculate I_minus----
                        //GG = gamma;
                        gamma.CopyTo(GG, 0);
                        GG[i_r[i]] = GG[i_r[i]] - d_gamma;
                        Cal_T_P_I();
                        I_n = I_;
                        //----Evaluation----
                        if ((I_p < I) && (I_n >= I))
                        {
                            gamma[i_r[i]] = gamma[i_r[i]] + d_gamma;
                            I = I_p;
                        }
                        if ((I_p >= I) && (I_n < I))
                        {
                            gamma[i_r[i]] = gamma[i_r[i]] - d_gamma;
                            if (gamma[i_r[i]] < 0)
                            {
                                gamma[i_r[i]] = 0;
                            }
                            else
                            {
                                I = I_n;
                            }
                        }
                        if ((I_p < I) && (I_n < I))
                        {
                            if (I_p < I_n)
                            {
                                gamma[i_r[i]] = gamma[i_r[i]] + d_gamma;
                                I = I_p;
                            }
                            else
                            {
                                gamma[i_r[i]] = gamma[i_r[i]] - d_gamma;
                                if (gamma[i_r[i]] < 0)
                                {
                                    gamma[i_r[i]] = 0;
                                }
                                else
                                {
                                    I = I_n;
                                }
                            }
                        }
                    }
                    //----Count up----
                    counter = counter + 1;
                    //進捗状況を報告
                    double L_d_gamma = Math.Log(d_gamma, 2);
                    double L_error = Math.Log(error, 2);
                    int percentComplete = (int)(((L_d_gamma - Math.Log(0.02, 2)) / L_error)
                        * (L_error / (L_error - Math.Log(0.02, 2))) * 100)
                        + (int)(counter / 100.0 * 6.5);
                    if (percentComplete > 100) { percentComplete = 100; }
                    worker.ReportProgress(percentComplete);

                    if (counter > 100)
                    {
                        d_gamma = d_gamma / 2;
                        counter = 0;
                    }
                }
                power0 = power0 + alpha * (power - P_);
            }
        }

        //-----Calcuration of time-----
        /// <summary>
        /// CalcHaradaとPaformanceAnalysisで使用しているtime生成関数
        /// </summary>
        private void Calcuration_of_time()
        {
            for (int i = 0; i < t_length + 1; i++)
            {
                t[i] = (double)i;
                t_step[i] = 1 / (1 + Math.Exp(t[i] / lambda));
                t_step[i] = 1 - 2 * t_step[i];
                t_step[i] = (t_step[i]) * (t2 - t1) + t1;
                if (i == 0)
                {
                    time[i] = t_step[i];
                }
                else
                {
                    time[i] = time[i - 1] + t_step[i];
                }
            }
        }

        //-----Definition of CP, DP-----
        /// <summary>
        /// CalcHaradaとPaformanceAnalysisで使用しているCP,DP定義関数
        /// </summary>
        private void Definition_of_CP_DP()
        {
            for (int i = 0; i < n + 1; i++)
            {
                R[i] = (double)i / (double)(n - 1) * (radius - radius_in) + radius_in;
                Rplus[i] = (double)(i + 1) / (double)(n - 1) * (radius - radius_in) + radius_in;
                CPx[i] = 0;
                CPy[i] = (R[i] + Rplus[i]) / 2.0;
                CPz[i] = 0;
            }
            for (int j = 0; j < t_length + 1; j++)
            {
                psi[j] = omega * time[j];
            }
            for (int i = 0; i < n + 1; i++)
            {
                for (int j = 0; j < t_length + 1; j++)
                {
                    DPx1[i, j] = -uinf * time[j];
                    DPy1[i, j] = R[i] * Math.Cos(psi[j]);
                    DPz1[i, j] = -R[i] * Math.Sin(psi[j]);
                    DPx2[i, j] = -uinf * time[j];
                    DPy2[i, j] = -R[i] * Math.Cos(psi[j]);
                    DPz2[i, j] = R[i] * Math.Sin(psi[j]);
                }
            }
        }

        //-----
        /// <summary>
        /// CalcHaradaとPaformanceAnalysisで使用しているKx,Kz生成関数
        /// 内部でF_influence使用
        /// </summary>
        private void Calcuration_of_Kx_Kz()
        {
            //----Calculation of K_x, K_z----
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    K_x[i, j] = 0;
                    K_y[i, j] = 0;
                    K_z[i, j] = 0;
                }
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    V_f = F_influence(i, j, CPx, CPy, CPz, DPx1, DPy1, DPz1);
                    K_x[i, j] = K_x[i, j] - V_f[0];
                    K_y[i, j] = K_y[i, j] - V_f[1];
                    K_z[i, j] = K_z[i, j] - V_f[2];
                    V_f = F_influence(i, j + 1, CPx, CPy, CPz, DPx1, DPy1, DPz1);
                    K_x[i, j] = K_x[i, j] + V_f[0];
                    K_y[i, j] = K_y[i, j] + V_f[1];
                    K_z[i, j] = K_z[i, j] + V_f[2];
                    V_f = F_influence(i, j, CPx, CPy, CPz, DPx2, DPy2, DPz2);
                    K_x[i, j] = K_x[i, j] - V_f[0];
                    K_y[i, j] = K_y[i, j] - V_f[1];
                    K_z[i, j] = K_z[i, j] - V_f[2];
                    V_f = F_influence(i, j + 1, CPx, CPy, CPz, DPx2, DPy2, DPz2);
                    K_x[i, j] = K_x[i, j] + V_f[0];
                    K_y[i, j] = K_y[i, j] + V_f[1];
                    K_z[i, j] = K_z[i, j] + V_f[2];
                }
            }
        }

        private double[] F_influence(int i, int j, double[] CPx, double[] CPy, double[] CPz,
                                    double[,] DPx, double[,] DPy, double[,] DPz)
        {
            int k = DPx.GetLength(1) - 1; //length(x)は長い方の配列 getLength()は0スタート
            double[] a1 = new double[k];
            double[] a2 = new double[k];
            double[] a3 = new double[k];
            double[] b1 = new double[k];
            double[] b2 = new double[k];
            double[] b3 = new double[k];
            double[] l1 = new double[k];
            double[] l2 = new double[k];
            double[] l3 = new double[k];
            double[] a_norm = new double[k];
            double[] b_norm = new double[k];
            double[] aa1 = new double[k];
            double[] aa2 = new double[k];
            double[] aa3 = new double[k];
            double[] bb1 = new double[k];
            double[] bb2 = new double[k];
            double[] bb3 = new double[k];
            double[] al1 = new double[k];
            double[] al2 = new double[k];
            double[] al3 = new double[k];
            double[] al_norm = new double[k];
            double[] alal1 = new double[k];
            double[] alal2 = new double[k];
            double[] alal3 = new double[k];
            double[] c1 = new double[k];
            double[] c2 = new double[k];
            double[] c3 = new double[k];
            double[] cl = new double[k];
            double[] V_f = new double[3] { 0.0, 0.0, 0.0 };

            for (int iter = 0; iter < k; iter++)
            {
                a1[iter] = DPx[j, iter] - CPx[i]; //DPx -0;
                a2[iter] = DPy[j, iter] - CPy[i]; //DPy - R[i];
                a3[iter] = DPz[j, iter] - CPz[i]; //DPz - 0;
                b1[iter] = DPx[j, iter + 1] - CPx[i];
                b2[iter] = DPy[j, iter + 1] - CPy[i];
                b3[iter] = DPz[j, iter + 1] - CPz[i];
                l1[iter] = b1[iter] - a1[iter];
                l2[iter] = b2[iter] - a2[iter];
                l3[iter] = b3[iter] - a3[iter];
                a_norm[iter] = Math.Sqrt(a1[iter] * a1[iter] + a2[iter] * a2[iter]
                    + a3[iter] * a3[iter]);
                b_norm[iter] = Math.Sqrt(b1[iter] * b1[iter] + b2[iter] * b2[iter]
                    + b3[iter] * b3[iter]);
                aa1[iter] = a1[iter] / a_norm[iter];
                aa2[iter] = a2[iter] / a_norm[iter];
                aa3[iter] = a3[iter] / a_norm[iter];
                bb1[iter] = b1[iter] / b_norm[iter];
                bb2[iter] = b2[iter] / b_norm[iter];
                bb3[iter] = b3[iter] / b_norm[iter];
                al1[iter] = a2[iter] * l3[iter] - a3[iter] * l2[iter];
                al2[iter] = a3[iter] * l1[iter] - a1[iter] * l3[iter];
                al3[iter] = a1[iter] * l2[iter] - a2[iter] * l1[iter];
                al_norm[iter] = Math.Sqrt(al1[iter] * al1[iter] + al2[iter] * al2[iter]
                    + al3[iter] * al3[iter]);
                if (al_norm[iter] == 0)
                {
                    V_f[0] += 0; V_f[1] += 0; V_f[2] += 0;
                }
                else
                {
                    alal1[iter] = al1[iter] / (al_norm[iter] * al_norm[iter]);
                    alal2[iter] = al2[iter] / (al_norm[iter] * al_norm[iter]);
                    alal3[iter] = al3[iter] / (al_norm[iter] * al_norm[iter]);
                    c1[iter] = bb1[iter] - aa1[iter];
                    c2[iter] = bb2[iter] - aa2[iter];
                    c3[iter] = bb3[iter] - aa3[iter];
                    cl[iter] = c1[iter] * l1[iter] + c2[iter] * l2[iter] + c3[iter] * l3[iter];
                    V_f[0] += 1.0 / 4.0 / Math.PI * alal1[iter] * cl[iter];
                    V_f[1] += 1.0 / 4.0 / Math.PI * alal2[iter] * cl[iter];
                    V_f[2] += 1.0 / 4.0 / Math.PI * alal3[iter] * cl[iter];
                }
            }

            return V_f;
        }

        private void Cal_T_P_I()
        {
            double[] uu = new double[n];
            double[] ww = new double[n];
            double[] Up = new double[n];
            double[] Ut = new double[n];
            //double[] phi = new double[n];
            double[] Vi = new double[n];
            //double[] ci = new double[n];
            //double[] Re = new double[n];
            double[] CD0 = new double[n];
            double[] Li = new double[n];
            double[] Di = new double[n];
            double[] Ti = new double[n];
            double[] Pi = new double[n];
            //use global parameter 
            //K_x, K_z, GG, uinf, omega, CPy, CL, nu, factor, CD_0, rho, db, ci, Re, CD0
            //Li, Di, Ti, Pi, T_, P_, I_, 

            T_ = 0; P_ = 0;

            for (int f_iter = 0; f_iter < n; ++f_iter)
            {
                uu[f_iter] = 0;
                ww[f_iter] = 0;
                for (int k = 0; k < n; ++k)
                {
                    uu[f_iter] += K_x[f_iter, k] * GG[k];
                    ww[f_iter] += K_z[f_iter, k] * GG[k];
                    //c[f_iter, j] += a[f_iter, k] * b[k, j];
                }
                Up[f_iter] = uinf - uu[f_iter];
                Ut[f_iter] = omega * CPy[f_iter] - ww[f_iter];
                phi[f_iter] = Math.Atan2(Up[f_iter], Ut[f_iter]);
                Vi[f_iter] = Math.Sqrt(Up[f_iter] * Up[f_iter] + Ut[f_iter] * Ut[f_iter]);
                //ci[f_iter] = 2 * GG[f_iter] / Vi[f_iter] / CL;
                ci[f_iter] = 2 * GG[f_iter] / Vi[f_iter] / CLs[f_iter];
                Re[f_iter] = ci[f_iter] * Vi[f_iter] / nu;
                //CD0[f_iter] = (1 / (1 + Math.Exp(Re[f_iter] / 10000))) * factor + CD_0;
                CD0[f_iter] = (1 / (1 + Math.Exp(Re[f_iter] / 10000))) * factor + CDs[f_iter];
                Li[f_iter] = rho * Vi[f_iter] * GG[f_iter] * db;
                Di[f_iter] = 0.5 * rho * Vi[f_iter] * Vi[f_iter] * CD0[f_iter] * ci[f_iter] * db;
                Ti[f_iter] = Li[f_iter] * Math.Cos(phi[f_iter]) - Di[f_iter] * Math.Sin(phi[f_iter]);
                Pi[f_iter] = (Di[f_iter] * Math.Cos(phi[f_iter]) + Li[f_iter] * Math.Sin(phi[f_iter]))
                    * CPy[f_iter] * omega;
                T_ += Ti[f_iter] * Blade;
                P_ += Pi[f_iter] * Blade;

                theta[f_iter] = phi[f_iter] + AOAs[f_iter];
            }
            I_ = -T_ + w1 * (P_ - power0) * (P_ - power0);
            efficiency = T_ * uinf / P_ * 100;
        }

        //-----Larrabee用変数----
        //-----Larrabee----
        public void CulcLarrabee()
        {
            Method = "Larrabee";
            double[] x = new double[n];
            double[] sinphi = new double[n];
            double[] cosphi = new double[n];

            double Pc, Tc, L_lambda;
            //double DL;
            double[] DLs = new double[n];   //DLを配列にした
            double[] f = new double[n];
            double[] F = new double[n];
            double[] G = new double[n];
            double[] xi = new double[n];
            double dxi = 1.0 / n;
            double[] dI1dxi = new double[n];
            double[] dI2dxi = new double[n];
            double[] dJ1dxi = new double[n];
            double[] dJ2dxi = new double[n];
            double I1 = 0, I2 = 0, zeta, vd;
            double J1 = 0, J2 = 0;
            double[] ad = new double[n];
            double[] aa = new double[n];
            double[] planform = new double[n];
            double[] beta = new double[n];
            double[] dTdrL = new double[n];
            double[] dTdr = new double[n];
            double[] rdQdrL = new double[n];    // (1/r * dQ/dr)L
            double[] rdQdr = new double[n];     // 1/r * dQ/dr
            //double epsilon;
            double[] epsilons = new double[n];    //epsilonを配列化
            double[] etae = new double[n];
            double[] dTcdxi = new double[n];
            double eta;
            double[] dQdr = new double[n];

            //powerとthrustの切り替えを導入したので入り組んでいる2012-12-28
            thrust = 28.0;
            db = (radius - radius_in) / (n - 1);
            omega = rpm / 60 * 2 * Math.PI;
            double radius_diff = radius - radius_in;
            
            //DL = CD_0 / CL;
            Pc = 2 * power / (rho * uinf * uinf * uinf * Math.PI * radius_diff * radius_diff);
            Tc = 2 * thrust / (rho * uinf * uinf * Math.PI * radius_diff * radius_diff);
            L_lambda = uinf / omega / radius;

            for (int i = 0; i < n; i++)
            {
                //DLsのテスト
                DLs[i] = CDs[i] / CLs[i];

                //ci[i] = 1;
                if (i == 0 && radius_in ==0)
                {
                    R[i] = 0.000001;
                }
                else
                {
                    R[i] = (double)i / (double)(n -1) * (radius - radius_in) + radius_in;
                }
                x[i] = omega * R[i] / uinf;
                sinphi[i] = Math.Sqrt(1 / (1 + x[i] * x[i]));
                cosphi[i] = x[i] * sinphi[i];
                f[i] = blade / 2.0 * Math.Sqrt(L_lambda * L_lambda + 1) / L_lambda
                    * (1 - R[i] / radius);
                F[i] = 2 / Math.PI * Math.Acos(Math.Exp(-f[i]));
                G[i] = F[i] * x[i] * x[i] / (1 + x[i] * x[i]);
                xi[i] = R[i] / radius;
                //dxi
                //Tc
                //dI1dxi[i] = G[i] * (1 - DL / x[i]) * xi[i];
                //dI2dxi[i] = G[i] * (1 - DL / x[i]) * xi[i] / (x[i] * x[i] + 1);
                dI1dxi[i] = G[i] * (1 - DLs[i] / x[i]) * xi[i];
                dI2dxi[i] = G[i] * (1 - DLs[i] / x[i]) * xi[i] / (x[i] * x[i] + 1);
                //Pc
                //dJ1dxi[i] = G[i] * (1 + DL * x[i]) * xi[i];
                //dJ2dxi[i] = G[i] * (1 + DL * x[i]) * xi[i] * (x[i] * x[i]) / (x[i] * x[i] +1);
                dJ1dxi[i] = G[i] * (1 + DLs[i] * x[i]) * xi[i];
                dJ2dxi[i] = G[i] * (1 + DLs[i] * x[i]) * xi[i] * (x[i] * x[i]) / (x[i] * x[i] +1);
                //以下4行は最低でも台形則ぐらいにはしたい
                I1 += 4 * dI1dxi[i] * dxi;
                I2 += 2 * dI2dxi[i] * dxi;
                J1 += 4 * dJ1dxi[i] * dxi;
                J2 += 2 * dJ2dxi[i] * dxi;
            }
            // 上のはthrust,下のはpowerの時のzeta
            //zeta = I1 / (2 * I2) * (1 - Math.Sqrt(1 - ( 4 * I2 * Tc / I1 / I1)));
            zeta = J1 / (2 * J2) * (Math.Sqrt(1 + (4 * J2 * Pc / J1 / J1)) - 1);
            vd = zeta * uinf;

            thrust = 0.0;
            //power = 0.0;
            double torque = 0.0;
            for (int i = 0; i < n; i++)
            {
                gamma[i] = 2 * Math.PI * R[i] * vd * sinphi[i] * cosphi[i] * F[i] / blade;
                ad[i] = 0.5 * vd / uinf / (x[i] * x[i] + 1);
                aa[i] = 0.5 * vd / uinf * (x[i] * x[i]) / (x[i] * x[i] + 1);
                planform[i] = 4 * Math.PI / blade * L_lambda * G[i] / Math.Sqrt(1 + x[i] * x[i]);
                //ci[i] = planform[i] * zeta / CL * radius;
                ci[i] = planform[i] * zeta / CLs[i] * radius;
                phi[i] = Math.Atan(L_lambda / xi[i] * (1 + zeta / 2));
                theta[i] = phi[i] + AOAs[i];
                beta[i] = phi[i] + (AOAs[i] / 180 * Math.PI);
                dTdrL[i] = rho * omega * R[i] * (1 - ad[i]) * blade * gamma[i];
                //dTdr[i] = dTdrL[i] * (1 - DL / x[i]);
                dTdr[i] = dTdrL[i] * (1 - DLs[i] / x[i]);
                rdQdrL[i] = rho * uinf * (1 + aa[i]) * blade * gamma[i];
                //rdQdr[i] = rdQdrL[i] * (1 + DL * x[i]);
                rdQdr[i] = rdQdrL[i] * (1 + DLs[i] * x[i]);
                thrust += dTdr[i] * db;
                torque += rdQdr[i] * db * R[i];

                epsilons[i] = Math.Atan(DLs[i]);
            }
            //epsilon = Math.Atan(DL);
            eta = 0;
            for (int i = 0; i < n; i++)
            {
                //etae[i] = Math.Tan(phi[i]) / Math.Tan(phi[i] + epsilon)
                //    * (1 / (1 + 0.5 * zeta));
                //dTcdxi[i] = 2 * zeta * G[i] * (1 - DL / x[i]) * xi[i]
                //    * (2 - zeta / (x[i] * x[i] + 1));
                etae[i] = Math.Tan(phi[i]) / Math.Tan(phi[i] + epsilons[i])
                    * (1 / (1 + 0.5 * zeta));
                dTcdxi[i] = 2 * zeta * G[i] * (1 - DLs[i] / x[i]) * xi[i]
                    * (2 - zeta / (x[i] * x[i] + 1));
                eta += etae[i] * dTcdxi[i] * dxi / Tc;
            }
            for (int i = 0; i < n; i++)
            {
                dQdr[i] = dTdr[i] * uinf / (eta * omega);
                Re[i] = Math.Sqrt(uinf * uinf + (omega * R[i] * (1 - ad[i]) * omega * R[i] * (1 - ad[i])))
                    * ci[i] / nu;
            }
            //double Torque = thrust * uinf / (eta * omega);
            T_ = thrust;
            P_ = torque * omega;
            //P_ = power;
            efficiency = T_ * uinf / P_ * 100;
        }

        //-----Adkins & Liebeck
        public double thrust = 28;
        public double efficiency;
        //----Adkins & Liebeck----
        //BEM(blade Element Momentum Theory)
        //分割数(section)を多く持っているのでSpline補間する必要がある
        public void CulcBEM()
        {
            Method = "BEM";
            int section = 1000;

            double zeta = 0.129;    //BEMに必要な係数

            double[] xi = new double[section];
            double[] tanphi = new double[section];
            double[] BEM_phi = new double[section]; //補間対象phi   gamma無し
            double[] f = new double[section];
            double[] F = new double[section];
            double[] BEM_Re = new double[section];  //補間対象Re
            double[] BEM_alpha = new double[section];
            double[] BEM_Cd = new double[section];  //補間対象CDs
            double[] BEM_Cl = new double[section];  //補間対象CLs
            double[] epsilon = new double[section];
            double[] aa = new double[section];
            double[] ad = new double[section];
            double[] WW = new double[section];
            double[] J1 = new double[section];
            double[] delta_J1 = new double[section];
            double[] J2 = new double[section];
            double[] delta_J2 = new double[section];
            double[] I1 = new double[section];
            double[] delta_I1 = new double[section];
            double[] I2 = new double[section];
            double[] delta_I2 = new double[section];
            double sum_J1 = 0, sum_J2 = 0, sum_I1 = 0, sum_I2 = 0;
            double BEM_omega, BEM_lambda;
            double Pc, Tc;
            double Pc_zeta = 0, Tc_zeta = 0;
            double[] BEM_R = new double[section];       //補間対象x軸radius
            double[] BEM_chord = new double[section];   //補間対象chord
            double[] BEM_beta = new double[section];

            double d_zeta = 0.01;        //ζの大小によってζを上下
            //ζの大小評価フラグ　Pc_zetaがPcに比べて 0:未定義 1:大 2:小
            int zeta_large = 0;

            double radius_diff = radius - radius_in;
            BEM_omega = rpm * Math.PI / 30;     //回転角速度Ω[rad/s]
            Pc = 2 * power / rho / uinf / uinf / uinf / Math.PI / radius_diff / radius_diff;
            Tc = 2 * thrust / rho / uinf / uinf / Math.PI / radius_diff / radius_diff;
            BEM_lambda = uinf / BEM_omega / radius;

            //半径方向のProp.n個の配列のRを定義。補間に使用。
            for (int i = 0; i < n; i++)
            {
                if (i == 0 && radius_in == 0)
                {
                    R[i] = 0.000001;
                }
                else
                {
                    R[i] = (double)i / (double)(n - 1) * (radius - radius_in) + radius_in;
                }
            }
            //R[n] = radius;
            //スプライン補間　翼型
            BEM_Cl = Spline_Interpolate(R, CLs, section);
            BEM_Cd = Spline_Interpolate(R, CDs, section);

            while ((Pc - Pc_zeta) * (Pc - Pc_zeta) > 0.000000001)
            {
                for (int i = 0; i < section; i++)
                {
                    //内側半径が0の時はゼロ割をやめる。正規化
                    if (i == 0 && radius_in == 0)
                    {
                        xi[i] = 0.000001;
                    }
                    else
                    {
                        xi[i] = (double)i / (double)(section- 1) * (1 - radius_in / radius) + (radius_in / radius);
                    }
                    tanphi[i] = (1.0 + zeta / 2) * BEM_lambda / xi[i];
                    BEM_phi[i] = Math.Atan(tanphi[i]);
                    f[i] = blade / 2.0 * (1.0 - xi[i])
                        / Math.Sin(Math.Atan(1.0 + (zeta / 2.0)) * BEM_lambda);
                    F[i] = 2 / Math.PI * Math.Acos(Math.Exp(-f[i]));
                    BEM_Re[i] = 4 * Math.PI * xi[i] * radius * F[i]
                        * Math.Cos(BEM_phi[i]) * Math.Sin(BEM_phi[i])
                        * uinf * zeta / nu / BEM_Cl[i] / blade;
                    BEM_alpha[i] = 4;
                    //BEM_Cd[i] = 0.014;
                    epsilon[i] = BEM_Cd[i] / BEM_Cl[i];
                    aa[i] = zeta / 2 * Math.Cos(BEM_phi[i]) * Math.Cos(BEM_phi[i])
                        * (1 - epsilon[i] * Math.Tan(BEM_phi[i]));
                    ad[i] = zeta / 2 * BEM_lambda / xi[i]
                        * Math.Cos(BEM_phi[i]) * Math.Sin(BEM_phi[i])
                        * (1 + epsilon[i] / Math.Tan(BEM_phi[i]));
                    WW[i] = uinf * (1 + aa[i]) / Math.Sin(BEM_phi[i]);
                    J1[i] = 4 * xi[i] * F[i] * xi[i] / BEM_lambda
                        * Math.Cos(BEM_phi[i]) * Math.Sin(BEM_phi[i])
                        * (1 + epsilon[i] / Math.Tan(BEM_phi[i]));
                    //delta_J1[i] = 
                    J2[i] = J1[i] / 2 * (1 - epsilon[i] * Math.Tan(BEM_phi[i]))
                        * Math.Cos(BEM_phi[i]) * Math.Cos(BEM_phi[i]);
                    I1[i] = 4 * xi[i] * F[i] * xi[i] / BEM_lambda
                        * Math.Cos(BEM_phi[i]) * Math.Sin(BEM_phi[i])
                        * (1 - epsilon[i] * Math.Tan(BEM_phi[i]));
                    I2[i] = BEM_lambda * I1[i] / 2 / xi[i]
                        * (1 + epsilon[i] / tanphi[i])
                        * Math.Sin(BEM_phi[i]) * Math.Cos(BEM_phi[i]);
                    BEM_R[i] = radius * xi[i];
                    BEM_chord[i] = 4 * Math.PI * BEM_R[i] * F[i]
                        * Math.Cos(BEM_phi[i]) * Math.Sin(BEM_phi[i])
                        * uinf * zeta / BEM_Cl[i] / blade / WW[i];
                }
                for (int i = 1; i < section; i++)
                {
                    delta_J1[i] = (J1[i - 1] + J1[i]) * (xi[i] - xi[i - 1]) / 2;
                    delta_J2[i] = (J2[i - 1] + J2[i]) * (xi[i] - xi[i - 1]) / 2;
                    delta_I1[i] = (I1[i - 1] + I1[i]) * (xi[i] - xi[i - 1]) / 2;
                    delta_I2[i] = (I2[i - 1] + I2[i]) * (xi[i] - xi[i - 1]) / 2;
                    sum_J1 += delta_J1[i];
                    sum_J2 += delta_J2[i];
                    sum_I1 += delta_I1[i];
                    sum_I2 += delta_I2[i];
                }
                Pc_zeta = sum_J1 * zeta + sum_J2 * zeta * zeta;
                Tc_zeta = sum_I1 * zeta - sum_J2 * zeta * zeta;
                sum_I1 = 0; sum_I2 = 0; sum_J1 = 0; sum_J2 = 0;

                if ((Pc > Pc_zeta && zeta_large == 2) ||
                    (Pc < Pc_zeta && zeta_large == 1))
                {
                    d_zeta = d_zeta / 2;
                    zeta_large = 0;
                }
                else
                {
                    if (Pc > Pc_zeta)
                    {
                        zeta += d_zeta;
                        zeta_large = 1;
                    }
                    else
                    {
                        zeta -= d_zeta;
                        zeta_large = 2;
                    }
                }
            }
            P_ = 0.5 * Pc_zeta * rho * uinf * uinf * uinf * Math.PI * radius_diff * radius_diff;
            T_ = Tc_zeta / 2.0 * rho * uinf * uinf * Math.PI * radius_diff * radius_diff;
            efficiency = Tc_zeta / Pc_zeta * 100;   //他の手法と比較して式が異なるがこれはパワー係数だから良い

            //スプライン補間 section -> Prop.n
            //補間対象：R, ci, beta, (gamma), Re
            ci = Spline_Interpolate(BEM_R, BEM_chord, Prop.n);
            phi = Spline_Interpolate(BEM_R, BEM_phi, Prop.n);
            Re = Spline_Interpolate(BEM_R, BEM_Re, Prop.n);

            for (int i = 0; i < Prop.n; i++)
            {
                theta[i] = phi[i] + AOAs[i];
            }
        }

        /// <summary>
        /// 補間のためのメソッド。開始点と終了点は補間前後で不変。
        /// </summary>
        /// <param name="x1">補間したい既知のx軸</param>
        /// <param name="y1">補間したい既知のy軸</param>
        /// <param name="count2">補間した後の個数</param>
        /// <returns>補間された後のy軸</returns>
        private double[] Spline_Interpolate(double[] x1, double[] y1, int count2)
        {
            double[] y2 = new double[count2];
            var y_known = new Dictionary<double, double>();
            for (int i = 0; i < y1.Count(); i++)
            {
                y_known.Add(x1[i], y1[i]);
            }
            var y_scaler = new SplineInterpolator(y_known);
            double start = y_known.First().Key;
            double end = y_known.Last().Key;
            double step = (end - start) / count2;
            int iter  = 0;
            for (double x = start + 0.00000001, i = 0; x <= end; x += step, i++)
            {
                iter = (int)i;
                y2[iter] = y_scaler.GetValue(x);
            }
            return y2;
        }

        //-----Paformance Analysis-----
        /// <summary>
        /// 性能解析メソッド。設計した後にだけ出来るようにGUIで調整
        /// </summary>
        public void PaformanceAnalysis()
        {
            double[] gamma_old = new double[n];
            double sum_GammaError = 1;

            double[] uu = new double[n];
            double[] ww = new double[n];
            double[] Up = new double[n];
            double[] Ut = new double[n];
            //double[] phi = new double[n];
            double[] CD0 = new double[n];
            double[] CL0 = new double[n];
            double[] Vi = new double[n];
            double[] Li = new double[n];
            double[] Di = new double[n];
            double[] Ti = new double[n];
            double[] Pi = new double[n];

            double[] paform_alpha = new double[n];
                                    
            double analysis_factor = 0.2;

            omega = rpm / 60 * 2 * Math.PI;
            Calcuration_of_time();
            Definition_of_CP_DP();
            Calcuration_of_Kx_Kz();

            for (int i = 0; i < n; i++)
            {
                gamma[i] = 0;
                gamma_old[i] = 1;
            }

            while (sum_GammaError > error)
            {
                T_ = 0; P_ = 0;
                sum_GammaError = 0;
                for (int i = 0; i < n; ++i)
                {
                    uu[i] = 0;
                    ww[i] = 0;
                    for (int k = 0; k < n; ++k)
                    {
                        uu[i] += K_x[i, k] * GG[k];
                        ww[i] += K_z[i, k] * GG[k];
                        //c[i, j] += a[i, k] * b[k, j];
                    }
                    Up[i] = uinf - uu[i];
                    Ut[i] = omega * CPy[i] - ww[i];
                    phi[i] = Math.Atan2(Up[i], Ut[i]);
                    Vi[i] = Math.Sqrt(Up[i] * Up[i] + Ut[i] * Ut[i]);
                    //ci[i] = 2 * GG[i] / Vi[i] / CL;
                    Re[i] = ci[i] * Vi[i] / nu;
                    paform_alpha[i] = theta[i] - phi[i];
                    //get_CLCD_from_alphaRe(paform_alpha[i], Re[i], out CLs[i], out CDs[i]);
                    CDs[i] = (1 / (1 + Math.Exp(Re[i] / 10000))) * factor + CD_0;
                    CLs[i] = CL;
                    GG[i] = 0.5 * ci[i] * Vi[i] * CLs[i];
                    gamma_old[i] = gamma[i];
                    gamma[i] = gamma[i] * (1 - analysis_factor) + GG[i] * analysis_factor;
                    Li[i] = 0.5 * rho * Vi[i] * Vi[i] * CLs[i] * ci[i] * db;
                    Di[i] = 0.5 * rho * Vi[i] * Vi[i] * CDs[i] * ci[i] * db;
                    Ti[i] = Li[i] * Math.Cos(phi[i]) - Di[i] * Math.Sin(phi[i]);
                    Pi[i] = (Di[i] * Math.Cos(phi[i]) + Li[i] * Math.Sin(phi[i]))
                        * CPy[i] * omega;
                    T_ += Ti[i] * 2;
                    P_ += Pi[i] * 2;
                    sum_GammaError += (gamma_old[i] - gamma[i])
                        * (gamma_old[i] - gamma[i]);
                }
            }
            power = P_;
            thrust = T_;
        }

        private void get_CLCD_from_alphaRe(double alpha, double Re, out double CL, out double CD)
        {
            double alpha_CL0 = -5.0 / 180 * Math.PI;
            double CLa = 5.7;
            double CD_0 = 0.014;

            CL = CLa * (alpha - alpha_CL0);
            CD = CD_0;
            }

        // コピーを作製するメソッドMemberwiseClone()を使う。使用方法はPropcopy = Prop.Clone();
        public Prop Clone()
        {
            return (Prop)MemberwiseClone();
        }

        //-----Property----
        //Uinf, Rpm, Radius, Rho, Nu, T1, T2, Lambda, Power, W1, Error, Alpha
        //P_CL, P_CD_0, P_AOA, Factor
        //-----------------
        public double Uinf
        {
            set { uinf = value; }
            get { return uinf; }
        }
        public double Rpm
        {
            set { rpm = value; }
            get { return rpm; }
        }
        public double Radius
        {
            set { radius = value; }
            get { return radius; }
        }
        public double Radius_in
        {
            set { radius_in = value; }
            get { return radius_in; }
        }
        public double Rho
        {
            set { rho = value; }
            get { return rho; }
        }
        public double Nu
        {
            set { nu = value; }
            get { return nu; }
        }
        public double T1
        {
            set { t1 = value; }
            get { return t1; }
        }
        public double T2
        {
            set { t2 = value; }
            get { return t2; }
        }
        public double Power
        {
            set { power = value; }
            get { return power; }
        }
        public double W1
        {
            set { w1 = value; }
            get { return w1; }
        }
        public double Error
        {
            set { error = value; }
            get { return error; }
        }
        public double Alpha
        {
            set { alpha = value; }
            get { return alpha; }
        }
        public double P_CL
        {
            set { CL = value; }
            get { return CL; }
        }
        public double P_CD_0
        {
            set { CD_0 = value; }
            get { return CD_0; }
        }
        public double P_AOA
        {
            set { AOA = value; }
            get { return AOA; }
        }
        public double Factor
        {
            set { factor = value; }
            get { return factor; }
        }
        public double Blade
        {
            set { blade = value; }
            get { return blade; }
        }
        public double Power_Output
        {
            set { P_ = value; }
            get { return P_; }
        }
        public double Thrust
        {
            set { T_ = value; }
            get { return T_; }
        }
        public double Efficiency
        {
            set { efficiency = value; }
            get { return efficiency; }
        }
        public string Name { get; set; }
        public bool Show { get; set; }
        public Color Style { get; set; }
        public string Method { get; set; }
    }
}
