//========= Copyright 2016, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public enum ButtonEventType
    {
        Down,
        Press,
        Up,
        Click,
    }

    public enum ControllerButton
    {
        Trigger,
        Pad,
        Grip,
        PadTouch,
        Menu,
        HairTrigger,
        FullTrigger,
    }

    // Singleton that manage and update controllers input.
    public partial class ViveInput : MonoBehaviour
    {
        public const int CONTROLLER_BUTTON_COUNT = 7;

        private static ViveInput instance = null;
        private static bool isApplicationQuitting = false;
        private static readonly ControllerState emptyState = new ControllerState(HandRole.RightHand);
        private static readonly ControllerState[] roleStates = new ControllerState[ViveRole.HAND_ROLE_COUNT];
        private static float m_clickInterval = 0.3f;

        public static bool Active { get { return instance != null; } }

        public static float clickInterval
        {
            get { return m_clickInterval; }
            set { m_clickInterval = Mathf.Max(0f, value); }
        }

        public static ViveInput Instance
        {
            get
            {
                Initialize();
                return instance;
            }
        }

        public static void Initialize()
        {
            if (Active || isApplicationQuitting) { return; }

            var instances = FindObjectsOfType<ViveInput>();
            if (instances.Length > 0)
            {
                instance = instances[0];
                if (instances.Length > 1) { Debug.LogWarning("Multiple ViveInput not supported!"); }
            }

            if (!Active)
            {
                instance = new GameObject("[ViveInput]").AddComponent<ViveInput>();
            }

            if (Active)
            {
                DontDestroyOnLoad(instance.gameObject);
                for (int i = roleStates.Length - 1; i >= 0; --i)
                {
                    roleStates[i] = new ControllerState((HandRole)i);
                }
            }
        }

        private static ControllerState GetState(HandRole role)
        {
            Initialize();
            var index = (uint)role;
            if (!Active || index >= roleStates.Length) { return emptyState; }
            return roleStates[index];
        }

        protected virtual void Update()
        {
            if (instance == this)
            {
                foreach (var state in roleStates)
                {
                    if (state != null) { state.Update(); }
                }
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }
    }
}