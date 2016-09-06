//====================================================================================
//
// Purpose: Provide basic touch detection of controller to interactable objects
//
// This script must be attached to a Controller within the [CameraRig] Prefab
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;


    /// <summary>
    /// 事件参数
    /// </summary>
    public struct ObjectInteractEventArgs
    {
        public uint controllerIndex;// 正在交互的手柄索引
        public GameObject target;// 正在和手柄交互的物体的游戏对象
    }

    public delegate void ObjectInteractEventHandler(object sender, ObjectInteractEventArgs e);

    [RequireComponent(typeof(VRTK_ControllerActions))]
    public class VRTK_InteractTouch : MonoBehaviour
    {
        // Inspector可见参数
        public bool hideControllerOnTouch = false;// 当发生有效触碰时是否隐藏手柄模型
        public float hideControllerDelay = 0f;// 当手柄触碰物体到隐藏手柄之间的延迟时间
        public Color globalTouchHighlightColor = Color.clear;// 如果可交互物体被触碰的时候可以被高亮，但是物体没有设置高亮颜色，那么使用这个全局高亮颜色.
        public GameObject customRigidbodyObject;// 如果需要额外定制刚体和碰撞体，那么可以通过这个参数来传递.如果为空，那么运行时系统会自动适配HTC Vive的默认手柄

        // 事件类
        public event ObjectInteractEventHandler ControllerTouchInteractableObject;// 触碰有效物体时发送事件，调用委托给自己的方法
        public event ObjectInteractEventHandler ControllerUntouchInteractableObject;// 不再触碰有效物体时发送事件，调用委托给自己的方法

        private GameObject touchedObject = null;// 正在被当前手柄触碰的游戏对象
        private GameObject lastTouchedObject = null;// 最新的手柄触碰游戏对象
        private bool updatedHideControllerOnTouch = false;

        private SteamVR_TrackedObject trackedController;
        private VRTK_ControllerActions controllerActions;// 手柄上的VRTK_ControllerActions脚本
        private GameObject controllerCollisionDetector;
        private bool triggerRumble;
        private bool destroyColliderOnDisable;
        private Rigidbody touchRigidBody;// 手柄上的刚体组件
        private Object defaultColliderPrefab;// 手柄上面的碰撞器的预置体

        // 下面两个是事件发送方法
        public virtual void OnControllerTouchInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerTouchInteractableObject != null)
            {
                ControllerTouchInteractableObject(this, e);
            }
        }

        public virtual void OnControllerUntouchInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUntouchInteractableObject != null)
            {
                ControllerUntouchInteractableObject(this, e);
            }
        }

        /// <summary>
        /// 设置事件参数
        /// </summary>
        /// <param name="target">游戏对象</param>
        /// <returns>ObjectInteractEventArgs</returns>
        public ObjectInteractEventArgs SetControllerInteractEvent(GameObject target)
        {
            ObjectInteractEventArgs e;
            e.controllerIndex = (uint)trackedController.index;
            e.target = target;
            return e;
        }

        /// <summary>
        /// ForceTouch方法会试图强制手柄去触碰给定的游戏对象. 当一个物体没有接触手柄，但是又需要被抓取或使用时，这个方法很有用.手柄无需接触物体，但是可以强制与它交互.
        /// </summary>
        /// <param name="obj">试图强行触碰的游戏对象</param>
        public void ForceTouch(GameObject obj)
        {

            // 手动调用脚本的OnTriggerStay()方法
            if (obj.GetComponent<Collider>())
            {
                OnTriggerStay(obj.GetComponent<Collider>());
            }
            else if (obj.GetComponentInChildren<Collider>())
            {
                OnTriggerStay(obj.GetComponentInChildren<Collider>());
            }
        }

        /// <summary>
        /// GetTouchedObject方法返回正在被当前手柄触碰的游戏对象
        /// </summary>
        /// <returns>正在被当前手柄触碰的游戏对象</returns>
        public GameObject GetTouchedObject()
        {
            return touchedObject;
        }

        /// <summary>
        /// 如果游戏对象绑定了 `VRTK_InteractableObject`且脚本可用返回`true`
        /// </summary>
        /// <param name="obj">需要判断是否可交互的游戏对象</param>
        /// <returns></returns>
        public bool IsObjectInteractable(GameObject obj)
        {
            if (obj)
            {
                var io = obj.GetComponent<VRTK_InteractableObject>();
                if (io)
                {
                    return io.enabled;
                }
                else
                {
                    io = obj.GetComponentInParent<VRTK_InteractableObject>();
                    if (io)
                    {
                        return io.enabled;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// ToggleControllerRigidBody方法控制手柄上面刚体组件的碰撞检测能力.如果它为真，那么手柄会与其他物体发生碰撞.
        /// 如果传入true，那么isTrigger/isKinematic为false
        /// 如果传入false，那么isTrigger/isKinematic为true
        /// </summary>
        /// <param name="state">手柄刚体碰撞能力开/关的状态. `true`打开，`false`关闭</param>
        public void ToggleControllerRigidBody(bool state)
        {

            if (controllerCollisionDetector && touchRigidBody)
            {
                touchRigidBody.isKinematic = !state;
                foreach (var collider in controllerCollisionDetector.GetComponents<Collider>())
                {
                    collider.isTrigger = !state;
                }

                foreach (var collider in controllerCollisionDetector.GetComponentsInChildren<Collider>())
                {
                    collider.isTrigger = !state;
                }
            }
        }

        /// <summary>
        /// IsRigidBodyActive方法用于检查前手柄的刚体组件是否可用并且可以影响场景中其他的刚体
        /// </summary>
        /// <returns>如果当前手柄的刚体组件可用并且可以影响场景中其他的刚体时返回真</returns>
        public bool IsRigidBodyActive()
        {
            return !touchRigidBody.isKinematic;
        }

        /// <summary>
        /// ForceStopTouching方法会停止手柄与物体的交互，即使在视觉上他们仍然是接触的
        /// </summary>
        public void ForceStopTouching()
        {
            if (touchedObject != null)
            {
                StopTouching(touchedObject);
            }
        }

        /// <summary>
        /// ControllerColliders方法检索返回所有与手柄相关的碰撞器
        /// </summary>
        /// <returns>与手柄关联的碰撞器数组</returns>
        public Collider[] ControllerColliders()
        {
            return (controllerCollisionDetector.GetComponents<Collider>().Length > 0 ? controllerCollisionDetector.GetComponents<Collider>() : controllerCollisionDetector.GetComponentsInChildren<Collider>());
        }

        private void Awake()
        {
            // 获取一些脚本
            trackedController = GetComponent<SteamVR_TrackedObject>();
            controllerActions = GetComponent<VRTK_ControllerActions>();
            Utilities.SetPlayerObject(gameObject, VRTK_PlayerObject.ObjectTypes.Controller);
            destroyColliderOnDisable = false;
            // 从预置体目录中加载HTCVive手柄的碰撞器预置体
            defaultColliderPrefab = Resources.Load("ControllerColliders/HTCVive");
        }

        private void OnEnable()
        {
            triggerRumble = false;
            CreateTouchCollider();// 初始化碰撞器
            CreateTouchRigidBody();// 初始化刚体组件
        }

        private void OnDisable()
        {
            ForceStopTouching();// 强制停止触碰
            DestroyTouchCollider();// 销毁碰撞器
        }

        /// <summary>
        /// 判断一个碰撞器对应的游戏对象是不是含有可交互脚本(物体是否可交互)，且返回这个可交互游戏对象
        /// </summary>
        /// <param name="collider">碰撞器对象</param>
        /// <returns>返回该游戏对象</returns>
        private GameObject GetColliderInteractableObject(Collider collider)
        {
            GameObject found = null;
            if (collider.gameObject.GetComponent<VRTK_InteractableObject>())
            {
                found = collider.gameObject;
            }
            else
            {
                found = collider.gameObject.GetComponentInParent<VRTK_InteractableObject>().gameObject;
            }
            return found;
        }

        private void OnTriggerEnter(Collider collider)
        {
            // 如果进入手柄trigger范围的碰撞器是可交互的物体，并且当前没有触碰的物体或者当前触碰的物体没有被抓取
            if (IsObjectInteractable(collider.gameObject) && (touchedObject == null || !touchedObject.GetComponent<VRTK_InteractableObject>().IsGrabbed()))
            {
                // 将进入范围的碰撞器对应的可交互对象保存为lastTouchedObject
                lastTouchedObject = GetColliderInteractableObject(collider);
            }
        }

        private void OnTriggerStay(Collider collider)
        {

            if (!enabled)
            {
                return;
            }

            if (touchedObject != null && touchedObject != lastTouchedObject && !touchedObject.GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                // 如果当前触碰物体不为空，并且和最新的触碰对象不一样，且此物体还没有被抓取，那么手柄应该触碰最新的进入范围的游戏对象

                CancelInvoke("ResetTriggerRumble");// 取消ResetTriggerRumble调用
                ResetTriggerRumble();// 调用ResetTriggerRumble
                ForceStopTouching();// 停止触碰当前的触碰物体,touchedObject = null
            }

            if (touchedObject == null && IsObjectInteractable(collider.gameObject))
            {
                // 如果当前没有正在触碰的物体且进入范围的碰撞器对应的游戏对象可以交互
                
                // 将停留在范围内的碰撞器对应的游戏对象作为当前触碰对象和最新触碰对象
                touchedObject = GetColliderInteractableObject(collider);
                lastTouchedObject = touchedObject;

                var touchedObjectScript = touchedObject.GetComponent<VRTK_InteractableObject>();

                if (!touchedObjectScript.IsValidInteractableController(gameObject, touchedObjectScript.allowedTouchControllers))
                {
                    // 如果新进来的物体不允许与当前手柄交互，那么就不能交互
                    touchedObject = null;
                    return;
                }

                // 是否隐藏手柄
                updatedHideControllerOnTouch = touchedObjectScript.CheckHideMode(hideControllerOnTouch, touchedObjectScript.hideControllerOnTouch);
                // 发送事件，回调委托给ControllerTouchInteractableObject的方法，参数为touchedObject
                OnControllerTouchInteractableObject(SetControllerInteractEvent(touchedObject));
                // 让可交互物体开启高亮
                touchedObjectScript.ToggleHighlight(true, globalTouchHighlightColor);
                touchedObjectScript.StartTouching(gameObject);

                if (controllerActions.IsControllerVisible() && updatedHideControllerOnTouch)
                {
                    // 如果当前手柄可见并且设定触碰时隐藏，就调用HideController方法
                    Invoke("HideController", hideControllerDelay);
                }

                // 处理手柄上的震动反馈，需要controllerActions
                // 从物体上的脚本获取震动数据
                var rumbleAmount = touchedObjectScript.rumbleOnTouch;
                if (!rumbleAmount.Equals(Vector2.zero) && !triggerRumble)
                {
                    // 如果震动不为0，并且还没有震动，就开始震动
                    triggerRumble = true;
                    controllerActions.TriggerHapticPulse((ushort)rumbleAmount.y, rumbleAmount.x, 0.05f);
                    // rumbleAmount.x秒后停止震动
                    Invoke("ResetTriggerRumble", rumbleAmount.x);
                }
            }
        }

        /// <summary>
        /// 重置震动状态
        /// </summary>
        private void ResetTriggerRumble()
        {
            triggerRumble = false;
        }

        private bool IsColliderChildOfTouchedObject(GameObject collider)
        {
            if (touchedObject != null && collider.GetComponentInParent<VRTK_InteractableObject>() && collider.GetComponentInParent<VRTK_InteractableObject>().gameObject == touchedObject)
            {
                return true;
            }
            return false;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (touchedObject != null && (touchedObject == collider.gameObject || IsColliderChildOfTouchedObject(collider.gameObject)))
            {
                StopTouching(collider.gameObject);
            }
        }

        /// <summary>
        /// 停止触碰一个游戏对象
        /// </summary>
        /// <param name="obj">要被停止触碰的游戏对象</param>
        private void StopTouching(GameObject obj)
        {
            if (IsObjectInteractable(obj))
            {
                GameObject untouched;
                if (obj.GetComponent<VRTK_InteractableObject>())
                {
                    // 如果是该对象绑定了交互脚本
                    untouched = obj;
                }
                else
                {
                    // 如果是该对象的父对象绑定了交互脚本
                    untouched = obj.GetComponentInParent<VRTK_InteractableObject>().gameObject;
                }

                // 发送事件，回调委托给ControllerUntouchInteractableObject执行的方法
                OnControllerUntouchInteractableObject(SetControllerInteractEvent(untouched.gameObject));
                // 关闭高亮
                untouched.GetComponent<VRTK_InteractableObject>().ToggleHighlight(false);
                // 物体中要置触碰对象为null并进行停止一些参数的重置
                untouched.GetComponent<VRTK_InteractableObject>().StopTouching(gameObject);
            }

            if (updatedHideControllerOnTouch)
            {
                // 如果设置了触碰时隐藏手柄，此时要重新打开手柄模型
                controllerActions.ToggleControllerModel(true, touchedObject);
            }
            // 重置当前触碰对象为null
            touchedObject = null;
        }

        /// <summary>
        /// 移除手柄下的controllerCollisionDetector
        /// </summary>
        private void DestroyTouchCollider()
        {
            if (destroyColliderOnDisable)
            {
                Destroy(controllerCollisionDetector);
            }
        }

        /// <summary>
        /// 检查定制的刚体对象是不是在手柄的子目录下
        /// </summary>
        /// <returns></returns>
        private bool CustomRigidBodyIsChild()
        {
            foreach (var childTransform in GetComponentsInChildren<Transform>())
            {
                // 遍历手柄的子目录下所有的Transform对象
                if (childTransform == customRigidbodyObject.transform)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 新建触碰碰撞器controllerCollisionDetector
        /// </summary>
        private void CreateTouchCollider()
        {
            if (customRigidbodyObject == null)
            {
                // 若没有定制刚体组件
                // 使用HTCVive手柄碰撞器的预置体，在当前手柄游戏对象下生成碰撞器
                controllerCollisionDetector = Instantiate(defaultColliderPrefab, transform.position, transform.rotation) as GameObject;
                controllerCollisionDetector.transform.SetParent(transform);
                controllerCollisionDetector.name = "ControllerColliders";
                destroyColliderOnDisable = true;
            }
            else
            {
                // 如果有另外自己指定手柄的碰撞器
                if (CustomRigidBodyIsChild())
                {
                    // 如果定制的碰撞器本来就是手柄的子对象，那么不需要新建，直接引用
                    controllerCollisionDetector = customRigidbodyObject;
                    destroyColliderOnDisable = false;
                }
                else
                {
                    // 如果手柄的子对象里没有定制的碰撞器，就要新建一个
                    controllerCollisionDetector = Instantiate(customRigidbodyObject, transform.position, transform.rotation) as GameObject;
                    controllerCollisionDetector.transform.SetParent(transform);
                    destroyColliderOnDisable = true;
                }
            }
        }

        /// <summary>
        /// 如果手柄没有刚体组件则新建触碰刚体组件touchRigidBody
        /// </summary>
        private void CreateTouchRigidBody()
        {
            touchRigidBody = gameObject.GetComponent<Rigidbody>();
            if (touchRigidBody == null)
            {
                touchRigidBody = gameObject.AddComponent<Rigidbody>();
                touchRigidBody.isKinematic = true;
                touchRigidBody.useGravity = false;
                touchRigidBody.constraints = RigidbodyConstraints.FreezeAll;
                touchRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        /// <summary>
        /// 隐藏手柄模型
        /// </summary>
        private void HideController()
        {
            if (touchedObject != null)
            {
                controllerActions.ToggleControllerModel(false, touchedObject);
            }
        }
    }
}