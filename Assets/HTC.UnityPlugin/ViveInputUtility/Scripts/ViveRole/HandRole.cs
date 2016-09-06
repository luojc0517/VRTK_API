//========= Copyright 2016, HTC Corporation. All rights reserved. ===========

namespace HTC.UnityPlugin.Vive
{
    // Defines HandRole. Controllers that have button.
    public enum HandRole
    {
        RightHand,
        LeftHand,
    }

    public static class ConvertRoleExtension
    {
        public static DeviceRole ToDeviceRole(this HandRole role)
        {
            switch (role)
            {
                case HandRole.RightHand: return DeviceRole.RightHand;
                case HandRole.LeftHand: return DeviceRole.LeftHand;
                default: return (DeviceRole)((int)DeviceRole.Hmd - 1); // returns invalid value
            }
        }
    }
}