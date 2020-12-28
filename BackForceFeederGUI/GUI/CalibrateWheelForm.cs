﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BackForceFeeder.vJoyIOFeederAPI;

namespace BackForceFeederGUI.GUI
{
    public partial class CalibrateWheelForm : Form
    {

        public int SelectedvJoyAxis;
        public double RawMostLeft_cts;
        public double RawMostRight_cts;
        public double RawMostCenter_cts;


        public CalibrateWheelForm()
        {
            InitializeComponent();
        }

        enum CalibrateSteps : int
        {
            TurnLeft = 0,
            TurnRight,
            Center,
            Done,
        }

        CalibrateSteps CalibrateStep;
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Raw value
            if (Program.Manager.vJoy != null) {
                var mapping = Program.Manager.vJoy.SafeGetMappingRawTovJoyAxis(SelectedvJoyAxis);
                if (mapping!=null) {
                    this.lbRawValue.Text = "Raw value=" + mapping.RawValue_cts;
                }
            }


            // Steps
            switch (CalibrateStep) {
                case CalibrateSteps.TurnLeft:
                    this.lbInstructions.Text = "Turn wheel maximum left";
                    this.btnNext.Text = "Next";
                    this.lbResult.Text = "Result:" + Environment.NewLine;
                    break;
                case CalibrateSteps.TurnRight:
                    this.lbInstructions.Text = "Turn wheel maximum right";
                    this.btnNext.Text = "Next";
                    this.lbResult.Text = "Result:" + Environment.NewLine 
                        + " Mostleft=" + this.RawMostLeft_cts + Environment.NewLine;
                    break;
                case CalibrateSteps.Center:
                    this.lbInstructions.Text = "Center wheel";
                    this.btnNext.Text = "Next";
                    this.lbResult.Text = "Result:" + Environment.NewLine
                        + " Most left=" + this.RawMostLeft_cts + Environment.NewLine
                        + " Most right=" + this.RawMostRight_cts + Environment.NewLine;
                    break;
                case CalibrateSteps.Done:
                    this.lbInstructions.Text = "Done";
                    this.btnNext.Text = "Done";
                    this.lbResult.Text = "Result:" + Environment.NewLine
                        + " Most left=" + this.RawMostLeft_cts + Environment.NewLine
                        + " Most right=" + this.RawMostRight_cts + Environment.NewLine
                        + " Center=" + this.RawMostCenter_cts + Environment.NewLine;
                    break;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            var mapping = Program.Manager.vJoy.SafeGetMappingRawTovJoyAxis(SelectedvJoyAxis);
            if (mapping==null) {
                MessageBox.Show("Error axis is not present anymore", "Error", MessageBoxButtons.OK);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }
            // Take value
            switch (CalibrateStep) {
                case CalibrateSteps.TurnLeft:
                    RawMostLeft_cts = mapping.RawValue_cts;
                    break;
                case CalibrateSteps.TurnRight:
                    RawMostRight_cts = mapping.RawValue_cts;
                    break;
                case CalibrateSteps.Center:
                    RawMostCenter_cts = mapping.RawValue_cts;
                    break;
                case CalibrateSteps.Done:
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
            }

            CalibrateStep++;
        }
    }
}
