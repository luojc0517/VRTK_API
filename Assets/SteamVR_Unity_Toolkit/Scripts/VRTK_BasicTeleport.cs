//====================================================================================
//
// Purpose: Provide basic teleportation of VR CameraRig
//
// This script must be attached to the [CameraRig] Prefab
//
// A GameObject must have the VRTK_WorldPointer attached to it to listen for the
// updated world position to teleport to.
//
//====================================================================================

namespace VRTK
{
    using UnityEngine;

    public delegate void TeleportEventHandler(object sender, DestinationMarkerEventArgs e);

    public class VRTK_BasicTeleport : MonoBehaviour
    {
        public float blinkTransitionSpeed = 0.6f;
        [Range(0f, 32f)]
        public float distanceBlinkDelay = 0f;
        public bool headsetPositionCompensation = true;
        public string ignoreTargetWithTagOrClass;
        [Tooltip("The max distance the nav mesh edge can be from the teleport destination to be considered valid.\n[0 = ignore nav mesh limits]")]
        public float navMeshLimitDistance = 0f;

        public event TeleportEventHandler Teleporting;
        public event TeleportEventHandler Teleported;

        protected Transform eyeCamera;// 用于视物的camera，vive中有两个camera，一个用来看，一个用来听
        protected bool adjustYForTerrain = false;
        protected bool enableTeleport = true;

        private float blinkPause = 0f;
        private float fadeInTime = 0f;
        private float maxBlinkTransitionSpeed = 1.5f;
        private float maxBlinkDistance = 33f;
        private SteamVR_ControllerManager controllerManager;


        /// <summary>
        /// 为markerMaker下所有绑定了VRTK_DestinationMarker的组件绑定DoTeleport方法，这些组件会在合适的时候回调它，触发传送
        /// </summary>
        /// <param name="markerMaker"></param>
        /// <param name="register"></param>
        public void InitDestinationSetListener(GameObject markerMaker, bool register)
        {
            if (markerMaker)
            {
                foreach (var worldMarker in markerMaker.GetComponents<VRTK_DestinationMarker>())
                {
                    // worldMarker是VRTK_DestinationMarker的子类，SimplePointer或者BezierPointer等
                    if (register)
                    {
                        worldMarker.DestinationMarkerSet += new DestinationMarkerEventHandler(DoTeleport);// 绑定
                        worldMarker.SetInvalidTarget(ignoreTargetWithTagOrClass);
                        worldMarker.SetNavMeshCheckDistance(navMeshLimitDistance);
                        worldMarker.SetHeadsetPositionCompensation(headsetPositionCompensation);
                    }
                    else
                    {
                        worldMarker.DestinationMarkerSet -= new DestinationMarkerEventHandler(DoTeleport);// 解绑
                    }
                }
            }
        }

        protected virtual void Awake()
        {
            // 传送的主体是玩家，即CameraRig
            Utilities.SetPlayerObject(gameObject, VRTK_PlayerObject.ObjectTypes.CameraRig);
            eyeCamera = Utilities.AddCameraFade();
            controllerManager = FindObjectOfType<SteamVR_ControllerManager>();
        }

        protected virtual void OnEnable()
        {
            adjustYForTerrain = false;
            enableTeleport = true;
            InitDestinationMarkerListeners(true);
            InitHeadsetCollisionListener(true);
        }

        protected virtual void OnDisable()
        {
            InitDestinationMarkerListeners(false);
            InitHeadsetCollisionListener(false);
        }

        protected void OnTeleporting(object sender, DestinationMarkerEventArgs e)
        {
            if (Teleporting != null)
            {
                Teleporting(this, e);
            }
        }

        protected void OnTeleported(object sender, DestinationMarkerEventArgs e)
        {
            if (Teleported != null)
            {
                Teleported(this, e);
            }
        }


        /// <summary>
        /// 屏幕闪烁渐变效果，最后要调用ReleaseBlink
        /// </summary>
        /// <param name="transitionSpeed">转变过程的速度</param>
        protected virtual void Blink(float transitionSpeed)
        {
            fadeInTime = transitionSpeed;
            SteamVR_Fade.Start(Color.black, 0);
            Invoke("ReleaseBlink", blinkPause);
        }

        /// <summary>
        /// 判断目标gameobject和坐标是否合法
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destinationPosition"></param>
        /// <returns></returns>
        protected virtual bool ValidLocation(Transform target, Vector3 destinationPosition)
        {
            // 如果目标gameobject是头显、手柄、射线和CameraRig以及UI Canvas那么永远是无效的传送地点
            if (target.GetComponent<VRTK_PlayerObject>() || target.GetComponent<VRTK_UIGraphicRaycaster>())
            {
                return false;
            }

            bool validNavMeshLocation = false;
            if (target)
            {
                NavMeshHit hit;
                validNavMeshLocation = NavMesh.SamplePosition(destinationPosition, out hit, 0.1f, NavMesh.AllAreas);
            }
            if (navMeshLimitDistance == 0f)
            {
                validNavMeshLocation = true;
            }

            return (validNavMeshLocation && target && target.tag != ignoreTargetWithTagOrClass && target.GetComponent(ignoreTargetWithTagOrClass) == null);
        }

        /// <summary>
        /// 传送行为的主要逻辑，不在本脚本被调用，绑定在控制组件的DestinationMarkerSet上，委托给它们来控制传送时机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void DoTeleport(object sender, DestinationMarkerEventArgs e)
        {
            if (enableTeleport && ValidLocation(e.target, e.destinationPosition) && e.enableTeleport)
            {
                OnTeleporting(sender, e);// 回调委托给Teleporting的方法
                Vector3 newPosition = GetNewPosition(e.destinationPosition, e.target);
                CalculateBlinkDelay(blinkTransitionSpeed, newPosition);// 根据传送速度和传送点的坐标计算传送时间
                Blink(blinkTransitionSpeed);// 闪烁渐变
                SetNewPosition(newPosition, e.target);// 改变CameraRig的位置
                OnTeleported(sender, e);// 回调委托给Teleported事件的方法
            }
        }


        /// <summary>
        /// 这句话是真正的改变头显的位置，也就是传送
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        protected virtual void SetNewPosition(Vector3 position, Transform target)
        {
            transform.position = CheckTerrainCollision(position, target);
        }


        /// <summary>
        /// 计算传送后的CameraRig的位置坐标
        /// </summary>
        /// <param name="tipPosition"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        protected virtual Vector3 GetNewPosition(Vector3 tipPosition, Transform target)
        {
            // 若是考虑头显与play area的偏移，那么传送的时候要在tip位置加上这个偏移
            float newX = (headsetPositionCompensation ? (tipPosition.x - (eyeCamera.position.x - transform.position.x)) : tipPosition.x);
            float newY = transform.position.y;// 垂直无位移
            float newZ = (headsetPositionCompensation ? (tipPosition.z - (eyeCamera.position.z - transform.position.z)) : tipPosition.z);

            return new Vector3(newX, newY, newZ);
        }

        /// <summary>
        /// 考虑地形时，计算传送后的CameraRig的位置坐标
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        protected Vector3 CheckTerrainCollision(Vector3 position, Transform target)
        {
            if (adjustYForTerrain && target.GetComponent<Terrain>())
            {
                // 获取地形高度
                var terrainHeight = Terrain.activeTerrain.SampleHeight(position);

                // 如果要传送的地方没有地形高度高，直接传送，如果比山顶还高当然只能传到山顶啦
                position.y = (terrainHeight > position.y ? position.y : terrainHeight);
            }
            return position;
        }

        /// <summary>
        /// 根据传送速度和传送点的坐标计算传送时间
        /// </summary>
        /// <param name="blinkSpeed"></param>
        /// <param name="newPosition"></param>
        private void CalculateBlinkDelay(float blinkSpeed, Vector3 newPosition)
        {
            blinkPause = 0f;
            if (distanceBlinkDelay > 0f)
            {
                float distance = Vector3.Distance(transform.position, newPosition);
                blinkPause = Mathf.Clamp((distance * blinkTransitionSpeed) / (maxBlinkDistance - distanceBlinkDelay), 0, maxBlinkTransitionSpeed);
                blinkPause = (blinkSpeed <= 0.25 ? 0f : blinkPause);
            }
        }

        /// <summary>
        /// 结束变暗效果
        /// </summary>
        private void ReleaseBlink()
        {
            SteamVR_Fade.Start(Color.clear, fadeInTime);// 开始变亮
            fadeInTime = 0f;
        }

        /// <summary>
        /// 为指定组件初始化DestinationSetListener
        /// </summary>
        /// <param name="state"></param>
        private void InitDestinationMarkerListeners(bool state)
        {
            if (controllerManager)
            {
                InitDestinationSetListener(controllerManager.left, state);
                InitDestinationSetListener(controllerManager.right, state);
            }

            foreach (var destinationMarker in FindObjectsOfType<VRTK_DestinationMarker>())
            {
                if (destinationMarker.gameObject != controllerManager.left && destinationMarker.gameObject != controllerManager.right)
                {
                    InitDestinationSetListener(destinationMarker.gameObject, state);
                }
            }
        }

        /// <summary>
        /// 处理头显碰撞
        /// 如果CameraRig下绑定了VRTK_HeadsetCollisionFade脚本，那向它委托/取消委托DisableTeleport/EnableTeleport方法
        /// </summary>
        /// <param name="state"></param>
        private void InitHeadsetCollisionListener(bool state)
        {
            var headset = FindObjectOfType<VRTK_HeadsetCollisionFade>();
            if (headset)
            {
                if (state)
                {
                    headset.HeadsetCollisionDetect += new HeadsetCollisionEventHandler(DisableTeleport);
                    headset.HeadsetCollisionEnded += new HeadsetCollisionEventHandler(EnableTeleport);
                }
                else
                {
                    headset.HeadsetCollisionDetect -= new HeadsetCollisionEventHandler(DisableTeleport);
                    headset.HeadsetCollisionEnded -= new HeadsetCollisionEventHandler(EnableTeleport);
                }
            }
        }

        private void DisableTeleport(object sender, HeadsetCollisionEventArgs e)
        {
            enableTeleport = false;
        }

        private void EnableTeleport(object sender, HeadsetCollisionEventArgs e)
        {
            enableTeleport = true;
        }
    }
}