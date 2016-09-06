//========= Copyright 2016, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    // To provide static APIs to retrieve devices' tracking status.
    public static partial class VivePose
    {
        public static bool HasFocus() { return hasFocus; }

        public static bool IsValid(HandRole role) { return IsValid(role.ToDeviceRole()); }

        public static bool IsValid(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && poses[index].bDeviceIsConnected && poses[index].bPoseIsValid && hasFocus;
        }

        public static bool IsConnected(HandRole role) { return IsConnected(role.ToDeviceRole()); }

        public static bool IsConnected(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && poses[index].bDeviceIsConnected;
        }

        public static bool HasTracking(HandRole role) { return HasTracking(role.ToDeviceRole()); }

        public static bool HasTracking(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && poses[index].bPoseIsValid;
        }

        public static bool IsOutOfRange(HandRole role) { return IsOutOfRange(role.ToDeviceRole()); }

        public static bool IsOutOfRange(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && (poses[index].eTrackingResult == ETrackingResult.Running_OutOfRange || poses[index].eTrackingResult == ETrackingResult.Calibrating_OutOfRange);
        }

        public static bool IsCalibrating(HandRole role) { return IsCalibrating(role.ToDeviceRole()); }

        public static bool IsCalibrating(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && (poses[index].eTrackingResult == ETrackingResult.Calibrating_InProgress || poses[index].eTrackingResult == ETrackingResult.Calibrating_OutOfRange);
        }

        public static bool IsUninitialized(HandRole role) { return IsUninitialized(role.ToDeviceRole()); }

        public static bool IsUninitialized(DeviceRole role)
        {
            var index = ViveRole.GetDeviceIndex(role);
            return index < poses.Length && poses[index].eTrackingResult == ETrackingResult.Uninitialized;
        }

        public static Vector3 GetVelocity(HandRole role, Transform origin = null) { return GetVelocity(role.ToDeviceRole(), origin); }

        public static Vector3 GetVelocity(DeviceRole role, Transform origin = null)
        {
            var index = ViveRole.GetDeviceIndex(role);
            var rawValue = Vector3.zero;

            if (index < poses.Length)
            {
                rawValue = new Vector3(poses[index].vVelocity.v0, poses[index].vVelocity.v1, -poses[index].vVelocity.v2);
            }

            return origin == null ? rawValue : origin.TransformVector(rawValue);
        }

        public static Vector3 GetAngularVelocity(HandRole role, Transform origin = null) { return GetAngularVelocity(role.ToDeviceRole(), origin); }

        public static Vector3 GetAngularVelocity(DeviceRole role, Transform origin = null)
        {
            var index = ViveRole.GetDeviceIndex(role);
            var rawValue = Vector3.zero;

            if (index < poses.Length)
            {
                rawValue = new Vector3(-poses[index].vAngularVelocity.v0, -poses[index].vAngularVelocity.v1, poses[index].vAngularVelocity.v2);
            }

            return origin == null ? rawValue : origin.TransformVector(rawValue);
        }

        public static Pose GetPose(HandRole role, Transform origin = null) { return GetPose(role.ToDeviceRole(), origin); }

        public static Pose GetPose(DeviceRole role, Transform origin = null)
        {
            var index = ViveRole.GetDeviceIndex(role);
            var rawPose = new Pose();

            if (index < rigidPoses.Length) { rawPose = rigidPoses[index]; }

            if (origin != null)
            {
                rawPose = new Pose(origin) * rawPose;
                rawPose.pos.Scale(origin.localScale);
            }

            return rawPose;
        }

        public static void SetPose(Transform target, HandRole role, Transform origin = null) { SetPose(target, role.ToDeviceRole(), origin); }

        public static void SetPose(Transform target, DeviceRole role, Transform origin = null) { Pose.SetPose(target, GetPose(role), origin); }
    }
}