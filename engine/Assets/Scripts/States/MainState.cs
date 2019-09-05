using UnityEngine;
using System.Collections;
using BulletUnity;
using BulletSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Synthesis.FEA;
using Synthesis.FSM;
using System.Linq;
using UnityEngine.UI;
using Synthesis.BUExtensions;
using Synthesis.GUI;
using Synthesis.Input;
using Synthesis.MixAndMatch;
using Synthesis.Camera;
using Synthesis.Sensors;
using Synthesis.Utils;
using Synthesis.Robot;
using Synthesis.Field;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Newtonsoft.Json;
using Assets.Scripts;
using UnityEngine.SceneManagement;

namespace Synthesis.States
{
    /// <summary>
    /// This is the main class of the simulator; it handles all the initialization of robot and field objects within the simulator.
    /// Handles replay tracking and loading
    /// Handles interfaces between the SimUI and the main robot such as resetting, orienting, etc.
    /// </summary>
    public class MainState : State
    {
        private const int SolverIterations = 100;

        private BPhysicsWorld physicsWorld;
        private int lastFrameCount;

        private bool awaitingReplay;

        private DynamicCamera dynamicCamera;
        public GameObject DynamicCameraObject;

        private RobotCameraManager robotCameraManager;

        private SensorManager sensorManager;
        private SensorManagerGUI sensorManagerGUI;

        public CollisionTracker CollisionTracker { get; private set; }

        public FieldManager FieldManager { get; private set; }
        public RobotManager RobotManager { get; private set; }

        public bool IsMetric;

        public static List<List<GameObject>> spawnedGamepieces = new List<List<GameObject>>() { new List<GameObject>(), new List<GameObject>() };
        /// <summary>
        /// Called when the script instance is being initialized.
        /// Initializes the bullet physics environment
        /// </summary>
        public override void Awake()
        {
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("qualityLevel"));
            Screen.fullScreenMode = (FullScreenMode)PlayerPrefs.GetInt("fullscreen", 1);

            GameObject.Find("VersionNumber").GetComponent<Text>().text = "Version " + AppModel.Version;

            if (CheckConnection())
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
                var json = new WebClient().DownloadString("https://raw.githubusercontent.com/Autodesk/synthesis/master/VersionManager.json");
                VersionManager update = JsonConvert.DeserializeObject<VersionManager>(json);
                SimUI.updater = update.URL;

                var localVersion = new Version(AppModel.Version);
                var globalVersion = new Version(update.Version);

                var check = localVersion.CompareTo(globalVersion);

                if (check < 0)
                {
                    Auxiliary.FindGameObject("UpdatePrompt").SetActive(true);
                }
            }

            Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
            GImpactCollisionAlgorithm.RegisterAlgorithm((CollisionDispatcher)BPhysicsWorld.Get().world.Dispatcher);
            //BPhysicsWorld.Get().DebugDrawMode = DebugDrawModes.DrawWireframe | DebugDrawModes.DrawConstraints | DebugDrawModes.DrawConstraintLimits;
            BPhysicsWorld.Get().DebugDrawMode = DebugDrawModes.All;
            BPhysicsWorld.Get().DoDebugDraw = false;
            ((DynamicsWorld)BPhysicsWorld.Get().world).SolverInfo.NumIterations = SolverIterations;

            CollisionTracker = new CollisionTracker();

            sensorManager = GameObject.Find("SensorManager").GetComponent<SensorManager>();
            sensorManagerGUI = StateMachine.gameObject.GetComponent<SensorManagerGUI>();
            robotCameraManager = GameObject.Find("RobotCameraList").GetComponent<RobotCameraManager>();
            RobotManager = new RobotManager(robotCameraManager, sensorManager, sensorManagerGUI);
            FieldManager = new FieldManager();
        }

        /// <summary>
        /// Called after Awake() when the script instance is enabled.
        /// Initializes variables then loads the field and robot as well as setting up replay features.
        /// </summary>
        public override void Start()
        {
            AppModel.ClearError();

            //getting bullet physics information
            physicsWorld = BPhysicsWorld.Get();
            ((DynamicsWorld)physicsWorld.world).SetInternalTickCallback(BPhysicsTickListener.Instance.PhysicsTick);
            lastFrameCount = physicsWorld.frameCount;

            //setting up raycast robot tick callback
            BPhysicsTickListener.Instance.OnTick -= BRobotManager.Instance.UpdateRaycastRobots;
            BPhysicsTickListener.Instance.OnTick += BRobotManager.Instance.UpdateRaycastRobots;

            if (PlayerPrefs.GetString("simSelectedRobot", "").Equals(""))
            {
                AppModel.ErrorToMenu("ROBOT_SELECT|FIRST");
                return;
            }

            awaitingReplay = !string.IsNullOrEmpty(PlayerPrefs.GetString("simSelectedReplay"));

            //If a replay has been selected, load the replay. Otherwise, load the field and robot.
            if (!awaitingReplay)
            {
                if (!FieldManager.LoadField(PlayerPrefs.GetString("simSelectedField")))
                {
                    //AppModel.ErrorToMenu("FIELD_SELECT|FIRST");
                    AppModel.ErrorToMenu("FIELD_SELECT|Could not load field: \"" + PlayerPrefs.GetString("simSelectedField") + "\"\nHas it been moved or deleted?)");
                    return;
                }
                else
                {
                    FieldManager.MovePlane();
                }

                bool result = false;

                try
                {
                    result = RobotManager.LoadRobot(PlayerPrefs.GetString("simSelectedRobot"), false);
                }
                catch (Exception e)
                {
                    MonoBehaviour.Destroy(GameObject.Find("Robot"));
                }

                if (!result)
                {
                    AppModel.ErrorToMenu("ROBOT_SELECT|Could not find the selected robot \"" + PlayerPrefs.GetString("simSelectedRobot") + "\"");
                    return;
                }

                if (RobotTypeManager.IsMixAndMatch && RobotTypeManager.HasManipulator)
                {
                    MaMRobot mamRobot = RobotManager.MainRobot as MaMRobot;

                    if (mamRobot == null)
                    {
                        AppModel.ErrorToMenu("ROBOT_SELECT|Mix and match robot error");
                    }
                    else
                    {
                        Debug.Log(mamRobot.LoadManipulator(RobotTypeManager.ManipulatorPath) ? "Load manipulator success" : "Load manipulator failed");
                    }
                }
            }
            else
            {
                LoadReplay(PlayerPrefs.GetString("simSelectedReplay"));
            }

            //initializes the dynamic camera
            DynamicCameraObject = GameObject.Find("Main Camera");
            dynamicCamera = DynamicCameraObject.AddComponent<DynamicCamera>();
            DynamicCamera.ControlEnabled = true;

            IsMetric = PlayerPrefs.GetString("Measure").Equals("Metric");

            StateMachine.Link<MainState>(GameObject.Find("Main Camera").transform.GetChild(0).gameObject);
            StateMachine.Link<MainState>(GameObject.Find("Main Camera").transform.GetChild(1).gameObject, false);
            StateMachine.Link<ReplayState>(Auxiliary.FindGameObject("ReplayUI"));
            StateMachine.Link<SaveReplayState>(Auxiliary.FindGameObject("SaveReplayUI"));
            StateMachine.Link<GamepieceSpawnState>(Auxiliary.FindGameObject("ResetGamepieceSpawnpointUI"));
            StateMachine.Link<DefineNodeState>(Auxiliary.FindGameObject("DefineNodeUI"));
            StateMachine.Link<GoalState>(Auxiliary.FindGameObject("GoalStateUI"));
            StateMachine.Link<SensorSpawnState>(Auxiliary.FindGameObject("ResetSensorSpawnpointUI"));
            StateMachine.Link<DefineSensorAttachmentState>(Auxiliary.FindGameObject("DefineSensorAttachmentUI"));

            MediaManager.getInstance();

            Controls.Load();
            Controls.UpdateFieldControls();
            Controls.Save(true);
        }

        /// <summary>
        /// Called every step of the program to listen to input commands for various features
        /// </summary>
        public override void Update()
        {
            if (RobotManager.MainRobot == null)
            {
                AppModel.ErrorToMenu("ROBOT_SELECT|Robot instance not valid.");
                return;
            }

            if (RobotManager.MainRobot.transform.GetChild(0).transform.position.y < -10 || RobotManager.MainRobot.transform.GetChild(0).transform.position.y > 60)
            {
                RobotManager.MainRobot.Reset();
            }

            //Spawn a new robot from the same path or switch main robot
            if (!RobotManager.MainRobot.IsResetting)
            {
                foreach (var player in Controls.Players)
                {
                    if (InputControl.GetButtonDown(player.GetButtons().switchMainRobot))
                        RobotManager.SetMainRobot(RobotManager.GetNextMainRobotIndex());
                }
            }

            if (InputControl.GetButtonDown(Controls.Global.GetButtons().resetField))
            {
                Auxiliary.FindObject(GameObject.Find("Canvas"), "LoadingPanel").SetActive(true);
                FieldManager.ReloadField();

                AnalyticsManager.GlobalInstance.LogTimingAsync(AnalyticsLedger.TimingCatagory.MainSimulator,
                    AnalyticsLedger.TimingVarible.Playing,
                    AnalyticsLedger.TimingLabel.ChangeField);
            }

            // Toggles between the different camera states if the camera toggle button is pressed
            if (InputControl.GetButtonDown(Controls.Global.GetButtons().cameraToggle) && DynamicCameraObject.activeSelf && DynamicCamera.ControlEnabled)
                dynamicCamera.ToggleCameraState(dynamicCamera.ActiveState);

            // Switches to replay mode
            if (!RobotManager.MainRobot.IsResetting && InputControl.GetButtonDown(Controls.Global.GetButtons().replayMode))
            {
                StateMachine.PushState(new ReplayState(PlayerPrefs.GetString("simSelectedField"), CollisionTracker.ContactPoints));
            }
        }

        /// <summary>
        /// If a replay has been loaded, this is called at the end of the initialization process to switch to the replay state
        /// </summary>
        public override void LateUpdate()
        {
            if (awaitingReplay)
            {
                awaitingReplay = false;
                StateMachine.PushState(new ReplayState(PlayerPrefs.GetString("simSelectedField"), CollisionTracker.ContactPoints));
            }
        }

        public bool CheckConnection()
        {
            try
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

                using (client.OpenRead("https://raw.githubusercontent.com/Autodesk/synthesis/master/VersionManager.json"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }

        #region Replay Functions
        /// <summary>
        /// Loads the replay from the given replay file name.
        /// </summary>
        /// <param name="name"></param>
        void LoadReplay(string name)
        {
            PlayerPrefs.SetString("simSelectedReplay", "");
            ReplayImporter.Read(name, 
                out string fieldDirectory, 
                out List<FixedQueue<StateDescriptor>> fieldStates,
                out List<KeyValuePair<string, List<FixedQueue<StateDescriptor>>>> robotStates,
                out Dictionary<string, List<FixedQueue<StateDescriptor>>> gamePieceStates,
                out List<List<KeyValuePair<ContactDescriptor, int>>> contacts);

            if (string.IsNullOrEmpty(fieldDirectory) || !FieldManager.LoadField(fieldDirectory))
            {
                AppModel.ErrorToMenu("FIELD_SELECT|Could not load field: \"" + fieldDirectory + "\"\nHas it been moved or deleted?");
                return;
            }

            foreach (KeyValuePair<string, List<FixedQueue<StateDescriptor>>> rs in robotStates)
            {
                if (!RobotManager.LoadRobot(rs.Key, false))
                {
                    AppModel.ErrorToMenu("ROBOT_SELECT|Could not load robot: \"" + rs.Key + "\"\nHas it been moved or deleted?");
                    return;
                }

                int j = 0;

                foreach (Tracker t in RobotManager.GetSpawnedRobots().Last().GetComponentsInChildren<Tracker>())
                {
                    t.States = rs.Value[j];
                    j++;
                }
            }

            FieldManager.ApplyTrackers(fieldStates);

            foreach (KeyValuePair<string, List<FixedQueue<StateDescriptor>>> k in gamePieceStates)
            {
                GameObject referenceObject = GameObject.Find(k.Key);

                if (referenceObject == null)
                    continue;

                foreach (FixedQueue<StateDescriptor> f in k.Value)
                {
                    GameObject currentPiece = UnityEngine.Object.Instantiate(referenceObject);
                    currentPiece.name = k.Key + "(Clone)";
                    currentPiece.GetComponent<Tracker>().States = f;
                }
            }

            foreach (var c in contacts)
            {
                if (c != null)
                {
                    List<ContactDescriptor> currentContacts = new List<ContactDescriptor>();

                    foreach (var d in c)
                    {
                        ContactDescriptor currentContact = d.Key;
                        currentContact.RobotBody = RobotManager.MainRobot.transform.GetChild(d.Value).GetComponent<BRigidBody>();
                        currentContacts.Add(currentContact);
                    }

                    CollisionTracker.ContactPoints.Add(currentContacts);
                }
                else
                {
                    CollisionTracker.ContactPoints.Add(null);
                }
            }
        }

        /// <summary>
        /// Starts the replay state.
        /// </summary>
        public void EnterReplayState()
        {
            if (!RobotManager.MainRobot.IsResetting)
            {
                StateMachine.PushState(new ReplayState(PlayerPrefs.GetString("simSelectedField"), CollisionTracker.ContactPoints));
            }
            else
            {
                UserMessageManager.Dispatch("Please finish resetting before entering replay mode!", 5f);
            }
        }
        #endregion

        /// <summary>
        /// Resumes the normal simulation and exits the replay mode, showing all UI elements again
        /// </summary>
        public override void Resume()
        {
            lastFrameCount = physicsWorld.frameCount;
            CollisionTracker.Tracking = true;
        }

        /// <summary>
        /// Pauses the normal simulation for rpelay mode by disabling tracking of physics objects and disabling UI elements
        /// </summary>
        public override void Pause()
        {
            CollisionTracker.Tracking = false;
        }

        public UnityEngine.Camera GetCamera()
        {
            return DynamicCameraObject.GetComponent<UnityEngine.Camera>();
        }
    }
}
