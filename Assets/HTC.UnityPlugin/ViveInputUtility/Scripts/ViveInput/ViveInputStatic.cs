//========= Copyright 2016, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [DisallowMultipleComponent]
    // To provide static APIs to retrieve controller's button status.
    public partial class ViveInput : MonoBehaviour
    {
        public static bool GetPress(HandRole role, ControllerButton button)
        {
            return GetState(role).GetPress(button);
        }

        public static bool GetPressDown(HandRole role, ControllerButton button)
        {
            return GetState(role).GetPressDown(button);
        }

        public static bool GetPressUp(HandRole role, ControllerButton button)
        {
            return GetState(role).GetPressUp(button);
        }

        public static float LastPressDownTime(HandRole role, ControllerButton button)
        {
            return GetState(role).LastPressDownTime(button);
        }

        public static int ClickCount(HandRole role, ControllerButton button)
        {
            return GetState(role).ClickCount(button);
        }

        public static float GetTriggerValue(HandRole role)
        {
            return GetState(role).GetTriggerValue();
        }

        public static Vector2 GetPadAxis(HandRole role)
        {
            return GetState(role).GetAxis();
        }

        public static Vector2 GetPadPressAxis(HandRole role)
        {
            var handState = GetState(role);
            return handState.GetPress(ControllerButton.Pad) ? handState.GetAxis() : Vector2.zero;
        }

        public static Vector2 GetPadTouchAxis(HandRole role)
        {
            var handState = GetState(role);
            return handState.GetPress(ControllerButton.PadTouch) ? handState.GetAxis() : Vector2.zero;
        }

        public static Vector2 GetPadPressVector(HandRole role)
        {
            return GetState(role).GetPadPressVector();
        }

        public static Vector2 GetPadTouchVector(HandRole role)
        {
            return GetState(role).GetPadTouchVector();
        }

        public static Vector2 GetPadPressDelta(HandRole role)
        {
            var handState = GetState(role);
            if (handState.GetPress(ControllerButton.Pad) && !handState.GetPressDown(ControllerButton.Pad))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        public static Vector2 GetPadTouchDelta(HandRole role)
        {
            var handState = GetState(role);
            if (handState.GetPress(ControllerButton.PadTouch) && !handState.GetPressDown(ControllerButton.PadTouch))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        public static void AddPressDown(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).AddListener(button, callback, ButtonEventType.Down);
        }

        public static void AddPress(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).AddListener(button, callback, ButtonEventType.Press);
        }

        public static void AddPressUp(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).AddListener(button, callback, ButtonEventType.Up);
        }

        public static void AddClick(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).AddListener(button, callback, ButtonEventType.Click);
        }

        public static void RemovePressDown(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).RemoveListener(button, callback, ButtonEventType.Down);
        }

        public static void RemovePress(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).RemoveListener(button, callback, ButtonEventType.Press);
        }

        public static void RemovePressUp(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).RemoveListener(button, callback, ButtonEventType.Up);
        }

        public static void RemoveClick(HandRole role, ControllerButton button, Action callback)
        {
            GetState(role).RemoveListener(button, callback, ButtonEventType.Click);
        }
        
        public static void TriggerHapticPulse(HandRole role, ushort intensity = 500)
        {
            var system = Valve.VR.OpenVR.System;
            if (system != null)
            {
                system.TriggerHapticPulse(ViveRole.GetDeviceIndex(role), (uint)Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)Valve.VR.EVRButtonId.k_EButton_Axis0, (char)intensity);
            }
        }
    }
}