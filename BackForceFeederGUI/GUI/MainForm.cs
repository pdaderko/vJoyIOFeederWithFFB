﻿using BackForceFeeder.Configuration;
using BackForceFeeder.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BackForceFeeder.vJoyIOFeederAPI;

namespace BackForceFeederGUI.GUI
{

    public partial class MainForm : Form
    {
        /// <summary>
        /// Always here to save logs
        /// </summary>
        public LogForm Log;

        public MainForm()
        {
            InitializeComponent();

            Log = new LogForm();
        }

        List<CheckBox> AllRawInputs = new List<CheckBox>();
        List<CheckBox> AllvJoyBtns = new List<CheckBox>();
        List<CheckBox> AllOutputs = new List<CheckBox>();
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "BackForceFeeder v" +typeof(BFFManager).Assembly.GetName().Version.ToString() + " Made for Gamoover by njz3";

            // Must do this to create controls and allow for Log() to have 
            // right thread ID when calling InvokeReduired
            Log.Show();
            Log.Hide();

            ToolTip tooltip = new ToolTip();

            axesJoyGauge.FromValue = -135;
            axesJoyGauge.ToValue = 135;
            axesJoyGauge.Wedge = 270;
            axesJoyGauge.LabelsStep = 270.0 / 4.0;
            axesJoyGauge.TickStep = 135 / 10.0;
            //axesGauge.DisableAnimations = true;
            axesJoyGauge.AnimationsSpeed = new TimeSpan(0, 0, 0, 0, 100);
            //axesJoyGauge.RightToLeft = RightToLeft.Yes;

            cmbSelectedAxis.Items.Clear();
            cmbSelectedAxis.SelectedIndex = -1;

            // Only display first 16 buttons/io
            for (int i = 1; i <= 16; i++) {
                // Checkboxes for Raw inputs
                var chkBox = new CheckBox();
                chkBox.AutoSize = true;
                chkBox.Enabled = false;
                chkBox.Location = new System.Drawing.Point(320 + 48*((i-1)>>3), 23 + 20*((i-1)&0b111));
                chkBox.Name = "RawBtn" + i;
                chkBox.Size = new System.Drawing.Size(32, 17);
                chkBox.TabIndex = i;
                chkBox.Text = i.ToString();
                chkBox.Tag = i;
                chkBox.UseVisualStyleBackColor = true;
                AllRawInputs.Add(chkBox);
                this.splitContainerMain.Panel2.Controls.Add(chkBox);

                // Checkboxes for vJoy Buttons
                chkBox = new CheckBox();
                chkBox.AutoSize = true;
                chkBox.Enabled = false;
                chkBox.Location = new System.Drawing.Point(440 + 48*((i-1)>>3), 23 + 20*((i-1)&0b111));
                chkBox.Name = "vJoyBtn" + i;
                chkBox.Size = new System.Drawing.Size(32, 17);
                chkBox.TabIndex = i;
                chkBox.Text = i.ToString();
                chkBox.Tag = i;
                chkBox.UseVisualStyleBackColor = true;
                AllvJoyBtns.Add(chkBox);
                this.splitContainerMain.Panel2.Controls.Add(chkBox);

                // Checkboxes for Raw outputs

                chkBox = new CheckBox();
                chkBox.AutoSize = true;
                chkBox.Enabled = false;
                chkBox.Location = new System.Drawing.Point(560 + 48*((i-1)>>3), 23 + 20*((i-1)&0b111));
                chkBox.Name = "Output" + i;
                chkBox.Size = new System.Drawing.Size(32, 17);
                chkBox.TabIndex = i;
                chkBox.Text = i.ToString();
                chkBox.Tag = i;
                chkBox.UseVisualStyleBackColor = true;
                AllOutputs.Add(chkBox);
                this.splitContainerMain.Panel2.Controls.Add(chkBox);
            }

            FillControlSet();
        }


        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (!BFFManager.Config.Application.StartMinimized &&
                WindowState == FormWindowState.Minimized &&
                Program.TrayIcon!=null) {
                Program.TrayIcon.ShowBalloonTip(3000,
                        "BackForceFeeder by njz3",
                        "Running mode is " + BFFManager.Config.Hardware.TranslatingModes.ToString(),
                        ToolTipIcon.Info);
            }
        }

        private static int WM_QUERYENDSESSION = 0x11;
        /// <summary>
        /// Catch specific OS events like ENDSESSION which closes any application
        /// </summary>
        /// <param name="msg"></param>
        protected override void WndProc(ref Message msg)
        {
            var message = msg.Msg;
            // For debugging only
            //Console.WriteLine(message);
            // WM_ENDSESSION: invoke Close in the pump
            if (message == WM_QUERYENDSESSION) {
                BeginInvoke(new EventHandler(delegate { Close(); }));
            }
            // everything else is default
            base.WndProc(ref msg);
        }


        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            // Scan vJoy used axis to refresh list of axes
            int comboidx = 0;
            if (Program.Manager.vJoy!=null) {
                for (int i = 0; i<Program.Manager.vJoy.NbAxes; i++) {
                    var axisinfo = Program.Manager.vJoy.SafeGetMappingRawTovJoyAxis(i);
                    if (axisinfo==null)
                        return;
                    var name = axisinfo.vJoyAxisInfo.Name.ToString().Replace("HID_USAGE_", "");
                    if (comboidx>=cmbSelectedAxis.Items.Count) {
                        cmbSelectedAxis.Items.Add(name);
                    } else {
                        cmbSelectedAxis.Items[comboidx] = name;
                    }
                    comboidx++;
                }
            }
            int selectedvJoyAxis = cmbSelectedAxis.SelectedIndex;

            if (!cmbConfigSet.DroppedDown) {
                cmbConfigSet.SelectedItem = BFFManager.Config.CurrentControlSet.UniqueName;
                this.lblCurrentGame.Text = BFFManager.Config.CurrentControlSet.GameName;
            }

            // Axes
            if (Program.Manager.vJoy != null) {
                var axis = Program.Manager.vJoy.SafeGetMappingRawTovJoyAxis(selectedvJoyAxis);
                if (axis!=null) {

                    txtRawAxisValue.Text = (axis.RawValue_pct*100).ToString("F2");
                    slRawAxis.Maximum = 100;
                    slRawAxis.Value = (int)(axis.RawValue_pct*100);

                    txtJoyAxisValue.Text = (axis.vJoyAxisInfo.AxisValue_pct*100).ToString("F2");
                    slJoyAxis.Maximum = 100;
                    slJoyAxis.Value = (int)(axis.vJoyAxisInfo.AxisValue_pct*100);

                    axesJoyGauge.Value = (((double)slJoyAxis.Value / (double)slJoyAxis.Maximum) - 0.5) * 270;
                } else {
                    if (Program.Manager.vJoy.NbAxes>0)
                        cmbSelectedAxis.SelectedIndex = 0;
                }
            } else {

                txtRawAxisValue.Text = "NA";
                txtJoyAxisValue.Text = "NA";
                slRawAxis.Value = 0;
                slJoyAxis.Value = 0;
                axesJoyGauge.Value = 0;
            }

            // Buttons
            if ((Program.Manager.vJoy != null)) {
                var buttons = Program.Manager.vJoy.GetFirst32ButtonsState();
                for (int i = 0; i < AllvJoyBtns.Count; i++) {
                    var chk = AllvJoyBtns[i];
                    if ((buttons & (1 << i)) != 0)
                        chk.Checked = true;
                    else
                        chk.Checked = false;
                }
            }
            // Raw inputs
            if (Program.Manager.IOboard != null) {
                var inputs = Program.Manager.RawInputsStates;
                for (int i = 0; i < AllRawInputs.Count; i++) {
                    var chk = AllRawInputs[i];
                    if ((inputs & (UInt64)(1 << i)) != 0)
                        chk.Checked = true;
                    else
                        chk.Checked = false;
                }
            }

            if (Program.Manager.Outputs != null) {
                var outputs = Program.Manager.RawOutputs;
                for (int i = 0; i < 16; i++) {
                    var chk = AllOutputs[i];
                    if ((outputs & (1 << i)) != 0)
                        chk.Checked = true;
                    else
                        chk.Checked = false;
                }
            }

            // IOBoard status
            if (Program.Manager.IOboard ==null) {
                // No IO BOard
                this.labelStatus.ForeColor = Color.Red;
                this.labelStatus.Text = "IOBoard scanning, not found yet (check cables or baudrate)";
            } else {
                // Outputs mode only?
                if (BFFManager.Config.Application.OutputOnly) {
                    // Check manager state only
                    this.labelStatus.ForeColor = Color.Black;
                    if (Program.Manager.IsRunning)
                        this.labelStatus.Text = "Running (outputs only)";
                    else
                        this.labelStatus.Text = "Stopped (outputs only)";
                } else {
                    // vJoy ?
                    if (Program.Manager.vJoy!=null &&
                           !Program.Manager.vJoy.vJoyVersionMatch) {
                        // Wrong vJoy
                        this.labelStatus.ForeColor = Color.Red;
                        this.labelStatus.Text = "vJoy error, driver version=" + String.Format("{0:X}", Program.Manager.vJoy.vJoyVersionDriver)
                            + ", dll version=" + String.Format("{0:X}", Program.Manager.vJoy.vJoyVersionDll);
                    } else {
                        // All good
                        /*
                        this.labelStatus.ForeColor = Color.Red;
                        this.labelStatus.Text = "vJoy error, wrong Driver version=" + String.Format("{0:X}", Program.Manager.vJoy.vJoyVersionDriver)
                            + " expecting dll version=" + String.Format("{0:X}", Program.Manager.vJoy.vJoyVersionDll);
                        */
                        this.labelStatus.ForeColor = Color.Black;
                        if (Program.Manager.IsRunning)
                            this.labelStatus.Text = "Running";
                        else
                            this.labelStatus.Text = "Stopped";
                    }
                }
            }
        }


        private void btnConfigureHardware_Click(object sender, EventArgs e)
        {
            AppHwdEditor editor = new AppHwdEditor();
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
            }
        }

        private void btnShowLogWindow_Click(object sender, EventArgs e)
        {
            Log.Show();
        }


        private void btnAxisMappingEditor_Click(object sender, EventArgs e)
        {
            int selectedvJoyIndexAxis = cmbSelectedAxis.SelectedIndex;

            if (Program.Manager.vJoy == null) return;

            var axis = Program.Manager.vJoy.SafeGetMappingRawTovJoyAxis(selectedvJoyIndexAxis);
            if (axis==null) return;

            AxisMappingEditor editor = new AxisMappingEditor(BFFManager.Config.CurrentControlSet);
            editor.SelectedAxis = selectedvJoyIndexAxis;
            editor.InputRawDB = (RawAxisDB)axis.RawAxisDB.Clone();
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
                axis.RawAxisDB = editor.ResultRawDB;
                Program.Manager.SaveControlSetFiles();
            }
            editor.Dispose();
        }

        private void btnButtons_Click(object sender, EventArgs e)
        {
            ButtonsEditor editor = new ButtonsEditor(BFFManager.Config.CurrentControlSet);
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
            }
            editor.Dispose();
        }


        private void btnOutputs_Click(object sender, EventArgs e)
        {
            OutputsEditor editor = new OutputsEditor(BFFManager.Config.CurrentControlSet);
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
            }
            editor.Dispose();
        }

        private void btnKeyStrokes_Click(object sender, EventArgs e)
        {
            KeyEmulationEditor editor = new KeyEmulationEditor(BFFManager.Config.CurrentControlSet);
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
            }
            editor.Dispose();
        }

        private void btnTuneEffects_Click(object sender, EventArgs e)
        {
            EffectTuningEditor editor = new EffectTuningEditor(BFFManager.Config.CurrentControlSet);
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
            }
            editor.Dispose();
        }

        private void btnControlSets_Click(object sender, EventArgs e)
        {
            ControlSetEditor editor = new ControlSetEditor();
            var res = editor.ShowDialog(this);
            if (res == DialogResult.OK) {
                Program.Manager.SortControlSets();
                FillControlSet();
            }
        }

        private void FillControlSet()
        {
            cmbConfigSet.Items.Clear();
            for (int i = 0; i<BFFManager.Config.AllControlSets.ControlSets.Count; i++) {
                var cs = BFFManager.Config.AllControlSets.ControlSets[i];
                cmbConfigSet.Items.Add(cs.UniqueName);
            }
            cmbConfigSet.SelectedItem = BFFManager.Config.CurrentControlSet.UniqueName;
            this.lblCurrentGame.Text = BFFManager.Config.CurrentControlSet.GameName;
        }

        private void cmbConfigSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cs = BFFManager.Config.AllControlSets.ControlSets.Find(x => (x.UniqueName == (string)cmbConfigSet.SelectedItem));
            if (cs!=null) {
                BFFManager.Config.CurrentControlSet = cs;
                this.lblCurrentGame.Text = BFFManager.Config.CurrentControlSet.GameName;
            }
        }

    }
}
