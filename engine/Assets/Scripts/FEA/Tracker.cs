using Synthesis.FSM;
using BulletSharp;
using BulletUnity;
using Synthesis.BUExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Synthesis.States;

namespace Synthesis.FEA
{
    public class Tracker : LinkedMonoBehaviour<MainState>
    {
        /// <summary>
        /// The number of samples per second
        /// </summary>
        private const float SampleRate = 60f;

        /// <summary>
        /// The rigid body to track
        /// </summary>
        private RigidBody rigidBody;

        /// <summary>
        /// The collection of states stored by the Tracker.
        /// </summary>
        public FixedQueue<StateDescriptor> States { get; set; }

        /// <summary>
        /// The number of seconds the tracker keeps any given state.
        /// </summary>
        public const float Lifetime = 3f;

        /// <summary>
        /// The number of states in the queue.
        /// </summary>
        public const int Length = (int)(Lifetime * SampleRate);

        /// <summary>
        /// If true, lines will be drawn showing the history of the parent's motion.
        /// </summary>
        public bool Trace { get; set; }

        /// <summary>
        /// Returns a StateDescriptor based on the current frame.
        /// </summary>
        private StateDescriptor StateDescriptor
        {
            get
            {
                return new StateDescriptor
                {
                    Position = rigidBody.WorldTransform.Origin,
                    Rotation = rigidBody.WorldTransform.Basis,
                    LinearVelocity = rigidBody.LinearVelocity,
                    AngularVelocity = rigidBody.AngularVelocity
                };
            }
        }

        /// <summary>
        /// Clears all saved states with the current state.
        /// </summary>
        public void Clear()
        {
            States.Fill(StateDescriptor);
        }

        /// <summary>
        /// Adds the current state to the states queue.
        /// </summary>
        public void AddState(DynamicsWorld world, float timeStep, int framesPassed)
        {
            if (State == null || !CollisionTracker.Tracking)
                return;

            StateDescriptor nextState = StateDescriptor;

            if (framesPassed == 1) // This will be the case the vast majority of the time
            {
                States.Add(StateDescriptor);
            }
            else // If there's some random lag spike
            {
                StateDescriptor lastState = States.Front;
                float lerpAmount = 1f / framesPassed;

                for (int i = 0; i < framesPassed; i++)
                {
                    float percent = (i + 1) * lerpAmount;

                    States.Add(new StateDescriptor
                    {
                        Position = BulletSharp.Math.Vector3.Lerp(lastState.Position, nextState.Position, percent),
                        Rotation = BulletSharp.Math.Matrix.Lerp(lastState.Rotation, nextState.Rotation, percent),
                        LinearVelocity = BulletSharp.Math.Vector3.Lerp(lastState.LinearVelocity, nextState.LinearVelocity, percent),
                        AngularVelocity = BulletSharp.Math.Vector3.Lerp(lastState.AngularVelocity, nextState.AngularVelocity, percent)
                    });
                }
            }
        }

        /// <summary>
        /// Called when the Tracker is initialized.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            rigidBody = (RigidBody)GetComponent<BRigidBody>().GetCollisionObject();

            States = new FixedQueue<StateDescriptor>(Length, StateDescriptor);

            BPhysicsTickListener.Instance.OnTick += AddState;
        }

        /// <summary>
        /// Called when the Tracker is destroyed.
        /// </summary>
        protected void OnDestroy()
        {
            BPhysicsTickListener.Instance.OnTick -= AddState;
        }
    }
}