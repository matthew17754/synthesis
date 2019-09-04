using Synthesis.FEA;
using Synthesis.GUI;
using Synthesis.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Synthesis.Field
{
    /// <summary>
    /// Manages field object data, including loading fields
    /// </summary>
    public class FieldManager
    {
        private GameObject fieldObject = null;
        private UnityFieldDefinition fieldDefinition = null;

        public FieldManager()
        {

        }

        /// <summary>
        /// Loads the field from a given directory
        /// </summary>
        /// <param name="directory">field directory</param>
        /// <returns>whether the process was successful</returns>
        public bool LoadField(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                UserMessageManager.Dispatch("Field not found", 7);
            }

            fieldObject = new GameObject("Field");

            FieldDefinition.Factory = delegate (Guid guid, string name)
            {
                return new UnityFieldDefinition(guid, name);
            };

            bool isEmptyGrid = directory == "" || new DirectoryInfo(directory).Name == UnityFieldDefinition.EmptyGridName;

            if (!File.Exists(directory + Path.DirectorySeparatorChar + "definition.bxdf") && !isEmptyGrid)
                return false;

            FieldDataHandler.LoadFieldMetaData(directory);

            Controls.Load();
            Controls.UpdateFieldControls();
            if (!Controls.HasBeenSaved())
                Controls.Save(true);

            if (isEmptyGrid)
            {
                return true;
            }
            fieldDefinition = (UnityFieldDefinition)BXDFProperties.ReadProperties(directory + Path.DirectorySeparatorChar + "definition.bxdf", out string loadResult);
            // Debug.Log("Field load result: " + loadResult);
            fieldDefinition.CreateTransform(fieldObject.transform);
            return fieldDefinition.CreateMesh(directory + Path.DirectorySeparatorChar + "mesh.bxda");
        }

        public void MovePlane()
        {
            float? lowPoint = null;
            foreach (MeshRenderer singleMesh in fieldObject.GetComponentsInChildren<MeshRenderer>())
            {
                if (lowPoint == null || singleMesh.bounds.min.y < lowPoint)
                {
                    lowPoint = singleMesh.bounds.min.y;
                }
            }

            GameObject.Find("Environment").transform.position = new Vector3(0, lowPoint ?? 0, 0);
        }

        public void ApplyTrackers(List<FixedQueue<StateDescriptor>> fieldStates)
        {
            if (fieldObject.GetComponentsInChildren<Tracker>().Count() != fieldStates.Count)
            {
                throw new Exception("Mismatched lengths between field trackers and replay field states");
            }

            for (int i = 0; i < fieldObject.GetComponentsInChildren<Tracker>().Count(); ++i)
                fieldObject.GetComponentsInChildren<Tracker>()[i].States = fieldStates[i];
        }
    }
}
