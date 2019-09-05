using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Synthesis.FSM;
using Synthesis.GUI;
using Synthesis.GUI.Scrollables;
using Synthesis.States;
using Synthesis.Utils;

namespace Synthesis.GUI
{

    /// <summary>
    /// Class for controlling the various aspects of local multiplayer
    /// </summary>
    public class LocalMultiplayer : LinkedMonoBehaviour<MainState>
    {
        private GameObject canvas;

        private GameObject mainMultiplayerWindow;
        private GameObject localMultiplayerWindow;
        private GameObject addRobotWindow;

        private Text robotNameLabel;

        public const int MAX_ROBOT_COUNT = Input.Player.PLAYER_COUNT;

        public int ActiveTab { get; private set; } = 0;

        private static Sprite selectedTabImage;
        private static Sprite defaultTabImage;

        private GameObject[] robotTabs = new GameObject[MAX_ROBOT_COUNT];

        /// <summary>
        /// Finds all the gameobjects and stores them in variables for efficiency
        /// </summary>
        private void Start()
        {
            canvas = GameObject.Find("Canvas");
            mainMultiplayerWindow = Auxiliary.FindObject(canvas, "MultiplayerPanel");
            localMultiplayerWindow = Auxiliary.FindObject(Auxiliary.FindObject(mainMultiplayerWindow, "LocalPanel"), "Panel");
            addRobotWindow = Auxiliary.FindObject(canvas, "AddRobotPanel");

            robotNameLabel = Auxiliary.FindObject(Auxiliary.FindObject(localMultiplayerWindow, "RobotName"), "Name").GetComponent<Text>();

            selectedTabImage = Resources.Load<Sprite>("Images/New Textures/greenButton");
            defaultTabImage = Resources.Load<Sprite>("Images/New Textures/TopbarHighlight");

            for (int i = 0; i < MAX_ROBOT_COUNT; i++)
            {
                int j = i; // Make a new reference
                robotTabs[j] = Auxiliary.FindObject(localMultiplayerWindow, "RobotTab" + (j + 1));
                robotTabs[j].GetComponent<Button>().onClick.AddListener(() =>
                {
                    ActiveTab = j;
                    SimUI.getSimUI().SetAddRobotPanelActive(State.RobotManager.Robots[j] == null);
                });
            }
        }

        /// <summary>
        /// Changes which robot is currently the main robot
        /// </summary>
        /// <param name="index">the index of the new main robot</param>
        public void ChangeMainRobot()
        {
            if (State.RobotManager.Robots[ActiveTab] != null)
            {
                State.RobotManager.SetMainRobot(ActiveTab);
                UpdateUI();
            }
        }

        /// <summary>
        /// Adds a new robot to the field based on user selection in the popup robot list window
        /// </summary>
        public void AddRobot()
        {
            AnalyticsManager.GlobalInstance.LogEventAsync(AnalyticsLedger.EventCatagory.AddRobot,
                AnalyticsLedger.EventAction.Clicked,
                "Local Multiplayer - Robot",
                AnalyticsLedger.getMilliseconds().ToString());

            GameObject panel = GameObject.Find("RobotListPanel");
            string directory = PlayerPrefs.GetString("RobotDirectory") + Path.DirectorySeparatorChar + panel.GetComponent<ChangeRobotScrollable>().selectedEntry;
            if (Directory.Exists(directory))
            {
                PlayerPrefs.SetString("simSelectedReplay", string.Empty);
                State.RobotManager.LoadRobot(ActiveTab, directory, false);
            }
            else
            {
                UserMessageManager.Dispatch("Robot directory not found!", 5);
            }
            ToggleAddRobotWindow();
            UpdateUI();

            PlayerPrefs.SetInt("hasManipulator", 0); //0 for false, 1 for true
        }

        /// <summary>
        /// Adds a new robot to the field based on user selection in the popup robot list window
        /// </summary>
        public void AddMaMRobot(string baseDirectory, string manipulatorDirectory, bool hasManipulator)
        {
            if (hasManipulator)
                State.RobotManager.LoadRobotWithManipulator(ActiveTab, baseDirectory, manipulatorDirectory);
            else
                State.RobotManager.LoadRobot(ActiveTab, baseDirectory, true);

            UpdateUI();
        }

        /// <summary>
        /// Removes a robot from the field and shifts the indexes to remove any gaps
        /// </summary>
        public void RemoveRobot()
        {
            if (State.RobotManager.GetRobotCount() > 1)
            {
                State.RobotManager.RemoveRobot(ActiveTab);

                ActiveTab = State.RobotManager.GetNextMainRobotIndex();

                UpdateUI();
            }
            else
            {
                UserMessageManager.Dispatch("Cannot delete. Must have at least one robot on field.", 5);
            }
        }

        /// <summary>
        /// Toggles the popup add robot window
        /// </summary>
        public void ToggleAddRobotWindow()
        {
            if (addRobotWindow.activeSelf)
            {
                addRobotWindow.SetActive(false);
                DynamicCamera.ControlEnabled = true;
            }
            else
            {
                addRobotWindow.SetActive(true);
            }
        }

        /// <summary>
        /// Updates the multiplayer window to reflect changes in indexes, controls, etc.
        /// </summary>
        public void UpdateUI()
        {
            if (State.RobotManager.Robots[ActiveTab] != null)
            { // Only update once adding robot completes or is abandoned
              // Update tabs
                for (int i = 0; i < MAX_ROBOT_COUNT; i++)
                {
                    robotTabs[i].GetComponent<Image>().sprite = (i == ActiveTab) ? selectedTabImage : defaultTabImage;
                    if (State.RobotManager.Robots[i] != null)
                    {
                        robotTabs[i].GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1);
                    }
                    else
                    {
                        robotTabs[i].GetComponentInChildren<Text>().color = new Color(.8f, .8f, .8f, .5f);
                    }
                }

                // Update configuration window
                robotNameLabel.text = State.RobotManager.Robots[ActiveTab].RobotName;
            }

            for (int i = 0; i < MAX_ROBOT_COUNT; i++)
            {
                Auxiliary.FindObject(robotTabs[i], "MainRobotImage").SetActive(i == State.RobotManager.MainRobotIndex);
            }
        }

        /// <summary>
        /// Permanently hides the multiplayer tooltip
        /// </summary>
        public void HideTooltip()
        {
            Auxiliary.FindObject(localMultiplayerWindow, "MultiplayerTooltip").SetActive(false);
        }
    }
}