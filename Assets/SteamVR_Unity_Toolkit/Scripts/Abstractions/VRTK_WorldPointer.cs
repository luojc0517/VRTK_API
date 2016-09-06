//====================================================================================
//
// Purpose: Provide abstraction into projecting a raycast into the game world.
// As this is an abstract class, it should never be used on it's own.
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;

    public abstract class VRTK_WorldPointer : VRTK_DestinationMarker
    {

        // 射线的状态
        public enum pointerVisibilityStates
        {
            On_When_Active,// 当Pointer按钮被按下时才显示.
            Always_On,// 总是显示，但只有按下Pointer button时才触发destination set事件.
            Always_Off// 从不显示，但是仍可以设置目标点，按下Pointer按钮仍然触发destination set事件.
        }

        public VRTK_ControllerEvents controller = null;// controller绑定的VRTK_ControllerEvents脚本，用于触发按钮事件
        public Material pointerMaterial;
        public Color pointerHitColor = new Color(0f, 0.5f, 0f, 1f);
        public Color pointerMissColor = new Color(0.8f, 0f, 0f, 1f);
        public bool showPlayAreaCursor = false;// 是否显示play area cusor
        public Vector2 playAreaCursorDimensions = Vector2.zero;
        public bool handlePlayAreaCursorCollisions = false;// 确定传送区域的时候要不要考虑区域与场景物体的碰撞
        public string ignoreTargetWithTagOrClass;// 如果考虑传送区域与场景物体的碰撞，可以标记一些物体不计入碰撞考虑
        public pointerVisibilityStates pointerVisibility = pointerVisibilityStates.On_When_Active;// 默认射线状态为按键才显示
        public bool holdButtonToActivate = true;
        public float activateDelay = 0f;

        protected Vector3 destinationPosition;// 目标三维坐标
        protected float pointerContactDistance = 0f;
        protected Transform pointerContactTarget = null;// 射线击中的object的Transform
        protected uint controllerIndex;// controller的index

        protected bool playAreaCursorCollided = false;

        private SteamVR_PlayArea playArea;
        private GameObject playAreaCursor;
        private GameObject[] playAreaCursorBoundaries;
        private BoxCollider playAreaCursorCollider;// play area cursor的Collider
        private Transform headset;// 头显的Transform
        private bool isActive;
        private bool destinationSetActive;

        private float activateDelayTimer = 0f;
        private int beamEnabledState = 0;

        private VRTK_InteractableObject interactableObject = null;// 可交互物体

        /// <summary>
        /// setPlayAreaCursorCollision方法用于设置playAreaCursorCollided的碰撞状态，前提是handlePlayAreaCursorCollisions为真，即考虑检测碰撞.
        /// </summary>
        /// <param name="state">是否碰撞</param>
        public virtual void setPlayAreaCursorCollision(bool state)
        {
            if (handlePlayAreaCursorCollisions)
            {
                playAreaCursorCollided = state;
            }
        }

        /// <summary>
        /// IsActive用于查看当前指示射线是否active.
        /// </summary>
        /// <returns>当前射线为active时为真.</returns>
        public virtual bool IsActive()
        {
            return isActive;
        }


        /// <summary>
        /// CanActivate方法确认是否计时完毕，使得下一束射线可用.
        /// </summary>
        /// <returns>如果为真代表时间间隔已过，可以使下一束射线可用.</returns>
        public virtual bool CanActivate()
        {
            return (Time.time >= activateDelayTimer);
        }

        /// <summary>
        /// ToggleBeam方法可以在脚本运行时动态的控制射线的开关. 参数传入真则射线会打开,参数传入假那么射线将会关闭.
        /// </summary>
        /// <param name="state">是否打开射线.</param>
        public virtual void ToggleBeam(bool state)
        {
            // 找到当前脚本绑定gameobject的Controller索引
            var index = VRTK_DeviceFinder.GetControllerIndex(gameObject);
            if (state)
            {
                //开
                TurnOnBeam(index);
            }
            else
            {
                //关
                TurnOffBeam(index);
            }
        }

        protected virtual void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<VRTK_ControllerEvents>();
            }

            if (controller == null)
            {
                Debug.LogError("VRTK_WorldPointer requires a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }

            // 给当前脚本绑定的gameobject绑定VRTK_PlayerObject脚本并且设置类型为Controller(手柄)
            Utilities.SetPlayerObject(gameObject, VRTK_PlayerObject.ObjectTypes.Controller);

            // 找到头显的位置
            headset = VRTK_DeviceFinder.HeadsetTransform();
            playArea = FindObjectOfType<SteamVR_PlayArea>();
            playAreaCursorBoundaries = new GameObject[4];
        }

        protected virtual void OnEnable()
        {
            // 绑定事件，开，关，设置目标
            controller.AliasPointerOn += new ControllerInteractionEventHandler(EnablePointerBeam);
            controller.AliasPointerOff += new ControllerInteractionEventHandler(DisablePointerBeam);
            controller.AliasPointerSet += new ControllerInteractionEventHandler(SetPointerDestination);

            // 射线材质处理
            var tmpMaterial = Resources.Load("WorldPointer") as Material;
            if (pointerMaterial != null)
            {
                tmpMaterial = pointerMaterial;
            }

            pointerMaterial = new Material(tmpMaterial);
            pointerMaterial.color = pointerMissColor;
        }

        protected virtual void OnDisable()
        {
            DisableBeam();
            destinationSetActive = false;
            pointerContactDistance = 0f;
            pointerContactTarget = null;
            destinationPosition = Vector3.zero;

            //解绑事件
            controller.AliasPointerOn -= new ControllerInteractionEventHandler(EnablePointerBeam);
            controller.AliasPointerOff -= new ControllerInteractionEventHandler(DisablePointerBeam);
            controller.AliasPointerSet -= new ControllerInteractionEventHandler(SetPointerDestination);

            if (playAreaCursor != null)
            {
                Destroy(playAreaCursor);
            }
        }


        
        /// <summary>
        /// 当playAreaCursor可用且活跃时，每帧更新它的Collider
        /// </summary>
        protected virtual void Update()
        {
            if (playAreaCursor && playAreaCursor.activeSelf)
            {
                UpdateCollider();
            }
        }

        protected virtual void InitPointer()
        {
            InitPlayAreaCursor();
        }


        /// <summary>
        /// 根据目的坐标点，计算对应的play area位置，根据设置考虑是否要计入头显位移的影响
        /// 比如，传送之前人站在原来play area的左下角，那么传送后这个相对位置应该是不变的
        /// </summary>
        /// <param name="destination"></param>
        protected virtual void SetPlayAreaCursorTransform(Vector3 destination)
        {
            var offset = Vector3.zero;
            if (headsetPositionCompensation)
            {
                // 如果考虑头显位移对目的play area的影响
                var playAreaPos = new Vector3(playArea.transform.position.x, 0, playArea.transform.position.z);// play area肯定是地面上，y为0
                var headsetPos = new Vector3(headset.position.x, 0, headset.position.z);// 把头显的位置也投影在x-z平面上
                offset = playAreaPos - headsetPos;// 蓝色play area与头显之间的偏移
            }
            playAreaCursor.transform.position = destination + offset;// 目标play area(红色或绿色)也要加上偏移
        }

        // 要委托ControllerEvents执行的方法
        protected virtual void EnablePointerBeam(object sender, ControllerInteractionEventArgs e)
        {
            TurnOnBeam(e.controllerIndex);// 打开射线
        }

        protected virtual void DisablePointerBeam(object sender, ControllerInteractionEventArgs e)
        {
            TurnOffBeam(e.controllerIndex);// 关闭射线
        }

        protected virtual void SetPointerDestination(object sender, ControllerInteractionEventArgs e)
        {
            PointerSet();// 设置目标点
        }

        protected virtual void PointerIn()
        {
            // 射线没有击中任何东西则返回
            if (!enabled || !pointerContactTarget)
            {
                return;
            }
            
            // 回调委托给自己的事件
            OnDestinationMarkerEnter(SetDestinationMarkerEvent(pointerContactDistance, pointerContactTarget, destinationPosition, controllerIndex));

            // 获取被击中物体的VRTK_InteractableObject脚本，如果不能交互则获取不到，为null
            interactableObject = pointerContactTarget.GetComponent<VRTK_InteractableObject>();

            // 判断这个物体的交互方式，如果此物体只有被抓取才能交互则为true
            bool cannotUseBecauseNotGrabbed = (interactableObject && interactableObject.useOnlyIfGrabbed && !interactableObject.IsGrabbed());

            //使用物体
            if (interactableObject && interactableObject.pointerActivatesUseAction && interactableObject.holdButtonToUse && !cannotUseBecauseNotGrabbed)
            {
                interactableObject.StartUsing(gameObject);
            }
        }

        protected virtual void PointerOut()
        {
            if (!enabled || !pointerContactTarget)
            {
                return;
            }

            // 回调委托给自己的事件
            OnDestinationMarkerExit(SetDestinationMarkerEvent(pointerContactDistance, pointerContactTarget, destinationPosition, controllerIndex));

            // 停止使用物品
            if (interactableObject && interactableObject.pointerActivatesUseAction && interactableObject.holdButtonToUse)
            {
                interactableObject.StopUsing(gameObject);
            }
        }

        protected virtual void PointerSet()
        {
            if (!enabled || !destinationSetActive || !pointerContactTarget || !CanActivate() || (!holdButtonToActivate && beamEnabledState != 0))
            {
                return;
            }

            activateDelayTimer = Time.time + activateDelay;

            // 物体交互
            var interactableObject = pointerContactTarget.GetComponent<VRTK_InteractableObject>();
            if (interactableObject && interactableObject.pointerActivatesUseAction)
            {
                if (interactableObject.IsUsing())
                {
                    interactableObject.StopUsing(gameObject);
                }
                else if (!interactableObject.holdButtonToUse)
                {
                    interactableObject.StartUsing(gameObject);
                }
            }

            // 发送DestinationMarkerSet事件通知
            if (!playAreaCursorCollided && (interactableObject == null || !interactableObject.pointerActivatesUseAction))
            {
                OnDestinationMarkerSet(SetDestinationMarkerEvent(pointerContactDistance, pointerContactTarget, destinationPosition, controllerIndex));
            }

            if (!isActive)
            {
                destinationSetActive = false;
            }
        }


        /// <summary>
        /// 指示线开关的时候要处理play area cursor的显示/消失和物体交互
        /// </summary>
        /// <param name="state">指示线开关状态</param>
        protected virtual void TogglePointer(bool state)
        {
            // 如果play area cursor可用，那么根据射线开，area显示，射线关，area不显示；如果不可用，那么一直为false
            var playAreaState = (showPlayAreaCursor ? state : false);
            if (playAreaCursor)
            {
                playAreaCursor.gameObject.SetActive(playAreaState);
            }
            if (!state && interactableObject && interactableObject.pointerActivatesUseAction && interactableObject.holdButtonToUse && interactableObject.IsUsing())
            {
                //如果射线关闭，停止物体交互
                interactableObject.StopUsing(this.gameObject);
            }
        }


        /// <summary>
        /// 把playAreaCursorBoundaries的每条边的材质都设置为射线的材质，子类可重写
        /// </summary>
        protected virtual void SetPointerMaterial()
        {
            foreach (GameObject playAreaCursorBoundary in playAreaCursorBoundaries)
            {
                playAreaCursorBoundary.GetComponent<Renderer>().material = pointerMaterial;
            }
        }


        /// <summary>
        /// 如果目标点无效或者目标点play area cusor与物体发生碰撞，play area cursor颜色为miss
        /// </summary>
        /// <param name="color"></param>
        protected void UpdatePointerMaterial(Color color)
        {
            // 如果play area cursor中进入了collider，发生碰撞，或者是无效地点，则显示miss的颜色，意味着不能传送
            if (playAreaCursorCollided || !ValidDestination(pointerContactTarget, destinationPosition))
            {
                color = pointerMissColor;
            }
            pointerMaterial.color = color;
            SetPointerMaterial();// 调用的是子类重写过的方法
        }


        /// <summary>
        /// 判断碰撞点和碰撞物体的有效性，碰撞点的有效性根据NavMesh采样判断，碰撞物体的有效性根据它是否包含无效tag或者脚本名字
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destinationPosition"></param>
        /// <returns></returns>
        protected virtual bool ValidDestination(Transform target, Vector3 destinationPosition)
        {
            bool validNavMeshLocation = false;
            if (target)
            {
                NavMeshHit hit;
                // NavMesh.SamplePosition 根据给的点进行采样，可传入最大距离，返回true说明采样到了点
                validNavMeshLocation = NavMesh.SamplePosition(destinationPosition, out hit, 0.1f, NavMesh.AllAreas);
            }
            if (navMeshCheckDistance == 0f)
            {
                validNavMeshLocation = true;
            }
            return (validNavMeshLocation && target && target.tag != invalidTargetWithTagOrClass && target.GetComponent(invalidTargetWithTagOrClass) == null);
        }

        
        /// <summary>
        /// 打开index对应Controller的射线
        /// </summary>
        /// <param name="index"></param>
        private void TurnOnBeam(uint index)
        {
            beamEnabledState++;
            if (enabled && !isActive && CanActivate())
            {
                setPlayAreaCursorCollision(false);
                controllerIndex = index;
                TogglePointer(true);// 如果子类重写了TogglePointer，那么调用的是子类的TogglePointer
                isActive = true;
                destinationSetActive = true;
            }
        }


        /// <summary>
        /// 关闭index对应Controller的射线
        /// </summary>
        /// <param name="index"></param>
        private void TurnOffBeam(uint index)
        {
            if (enabled && isActive && (holdButtonToActivate || (!holdButtonToActivate && beamEnabledState >= 2)))
            {
                controllerIndex = index;
                DisableBeam();
            }
        }


        /// <summary>
        /// 禁用射线
        /// </summary>
        private void DisableBeam()
        {
            TogglePointer(false);// 如果子类重写了TogglePointer，那么调用的是子类的TogglePointer
            isActive = false;
            beamEnabledState = 0;
        }


        /// <summary>
        /// 绘制play area cursor，创建Cube，更改缩放比以及位置、层次等
        /// </summary>
        /// <param name="index"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="thickness"></param>
        /// <param name="localPosition"></param>
        private void DrawPlayAreaCursorBoundary(int index, float left, float right, float top, float bottom, float thickness, Vector3 localPosition)
        {
            var playAreaCursorBoundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playAreaCursorBoundary.name = string.Format("[{0}]WorldPointer_PlayAreaCursorBoundary_" + index, gameObject.name);
            Utilities.SetPlayerObject(playAreaCursorBoundary, VRTK_PlayerObject.ObjectTypes.Pointer);

            var width = (right - left) / 1.065f;
            var length = (top - bottom) / 1.08f;
            var height = thickness;

            playAreaCursorBoundary.transform.localScale = new Vector3(width, height, length);
            Destroy(playAreaCursorBoundary.GetComponent<BoxCollider>());
            playAreaCursorBoundary.layer = LayerMask.NameToLayer("Ignore Raycast");

            playAreaCursorBoundary.transform.parent = playAreaCursor.transform;
            playAreaCursorBoundary.transform.localPosition = localPosition;

            playAreaCursorBoundaries[index] = playAreaCursorBoundary;
        }

        
        /// <summary>
        /// 初始化play area cursor
        /// </summary>
        private void InitPlayAreaCursor()
        {
            var btmRightInner = 0;
            var btmLeftInner = 1;
            var topLeftInner = 2;
            var topRightInner = 3;

            var btmRightOuter = 4;
            var btmLeftOuter = 5;
            var topLeftOuter = 6;
            var topRightOuter = 7;

            // 获取play area 的顶点坐标
            Vector3[] cursorDrawVertices = playArea.vertices;

            if (playAreaCursorDimensions != Vector2.zero)
            {
                var customAreaPadding = playArea.borderThickness;

                cursorDrawVertices[btmRightOuter] = new Vector3(playAreaCursorDimensions.x / 2, 0f, (playAreaCursorDimensions.y / 2) * -1);
                cursorDrawVertices[btmLeftOuter] = new Vector3((playAreaCursorDimensions.x / 2) * -1, 0f, (playAreaCursorDimensions.y / 2) * -1);
                cursorDrawVertices[topLeftOuter] = new Vector3((playAreaCursorDimensions.x / 2) * -1, 0f, playAreaCursorDimensions.y / 2);
                cursorDrawVertices[topRightOuter] = new Vector3(playAreaCursorDimensions.x / 2, 0f, playAreaCursorDimensions.y / 2);

                cursorDrawVertices[btmRightInner] = cursorDrawVertices[btmRightOuter] + new Vector3(-customAreaPadding, 0f, customAreaPadding);
                cursorDrawVertices[btmLeftInner] = cursorDrawVertices[btmLeftOuter] + new Vector3(customAreaPadding, 0f, customAreaPadding);
                cursorDrawVertices[topLeftInner] = cursorDrawVertices[topLeftOuter] + new Vector3(customAreaPadding, 0f, -customAreaPadding);
                cursorDrawVertices[topRightInner] = cursorDrawVertices[topRightOuter] + new Vector3(-customAreaPadding, 0f, -customAreaPadding);
            }

            var width = cursorDrawVertices[btmRightOuter].x - cursorDrawVertices[topLeftOuter].x;// v4.x-v6.x,外侧矩形的水平宽度
            var length = cursorDrawVertices[topLeftOuter].z - cursorDrawVertices[btmRightOuter].z;// v4.z-v6.z,外侧矩形的垂直宽度
            var height = 0.01f;// 近乎0，扁平

            // 初始化一个playAreaCursor，也是一个3D方块
            playAreaCursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playAreaCursor.name = string.Format("[{0}]WorldPointer_PlayAreaCursor", gameObject.name);
            Utilities.SetPlayerObject(playAreaCursor, VRTK_PlayerObject.ObjectTypes.Pointer);
            playAreaCursor.transform.parent = null;
            playAreaCursor.transform.localScale = new Vector3(width, height, length);
            playAreaCursor.SetActive(false);

            playAreaCursor.GetComponent<Renderer>().enabled = false;

            CreateCursorCollider(playAreaCursor);

            playAreaCursor.AddComponent<Rigidbody>().isKinematic = true;

            var playAreaCursorScript = playAreaCursor.AddComponent<VRTK_PlayAreaCollider>();// 添加脚本
            playAreaCursorScript.SetParent(gameObject);
            playAreaCursorScript.SetIgnoreTarget(ignoreTargetWithTagOrClass);
            playAreaCursor.layer = LayerMask.NameToLayer("Ignore Raycast");

            var playAreaBoundaryX = playArea.transform.localScale.x / 2;
            var playAreaBoundaryZ = playArea.transform.localScale.z / 2;
            var heightOffset = 0f;

            DrawPlayAreaCursorBoundary(0, cursorDrawVertices[btmLeftOuter].x, cursorDrawVertices[btmRightOuter].x, cursorDrawVertices[btmRightInner].z, cursorDrawVertices[btmRightOuter].z, height, new Vector3(0f, heightOffset, playAreaBoundaryZ));
            DrawPlayAreaCursorBoundary(1, cursorDrawVertices[btmLeftOuter].x, cursorDrawVertices[btmLeftInner].x, cursorDrawVertices[topLeftOuter].z, cursorDrawVertices[btmLeftOuter].z, height, new Vector3(playAreaBoundaryX, heightOffset, 0f));
            DrawPlayAreaCursorBoundary(2, cursorDrawVertices[btmLeftOuter].x, cursorDrawVertices[btmRightOuter].x, cursorDrawVertices[btmRightInner].z, cursorDrawVertices[btmRightOuter].z, height, new Vector3(0f, heightOffset, -playAreaBoundaryZ));
            DrawPlayAreaCursorBoundary(3, cursorDrawVertices[btmLeftOuter].x, cursorDrawVertices[btmLeftInner].x, cursorDrawVertices[topLeftOuter].z, cursorDrawVertices[btmLeftOuter].z, height, new Vector3(-playAreaBoundaryX, heightOffset, 0f));
        }


        /// <summary>
        /// 为play area cursor添加Collider
        /// </summary>
        /// <param name="cursor"></param>
        private void CreateCursorCollider(GameObject cursor)
        {
            playAreaCursorCollider = cursor.GetComponent<BoxCollider>();
            playAreaCursorCollider.isTrigger = true;
            playAreaCursorCollider.center = new Vector3(0f, 65f, 0f);
            playAreaCursorCollider.size = new Vector3(1f, 1f, 1f);
        }


        /// <summary>
        /// 更新player area cursor的Collider
        /// </summary>
        private void UpdateCollider()
        {
            var playAreaHeightAdjustment = 1f;
            var newBCYSize = (headset.transform.position.y - playArea.transform.position.y) * 100f;
            var newBCYCenter = (newBCYSize != 0 ? (newBCYSize / 2) + playAreaHeightAdjustment : 0);

            playAreaCursorCollider.size = new Vector3(playAreaCursorCollider.size.x, newBCYSize, playAreaCursorCollider.size.z);
            playAreaCursorCollider.center = new Vector3(playAreaCursorCollider.center.x, newBCYCenter, playAreaCursorCollider.center.z);
        }
    }

    public class VRTK_PlayAreaCollider : MonoBehaviour
    {
        private GameObject parent;
        private string ignoreTargetWithTagOrClass;

        public void SetParent(GameObject setParent)
        {
            parent = setParent;
        }


        /// <summary>
        /// 设置无效collider的字符串
        /// </summary>
        /// <param name="ignore"></param>
        public void SetIgnoreTarget(string ignore)
        {
            ignoreTargetWithTagOrClass = ignore;
        }


        /// <summary>
        /// 判断一个collider是不是有效的
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        private bool ValidTarget(Collider collider)
        {
            //collider不能是头显、手柄等设备，不能包含无效字符的tag和脚本
            return (!collider.GetComponent<VRTK_PlayerObject>() & collider.tag != ignoreTargetWithTagOrClass && collider.GetComponent(ignoreTargetWithTagOrClass) == null);
        }

        private void OnTriggerStay(Collider collider)
        {
            if (parent.GetComponent<VRTK_WorldPointer>().IsActive() && ValidTarget(collider))
            {
                parent.GetComponent<VRTK_WorldPointer>().setPlayAreaCursorCollision(true);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (ValidTarget(collider))
            {
                parent.GetComponent<VRTK_WorldPointer>().setPlayAreaCursorCollision(false);
            }
        }
    }
}