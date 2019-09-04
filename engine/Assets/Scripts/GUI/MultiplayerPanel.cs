using Synthesis.Input;
using Synthesis.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synthesis.GUI
{
    class MultiplayerPanel : MonoBehaviour
    {
        public static MultiplayerPanel Instance { get; private set; }

        private GameObject canvas;
        private GameObject mainPanel;

        private GameObject localPanel;

        private GameObject hostPanel;

        private GameObject joinPanel;
        private GameObject serverViewport;

        public GameObject ServerEntryPrefab;

        public class HostGameProperties
        {
            private InputField id;
            private InputField name;
            private InputField password;
            private InputField capactiy;
            private InputField tags;
            public const int MAX_CAPACITY = 12;
            public const int MIN_CAPACITY = 1;

            public HostGameProperties(GameObject parent)
            {
                name = Auxiliary.FindObject(parent, "Name").GetComponentInChildren<InputField>();
                id = Auxiliary.FindObject(parent, "ID").GetComponentInChildren<InputField>();
                password = Auxiliary.FindObject(parent, "Password").GetComponentInChildren<InputField>();
                capactiy = Auxiliary.FindObject(parent, "Capacity").GetComponentInChildren<InputField>();
                tags = Auxiliary.FindObject(parent, "Tags").GetComponentInChildren<InputField>();

                capactiy.onValidateInput += (input, charIndex, addedChar) => {
                    if (addedChar == '-' || !int.TryParse(addedChar.ToString(), out int dummy)) // Ensure is positive integer
                    {
                        return '\0';
                    }
                    if (int.Parse(input + addedChar) > MAX_CAPACITY) // Ensure is in valid range
                    {
                        capactiy.text = MAX_CAPACITY.ToString();
                        UserMessageManager.Dispatch("Maximum capacity is " + MAX_CAPACITY, 3);
                        return '\0';
                    }
                    if (int.Parse(input + addedChar) < MIN_CAPACITY) // Ensure is in valid range
                    {
                        capactiy.text = MIN_CAPACITY.ToString();
                        UserMessageManager.Dispatch("Minimum capacity is " + MIN_CAPACITY, 3);
                        return '\0';
                    }
                    return addedChar;
                };
            }

            public string GetName()
            {
                return name.text;
            }

            public string GetID()
            {
                return id.text;
            }

            public void SetID(string i)
            {
                id.text = i;
            }

            public string GetPassword()
            {
                return password.text;
            }

            public int GetCapacity()
            {
                return int.Parse(capactiy.text);
            }

            public string GetTags()
            {
                return tags.text;
            }

            public void SelectFirstEmptyProperty()
            {
                if (name.text == "")
                {
                    name.Select();
                }
                else if (password.text == "")
                {
                    password.Select();
                }
                else if (capactiy.text == "")
                {
                    capactiy.Select();
                }
                else if (tags.text == "")
                {
                    tags.Select();
                }
            }
        }

        public class ServerEntry
        {
            public InputField name;
            public InputField id;
            public InputField capacity;
            public InputField version;
            public InputField tags;
            public InputField status;

            public GameObject gameObject;

            private int maxCapacity = HostGameProperties.MAX_CAPACITY;

            public ServerEntry(GameObject parent, string nameValue, string idValue, int capacityValue, string versionValue, string tagsValue, string statusValue)
            {
                gameObject = Instantiate(Instance.ServerEntryPrefab, parent.transform);
                gameObject.name = "Server Entry";

                name = Auxiliary.FindObject(gameObject, "Name").GetComponent<InputField>();
                name.text = nameValue;

                id = Auxiliary.FindObject(gameObject, "ID").GetComponent<InputField>();
                id.text = idValue;

                capacity = Auxiliary.FindObject(gameObject, "Capacity").GetComponent<InputField>();
                maxCapacity = Math.Min(Math.Max(capacityValue, 0), HostGameProperties.MAX_CAPACITY);

                SetCurrentUserCount(0);
                version = Auxiliary.FindObject(gameObject, "Version").GetComponent<InputField>();
                version.text = versionValue;

                tags = Auxiliary.FindObject(gameObject, "Tags").GetComponent<InputField>();
                tags.text = tagsValue;

                status = Auxiliary.FindObject(gameObject, "Status").GetComponent<InputField>();
                UpdateStatus(statusValue);
            }

            public void SetCurrentUserCount(int count)
            {
                capacity.text = count.ToString() + "/" + maxCapacity.ToString();
            }

            public void UpdateStatus(string statusValue)
            {
                status.text = statusValue;
            }
        }

        private LocalMultiplayer localMultiplayer;
        private HostGameProperties hostGameProperties;
        private List<ServerEntry> serverList;

        private Rect lastSetPixelRect;
        private bool lastActive;
        private MultiplayerToolbarState.TabState? lastTabState;

        public void Awake()
        {
            Instance = this;

            canvas = GameObject.Find("Canvas");
            mainPanel = Auxiliary.FindObject(canvas, "MultiplayerPanel");

            localPanel = Auxiliary.FindObject(canvas, "LocalPanel");
            hostPanel = Auxiliary.FindObject(canvas, "HostPanel");
            joinPanel = Auxiliary.FindObject(canvas, "JoinPanel");

            localMultiplayer = FSM.StateMachine.SceneGlobal.GetComponent<LocalMultiplayer>();
            serverViewport = Auxiliary.FindObject(joinPanel, "Content");
            hostGameProperties = new HostGameProperties(hostPanel);
            serverList = new List<ServerEntry>();

            lastTabState = MultiplayerToolbarState.DEFAULT_TAB_STATE;

            for (int i = 0; i < 20; i++)
            {
                AddServerEntry("Test Lobby " + i, "12334", 12, States.MainState.CurrentVersion, "test-field", "Connecting"); // TODO - for testing
            }
        }

        public void Update()
        {
            SetActive(SimUI.getSimUI().getTabStateMachine() != null && 
                SimUI.getSimUI().getTabStateMachine().CurrentState is MultiplayerToolbarState);

            if (mainPanel.activeSelf) // Update rest of UI
            {

                var tabState = ((MultiplayerToolbarState)SimUI.getSimUI().getTabStateMachine().CurrentState).tabState;

                localPanel.SetActive(tabState == MultiplayerToolbarState.TabState.Local);
                hostPanel.SetActive(tabState == MultiplayerToolbarState.TabState.Host);
                joinPanel.SetActive(tabState == MultiplayerToolbarState.TabState.Join);

                if (tabState == MultiplayerToolbarState.TabState.Local)
                {

                    localMultiplayer.UpdateUI();
                }

                if(lastTabState != tabState)
                {
                    if (hostPanel.activeSelf)
                    {
                        hostGameProperties.SelectFirstEmptyProperty();
                    }
                }
                lastTabState = tabState;

                InputControl.DisableSimControls(); // TODO not all controls use InputControl (can be freezed)
            }
            else if(lastActive)
            {
                InputControl.EnableSimControls();
            }

            lastActive = mainPanel.activeSelf;
        }

        public void SetActive(bool value)
        {
            mainPanel.SetActive(value);
            if (mainPanel.activeSelf && !lastActive)
            {
                SimUI.getSimUI().EndOtherProcesses();
            }
        }

        private void Resize(bool forceUpdate = false)
        {
            if (!lastSetPixelRect.Equals(UnityEngine.Camera.main.pixelRect) || forceUpdate)
            {
                // TODO
            }
        }

        private void AddServerEntry(string name, string id, int capacity, string version, string tags, string status)
        {
            serverList.Add(new ServerEntry(serverViewport, name, id, capacity, version, tags, status)); // TODO
        }
    }
}