using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SOFM;

namespace SOFMTest
{
    public partial class frmMain : Form
    {
        private SOFM.NeuralNetwork nn;

        public frmMain()
        {
            InitializeComponent();
            lblStatus.Text = "Ready";
        }

        private void ShowInputPatternsOnChart()
        {
            inputDataChart.Series[0].Clear();
            foreach (List<double> pattern in nn.Patterns)
            {
                inputDataChart.Series[0].Add(pattern[0], pattern[1]);
            }
        }

        private void nn_EndEpochEvent(object sender, EndEpochEventArgs e)
        {
            if (chbVisualization.Checked)
            {
                outputDataChart.Series[0].Clear();
                for (int i = 0; i < nn.OutputLayerDimension; i++)
                    for (int j = 0; j < nn.OutputLayerDimension; j++)
                    {
                        outputDataChart.Series[0].Add(nn.OutputLayer[i, j].Weights[0], nn.OutputLayer[i, j].Weights[1]);
                    }
            }

            Application.DoEvents();
        }
        private void nn_EndIterationEvent(object sender, EventArgs e)
        {
            if (pbStatus.Value < pbStatus.Maximum) pbStatus.Value++;
        }

        private void lbPatterns_SelectedIndexChanged(object sender, EventArgs e)
        {
            inputDataChart.Series[1].Clear();
            outputDataChart.Series[1].Clear();
            inputDataChart.Series[1].Add(nn.Patterns[lbPatterns.SelectedIndex][0], nn.Patterns[lbPatterns.SelectedIndex][1]);
            Neuron Winner = nn.FindWinner(nn.Patterns[lbPatterns.SelectedIndex]);
            outputDataChart.Series[1].Add(Winner.Weights[0], Winner.Weights[1]);
            sofmVisualizer.LightUpThePixel(Winner.Coordinate.X, Winner.Coordinate.Y);
        }

        private void AddLegend()
        {
            panelLegend.Controls.Clear();
            Label label = new Label();
            label.Name = "lblLegend";
            label.Top = 5;
            label.Left = 5;
            label.Text = "Legend";
            label.AutoSize = true;
            panelLegend.Controls.Add(label);
            for (int i = 0; i < nn.ExistentClasses.Count; i++)
            {
                Label lbl = new Label();
                lbl.Name = "lbl"+nn.ExistentClasses.Keys[i];
                lbl.Text = " - "+nn.ExistentClasses.Keys[i];
                lbl.Top = 20 * (i+1);
                lbl.AutoSize = true;
                lbl.Left = 15 + (int)lbl.Font.Size;
                this.panelLegend.Controls.Add(lbl);

                Panel panel = new Panel();
                panel.Name = "panel" + nn.ExistentClasses.Keys[i];
                panel.Top = 20 * (i + 1) + (int)lbl.Font.Size/2;
                panel.Left = 15;
                panel.Width = (int)lbl.Font.Size;
                panel.Height = (int)lbl.Font.Size;
                panel.BackColor = nn.UsedColors[i];
                this.panelLegend.Controls.Add(panel);
            }
        }

        private void lbPatterns_Leave(object sender, EventArgs e)
        {
            inputDataChart.Series[1].Clear();
            outputDataChart.Series[1].Clear();
        }

        private void lblIONTLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.iont.ru");
        }

        private void chbVisualization_CheckedChanged(object sender, EventArgs e)
        {
            this.nn_EndEpochEvent(this, new EndEpochEventArgs());
        }

        private void AddPatternsToListBox()
        {
            lbPatterns.Items.Clear();
            string patternString;
            for (int i = 0; i < nn.Patterns.Count; i++)
            {
                patternString = "";
                patternString += nn.Classes[i]+" ";
                for (int j = 0; j < nn.InputLayerDimension; j++)
                    patternString += nn.Patterns[i][j].ToString("g2") +" ";                
                lbPatterns.Items.Add(patternString);
            }
        }

        private void SetError(TextBox tb, string s)
        {
            errorProvider.SetError(tb, s);

            bool errorOnOtherTextBoxControl = false;
            foreach (Control c in this.gbInputParams.Controls)
            {
                if (c is TextBox)
                {
                    if (errorProvider.GetError(c).Length!=0)
                        errorOnOtherTextBoxControl = true;
                }
            }
            if (!errorOnOtherTextBoxControl)
                btnLoadDataAndCreateNetwork.Enabled = true;
            else
                btnLoadDataAndCreateNetwork.Enabled = false;          
        }

        private void tb_Validating(object sender, CancelEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text.Length != 0)
            {
                bool notValidSymbol = false;
                char[] cArray = tb.Text.ToCharArray();
                foreach (char c in cArray)
                {
                    if (tb.Name == "tbEpsilon")
                    {
                        if (!Char.IsDigit(c) && c != '.') notValidSymbol = true;
                    }
                    else
                    {
                        if (!Char.IsDigit(c)) notValidSymbol = true;
                    }
                }
                if (!notValidSymbol)
                {
                    switch (tb.Name)
                    {
                        case "tbNumberOfCards":
                            {
                                if (Convert.ToInt32(tb.Text) <= Properties.Settings.Default.MaximalNumberOfNeurons) SetError(tb,"");
                                else SetError(tb, "Maximal number of neurons is - " + Properties.Settings.Default.MaximalNumberOfNeurons.ToString());
                                break;
                            }
                        case "tbIterationsNumber":
                            {
                                if (Convert.ToInt32(tb.Text) <= Properties.Settings.Default.MaximalNumberOfIterations) SetError(tb, "");
                                else SetError(tb, "Maximal number of iterations - " + Properties.Settings.Default.MaximalNumberOfIterations.ToString());
                                break;
                            }
                        case "tbEpsilon":
                            {
                                double epsilon = Convert.ToDouble(tb.Text);
                                if (epsilon <= Properties.Settings.Default.MaximalEpsilon && epsilon >= Properties.Settings.Default.MinimalEpsilon) SetError(tb, "");
                                else SetError(tb, "Epsilon must lie between " + Properties.Settings.Default.MinimalEpsilon.ToString() + "and " + Properties.Settings.Default.MaximalEpsilon.ToString());
                                break;
                            }
                    }
                }
                else
                {
                    SetError(tb,"Incorrect input. Please try again.");
                }
            }

        }
        
        private void SwitchControls(bool switcher)
        {
            gbInputParams.Enabled = switcher;
            lbPatterns.Enabled = switcher;
        }

        private void btnLoadDataAndCreateNetwork_Click(object sender, EventArgs e)
        {
            int NumberOfCards = (int)Math.Sqrt(Int32.Parse(tbNumberOfCards.Text));
            Functions f = Functions.Gaus;
            foreach (Control c in this.gbInputParams.Controls)
            {
                if (c is RadioButton)
                {
                    if (((RadioButton)c).Checked) f = (Functions)Enum.Parse(typeof(Functions), c.Tag.ToString());
                }
            }

            nn = new NeuralNetwork(NumberOfCards, Int32.Parse(tbIterationsNumber.Text), Double.Parse(tbEpsilon.Text), f);
            nn.EndEpochEvent += new EndEpochEventHandler(nn_EndEpochEvent);
            nn.EndIterationEvent += new EndIterationEventHandler(nn_EndIterationEvent);
            nn.Normalize = this.chbNormalize.Checked;
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                nn.ReadDataFromFile(ofd.FileName);
                sofmVisualizer.Matrix = null;
                panelLegend.Visible = false;
                ShowInputPatternsOnChart();
                AddPatternsToListBox();
                SwitchControls(false);
                lblStatus.Text = "Map constructing in progress. Please wait...";
                pbStatus.Visible = true;
                pbStatus.Minimum = 0;
                pbStatus.Value = 0;
                pbStatus.Maximum = Int32.Parse(tbIterationsNumber.Text);
                nn.StartLearning();
                sofmVisualizer.Matrix = nn.ColorSOFM();
                sofmVisualizer.Invalidate();
                panelLegend.Visible = true;
                AddLegend();
                pbStatus.Visible = false;
                lblStatus.Text = "Ready";                
                SwitchControls(true);
            }
        }
    }
}