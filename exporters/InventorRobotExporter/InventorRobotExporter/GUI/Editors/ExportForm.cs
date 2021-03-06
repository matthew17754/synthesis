﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using InventorRobotExporter.Managers;
using InventorRobotExporter.Utilities;

namespace InventorRobotExporter.GUI.Editors
{
    public partial class ExportForm : Form
    {
        private static Dictionary<string, string> fields = new Dictionary<string, string>();

        public ExportForm(string initialRobotName)
        {
            InitializeComponent();
            AnalyticsUtils.LogPage("Pre-Export Form");
            InitializeFields();

            RobotNameTextBox.Text = initialRobotName;
            ColorBox.Checked = RobotExporterAddInServer.Instance.AddInSettingsManager.DefaultExportWithColors;
            
        }

        /// <summary>
        /// Gets the paths for all the synthesis fields.
        /// </summary>
        private void InitializeFields()
        {
            OpenSynthesisBox.Checked = RobotExporterAddInServer.Instance.AddInSettingsManager.OpenSynthesis;
        }
        
        /// <summary>
        /// Prompts the user for the name of the robot, as well as other information.
        /// </summary>
        /// <returns>True if user pressed okay, false if they pressed cancel</returns>
        public static bool PromptExportSettings(RobotDataManager robotDataManager)
        { // TODO: Compact this down
            if (Prompt(robotDataManager.RobotName, out var robotName, out var colors, out var openSynthesis) == DialogResult.OK)
            {
                robotDataManager.RobotName = robotName;

                RobotExporterAddInServer.Instance.AddInSettingsManager.DefaultExportWithColors = colors;
                RobotExporterAddInServer.Instance.AddInSettingsManager.SaveSettings();

                return true;
            }

            return false;
        }

        public static DialogResult Prompt(string initialRobotName, out string robotName, out bool colors, out bool openSynthesis)
        {
            try
            {
                ExportForm settingsForm = new ExportForm(initialRobotName);
                settingsForm.ShowDialog();
                robotName = settingsForm.RobotNameTextBox.Text;
                colors = settingsForm.ColorBox.Checked;
                openSynthesis = settingsForm.OpenSynthesisBox.Checked;
                RobotExporterAddInServer.Instance.AddInSettingsManager.OpenSynthesis = settingsForm.OpenSynthesisBox.Checked;

                return settingsForm.DialogResult;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (CheckFormIsValid())
            {
                var invalidChars = (new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars())).Distinct();

                foreach (char c in invalidChars)
                {
                    RobotNameTextBox.Text = RobotNameTextBox.Text.Replace(c.ToString(), "");
                }

                if(File.Exists(RobotExporterAddInServer.Instance.AddInSettingsManager.ExportPath + "\\" + RobotNameTextBox.Text + @"\skeleton.bxdj") && MessageBox.Show("Overwrite Existing Robot?", "Save Robot", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a name for your robot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSynthesisBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckFormIsValid();
        }

        private bool CheckFormIsValid()
        {
            okButton.Enabled = RobotNameTextBox.Text.Length > 0;
            return okButton.Enabled;
        }

        private void RobotNameTextBox_TextChanged(object sender, EventArgs e)
        {
            CheckFormIsValid();
        }
    }
}
