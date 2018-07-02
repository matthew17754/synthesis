﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Inventor;
using System.Linq;

namespace JointResolver.ControlGUI
{
    public partial class SkeletonExporterForm : Form
    {
        public SkeletonExporterForm()
        {
            InitializeComponent();

            FormClosing += delegate (object sender, FormClosingEventArgs e)
            {
                InventorManager.Instance.UserInterfaceManager.UserInteractionDisabled = false;
            };
            Shown += delegate (object sender, EventArgs e)
            {
                ExporterWorker.RunWorkerAsync();
            };
        }

        private void SetProgress(string message, int current, int max)
        {
            // Allows function to be called by other threads
            if (InvokeRequired)
            {
                BeginInvoke((Action<string, int, int>)SetProgress, message, current, max);
                return;
            }

            ProgressLabel.Text = message;
            ProgressBar.Maximum = max;
            ProgressBar.Value = current;
        }

        private void ExporterWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            if (InventorManager.Instance == null)
            {
                MessageBox.Show("Couldn't detect a running instance of Inventor.");
                return;
            }

            if (InventorManager.Instance.ActiveDocument == null || !(InventorManager.Instance.ActiveDocument is AssemblyDocument))
            {
                MessageBox.Show("Couldn't detect an open assembly");
                return;
            }

            InventorManager.Instance.UserInterfaceManager.UserInteractionDisabled = true;

            RigidNode_Base Skeleton = ExportSkeleton(InventorManager.Instance.ComponentOccurrences.OfType<ComponentOccurrence>().ToList());
            
            SynthesisGUI.Instance.SkeletonBase = Skeleton;
        }

        /// <summary>
        /// The lightweight equivalent of the 'Add From Inventor' button in the <see cref="ExporterForm"/>. Used in <see cref="ExportMeshesLite(RigidNode_Base)"/>
        /// </summary>
        /// <param name="occurrences"></param>
        /// <returns></returns>
        public RigidNode_Base ExportSkeleton(List<ComponentOccurrence> occurrences)
        {
            if (occurrences.Count == 0)
            {
                throw new ArgumentException("ERROR: 0 Occurrences passed to ExportSkeletonLite", "occurrences");
            }

            #region CenterJoints
            int NumCentered = 0;

            SetProgress("Processing joints...", NumCentered, occurrences.Count);
            foreach (ComponentOccurrence component in occurrences)
            {
                try
                {
                    Exporter.CenterAllJoints(component);
                }
                catch (Exporter.InvalidJointException e)
                {
                    string caption = "Invalid Joint";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult r = MessageBox.Show(e.Message, caption, buttons);
                    return null;
                }
                catch (Exception e)
                {
                    throw e;
                }

                NumCentered++;

                SetProgress("Processing joints...", NumCentered, occurrences.Count + 3);
            }
            #endregion

            #region Build Models
            //Getting Rigid Body Info...
            SetProgress("Getting physics info...", occurrences.Count, occurrences.Count + 3);
            NameValueMap RigidGetOptions = InventorManager.Instance.TransientObjects.CreateNameValueMap();

            RigidGetOptions.Add("DoubleBearing", false);
            RigidBodyResults RawRigidResults = InventorManager.Instance.AssemblyDocument.ComponentDefinition.RigidBodyAnalysis(RigidGetOptions);

            //Getting Rigid Body Info...Done
            CustomRigidResults RigidResults = new CustomRigidResults(RawRigidResults);


            //Building Model...
            SetProgress("Building model...", occurrences.Count + 1, occurrences.Count + 3);
            RigidBodyCleaner.CleanGroundedBodies(RigidResults);
            RigidNode baseNode = RigidBodyCleaner.BuildAndCleanDijkstra(RigidResults);

            //Building Model...Done
            #endregion

            #region Cleaning Up
            //Cleaning Up...
            SetProgress("Cleaning up...", occurrences.Count + 2, occurrences.Count + 3);
            List<RigidNode_Base> nodes = new List<RigidNode_Base>();
            baseNode.ListAllNodes(nodes);

            foreach (RigidNode_Base node in nodes)
            {
                node.ModelFileName = ((RigidNode)node).group.ToString();
                node.ModelFullID = node.GetModelID();
            }
            //Cleaning Up...Done
            #endregion
            SetProgress("Done", occurrences.Count + 3, occurrences.Count + 3);
            return baseNode;
        }

        private void ExporterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ProgressLabel.Text = "An error occurred.";
                #region DEBUG SWITCH
#if DEBUG
                MessageBox.Show(e.Error.ToString());
#else
                MessageBox.Show(e.Error.Message);
#endif
                #endregion
                Close();
            }
            else
            {
                Close();
            }
        }
    }
}