//====================================================================================
//
// Purpose: Provide ability to use an interactable object when it is being touched
//
// This script must be attached to a Controller within the [CameraRig] Prefab
//
// The VRTK_ControllerEvents and VRTK_InteractTouch scripts must also be
// attached to the Controller
//
// Press the default 'Trigger' button on the controller to use an object
// Released the default 'Trigger' button on the controller to stop using an object
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;

    [RequireComponent(typeof(VRTK_InteractTouch)), RequireComponent(typeof(VRTK_ControllerEvents))]
    public class VRTK_InteractUse : MonoBehaviour
    {
        public bool hideControllerOnUse = false;// 当有效的使用行为开始时是否隐藏手柄
        public float hideControllerDelay = 0f;// 隐藏手柄前等待的时间，单位为秒

        public event ObjectInteractEventHandler ControllerUseInteractableObject;// 当有效对象开始被使用时通知正在监听本事件的方法开始调用
        public event ObjectInteractEventHandler ControllerUnuseInteractableObject;// 当有效对象停止被使用时通知正在监听本事件的方法开始调用

        private GameObject usingObject = null;// 手柄当前使用的游戏对象
        private VRTK_InteractTouch interactTouch;// 手柄上的touch脚本
        private VRTK_ControllerActions controllerActions;// 手柄上的action脚本
        private bool updatedHideControllerOnUse = false;// 综合手柄和游戏对象设置后的手柄隐藏指令

        public virtual void OnControllerUseInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUseInteractableObject != null)
            {
                ControllerUseInteractableObject(this, e);
            }
        }

        public virtual void OnControllerUnuseInteractableObject(ObjectInteractEventArgs e)
        {
            if (ControllerUnuseInteractableObject != null)
            {
                ControllerUnuseInteractableObject(this, e);
            }
        }

        /// <summary>
        /// GetUsingObject方法返回正在被当前手柄使用的游戏对象
        /// </summary>
        /// <returns>`GameObject` - 正在被当前手柄使用的游戏对象</returns>
        public GameObject GetUsingObject()
        {
            return usingObject;
        }

        /// <summary>
        /// ForceStopUsing方法会强制手柄停止对正在接触的游戏对象的使用行为，同时设置交互对象脚本的参数`UsingState`的值为0
        /// </summary>
        public void ForceStopUsing()
        {
            if (usingObject != null)
            {
                StopUsing();
            }
        }

        /// <summary>
        /// ForceResetUsing方法会强制手柄停止对正在接触的游戏对象的使用行为，但是不修改交互对象脚本的参数`UsingState`的值
        /// </summary>
        public void ForceResetUsing()
        {
            if (usingObject != null)
            {
                UnuseInteractedObject(false);
            }
        }

        private void Awake()
        {
            if (GetComponent<VRTK_InteractTouch>() == null)
            {
                Debug.LogError("VRTK_InteractUse is required to be attached to a SteamVR Controller that has the VRTK_InteractTouch script attached to it");
                return;
            }

            interactTouch = GetComponent<VRTK_InteractTouch>();
            controllerActions = GetComponent<VRTK_ControllerActions>();
        }

        private void OnEnable()
        {
            if (GetComponent<VRTK_ControllerEvents>() == null)
            {
                Debug.LogError("VRTK_InteractUse is required to be attached to a SteamVR Controller that has the VRTK_ControllerEvents script attached to it");
                return;
            }

            GetComponent<VRTK_ControllerEvents>().AliasUseOn += new ControllerInteractionEventHandler(DoStartUseObject);
            GetComponent<VRTK_ControllerEvents>().AliasUseOff += new ControllerInteractionEventHandler(DoStopUseObject);
        }

        private void OnDisable()
        {
            ForceStopUsing();
            GetComponent<VRTK_ControllerEvents>().AliasUseOn -= new ControllerInteractionEventHandler(DoStartUseObject);
            GetComponent<VRTK_ControllerEvents>().AliasUseOff -= new ControllerInteractionEventHandler(DoStopUseObject);
        }

        private bool IsObjectUsable(GameObject obj)
        {
            return (interactTouch.IsObjectInteractable(obj) && obj.GetComponent<VRTK_InteractableObject>().isUsable);
        }

        private bool IsObjectHoldOnUse(GameObject obj)
        {
            return (obj && obj.GetComponent<VRTK_InteractableObject>() && obj.GetComponent<VRTK_InteractableObject>().holdButtonToUse);
        }

        private int GetObjectUsingState(GameObject obj)
        {
            if (obj && obj.GetComponent<VRTK_InteractableObject>())
            {
                return obj.GetComponent<VRTK_InteractableObject>().UsingState;
            }
            return 0;
        }

        /// <summary>
        /// SetObjectUsingState方法用来设置交互对象脚本的参数`UsingState`的值。
        /// </summary>
        /// <param name="obj">交互对象</param>
        /// <param name="value">欲设置的值</param>
        private void SetObjectUsingState(GameObject obj, int value)
        {
            if (obj && obj.GetComponent<VRTK_InteractableObject>())
            {
                obj.GetComponent<VRTK_InteractableObject>().UsingState = value;
            }
        }

        /// <summary>
        /// UseInteractedObject方法用来开始使用手柄正在接触的游戏对象
        /// </summary>
        /// <param name="touchedObject">手柄当前接触的游戏对象</param>
        private void UseInteractedObject(GameObject touchedObject)
        {
            // 如果手柄第一次使用游戏对象或者之前使用的游戏对象和手柄现在接触的游戏对象不一致，且现在接触的游戏对象是可以使用的时候
            if ((usingObject == null || usingObject != touchedObject) && IsObjectUsable(touchedObject))
            {
                usingObject = touchedObject;
                var usingObjectScript = usingObject.GetComponent<VRTK_InteractableObject>();

                if (!usingObjectScript.IsValidInteractableController(gameObject, usingObjectScript.allowedUseControllers))
                {
                    usingObject = null;
                    return;
                }

                updatedHideControllerOnUse = usingObjectScript.CheckHideMode(hideControllerOnUse, usingObjectScript.hideControllerOnUse);

                // 发送事件给监听的方法，开始调用
                OnControllerUseInteractableObject(interactTouch.SetControllerInteractEvent(usingObject));

                // 调用交互对象的StartUsing
                usingObjectScript.StartUsing(gameObject);

                if (updatedHideControllerOnUse)
                {
                    // 隐藏手柄模型renderer
                    Invoke("HideController", hideControllerDelay);
                }

                // 关闭高亮
                usingObjectScript.ToggleHighlight(false);

                // 震动反馈
                var rumbleAmount = usingObjectScript.rumbleOnUse;
                if (!rumbleAmount.Equals(Vector2.zero))
                {
                    controllerActions.TriggerHapticPulse((ushort)rumbleAmount.y, rumbleAmount.x, 0.05f);
                }
            }
        }

        /// <summary>
        /// HideController方法调用VRTK_ControllerActions脚本来隐藏手柄模型的renderer
        /// </summary>
        private void HideController()
        {
            if (usingObject != null)
            {
                controllerActions.ToggleControllerModel(false, usingObject);
            }
        }

        /// <summary>
        /// UnuseInteractedObject方法停止当前usingObject对象的使用
        /// </summary>
        /// <param name="completeStop"></param>
        private void UnuseInteractedObject(bool completeStop)
        {
            if (usingObject != null)
            {
                OnControllerUnuseInteractableObject(interactTouch.SetControllerInteractEvent(usingObject));
                if (completeStop)
                {
                    usingObject.GetComponent<VRTK_InteractableObject>().StopUsing(gameObject);
                }
                if (updatedHideControllerOnUse)
                {
                    controllerActions.ToggleControllerModel(true, usingObject);
                }
                if (completeStop)
                {
                    usingObject.GetComponent<VRTK_InteractableObject>().ToggleHighlight(false);
                }
                usingObject = null;
            }
        }

        /// <summary>
        /// GetFromGrab从手柄的grab脚本获取到被手柄抓取到的游戏对象
        /// </summary>
        /// <returns></returns>
        private GameObject GetFromGrab()
        {
            if (GetComponent<VRTK_InteractGrab>())
            {
                return GetComponent<VRTK_InteractGrab>().GetGrabbedObject();
            }
            return null;
        }

        /// <summary>
        /// StopUsing方法调用UnuseInteractedObject()并且设置usingObject的UsingState为0
        /// </summary>
        private void StopUsing()
        {
            SetObjectUsingState(usingObject, 0);
            UnuseInteractedObject(true);
        }

        /// <summary>
        /// DoStartUseObject方法会监听VRTK_ControllerEvents中的AliasUseOn事件，当使用按钮按下时，会自动地调用
        /// 调用UseInteractedObject方法，更新游戏对象的UsingState值加一
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoStartUseObject(object sender, ControllerInteractionEventArgs e)
        {
            GameObject touchedObject = interactTouch.GetTouchedObject();
            if (touchedObject == null)
            {
                touchedObject = GetFromGrab();
            }

            if (touchedObject != null && interactTouch.IsObjectInteractable(touchedObject))
            {
                var interactableObjectScript = touchedObject.GetComponent<VRTK_InteractableObject>();

                if (interactableObjectScript.useOnlyIfGrabbed && !interactableObjectScript.IsGrabbed())
                {
                    // 如果手柄正在接触的游戏对象设定了自己只有在被抓取的时候才能使用，而此时该对象并没有被手柄抓取，那么不能使用
                    return;
                }

                UseInteractedObject(touchedObject);
                if (usingObject && !IsObjectHoldOnUse(usingObject))
                {
                    // 如果手柄当前使用的游戏对象不需要一直按着按钮才能持续使用，就更新其UsingState加一，下次按按钮就会停止使用
                    SetObjectUsingState(usingObject, GetObjectUsingState(usingObject) + 1);
                }
            }
        }

        /// <summary>
        /// DoStopUseObject方法会监听VRTK_ControllerEvents的AliasUseOff事件，当使用按钮释放时，自动调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoStopUseObject(object sender, ControllerInteractionEventArgs e)
        {
            // 如果游戏对象需要一直按着按钮才能保持使用，那么无论什么时候，只有使用按钮释放，则停止使用
            // 或者该游戏对象不需要一直按着按钮也可以保持使用，但是UsingState大于等于2，即使用按钮是第二次按下，则停止使用
            if (IsObjectHoldOnUse(usingObject) || GetObjectUsingState(usingObject) >= 2)
            {
                StopUsing();
            }
        }
    }
}