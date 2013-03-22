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
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializedataGridView1();
            InitializedataGridView2();
            InitializePlot();

            //TEST
            Airfoils Airfoilstest = new Airfoils();
            List<WingSection> WingSection1 = new List<WingSection>();
            //List<Airfoil> Airfoil2 = new List<Airfoil>();
            WingSection1.Add(new WingSection(0.0)); WingSection1.Add(new WingSection(1.0));
            //Prop Proptest = Airfoilstest.Transration(WingSection1);
        }

        [STAThread]
        public static void Main()
        {
            Application.Run(new MainForm());
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //クラス宣言
        Airfoil DAE51 = new Airfoil();
        List<WingSection> WingSections = new List<WingSection>();
        List<WingSection> SortedWingSections = new List<WingSection>();
        List<Prop> Props = new List<Prop>();
        public Prop SelectedProp = new Prop();
        int number_of_Prop = 0;

        // Culcボタン
        private void button4_Click(object sender, EventArgs e)
        {
            //SortedWingSectionsはdataGridView1_validCellで定義
            Airfoils Airfoilstest = new Airfoils();
            //if (SortedWingSections.Count > 2)
            //{
                Airfoilstest.Transration(ref SelectedProp, SortedWingSections);
            //}
            //Props.Add(new Prop());
            SelectedProp.Name = textBox1.Text;
            SelectedProp.Blade = (double)numericUpDown1.Value;
            SelectedProp.Uinf = (double)numericUpDown2.Value;
            SelectedProp.Rpm = (double)numericUpDown3.Value;
            SelectedProp.Radius = (double)numericUpDown4.Value;
            SelectedProp.Radius_in = (double)numericUpDown5.Value;
            //SelectedProp.Lambda = (double)numericUpDown5.Value;
            SelectedProp.Power = (double)numericUpDown6.Value;
            SelectedProp.Rho = (double)numericUpDown7.Value;
            SelectedProp.Nu = (double)numericUpDown8.Value;

            //半径より内側半径が大きい時はエラー
            if (SelectedProp.Radius <= SelectedProp.Radius_in)
            {
                MessageBox.Show("Radius must be larger than Radius_INNER", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SelectedProp.Show = true;

            if (radioButton1.Checked == true)   //Harada
            {
                button4.Text = "Now Calcurating...";
                button4.Enabled = false;
                backgroundWorker1.RunWorkerAsync();
            }
            if (radioButton2.Checked == true)   //Larrabee
            {
                SelectedProp.CulcLarrabee();
                label13.Text = SelectedProp.T_.ToString("F2") + "[N]";
                label14.Text = SelectedProp.P_.ToString("F2") + "[W]";
                label15.Text = SelectedProp.Efficiency.ToString("F2") + "%";

                //ShowPlot(SelectedProp);
                ShowPlot2();
            }
            if (radioButton3.Checked == true)   //BEM
            {
                SelectedProp.CulcBEM();
                label13.Text = SelectedProp.T_.ToString("F2") + "[N]";
                label14.Text = SelectedProp.P_.ToString("F2") + "[W]";
                label15.Text = SelectedProp.Efficiency.ToString("F2") + "%";

                ShowPlot2();
            }
            updata_dataGridView2();
            PlotDetect();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            SelectedProp.CalcHarada(worker, e);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label12.Text = "Power:" + SelectedProp.P_.ToString("F1");
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button4.Text = "Calcuration";
            button4.Enabled = true;
            label12.Text = "Progress";

            label13.Text = SelectedProp.T_.ToString("F2") + "[N]";
            label14.Text = SelectedProp.P_.ToString("F2") + "[W]";
            label15.Text = SelectedProp.Efficiency.ToString("F2") + "%";

            ShowPlot2();
            updata_dataGridView2();
        }



        /// <summary>
        /// dataGridView1のメソッド
        /// </summary>
        public void InitializedataGridView1()
        {
            WingSections.Add(new WingSection(0.0));
            WingSections.Add(new WingSection(1.0));

            IEnumerable<WingSection> subWingSections = from WingSection aWingSection in WingSections
                                                       orderby aWingSection.Position ascending
                                                       select aWingSection;
            dataGridView1.DataSource = subWingSections.ToList<WingSection>();

            //位置r/RでソートされたWingSectionリスト。これからAirfoilsクラスを用いてPropを生成できる
            SortedWingSections = new List<WingSection>(subWingSections);

            //テスト
            //List<WingSection> SortedWingSetions = new List<WingSection>(subWingSections);
            
            //読み取り専用（ReadOnly)にはならない
            //dataGridView1[0, 0].ReadOnly = true;
            //dataGridView1[0, dataGridView1.Rows.Count - 1].ReadOnly = true;
        }

        public void InitializedataGridView2()
        {
            Props.Add(new Prop());
            Props[0].Name = "Prop" + number_of_Prop;
            SelectedProp = Props[0];
            SelectedProp.dataGridView_index = 0;
            updata_dataGridView2();
        }

        // New Propellerボタン
        // PropsリストにPropコンストラクタを新規製作追加。リストをソート、表示
        private void button6_Click(object sender, EventArgs e)
        {
            Props.Add(new Prop());
            number_of_Prop++;
            Props[Props.Count - 1].Name = "Prop" + number_of_Prop;
            updata_dataGridView2();
        }
        //Delete Propellerボタン
        //１行づつ選択行を削除。並び替え後表示。もし全部削除なら一つだけ残るようにAddして表示。
        private void button8_Click(object sender, EventArgs e)
        {
            exportCSVprop(SelectedProp, 10);

            foreach (DataGridViewRow selectedRow in dataGridView2.SelectedRows)
            {
                Props.RemoveAt(selectedRow.Index);
            }
            updata_dataGridView2();
            if (dataGridView2.RowCount == 0)
            {
                Props.Add(new Prop());
                number_of_Prop++;
                Props[Props.Count - 1].Name = "Prop" + number_of_Prop;
            }
            updata_dataGridView2();
            reload_dataGridView2();
            
        }

        //WingSection部分
        //Insertボタンクリック
        private void button2_Click(object sender, EventArgs e)
        {
            WingSections.Add(new WingSection(0.1));
            IEnumerable<WingSection> subWingSections = from WingSection aWingSection in WingSections
                                                       orderby aWingSection.Position ascending
                                                       select aWingSection;
            dataGridView1.DataSource = subWingSections.ToList<WingSection>();
        }

        //Deleteボタンクリック
        private void button3_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow selectedRow in dataGridView1.SelectedRows)
            {
                if (selectedRow.Index == 0 || selectedRow.Index == dataGridView1.Rows.Count - 1)
                {
                    MessageBox.Show("This row is NOT removable", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    WingSections.RemoveAt(selectedRow.Index);
                }
            }
            IEnumerable<WingSection> subWingSections = from WingSection aWingSection in WingSections
                                                       orderby aWingSection.Position ascending
                                                       select aWingSection;
            dataGridView1.DataSource = subWingSections.ToList<WingSection>();
        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;

            ////セルに入力されているのが数字の場合result = true,
            double output;
            bool result = Double.TryParse(e.FormattedValue.ToString(), out output);
                //文字が入力されている時
            if (result == false && dgv.Columns[e.ColumnIndex].Name != "nameDataGridViewTextBoxColumn")
            {
                dgv.Rows[e.RowIndex].ErrorText = "Input valid value";
                dgv.CancelEdit();
                e.Cancel = true;
            }
            else
            {
                //r/Rのセル内容が上下と値が被っていたら値変更
                if (e.RowIndex == dgv.Rows.Count - 1 || e.RowIndex == 0) { }
                else
                {
                    dgv["dataGridViewTextBoxColumn1", e.RowIndex].Value =
                        Convert.ToDouble(dgv["dataGridViewTextBoxColumn1", e.RowIndex].Value);
                    if ((double)dgv["dataGridViewTextBoxColumn1", e.RowIndex].Value ==
                        (double)dgv["dataGridViewTextBoxColumn1", e.RowIndex + 1].Value ||
                        (double)dgv["dataGridViewTextBoxColumn1", e.RowIndex].Value ==
                        (double)dgv["dataGridViewTextBoxColumn1", e.RowIndex - 1].Value)
                    {
                        dgv["dataGridViewTextBoxColumn1", e.RowIndex].Value =
                                (double)dgv["dataGridViewTextBoxColumn1", e.RowIndex + 1].Value + 0.01;
                        //dgv.Update();
                    }
                }

                //r/Rのセル内容が変更されている時
                if (dgv.Columns[e.ColumnIndex].Name == "dataGridViewTextBoxColumn1" &&
                    (output < 0.0 || output > 1.0))
                {
                    dgv.Rows[e.RowIndex].ErrorText = "r/R value is only 0.0 to 1.0";
                    dgv.CancelEdit();
                    e.Cancel = true;
                }
                //CLのセル内容が変更されている時
                if (dgv.Columns[e.ColumnIndex].Name == "clDataGridViewTextBoxColumn" &&
                    (output < 0.0 || output > 2.0))
                {
                    dgv.Rows[e.RowIndex].ErrorText = "CL value is only 0.0 to Max. CL";
                    dgv.CancelEdit();
                    e.Cancel = true;
                }
                //r/Rの最後は必ず1になるように
                if (dgv.RowCount != 0)
                    dgv["dataGridViewTextBoxColumn1", dgv.RowCount - 1].Value = 1.0;
            }
        }

        private void dataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            //エラーを消去
            DataGridView dgv = (DataGridView)sender;
            dgv.Rows[e.RowIndex].ErrorText = null;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //Validの確認が終わったらソート
            IEnumerable<WingSection> subWingSections = from WingSection aWingSection in WingSections
                                                       orderby aWingSection.Position ascending
                                                       select aWingSection;
            dataGridView1.DataSource = subWingSections.ToList<WingSection>();

            //位置r/RでソートされたWingSectionリスト。これからAirfoilsクラスを用いてPropを生成できる
            SortedWingSections = new List<WingSection>(subWingSections);
        }



        //dataGridView2
        private void dataGridView2_Click(object sender, EventArgs e)
        {
            reload_dataGridView2();
        }
        private void dataGridView2_KeyPress(object sender, KeyPressEventArgs e)
        {
            reload_dataGridView2();
        }
        private void dataGridView2_KeyUp(object sender, KeyEventArgs e)
        {
            reload_dataGridView2();
        }
        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            reload_dataGridView2();
        }

        //dataGridView2の選択行をSelecedPropに代入
        private void reload_dataGridView2()
        {
            foreach (DataGridViewRow r in dataGridView2.SelectedRows)
            {
                SelectedProp = Props[r.Index];
                SelectedProp.dataGridView_index = r.Index;
            }
            textBox1.Text = SelectedProp.Name;
            numericUpDown1.Value = (decimal)SelectedProp.Blade;
            numericUpDown2.Value = (decimal)SelectedProp.Uinf;
            numericUpDown3.Value = (decimal)SelectedProp.Rpm;
            numericUpDown4.Value = (decimal)SelectedProp.Radius;
            numericUpDown5.Value = (decimal)SelectedProp.Radius_in;
            numericUpDown6.Value = (decimal)SelectedProp.Power;
            numericUpDown7.Value = (decimal)SelectedProp.Rho;
            numericUpDown8.Value = (decimal)SelectedProp.Nu;

            //半径より内側半径が大きい時はエラー
            if (SelectedProp.Radius <= SelectedProp.Radius_in)
            {
                MessageBox.Show("Radius must be larger than Radius_INNER", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //グラフ書き換え
            ShowPlot2();
        }

        // dataGridView2のデータの表示の更新
        private void updata_dataGridView2()
        {
            IEnumerable<Prop> subProps = from Prop aProp in Props
                                         //orderby aProp.Name ascending
                                         select aProp;
            dataGridView2.DataSource = subProps.ToList<Prop>();
            if (dataGridView2.Rows.Count != SelectedProp.dataGridView_index)
            {
                dataGridView2.Rows[SelectedProp.dataGridView_index].Selected = true;
            }
            else
            {
                SelectedProp.dataGridView_index = 0;
            }
        }

        /// <summary>
        /// Analysisボタンクリック。解析フォームが出る。選択中のPropが選ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            AnalysisForm analysisForm = new AnalysisForm(SelectedProp);
            analysisForm.ShowDialog();
        }
    }
}
