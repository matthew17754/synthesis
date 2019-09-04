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
        public const string CurrentVersion = "4.3.1.1";

        private const int SolverIterations = 100;

        private BPhysicsWorld physicsWorld;
        private int lastFrameCount;

        public bool Tracking { get; private set; }
        private bool awaitingReplay;

        private DynamicCamera dynamicCamera;
        public GameObject DynamicCameraObject;

        private RobotCameraManager robotCameraManager;

        private SensorManager sensorManager;
        private SensorManagerGUI sensorManagerGUI;


        public CollisionTracker CollisionTracker { get; private set; }

        public FieldManager fieldManager { get; private set; }
        public RobotManager robotManager { get; private set; }

        public bool IsMetric;

        bool reset;

        public static List<List<GameObject>> spawnedGamepieces = new List<List<GameObject>>() { new List<GameObject>(), new List<GameObject>() };
        /// <summary>
        /// Called when the script instance is being initialized.
        /// Initializes the bullet physics environment
        /// </summary>
        public override void Awake()
        {
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("qualityLevel"));
            Screen.fullScreenMode = (FullScreenMode)PlayerPrefs.GetInt("fullscreen", 1);

            GameObject.Find("VersionNumber").GetComponent<Text>().text = "Version " + CurrentVersion;

            if (CheckConnection())
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
                var json = new WebClient().DownloadString("https://raw.githubusercontent.com/Autodesk/synthesis/master/VersionManager.json");
                VersionManager update = JsonConvert.DeserializeObject<VersionManager>(json);
                SimUI.updater = update.URL;

                var localVersion = new Version(CurrentVersion);
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

            CollisionTracker = new CollisionTracker(this);

            sensorManager = GameObject.Find("SensorManager").GetComponent<SensorManager>();
            sensorManagerGUI = StateMachine.gameObject.GetComponent<SensorManagerGUI>();
            robotCameraManager = GameObject.Find("RobotCameraList").GetComponent<RobotCameraManager>();
            robotManager = new RobotManager(robotCameraManager, sensorManager, sensorManagerGUI);
            fieldManager = new FieldManager();
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

            //If a replay has been selected, load the replay. Otherwise, load the field and robot.
            string selectedReplay = PlayerPrefs.GetString("simSelectedReplay");

            if (PlayerPrefs.GetString("simSelectedRobot", "").Equals(""))
            {
                AppModel.ErrorToMenu("ROBOT_SELECT|FIRST");
                return;
            }

            if (string.IsNullOrEmpty(selectedReplay))
            {
                Tracking = true;

                if (!fieldManager.LoadField(PlayerPrefs.GetString("simSelectedField")))
                {
                    //AppModel.ErrorToMenu("FIELD_SELECT|FIRST");
                    AppModel.ErrorToMenu("FIELD_SELECT|Could not load field: \"" + PlayerPrefs.GetString("simSelectedField") + "\"\nHas it been moved or deleted?)");
                    return;
                }
                else
                {
                    fieldManager.MovePlane();
                }

                bool result = false;

                try
                {
                    result = robotManager.LoadRobot(PlayerPrefs.GetString("simSelectedRobot"), false);
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

                reset = FieldDataHandler.robotSpawn == new Vector3(99999, 99999, 99999);

                if (RobotTypeManager.IsMixAndMatch && RobotTypeManager.HasManipulator)
                {
                    Debug.Log(LoadManipulator(RobotTypeManager.ManipulatorPath) ? "Load manipulator success" : "Load manipulator failed");
                }
            }
            else
            {
                awaitingReplay = true;
                PlayerPrefs.SetString("simSelectedReplay", "");
                LoadReplay(selectedReplay);
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
            if (robotManager.MainRobot == null)
            {
                AppModel.ErrorToMenu("ROBOT_SELECT|Robot instance not valid.");
                return;
            }

            if (robotManager.MainRobot.transform.GetChild(0).transform.position.y < -10 || robotManager.MainRobot.transform.GetChild(0).transform.position.y > 60)
            {
                BeginRobotReset();
                EndRobotReset();
            }

            if (reset)
            {
                BeginRobotReset();
                reset = false;
            }

            //Spawn a new robot from the same path or switch main robot
            if (!robotManager.MainRobot.IsResetting)
            {
                foreach (var player in Controls.Players)
                {
                    if (InputControl.GetButtonDown(player.GetButtons().switchMainRobot)) robotManager.SetMainRobot(robotManager.GetNextMainRobotIndex());
                }
            }


            if (InputControl.GetButtonDown(Controls.Global.GetButtons().resetField))
            {
                Auxiliary.FindObject(GameObject.Find("Canvas"), "LoadingPanel").SetActive(true);
                SceneManager.LoadScene("Scene");

                AnalyticsManager.GlobalInstance.LogTimingAsync(AnalyticsLedger.TimingCatagory.MainSimulator,
                    AnalyticsLedger.TimingVarible.Playing,
                    AnalyticsLedger.TimingLabel.ChangeField);
            }

            // Toggles between the different camera states if the camera toggle button is pressed
            if ((InputControl.GetButtonDown(Controls.Global.GetButtons().cameraToggle)) &&
                DynamicCameraObject.activeSelf && DynamicCamera.ControlEnabled)
                dynamicCamera.ToggleCameraState(dynamicCamera.ActiveState);

            // Switches to replay mode
            if (!robotManager.MainRobot.IsResetting && InputControl.GetButtonDown(Controls.Global.GetButtons().replayMode))
            {
                CollisionTracker.ContactPoints.Add(null);
                StateMachine.PushState(new ReplayState(PlayerPrefs.GetString("simSelectedField"), CollisionTracker.ContactPoints));
            }
        }

        /// <summary>
        /// Called at a fixed rate - updates robot packet information.
        /// </summary>
        public override void FixedUpdate()
        {
            //This line is essential for the reset to work accurately
            //robotCameraObject.transform.position = activeRobot.transform.GetChild(0).transform.position;
            if (robotManager.MainRobot == null)
            {
                AppModel.ErrorToMenu("ROBOT_SELECT|Robot instance not valid.");
                return;
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

        public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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

        /// <summary>
        /// Used to delete manipulator nodes in MaM mode
        /// </summary>
        public void DeleteManipulatorNodes()
        {
            MaMRobot mamRobot = robotManager.MainRobot as MaMRobot;
            mamRobot?.DeleteManipulatorNodes();
        }

        #region Replay Functions
        /// <summary>
        /// Loads the replay from the given replay file name.
        /// </summary>
        /// <param name="name"></param>
        void LoadReplay(string name)
        {
            ReplayImporter.Read(name, 
                out string fieldDirectory, 
                out List<FixedQueue<StateDescriptor>> fieldStates,
                out List<KeyValuePair<string, List<FixedQueue<StateDescriptor>>>> robotStates,
                out Dictionary<string, List<FixedQueue<StateDescriptor>>> gamePieceStates,
                out List<List<KeyValuePair<ContactDescriptor, int>>> contacts);

            if (string.IsNullOrEmpty(fieldDirectory) || !fieldManager.LoadField(fieldDirectory))
            {
                AppModel.ErrorToMenu("FIELD_SELECT|Could not load field: \"" + fieldDirectory + "\"\nHas it been moved or deleted?");
                return;
            }

            foreach (KeyValuePair<string, List<FixedQueue<StateDescriptor>>> rs in robotStates)
            {
                if (!robotManager.LoadRobot(rs.Key, false))
                {
                    AppModel.ErrorToMenu("ROBOT_SELECT|Could not load robot: \"" + rs.Key + "\"\nHas it been moved or deleted?");
                    return;
                }

                int j = 0;

                foreach (Tracker t in robotManager.GetSpawnedRobots().Last().GetComponentsInChildren<Tracker>())
                {
                    t.States = rs.Value[j];
                    j++;
                }
            }

            fieldManager.ApplyTrackers(fieldStates);

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
                        currentContact.RobotBody = robotManager.MainRobot.transform.GetChild(d.Value).GetComponent<BRigidBody>();
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
        /// Loads a manipulator for Mix and Match mode and maps it to the robot. 
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public bool LoadManipulator(string directory)
        {
            MaMRobot mamRobot = robotManager.MainRobot as MaMRobot;

            if (mamRobot == null)
                return false;

            mamRobot.RobotHasManipulator = true;
            return mamRobot.InitializeManipulator(directory);
        }

        /// <summary>
        /// Resumes the normal simulation and exits the replay mode, showing all UI elements again
        /// </summary>
        public override void Resume()
        {
            lastFrameCount = physicsWorld.frameCount;
            Tracking = true;

            CollisionTracker.Reset();
        }

        /// <summary>
        /// Pauses the normal simulation for rpelay mode by disabling tracking of physics objects and disabling UI elements
        /// </summary>
        public override void Pause()
        {
            Tracking = false;
        }

        /// <summary>
        /// Starts the replay state.
        /// </summary>
        public void EnterReplayState()
        {
            if (!robotManager.MainRobot.IsResetting)
            {
                CollisionTracker.ContactPoints.Add(null);
                StateMachine.PushState(new ReplayState(PlayerPrefs.GetString("simSelectedField"), CollisionTracker.ContactPoints));
            }
            else
            {
                UserMessageManager.Dispatch("Please finish resetting before entering replay mode!", 5f);
            }
        }
        #endregion


        #region Robot Interaction Functions

        public void RevertSpawnpoint()
        {
            robotManager.MainRobot.BeginRevertSpawnpoint();
        }

        /// <summary>
        /// Starts the resetting process of the main robot
        /// </summary>
        public void BeginRobotReset()
        {
            robotManager.MainRobot.BeginReset();
        }

        /// <summary>
        /// Ends the restting process of the main robot and resets the replay tracking objects
        /// </summary>
        public void EndRobotReset()
        {
            robotManager.MainRobot.EndReset();
            foreach (Tracker t in UnityEngine.Object.FindObjectsOfType<Tracker>())
            {
                t.Clear();
                CollisionTracker.Reset();
            }
        }

        /// <summary>
        /// Shifts the main robot by a set transposition vector
        /// </summary>
        public void TranslateRobot(Vector3 transposition)
        {
            robotManager.MainRobot.TranslateRobot(transposition);
        }

        /// <summary>
        /// Rotates the main robot about its origin by a mathematical 4x4 matrix
        /// </summary>
        public void RotateRobot(BulletSharp.Math.Matrix rotationMatrix)
        {
            robotManager.MainRobot.RotateRobot(rotationMatrix);
        }

        /// <summary>
        /// Rotates the main robot about its origin by a set vector
        /// </summary>
        public void RotateRobot(Vector3 rotation)
        {
            robotManager.MainRobot.RotateRobot(rotation);
        }

        /// <summary>
        /// Resets the main robot orientation to how the CAD model was originally defined (should be standing upright and facing forward if CAD was done properly)
        /// </summary>
        public void ResetRobotOrientation()
        {
            robotManager.MainRobot.ResetRobotOrientation();
        }
        #endregion

        public UnityEngine.Camera GetCamera()
        {
            return DynamicCameraObject.GetComponent<UnityEngine.Camera>();
        }
    }
}
