//====================================================================================
//
// Purpose: Provide ability to grab an interactable object when it is being touched
//
// This script must be attached to a Controller within the [CameraRig] Prefab
//
// The VRTK_ControllerEvents and VRTK_InteractTouch scripts must also be
// attached to the Controller
//
// Press the default 'Trigger' button on the controller to grab an object
// Released the default 'Trigger' button on the controller to drop an object
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;

    [RequireComponent(typeof(VRTK_InteractTouch)), RequireComponent(typeof(VRTK_ControllerEvents))]
    public class VRTK_InteractGrab : MonoBehaviour
    {
        // Inspector可见参数
        public Rigidbody controllerAttachPoint = null;// 手柄模型上的刚体圆点，被抓取的物体通过这个点连接手柄(默认是射线末端的tip圆点).
        public bool hideControllerOnGrab = false;// 当抓取物体时是否隐藏手柄模型.
        public float hideControllerDelay = 0f;// 当手柄抓取物体到隐藏手柄之间的延迟时间.
        /// <summary>
        /// 预抓取时间.当物体以非常快的速度运动时，由于身体反应的速度，很难及时抓到物体.
        /// 这个参数的值设置的足够大的时候，
        /// 可以在手柄碰到物体之前按下抓取按钮，当碰撞发生时，如果抓取按钮还按着，整个抓取行为就算成功完成.
        /// </summary>
        public float grabPrecognition = 0f;
        public float throwMultiplier = 1f;// 丢出速度乘子.被丢出的物体的速度要乘上这个乘子，可以控制物体被丢出的程度.
        public bool createRigidBodyWhenNotTouching = false;// 如果此项勾选，当手柄按下抓取按钮时没有碰到一个可交互的物体，那么为手柄添加一个刚体组件，让手柄可以推开其他刚体对象.

        // 事件类
        public event ObjectInteractEventHandler ControllerGrabInteractableObject;// 当物体被抓取时发送事件，调用委托给自己的方法.
        public event ObjectInteractEventHandler ControllerUngrabInteractableObject;// 当物体被释放时发送事件，调用委托给自己的方法.

        private Joint controllerAttachJoint;
        private GameObject grabbedObject = null;// 被当前手柄抓取的物体游戏对象
        private bool updatedHideControllerOnGrab = false;

        private SteamVR_TrackedObject trackedController;
        private VRTK_InteractTouch interactTouch;
        private VRTK_ControllerActions controllerActions;
        private VRTK_ControllerEvents controllerEvents;

        /// <summary>
        /// 记录按下grab的次数，用于处理那种不需要一直按着按钮持续抓取的物体，大于等于2表示物体应被释放
        /// </summary>
        private int grabEnabledState = 0;
        private float grabPrecognitionTimer = 0f;

        // 下面两个是事件发送方法
        public virtual void OnControllerGrabInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerGrabInteractableObject != null)
            {
                ControllerGrabInteractableObject(this, e);
            }
        }

        public virtual void OnControllerUngrabInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUngrabInteractableObject != null)
            {
                ControllerUngrabInteractableObject(this, e);
            }
        }

        /// <summary>
        /// ForceRelease方法会强制手柄停止抓取当前的可交互物体.
        /// </summary>
        public void ForceRelease()
        {
            if (grabbedObject != null && grabbedObject.GetComponent<VRTK_InteractableObject>() && grabbedObject.GetComponent<VRTK_InteractableObject>().AttachIsTrackObject())
            {
                // 如果是追踪连接方式连接
                UngrabTrackedObject();
            }
            else
            {
                // 其他连接方式
                ReleaseObject((uint)trackedController.index, false);
            }
        }

        /// <summary>
        /// AttemptGrab方法会尝试抓取当前被触碰的物体，无需按下手柄上的抓取按钮.
        /// 目前看来好像是为外部脚本提供直接抓取的功能(ArrowSpawner,VRTK_ObjectAutoGrab等)
        /// </summary>
        public void AttemptGrab()
        {
            AttemptGrabObject();
        }

        /// <summary>
        /// GetGrabbedObject方法返回正在被当前手柄抓取的游戏对象.
        /// </summary>
        /// <returns></returns>
        public GameObject GetGrabbedObject()
        {
            return grabbedObject;
        }

        private void Awake()
        {
            // 获取需要的脚本
            if (GetComponent<VRTK_InteractTouch>() == null)
            {
                Debug.LogError("VRTK_InteractGrab is required to be attached to a SteamVR Controller that has the VRTK_InteractTouch script attached to it");
                return;
            }

            interactTouch = GetComponent<VRTK_InteractTouch>();
            trackedController = GetComponent<SteamVR_TrackedObject>();
            controllerActions = GetComponent<VRTK_ControllerActions>();
            controllerEvents = GetComponent<VRTK_ControllerEvents>();
        }

        private void OnEnable()
        {
            if (GetComponent<VRTK_ControllerEvents>() == null)
            {
                Debug.LogError("VRTK_InteractGrab is required to be attached to a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }
            // 监听事件，将方法委托给VRTK_ControllerEvents中的事件
            GetComponent<VRTK_ControllerEvents>().AliasGrabOn += new ControllerInteractionEventHandler(DoGrabObject);
            GetComponent<VRTK_ControllerEvents>().AliasGrabOff += new ControllerInteractionEventHandler(DoReleaseObject);

            SetControllerAttachPoint();
        }

        private void OnDisable()
        {
            // 强制释放抓取
            ForceRelease();

            // 取消事件监听
            GetComponent<VRTK_ControllerEvents>().AliasGrabOn -= new ControllerInteractionEventHandler(DoGrabObject);
            GetComponent<VRTK_ControllerEvents>().AliasGrabOff -= new ControllerInteractionEventHandler(DoReleaseObject);
        }

        /// <summary>
        /// 为连接点controllerAttachPoint赋值
        /// </summary>
        private void SetControllerAttachPoint()
        {
            // 如果没有指定连接点那么直接使用tip作为连接点
            if (controllerAttachPoint == null)
            {
                // 尝试在手柄上找到连接点
                var defaultAttachPoint = transform.Find("Model/tip/attach");
                if (defaultAttachPoint != null)
                {
                    // 找到连接点的刚体，若没有就添加一个
                    controllerAttachPoint = defaultAttachPoint.GetComponent<Rigidbody>();

                    if (controllerAttachPoint == null)
                    {
                        var autoGenRB = defaultAttachPoint.gameObject.AddComponent<Rigidbody>();
                        autoGenRB.isKinematic = true;
                        controllerAttachPoint = autoGenRB;
                    }
                }
            }
        }

        /// <summary>
        /// 判断obj是不是可以抓取的
        /// </summary>
        /// <param name="obj">要判断的游戏对象</param>
        /// <returns></returns>
        private bool IsObjectGrabbable(GameObject obj)
        {
            return (interactTouch.IsObjectInteractable(obj) && obj.GetComponent<VRTK_InteractableObject>().isGrabbable);
        }

        /// <summary>
        /// 判断obj是不是要按住按钮来持续抓取
        /// </summary>
        /// <param name="obj">要判断的游戏对象</param>
        /// <returns></returns>
        private bool IsObjectHoldOnGrab(GameObject obj)
        {
            return (obj && obj.GetComponent<VRTK_InteractableObject>() && obj.GetComponent<VRTK_InteractableObject>().holdButtonToGrab);
        }

        /// <summary>
        /// 返回一个可交互游戏对象的抓取部位，如果只指定了一侧手柄的抓取部位，那么同步两侧的手柄为同一个抓取部位
        /// </summary>
        /// <param name="objectScript">游戏对象</param>
        /// <returns>返回当前手柄抓取可交互物体的部位的Transform对象</returns>
        private Transform GetSnapHandle(VRTK_InteractableObject objectScript)
        {
            if (objectScript.rightSnapHandle == null && objectScript.leftSnapHandle != null)
            {
                objectScript.rightSnapHandle = objectScript.leftSnapHandle;
            }

            if (objectScript.leftSnapHandle == null && objectScript.rightSnapHandle != null)
            {
                objectScript.leftSnapHandle = objectScript.rightSnapHandle;
            }

            if (VRTK_DeviceFinder.IsControllerOfHand(gameObject, VRTK_DeviceFinder.ControllerHand.Right))
            {
                return objectScript.rightSnapHandle;
            }

            if (VRTK_DeviceFinder.IsControllerOfHand(gameObject, VRTK_DeviceFinder.ControllerHand.Left))
            {
                return objectScript.leftSnapHandle;
            }

            return null;
        }

        /// <summary>
        /// 改变抓取的游戏对象的位置(precisionSnap方式抓取)
        /// </summary>
        /// <param name="obj"></param>
        private void SetSnappedObjectPosition(GameObject obj)
        {
            var objectScript = obj.GetComponent<VRTK_InteractableObject>();

            if (objectScript.rightSnapHandle == null && objectScript.leftSnapHandle == null)
            {
                // 如果该物体没有指定抓取部位,手柄连接点和物体重合就可以
                obj.transform.position = controllerAttachPoint.transform.position;
            }
            else
            {
                // 如果指定了抓取部位，手柄连接点的位置应该与物体抓取点重合，根据抓取点的位置和手柄连接点的位置计算物体的位置
                var snapHandle = GetSnapHandle(objectScript);
                objectScript.SetGrabbedSnapHandle(snapHandle);

                // 此处有疑问，如果snapHandle的旋转要和连接点一致的话，应该乘以Quaternion.Euler(snapHandle.transform.localEulerAngles)的逆才对
                obj.transform.rotation = controllerAttachPoint.transform.rotation * Quaternion.Euler(snapHandle.transform.localEulerAngles);
                obj.transform.position = controllerAttachPoint.transform.position - (snapHandle.transform.position - obj.transform.position);
            }
        }

        /// <summary>
        /// 把物体连接到手柄上
        /// </summary>
        /// <param name="obj">要抓取的物体游戏对象</param>
        private void SnapObjectToGrabToController(GameObject obj)
        {
            var objectScript = obj.GetComponent<VRTK_InteractableObject>();

            if (!objectScript.precisionSnap)
            {
                // 如果不是precisionSnap方式，就调用SetSnappedObjectPosition
                SetSnappedObjectPosition(obj);
            }

            if (objectScript.grabAttachMechanic == VRTK_InteractableObject.GrabAttachType.Child_Of_Controller)
            {
                // 如果要将物体作为手柄的子对象
                obj.transform.parent = controllerAttachPoint.transform;
            }
            else
            {
                // 否则为物体游戏对象建立关节连接
                CreateJoint(obj);
            }
        }

        /// <summary>
        /// CreateJoint方法为被抓取物体的游戏对象创建关节，连接到手柄上，关节的属性值来自于物体的交互脚本
        /// </summary>
        /// <param name="obj">被抓取物体的游戏对象</param>
        private void CreateJoint(GameObject obj)
        {
            var objectScript = obj.GetComponent<VRTK_InteractableObject>();

            if (objectScript.grabAttachMechanic == VRTK_InteractableObject.GrabAttachType.Fixed_Joint)
            {
                // 固定关节
                controllerAttachJoint = obj.AddComponent<FixedJoint>();
            }
            else if (objectScript.grabAttachMechanic == VRTK_InteractableObject.GrabAttachType.Spring_Joint)
            {
                // 弹簧关节
                SpringJoint tempSpringJoint = obj.AddComponent<SpringJoint>();
                tempSpringJoint.spring = objectScript.springJointStrength;
                tempSpringJoint.damper = objectScript.springJointDamper;
                if (objectScript.precisionSnap)
                {
                    // 把连接点转换为物体的局部坐标
                    tempSpringJoint.anchor = obj.transform.InverseTransformPoint(controllerAttachPoint.position);
                }
                controllerAttachJoint = tempSpringJoint;
            }
            controllerAttachJoint.breakForce = objectScript.detachThreshold;
            controllerAttachJoint.connectedBody = controllerAttachPoint;
        }

        /// <summary>
        /// 销毁关节，返回刚体组件
        /// </summary>
        /// <param name="withThrow"></param>
        /// <returns></returns>
        private Rigidbody ReleaseGrabbedObjectFromController(bool withThrow)
        {
            if (controllerAttachJoint != null)
            {
                return ReleaseAttachedObjectFromController(withThrow);
            }
            else
            {
                return ReleaseParentedObjectFromController();
            }
        }

        /// <summary>
        /// 销毁手柄与物体之间的关节controllerAttachJoint，返回物体的刚体组件
        /// </summary>
        /// <param name="withThrow">是否丢出</param>
        /// <returns>Rigidbody</returns>
        private Rigidbody ReleaseAttachedObjectFromController(bool withThrow)
        {
            var jointGameObject = controllerAttachJoint.gameObject;
            var rigidbody = jointGameObject.GetComponent<Rigidbody>();
            if (withThrow)
            {
                DestroyImmediate(controllerAttachJoint);
            }
            else
            {
                Destroy(controllerAttachJoint);
            }
            controllerAttachJoint = null;

            return rigidbody;
        }

        /// <summary>
        /// 返回被抓取物体的刚体组件，当关节不存在的时候调用这个方法
        /// </summary>
        /// <returns>Rigidbody</returns>
        private Rigidbody ReleaseParentedObjectFromController()
        {
            var rigidbody = grabbedObject.GetComponent<Rigidbody>();
            return rigidbody;
        }

        /// <summary>
        /// 丢出释放后的物体，为物体添加初速度
        /// </summary>
        /// <param name="rb">物体的刚体组件</param>
        /// <param name="controllerIndex">执行操作的手柄索引</param>
        /// <param name="objectThrowMultiplier"></param>
        private void ThrowReleasedObject(Rigidbody rb, uint controllerIndex, float objectThrowMultiplier)
        {
            var origin = trackedController.origin ? trackedController.origin : trackedController.transform.parent;
            var device = SteamVR_Controller.Input((int)controllerIndex);
            if (origin != null)
            {
                rb.velocity = origin.TransformDirection(device.velocity) * (throwMultiplier * objectThrowMultiplier);
                rb.angularVelocity = origin.TransformDirection(device.angularVelocity);
            }
            else
            {
                rb.velocity = device.velocity * (throwMultiplier * objectThrowMultiplier);
                rb.angularVelocity = device.angularVelocity;
            }
            rb.maxAngularVelocity = rb.angularVelocity.magnitude;
        }

        /// <summary>
        /// 初始化抓取物体对象，将其与手柄连接
        /// </summary>
        /// <returns>grabbedObject为空则返回false，否则返回true</returns>
        private bool GrabInteractedObject()
        {
            if (controllerAttachJoint == null && grabbedObject == null && IsObjectGrabbable(interactTouch.GetTouchedObject()))
            {
                InitGrabbedObject();
                if (grabbedObject)
                {
                    SnapObjectToGrabToController(grabbedObject);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 初始化抓取物体对象，为物体设置对应当前手柄的抓取部位
        /// </summary>
        /// <returns>grabbedObject为空则返回false，否则返回true</returns>
        private bool GrabTrackedObject()
        {
            if (grabbedObject == null && IsObjectGrabbable(interactTouch.GetTouchedObject()))
            {
                InitGrabbedObject();
                if (grabbedObject)
                {
                    var objectScript = grabbedObject.GetComponent<VRTK_InteractableObject>();
                    objectScript.SetGrabbedSnapHandle(GetSnapHandle(objectScript));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 初始化抓取可攀爬物体对象
        /// </summary>
        /// <returns>grabbedObject为空则返回false，否则返回true</returns>
        private bool GrabClimbObject()
        {
            if (grabbedObject == null && IsObjectGrabbable(interactTouch.GetTouchedObject()))
            {
                InitGrabbedObject();
                if (grabbedObject)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// InitGrabbedObject方法通过手柄上的touch脚本初始化当前抓取物体grabbedObject
        /// </summary>
        private void InitGrabbedObject()
        {
            grabbedObject = interactTouch.GetTouchedObject();
            if (grabbedObject)
            {
                var grabbedObjectScript = grabbedObject.GetComponent<VRTK_InteractableObject>();

                if (!grabbedObjectScript.IsValidInteractableController(gameObject, grabbedObjectScript.allowedGrabControllers))
                {
                    // 判断当前手柄是否允许抓取该物体游戏对象，如果不允许，置物体对象为null
                    grabbedObject = null;
                    return;
                }

                // 发送事件
                OnControllerGrabInteractableObject(interactTouch.SetControllerInteractEvent(grabbedObject));

                // 物体交互脚本相关操作，如关闭高亮等
                grabbedObjectScript.SaveCurrentState();
                grabbedObjectScript.Grabbed(gameObject);
                grabbedObjectScript.ZeroVelocity();
                grabbedObjectScript.ToggleHighlight(false);
                grabbedObjectScript.ToggleKinematic(false);

                //Pause collisions (if allowed on object) for a moment whilst sorting out position to prevent clipping issues
                // 暂停碰撞(如果物体允许此功能)一会儿避免卡住
                grabbedObjectScript.PauseCollisions();

                if (grabbedObjectScript.grabAttachMechanic == VRTK_InteractableObject.GrabAttachType.Child_Of_Controller)
                {
                    // 如果物体以子对象的方式连接手柄，那么isKinematic要为true，避免引起混乱
                    grabbedObjectScript.ToggleKinematic(true);
                }
                updatedHideControllerOnGrab = grabbedObjectScript.CheckHideMode(hideControllerOnGrab, grabbedObjectScript.hideControllerOnGrab);
            }

            if (updatedHideControllerOnGrab)
            {
                // 隐藏手柄
                Invoke("HideController", hideControllerDelay);
            }
        }

        /// <summary>
        /// 隐藏手柄
        /// </summary>
        private void HideController()
        {
            if (grabbedObject != null)
            {
                controllerActions.ToggleControllerModel(false, grabbedObject);
            }
        }

        /// <summary>
        /// UngrabInteractedObject方法用于取消抓取当前物体，根据参数来控制是否在释放的同时丢出物体
        /// </summary>
        /// <param name="controllerIndex">执行操作的手柄索引</param>
        /// <param name="withThrow">是否丢出去</param>
        private void UngrabInteractedObject(uint controllerIndex, bool withThrow)
        {
            if (grabbedObject != null)
            {
                Rigidbody releasedObjectRigidBody = ReleaseGrabbedObjectFromController(withThrow);
                if (withThrow)
                {
                    ThrowReleasedObject(releasedObjectRigidBody, controllerIndex, grabbedObject.GetComponent<VRTK_InteractableObject>().throwMultiplier);
                }
            }
            InitUngrabbedObject();
        }

        /// <summary>
        /// UngrabTrackedObject方法用于曲线抓取追踪连接物体
        /// </summary>
        private void UngrabTrackedObject()
        {
            if (grabbedObject != null)
            {
                InitUngrabbedObject();
            }
        }

        /// <summary>
        /// UngrabClimbObject方法用于曲线攀爬物体的抓取
        /// </summary>
        private void UngrabClimbObject()
        {
            if (grabbedObject != null)
            {
                InitUngrabbedObject();
            }
        }

        /// <summary>
        /// 为当前被抓取对象设置未抓取状态，重置参数
        /// </summary>
        private void InitUngrabbedObject()
        {
            // 发送事件通知正在监听ControllerUngrabInteractableObject事件的方法执行
            OnControllerUngrabInteractableObject(interactTouch.SetControllerInteractEvent(grabbedObject));
            if (grabbedObject != null)
            {
                // 物体交互脚本参数设置和关闭高亮
                grabbedObject.GetComponent<VRTK_InteractableObject>().Ungrabbed(gameObject);
                grabbedObject.GetComponent<VRTK_InteractableObject>().ToggleHighlight(false);
            }

            if (updatedHideControllerOnGrab)
            {
                // 重新显示手柄
                controllerActions.ToggleControllerModel(true, grabbedObject);
            }

            grabEnabledState = 0;// 重置按键次数
            grabbedObject = null;
        }

        /// <summary>
        /// 调用UngrabInteractedObject()方法释放物体
        /// </summary>
        /// <param name="controllerIndex">执行操作的手柄索引</param>
        /// <param name="withThrow">是否丢出去</param>
        private void ReleaseObject(uint controllerIndex, bool withThrow)
        {
            UngrabInteractedObject(controllerIndex, withThrow);
        }

        private GameObject GetGrabbableObject()
        {
            GameObject obj = interactTouch.GetTouchedObject();
            if (obj != null && interactTouch.IsObjectInteractable(obj))
            {
                return obj;
            }
            return grabbedObject;
        }

        /// <summary>
        /// 如果手柄正在触碰的物体对象不需要一直按着按钮来保持抓取状态，就增加grabEnabledState标记的值
        /// </summary>
        private void IncrementGrabState()
        {
            if (!IsObjectHoldOnGrab(interactTouch.GetTouchedObject()))
            {
                grabEnabledState++;
            }
        }

        /// <summary>
        /// AttemptGrabObject方法尝试抓取物体，首先获取一个可抓取的游戏对象，然后根据此对象的连接设置，调用不同的方法初始化grabbedObject
        /// 根据该对象的内部参数，来添加手柄的震动反馈
        /// 如果没有可以抓取的游戏对象就更新计时器
        /// </summary>
        private void AttemptGrabObject()
        {
            // 获取一个可抓取的游戏对象
            var objectToGrab = GetGrabbableObject();
            if (objectToGrab != null)
            {
                IncrementGrabState();
                // 生成被抓取对象是否成功
                var initialGrabAttempt = false;

                // 根据可抓取的游戏对象的连接设置，调用不同的方法初始化grabbedObject
                if (objectToGrab.GetComponent<VRTK_InteractableObject>().AttachIsTrackObject())
                {
                    initialGrabAttempt = GrabTrackedObject();
                }
                else if (objectToGrab.GetComponent<VRTK_InteractableObject>().AttachIsClimbObject())
                {
                    initialGrabAttempt = GrabClimbObject();
                }
                else
                {
                    initialGrabAttempt = GrabInteractedObject();
                }

                // 震动反馈处理
                if (grabbedObject && initialGrabAttempt)
                {
                    var rumbleAmount = grabbedObject.GetComponent<VRTK_InteractableObject>().rumbleOnGrab;
                    if (!rumbleAmount.Equals(Vector2.zero))
                    {
                        controllerActions.TriggerHapticPulse((ushort)rumbleAmount.y, rumbleAmount.x, 0.05f);
                    }
                }
            }
            else
            {
                // 如果没有可抓取的对象，更新计时器
                grabPrecognitionTimer = Time.time + grabPrecognition;
            }
        }

        /// <summary>
        /// CanRelease方法调取被抓取的物体grabbedObject的交互脚本，
        /// 根据脚本中`isDroppable`参数(使用grab按钮是否可以把已被抓取的物体放下. 如果为false那么一旦物体被抓取就不能被放下)
        /// 来判断该物体是不是能被释放。
        /// </summary>
        /// <returns></returns>
        private bool CanRelease()
        {
            return (grabbedObject && grabbedObject.GetComponent<VRTK_InteractableObject>().isDroppable);
        }

        /// <summary>
        /// AttemptReleaseObject方法尝试释放物体，但是要判断物体的可释放状态，首先该物体被抓取后是可以被释放的
        /// 并且1.该物体需要一直按着按钮保持抓取 2.或者该物体不需要一直按着按钮来保持抓取，但是抓取按钮被再次按下
        /// 如果上述条件都成立，就根据物体与手柄的连接方式来调用不同的释放物体的方法
        /// </summary>
        /// <param name="controllerIndex">执行操作的手柄索引</param>
        private void AttemptReleaseObject(uint controllerIndex)
        {
            // 如果当前被抓取的游戏对象可以被释放，并且1.该物体需要一直按着按钮保持抓取 2.或者该物体不需要一直按着按钮来保持抓取，但是抓取按钮被再次按下
            if (CanRelease() && (IsObjectHoldOnGrab(grabbedObject) || grabEnabledState >= 2))
            {
                // 根据被抓取物体的类型来取消抓取状态
                if (grabbedObject.GetComponent<VRTK_InteractableObject>().AttachIsTrackObject())
                {
                    UngrabTrackedObject();
                }
                else if (grabbedObject.GetComponent<VRTK_InteractableObject>().AttachIsClimbObject())
                {
                    UngrabClimbObject();
                }
                else
                {
                    // 其他连接方式的物体释放
                    ReleaseObject(controllerIndex, true);
                }
            }
        }

        private void DoGrabObject(object sender, ControllerInteractionEventArgs e)
        {
            AttemptGrabObject();
        }

        private void DoReleaseObject(object sender, ControllerInteractionEventArgs e)
        {
            AttemptReleaseObject(e.controllerIndex);
        }

        private void Update()
        {
            // 如果连接点还没有值，设置连接点
            if (controllerAttachPoint == null)
            {
                SetControllerAttachPoint();
            }

            // 如果勾选了createRigidBodyWhenNotTouching，当手柄按下抓取按钮时没有碰到一个可交互的物体，那么为手柄添加一个刚体组件，让手柄可以推开其他刚体对象.
            if (createRigidBodyWhenNotTouching && grabbedObject == null)
            {
                // 如果抓取按钮是按下的，而手柄刚体组件的isKinematic为true，或者抓取按钮没有按下，但是手柄刚体组件的isKinematic为false
                if (interactTouch.IsRigidBodyActive() != controllerEvents.grabPressed)
                {
                    // 抓取按钮按下时，设手柄刚体组件的isKinematic为false，抓取按钮没有按下时，设手柄刚体组件的isKinematic为true
                    interactTouch.ToggleControllerRigidBody(controllerEvents.grabPressed);
                }
            }

            // 即使没有按下按钮，也可以尝试抓取物体
            if (grabPrecognitionTimer >= Time.time)
            {
                if (GetGrabbableObject() != null)
                {
                    AttemptGrabObject();
                    if (GetGrabbedObject() != null)
                    {
                        grabPrecognitionTimer = 0f;
                    }
                }
            }
        }
    }
}