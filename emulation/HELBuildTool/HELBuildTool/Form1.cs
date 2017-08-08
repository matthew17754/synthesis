﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        OpenFileDialog openFileDialog;

        public Form1()
        {
            openFileDialog = new OpenFileDialog();
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            String number = txtNumber.Text;
            String path = txtBrowse.Text;

            Console.WriteLine(path);
            String currentDir = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(path);
            Console.WriteLine(currentDir);

#if DEBUG
            String cygwinPath = "C:\\cygwin64";
            String buildTool = currentDir + "../";
            String makeArgs = "SYNTHESIS_LIBS=" + currentDir + "/../../emulation/hel/build SYNTHESIS_INCLUDES=" + currentDir + "../../emulation/hel/";
#else
            String cygwinPath = "C:\\Program Files (x86)\\Autodesk\\Synthesis\\cygwin64";
            String buildTool = "/cygdrive/c/Program Files (x86)/Autodesk/Synthesis/SynthesisDrive/HELBuildTool";
            String makeArgs = "";
#endif
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(
                cygwinPath + "\\bin\\mintty.exe",
                "'" + cygwinPath + "\\bin\\bash.exe' " +
                "-c \"mount -c /cygdrive && make -f " +
                "'" + buildTool + "/Makefile' " +
                "TEAM_ID=" + number + " " +
                makeArgs + " " +
                "&& echo 'Starting robot code' && ./build/FRC_UserProgram " +
                "|| read -p 'Press enter to continue'\"");
            startInfo.EnvironmentVariables["PATH"] = cygwinPath + "\\bin";
            startInfo.UseShellExecute = false;

            System.Diagnostics.Process.Start(startInfo);
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {
            //openFileDialog.ShowDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
              txtBrowse.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
