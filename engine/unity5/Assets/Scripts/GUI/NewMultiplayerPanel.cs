using Synthesis.Input;
using Synthesis.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synthesis.GUI
{
    class NewMultiplayerPanel : MonoBehaviour
    {
        public static NewMultiplayerPanel Instance { get; private set; }

        private GameObject canvas;
        private GameObject mainPanel;

        private GameObject localPanel;

        private GameObject hostPanel;

        private GameObject joinPanel;
        private GameObject serverViewport;

        public GameObject ServerEntryPrefab;

        public struct HostGameProperties
        {
            public InputField name;
            public InputField id;
            public InputField password;
            public InputField capactiy;
            public InputField tags;
            public const int MAX_CAPACITY = 12;
            public const int MIN_CAPACITY = 1;
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

            public ServerEntry(string nameValue, string idValue, int capacityValue, string versionValue, string tagsValue, string statusValue)
            {
                gameObject = Instantiate(Instance.ServerEntryPrefab, Instance.serverViewport.transform);
                gameObject.name = "Server Entry";

                name = Auxiliary.FindObject(gameObject, "Name").GetComponent<InputField>();
                name.text = nameValue;
                id = Auxiliary.FindObject(gameObject, "ID").GetComponent<InputField>();
                id.text = idValue;
                capacity = Auxiliary.FindObject(gameObject, "Capacity").GetComponent<InputField>();
                capacity.text = capacityValue.ToString();
                version = Auxiliary.FindObject(gameObject, "Version").GetComponent<InputField>();
                version.text = versionValue;
                tags = Auxiliary.FindObject(gameObject, "Tags").GetComponent<InputField>();
                tags.text = tagsValue;
                status = Auxiliary.FindObject(gameObject, "Status").GetComponent<InputField>();
                status.text = statusValue;
            }
        }

        private HostGameProperties hostGameProperties;
        private List<ServerEntry> serverList = new List<ServerEntry>();

        private Rect lastSetPixelRect;
        private Assets.Scripts.GUI.MultiplayerToolbarState.TabState? lastTabState;

        public void Awake()
        {
            Instance = this;

            canvas = GameObject.Find("Canvas");
            mainPanel = Auxiliary.FindObject(canvas, "NewMultiplayerPanel");

            localPanel = Auxiliary.FindObject(canvas, "LocalPanel");
            hostPanel = Auxiliary.FindObject(canvas, "HostPanel");
            joinPanel = Auxiliary.FindObject(canvas, "JoinPanel");

            serverViewport = Auxiliary.FindObject(joinPanel, "Content");

            lastTabState = Assets.Scripts.GUI.MultiplayerToolbarState.TabState.Local;

            hostGameProperties.name = Auxiliary.FindObject(hostPanel, "Name").GetComponentInChildren<InputField>();
            hostGameProperties.id = Auxiliary.FindObject(hostPanel, "ID").GetComponentInChildren<InputField>();
            hostGameProperties.password = Auxiliary.FindObject(hostPanel, "Password").GetComponentInChildren<InputField>();
            hostGameProperties.capactiy = Auxiliary.FindObject(hostPanel, "Capacity").GetComponentInChildren<InputField>();
            hostGameProperties.tags = Auxiliary.FindObject(hostPanel, "Tags").GetComponentInChildren<InputField>();

            hostGameProperties.capactiy.onValidateInput += (input, charIndex, addedChar) => {
                if(addedChar == '-' || !int.TryParse(addedChar.ToString(), out int dummy)) // Ensure is positive integer
                {
                    return '\0';
                }
                if(int.Parse(input + addedChar) > HostGameProperties.MAX_CAPACITY) // Ensure is in valid range
                {
                    hostGameProperties.capactiy.text = HostGameProperties.MAX_CAPACITY.ToString();
                    UserMessageManager.Dispatch("Maximum capacity is " + HostGameProperties.MAX_CAPACITY, 3);
                    return '\0';
                }
                if (int.Parse(input + addedChar) < HostGameProperties.MIN_CAPACITY) // Ensure is in valid range
                {
                    hostGameProperties.capactiy.text = HostGameProperties.MIN_CAPACITY.ToString();
                    UserMessageManager.Dispatch("Minimum capacity is " + HostGameProperties.MIN_CAPACITY, 3);
                    return '\0';
                }
                return addedChar;
            };

            AddServerEntry("", "", 1, "", "", ""); // TODO - for testing
        }

        public void Update()
        {
            mainPanel.SetActive(SimUI.getSimUI().getTabStateMachine().CurrentState is Assets.Scripts.GUI.MultiplayerToolbarState);

            InputControl.freeze = mainPanel.activeSelf; // TODO not all controls use InputControl (can be freezed)
            DynamicCamera.ControlEnabled = !mainPanel.activeSelf;

            if (mainPanel.activeSelf) // Update rest of UI
            {

                var tabState = ((Assets.Scripts.GUI.MultiplayerToolbarState)SimUI.getSimUI().getTabStateMachine().CurrentState).tabState;

                localPanel.SetActive(tabState == Assets.Scripts.GUI.MultiplayerToolbarState.TabState.Local);
                hostPanel.SetActive(tabState == Assets.Scripts.GUI.MultiplayerToolbarState.TabState.Host);
                joinPanel.SetActive(tabState == Assets.Scripts.GUI.MultiplayerToolbarState.TabState.Join);

                if(lastTabState != tabState)
                {
                    if (hostPanel.activeSelf && hostGameProperties.name.text == "")
                    {
                        hostGameProperties.name.Select();
                    }
                }
                lastTabState = tabState;
            }
        }

        private void Resize(bool forceUpdate = false)
        {
            if (!lastSetPixelRect.Equals(UnityEngine.Camera.main.pixelRect) || forceUpdate)
            {
            }
        }

        private void AddServerEntry(string name, string id, int capacity, string version, string tags, string status)
        {
            serverList.Add(new ServerEntry(name, id, capacity, version, tags, status)); // TODO
        }
    }
}