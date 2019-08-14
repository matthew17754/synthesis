using Synthesis.FSM;
using Synthesis.GUI;
using Synthesis.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.GUI
{
    public class MultiplayerToolbarState : State
    {
        public enum TabState {
            Local,
            Host,
            Join
        }

        public TabState tabState { get; private set; }

        private GameObject canvas;
        private GameObject multiplayerToolbar;

        private GameObject localButton;
        private GameObject hostButton;
        private GameObject joinButton;

        public Sprite selectedButtonImage;
        public Sprite unselectedButtonImage;

        public override void Awake()
        {
            canvas = GameObject.Find("Canvas");
            multiplayerToolbar = Auxiliary.FindObject(canvas, "MultiplayerToolbar");
            localButton = Auxiliary.FindObject(multiplayerToolbar, "LocalMultiplayerButton");

            hostButton = Auxiliary.FindObject(multiplayerToolbar, "HostMultiplayerButton");
            joinButton = Auxiliary.FindObject(multiplayerToolbar, "JoinMultiplayerButton");

            selectedButtonImage = localButton.GetComponent<Button>().spriteState.highlightedSprite;
            unselectedButtonImage = localButton.GetComponent<Button>().image.sprite;

            tabState = TabState.Local;
        }

        public override void Update()
        {
            localButton.GetComponent<Image>().sprite = (tabState == TabState.Local) ? selectedButtonImage : unselectedButtonImage;
            hostButton.GetComponent<Image>().sprite = (tabState == TabState.Host) ? selectedButtonImage : unselectedButtonImage;
            joinButton.GetComponent<Image>().sprite = (tabState == TabState.Join) ? selectedButtonImage : unselectedButtonImage;
        }

        public void OnLocalMultiplayerButtonClicked()
        {
            tabState = TabState.Local;
        }

        public void OnHostMultiplayerButtonClicked()
        {
            tabState = TabState.Host;
        }

        public void OnJoinMultiplayerButtonClicked()
        {
            tabState = TabState.Join;
        }

        public void OnDisconnectMultiplayerButtonClicked()
        {
            // TODO
        }
    }
}
