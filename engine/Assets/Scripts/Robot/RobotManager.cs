using Synthesis.Camera;
using Synthesis.DriverPractice;
using Synthesis.MixAndMatch;
using Synthesis.Sensors;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Synthesis.Robot
{
    /// <summary>
    /// Manages all robot data, including loading robots
    /// </summary>
    public class RobotManager
    {
        private readonly string[] SampleRobotGUIDs = {
            "ee85355c-6daf-4588-ba47-cdf3f9143922",
            "fde5a9e9-4a1d-4d07-bafd-ae18bada7a8d",
            "d7f2959a-f9eb-4581-a4bb-898550193bda",
            "d1859211-db0f-4b75-866c-2d0e81b6732b",
            "52eb1ada-b051-461a-9cc4-1b5b74764ce5",
            "decdc6a1-5f76-4dea-add7-4c358f4a9921",
            "6b5d4484-db3c-425b-98b8-546c06d8d8bf",
            "c3bb1b94-dad8-4a8c-aa67-9c09eb9379c1",
            "ef4e3e2b-8cfb-437d-b63d-8bebc05fa3ba",
            "7d31cb8a-01e8-4eeb-9086-2955a993a374",
            "1478855a-60bd-42cb-8841-eece4fa0fbeb",
            "0b43729a-d8d3-4df2-bcbb-684343933c23",
            "9f19586c-a26f-4b28-9fb9-e06731178166",
            "f1225b7a-180e-456b-88d1-7315b0086001"
        };

        private RobotCameraManager robotCameraManager;
        private SensorManager sensorManager;
        private SensorManagerGUI sensorManagerGUI;

        private const int MAX_ROBOTS = Input.Player.PLAYER_COUNT;

        public SimulatorRobot[] Robots { get; private set; }

        public int MainRobotIndex { get; private set; }

        /// <summary>
        /// The main robot in this state.
        /// </summary>
        public SimulatorRobot MainRobot { get { return Robots[MainRobotIndex]; } private set { Robots[MainRobotIndex] = value; } }


        public RobotManager(RobotCameraManager camera, SensorManager sensor, SensorManagerGUI sensorGUI)
        {
            robotCameraManager = camera;
            sensorManager = sensor;
            sensorManagerGUI = sensorGUI;

            Robots = new SimulatorRobot[MAX_ROBOTS];
            MainRobotIndex = 0;
        }

        /// <summary>
        /// Count the number of non-null robots
        /// </summary>
        /// <returns></returns>
        public int GetRobotCount()
        {
            int count = 0;
            for(var i = 0; i < Robots.Length; i++)
            {
                if (CheckExists(i))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Check if a robot at a given index has been spawned 
        /// </summary>
        /// <param name="index"></param>
        /// <returns>True if it has been spawned</returns>
        public bool CheckExists(int index)
        {
            return Robots[index] != null;
        }

        /// <summary>
        /// Check if an index is within the robot array range
        /// </summary>
        /// <param name="index">The index to check</param>
        /// <returns>true if acceptable</returns>
        private bool ValidateRobotIndex(int index)
        {
            return index >= 0 && index < Robots.Length;
        }

        /// <summary>
        /// Condense the array of robots to a list of spawned robots
        /// </summary>
        /// <returns>A list containing the non-null robots</returns>
        public List<SimulatorRobot> GetSpawnedRobots()
        {
            return Robots.Where(robot => robot != null).ToList();
        }

        public bool LoadRobot(string directory, bool isMixAndMatch)
        {
            return LoadRobot(MainRobotIndex, directory, isMixAndMatch);
        }

        /// <summary>
        /// Loads a new robot from a given directory
        /// </summary>
        /// <param name="index">index of robot to load and assign</param>
        /// <param name="directory">robot directory</param>
        /// <param name="isMixAndMatch"></param>
        /// <returns>true on success</returns>
        public bool LoadRobot(int index, string directory, bool isMixAndMatch)
        {
            bool contains_skeleton = false;

            if (!Directory.Exists(directory))
            {
                return false;
            }
            else
            {
                string[] files = Directory.GetFiles(directory);
                foreach (string a in files)
                {
                    string name = Path.GetFileName(a);
                    if (name.ToLower().Contains("skeleton"))
                    {
                        contains_skeleton = true;
                    }
                }
            }

            if (!contains_skeleton)
            {
                return false;
            }

            if (!ValidateRobotIndex(index))
            {
                return false;
            }

            if (CheckExists(index))
            {
                return false;
            }

            GameObject robotObject = new GameObject("Robot");
            SimulatorRobot robot = null;

            if (isMixAndMatch)
            {
                MaMRobot mamRobot = robotObject.AddComponent<MaMRobot>();
                mamRobot.RobotHasManipulator = false; // Defaults to false
                robot = mamRobot;
                robot.FilePath = RobotTypeManager.RobotPath;

                if (AnalyticsManager.GlobalInstance != null)
                {
                    AnalyticsManager.GlobalInstance.LogEventAsync(AnalyticsLedger.EventCatagory.LoadRobot,
                        AnalyticsLedger.EventAction.Load,
                        "Robot - Mix and Match",
                        AnalyticsLedger.getMilliseconds().ToString());
                }
            }
            else
            {
                robot = robotObject.AddComponent<SimulatorRobot>();
                robot.FilePath = directory;

                if (AnalyticsManager.GlobalInstance != null)
                {
                    AnalyticsManager.GlobalInstance.LogEventAsync(AnalyticsLedger.EventCatagory.LoadRobot,
                        AnalyticsLedger.EventAction.Load,
                        "Robot - Exported",
                        AnalyticsLedger.getMilliseconds().ToString());
                }
            }

            //Initialiezs the physical robot based off of robot directory. Returns false if not sucessful
            if (!robot.InitializeRobot(robot.FilePath))
                return false;

            //If this is the first robot spawned, then set it to be the main robot and initialize the robot camera on it
            if (MainRobot == null)
            {
                MainRobot = robot;
            }

            robot.ControlIndex = index;
            Robots[index] = robot; // TODO check if null first?

            DPMDataHandler.Load(robot.FilePath);

            if (!isMixAndMatch && !PlayerPrefs.HasKey(robot.RootNode.GUID.ToString()) && !SampleRobotGUIDs.Contains(robot.RootNode.GUID.ToString()))
            {
                AnalyticsManager.GlobalInstance.LogEventAsync(AnalyticsLedger.EventCatagory.LoadRobot,
                    AnalyticsLedger.EventAction.Load,
                    robot.RootNode.GUID.ToString(),
                    AnalyticsLedger.getMilliseconds().ToString());
            }

            return true;
        }

        /// <summary>
        /// Loads a new robot and manipulator from given directorys
        /// </summary>
        /// <param name="index">index of robot to load and assign</param>
        /// <param name="baseDirectory">robot directory</param>
        /// <param name="manipulatorDirectory">manipulator directory</param>
        /// <returns>true on success</returns>
        public bool LoadRobotWithManipulator(int index, string baseDirectory, string manipulatorDirectory)
        {
            if (!ValidateRobotIndex(index))
                return false;

            GameObject robotObject = new GameObject("Robot");
            MaMRobot robot = robotObject.AddComponent<MaMRobot>();

            robot.FilePath = baseDirectory;

            //Initialiezs the physical robot based off of robot directory. Returns false if not sucessful
            if (!robot.InitializeRobot(robot.FilePath)) return false;

            //If this is the first robot spawned, then set it to be the main robot and initialize the robot camera on it
            if (MainRobot == null)
                MainRobot = robot;

            robot.ControlIndex = index;
            if(CheckExists(index))
            {
                RemoveRobot(index);
            }
            Robots[index] = robot;

            DPMDataHandler.Load(robot.FilePath);
            return robot.LoadManipulator(manipulatorDirectory);
        }

        /// <summary>
        /// Find the index of the next non-null robot
        /// </summary>
        /// <returns>the index of the next non-null robot</returns>
        public int GetNextMainRobotIndex()
        {
            int new_main_index = MainRobotIndex;
            while (true)
            {
                new_main_index++;
                if (new_main_index >= Robots.Length)
                {
                    new_main_index = 0;
                }
                if (CheckExists(new_main_index))
                {
                    break;
                }
            }
            return new_main_index;
        }

        /// <summary>
        /// Update the main robot index to the highest indexed non-null robot
        /// </summary>
        private void UpdateMainRobotIndex()
        {
            if(Robots[MainRobotIndex] == null)
            {
                for(var i = Robots.Length - 1; i >= 0; i--)
                {
                    if(Robots[i] != null)
                    {
                        MainRobotIndex = i;
                        return;
                    }
                }
                // Error
            }
        }

        /// <summary>
        /// Remove and delete the robot at a given index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveRobot(int index)
        {
            if (ValidateRobotIndex(index) && CheckExists(index))
            {
                //remove attached sensors/cameras
                robotCameraManager.RemoveCamerasFromRobot(Robots[index]);
                sensorManager.RemoveSensorsFromRobot(Robots[index]);
                sensorManagerGUI.ShiftOutputPanels();
                sensorManagerGUI.EndProcesses();

                MaMRobot mamRobot = Robots[index] as MaMRobot;

                if (mamRobot != null && mamRobot.RobotHasManipulator)
                    Object.Destroy(mamRobot.ManipulatorObject);

                Object.Destroy(Robots[index].gameObject);
                Robots[index] = null;
                UpdateMainRobotIndex();
            }
        }

        public bool ChangeRobot(string directory, bool isMixAndMatch)
        {
            return ChangeRobot(MainRobotIndex, directory, isMixAndMatch);
        }

        /// <summary>
        /// Changes the main robot to a new robot with a given directory
        /// </summary>
        /// <param name="index"></param>
        /// <param name="directory"></param>
        /// <param name="isMixAndMatch"></param>
        /// <returns>whether the process was successful</returns>
        public bool ChangeRobot(int index, string directory, bool isMixAndMatch)
        {
            RemoveRobot(index);

            if (LoadRobot(index, directory, isMixAndMatch))
            {
                DynamicCamera.ControlEnabled = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the main robot to a different robot based on a given index
        /// </summary>
        public void SetMainRobot(int index)
        {
            if (ValidateRobotIndex(index))
            {
                MainRobotIndex = index;
                DPMDataHandler.Load(MainRobot.FilePath); // Reload robot data to allow for driver practice for multiplayer
            }
        }

        /// <summary>
        /// Locks all <see cref="SimulatorRobot"/>s currently in the simulation.
        /// </summary>
        public void LockRobots()
        {
            foreach (SimulatorRobot robot in Robots)
            {
                if (robot != null)
                {
                    robot.LockRobot();
                }
            }
        }

        /// <summary>
        /// Unlocks all <see cref="SimulatorRobot"/>s currently in the simulation.
        /// </summary>
        public void UnlockRobots()
        {
            foreach (SimulatorRobot robot in Robots)
            {
                if (robot != null)
                {
                    robot.UnlockRobot();
                }
            }
        }
    }
}
