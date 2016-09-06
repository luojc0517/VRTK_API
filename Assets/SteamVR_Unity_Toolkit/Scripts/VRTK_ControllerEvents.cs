namespace VRTK
{
    using UnityEngine;

    public struct ControllerInteractionEventArgs
    {
        public uint controllerIndex;
        public float buttonPressure;
        public Vector2 touchpadAxis;
        public float touchpadAngle;
    }

    public delegate void ControllerInteractionEventHandler(object sender, ControllerInteractionEventArgs e);

    public class VRTK_ControllerEvents : MonoBehaviour
    {
        public enum ButtonAlias
        {
            Trigger_Hairline,
            Trigger_Touch,
            Trigger_Press,
            Trigger_Click,
            Grip,
            Touchpad_Touch,
            Touchpad_Press,
            Application_Menu,
            Undefined
        }

        public ButtonAlias pointerToggleButton = ButtonAlias.Touchpad_Press;
        public ButtonAlias pointerSetButton = ButtonAlias.Touchpad_Press;
        public ButtonAlias grabToggleButton = ButtonAlias.Grip;
        public ButtonAlias useToggleButton = ButtonAlias.Trigger_Click;
        public ButtonAlias uiClickButton = ButtonAlias.Trigger_Click;
        public ButtonAlias menuToggleButton = ButtonAlias.Application_Menu;

        public int axisFidelity = 1;

        [HideInInspector]
        public bool triggerPressed = false;
        [HideInInspector]
        public bool triggerTouched = false;
        [HideInInspector]
        public bool triggerHairlinePressed = false;
        [HideInInspector]
        public bool triggerClicked = false;
        [HideInInspector]
        public bool triggerAxisChanged = false;
        [HideInInspector]
        public bool applicationMenuPressed = false;
        [HideInInspector]
        public bool touchpadPressed = false;
        [HideInInspector]
        public bool touchpadTouched = false;
        [HideInInspector]
        public bool touchpadAxisChanged = false;
        [HideInInspector]
        public bool gripPressed = false;

        [HideInInspector]
        public bool pointerPressed = false;
        [HideInInspector]
        public bool grabPressed = false;
        [HideInInspector]
        public bool usePressed = false;
        [HideInInspector]
        public bool uiClickPressed = false;
        [HideInInspector]
        public bool menuPressed = false;

        public event ControllerInteractionEventHandler TriggerPressed;
        public event ControllerInteractionEventHandler TriggerReleased;

        public event ControllerInteractionEventHandler TriggerTouchStart;
        public event ControllerInteractionEventHandler TriggerTouchEnd;

        public event ControllerInteractionEventHandler TriggerHairlineStart;
        public event ControllerInteractionEventHandler TriggerHairlineEnd;

        public event ControllerInteractionEventHandler TriggerClicked;
        public event ControllerInteractionEventHandler TriggerUnclicked;

        public event ControllerInteractionEventHandler TriggerAxisChanged;

        public event ControllerInteractionEventHandler ApplicationMenuPressed;
        public event ControllerInteractionEventHandler ApplicationMenuReleased;

        public event ControllerInteractionEventHandler GripPressed;
        public event ControllerInteractionEventHandler GripReleased;

        public event ControllerInteractionEventHandler TouchpadPressed;
        public event ControllerInteractionEventHandler TouchpadReleased;

        public event ControllerInteractionEventHandler TouchpadTouchStart;
        public event ControllerInteractionEventHandler TouchpadTouchEnd;

        public event ControllerInteractionEventHandler TouchpadAxisChanged;

        public event ControllerInteractionEventHandler AliasPointerOn;
        public event ControllerInteractionEventHandler AliasPointerOff;
        public event ControllerInteractionEventHandler AliasPointerSet;

        public event ControllerInteractionEventHandler AliasGrabOn;
        public event ControllerInteractionEventHandler AliasGrabOff;

        public event ControllerInteractionEventHandler AliasUseOn;
        public event ControllerInteractionEventHandler AliasUseOff;

        public event ControllerInteractionEventHandler AliasMenuOn;
        public event ControllerInteractionEventHandler AliasMenuOff;

        public event ControllerInteractionEventHandler AliasUIClickOn;
        public event ControllerInteractionEventHandler AliasUIClickOff;

        //和SteamVR有关的设备映射追踪信息
        private uint controllerIndex;
        private SteamVR_TrackedObject trackedController;
        private SteamVR_Controller.Device device;

        private Vector2 touchpadAxis = Vector2.zero;
        private Vector2 triggerAxis = Vector2.zero;
        private float hairTriggerDelta;

        private Vector3 controllerVelocity = Vector3.zero;
        private Vector3 controllerAngularVelocity = Vector3.zero;

        public virtual void OnTriggerPressed(ControllerInteractionEventArgs e)
        {
            if (TriggerPressed != null)
            {
                //真正调用
                TriggerPressed(this, e);
            }
        }

        public virtual void OnTriggerReleased(ControllerInteractionEventArgs e)
        {
            if (TriggerReleased != null)
            {
                TriggerReleased(this, e);
            }
        }

        public virtual void OnTriggerTouchStart(ControllerInteractionEventArgs e)
        {
            if (TriggerTouchStart != null)
            {
                TriggerTouchStart(this, e);
            }
        }

        public virtual void OnTriggerTouchEnd(ControllerInteractionEventArgs e)
        {
            if (TriggerTouchEnd != null)
            {
                TriggerTouchEnd(this, e);
            }
        }

        public virtual void OnTriggerHairlineStart(ControllerInteractionEventArgs e)
        {
            if (TriggerHairlineStart != null)
            {
                TriggerHairlineStart(this, e);
            }
        }

        public virtual void OnTriggerHairlineEnd(ControllerInteractionEventArgs e)
        {
            if (TriggerHairlineEnd != null)
            {
                TriggerHairlineEnd(this, e);
            }
        }

        public virtual void OnTriggerClicked(ControllerInteractionEventArgs e)
        {
            if (TriggerClicked != null)
            {
                TriggerClicked(this, e);
            }
        }

        public virtual void OnTriggerUnclicked(ControllerInteractionEventArgs e)
        {
            if (TriggerUnclicked != null)
            {
                TriggerUnclicked(this, e);
            }
        }

        public virtual void OnTriggerAxisChanged(ControllerInteractionEventArgs e)
        {
            if (TriggerAxisChanged != null)
            {
                TriggerAxisChanged(this, e);
            }
        }

        public virtual void OnApplicationMenuPressed(ControllerInteractionEventArgs e)
        {
            if (ApplicationMenuPressed != null)
            {
                ApplicationMenuPressed(this, e);
            }
        }

        public virtual void OnApplicationMenuReleased(ControllerInteractionEventArgs e)
        {
            if (ApplicationMenuReleased != null)
            {
                ApplicationMenuReleased(this, e);
            }
        }

        public virtual void OnGripPressed(ControllerInteractionEventArgs e)
        {
            if (GripPressed != null)
            {
                GripPressed(this, e);
            }
        }

        public virtual void OnGripReleased(ControllerInteractionEventArgs e)
        {
            if (GripReleased != null)
            {
                GripReleased(this, e);
            }
        }

        public virtual void OnTouchpadPressed(ControllerInteractionEventArgs e)
        {
            if (TouchpadPressed != null)
            {
                TouchpadPressed(this, e);
            }
        }

        public virtual void OnTouchpadReleased(ControllerInteractionEventArgs e)
        {
            if (TouchpadReleased != null)
            {
                TouchpadReleased(this, e);
            }
        }

        public virtual void OnTouchpadTouchStart(ControllerInteractionEventArgs e)
        {
            if (TouchpadTouchStart != null)
            {
                TouchpadTouchStart(this, e);
            }
        }

        public virtual void OnTouchpadTouchEnd(ControllerInteractionEventArgs e)
        {
            if (TouchpadTouchEnd != null)
            {
                TouchpadTouchEnd(this, e);
            }
        }

        public virtual void OnTouchpadAxisChanged(ControllerInteractionEventArgs e)
        {
            if (TouchpadAxisChanged != null)
            {
                TouchpadAxisChanged(this, e);
            }
        }

        public virtual void OnAliasPointerOn(ControllerInteractionEventArgs e)
        {
            if (AliasPointerOn != null)
            {
                AliasPointerOn(this, e);
            }
        }

        public virtual void OnAliasPointerOff(ControllerInteractionEventArgs e)
        {
            if (AliasPointerOff != null)
            {
                AliasPointerOff(this, e);
            }
        }

        public virtual void OnAliasPointerSet(ControllerInteractionEventArgs e)
        {
            if (AliasPointerSet != null)
            {
                AliasPointerSet(this, e);
            }
        }

        public virtual void OnAliasGrabOn(ControllerInteractionEventArgs e)
        {
            if (AliasGrabOn != null)
            {
                AliasGrabOn(this, e);
            }
        }

        public virtual void OnAliasGrabOff(ControllerInteractionEventArgs e)
        {
            if (AliasGrabOff != null)
            {
                AliasGrabOff(this, e);
            }
        }

        public virtual void OnAliasUseOn(ControllerInteractionEventArgs e)
        {
            if (AliasUseOn != null)
            {
                AliasUseOn(this, e);
            }
        }

        public virtual void OnAliasUseOff(ControllerInteractionEventArgs e)
        {
            if (AliasUseOff != null)
            {
                AliasUseOff(this, e);
            }
        }

        public virtual void OnAliasUIClickOn(ControllerInteractionEventArgs e)
        {
            if (AliasUIClickOn != null)
            {
                AliasUIClickOn(this, e);
            }
        }

        public virtual void OnAliasUIClickOff(ControllerInteractionEventArgs e)
        {
            if (AliasUIClickOff != null)
            {
                AliasUIClickOff(this, e);
            }
        }

        public virtual void OnAliasMenuOn(ControllerInteractionEventArgs e)
        {
            if (AliasMenuOn != null)
            {
                AliasMenuOn(this, e);
            }
        }

        public virtual void OnAliasMenuOff(ControllerInteractionEventArgs e)
        {
            if (AliasMenuOff != null)
            {
                AliasMenuOff(this, e);
            }
        }

        /// <summary>
        /// GetVelocity方法得到Controller的物理速度.这个方法可以用来确定Controller是否在摇摆以及它运动的方向.
        /// </summary>
        /// <returns>Vector3 - 一个三维向量，包含Controller当前在世界坐标系下的物理速度.</returns>
        public Vector3 GetVelocity()
        {
            SetVelocity();
            return controllerVelocity;
        }

        /// <summary>
        /// GetAngularVelocity方法得到Controller的旋转速度.这个方法可以用来确定Controller是否在旋转以及得到旋转的速度
        /// </summary>
        /// <returns>Vector3 - 一个三维向量，包含Controller当前在世界坐标系下的角速度(旋转速度).</returns>
        public Vector3 GetAngularVelocity()
        {
            SetVelocity();
            return controllerAngularVelocity;
        }

        /// <summary>
        /// 直接从touchpad获取当前被触碰点的坐标. x代表水平值，y代表垂直值.
        /// </summary>
        /// <returns>一个2维向量，包含touchpad被触碰的坐标(x,y). (0,0) 到 (1,1).</returns>
        public Vector2 GetTouchpadAxis()
        {
            return touchpadAxis;
        }

        /// <summary>
        /// GetTouchpadAxisAngle方法获取touchpad触摸的角度，正上方为0度，正下方为180度
        /// </summary>
        /// <returns>一个float变量，代表touchpad触碰点的角度，touchpad正中心为原点. 0f 到 360f.</returns>
        public float GetTouchpadAxisAngle()
        {
            return CalculateTouchpadAxisAngle(touchpadAxis);
        }

        /// <summary>
        /// GetTriggerAxis方法返回一个float值，代表trigger被按压的数值.可以用来处理高精度的任务，或者用于设置按压的数值达到阈值时触发trigger press.
        /// </summary>
        /// <returns>一个float值，代表trigger被按压的数值. 0f 到 1f.</returns>
        public float GetTriggerAxis()
        {
            return triggerAxis.x;
        }

        /// <summary>
        /// GetHairTriggerDelta方法返回一个float值，代表相对于hairline阈值，trigger按压的压力变化值.
        /// </summary>
        /// <returns>一个float值，代表相对于hairline阈值，trigger按压的压力变化值.</returns>
        public float GetHairTriggerDelta()
        {
            return hairTriggerDelta;
        }

        /// <summary>
        /// AnyButtonPressed返回真，如果任何Controller上的按钮被按下,可以确定当用户使用Controller时什么行为可以触发.
        /// </summary>
        /// <returns>一个bool值，当任何按钮被按下时返回真</returns>
        public bool AnyButtonPressed()
        {
            return (triggerClicked || triggerHairlinePressed || triggerTouched || triggerPressed || gripPressed || touchpadPressed || applicationMenuPressed);
        }


        /// <summary>
        /// 返回一个ControllerInteractionEventArgs，更新按钮的bool状态和事件装载参数(index,按压值，touchpad坐标等)信息
        /// </summary>
        /// <param name="buttonBool">按钮对应的bool变量</param>
        /// <param name="value">bool值</param>
        /// <param name="buttonPressure">按钮按压值</param>
        /// <returns>ControllerInteractionEventArgs</returns>
        private ControllerInteractionEventArgs SetButtonEvent(ref bool buttonBool, bool value, float buttonPressure)
        {
            buttonBool = value;
            ControllerInteractionEventArgs e;
            e.controllerIndex = controllerIndex;
            e.buttonPressure = buttonPressure;
            e.touchpadAxis = device.GetAxis();//调用SteamVR API获取当前的坐标
            e.touchpadAngle = CalculateTouchpadAxisAngle(e.touchpadAxis);
            return e;
        }

        private void Awake()
        {
            trackedController = GetComponent<SteamVR_TrackedObject>();
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        private void Start()
        {
            //初始化device，建立手柄和脚本的映射
            controllerIndex = (uint)trackedController.index;
            if (controllerIndex < uint.MaxValue)
            {
                device = SteamVR_Controller.Input((int)controllerIndex);
            }
        }

        /// <summary>
        /// 通过二维坐标来计算touchpad触摸点的角度
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private float CalculateTouchpadAxisAngle(Vector2 axis)
        {
            //tan值=y/x，根据tan值计算角度
            float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            angle = 90.0f - angle;
            if (angle < 0)
            {
                angle += 360.0f;
            }
            return angle;
        }


        /// <summary>
        /// 别名按钮事件发送(根据最后一个参数可以容易看出该别名对应的是哪个按钮)，发送按钮事件，更新按钮状态
        /// </summary>
        /// <param name="type">按钮类型</param>
        /// <param name="touchDown">是否按下</param>
        /// <param name="buttonPressure">按压值</param>
        /// <param name="buttonBool">按钮bool状态</param>
        private void EmitAlias(ButtonAlias type, bool touchDown, float buttonPressure, ref bool buttonBool)
        {
            if (pointerToggleButton == type)
            {
                if (touchDown)
                {
                    pointerPressed = true;
                    OnAliasPointerOn(SetButtonEvent(ref buttonBool, true, buttonPressure));
                }
                else
                {
                    pointerPressed = false;
                    OnAliasPointerOff(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }

            if (pointerSetButton == type)
            {
                if (!touchDown)
                {
                    OnAliasPointerSet(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }

            if (grabToggleButton == type)
            {
                if (touchDown)
                {
                    grabPressed = true;
                    OnAliasGrabOn(SetButtonEvent(ref buttonBool, true, buttonPressure));
                }
                else
                {
                    grabPressed = false;
                    OnAliasGrabOff(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }

            if (useToggleButton == type)
            {
                if (touchDown)
                {
                    usePressed = true;
                    OnAliasUseOn(SetButtonEvent(ref buttonBool, true, buttonPressure));
                }
                else
                {
                    usePressed = false;
                    OnAliasUseOff(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }

            if (uiClickButton == type)
            {
                if (touchDown)
                {
                    uiClickPressed = true;
                    OnAliasUIClickOn(SetButtonEvent(ref buttonBool, true, buttonPressure));
                }
                else
                {
                    uiClickPressed = false;
                    OnAliasUIClickOff(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }

            if (menuToggleButton == type)
            {
                if (touchDown)
                {
                    menuPressed = true;
                    OnAliasMenuOn(SetButtonEvent(ref buttonBool, true, buttonPressure));
                }
                else
                {
                    menuPressed = false;
                    OnAliasMenuOff(SetButtonEvent(ref buttonBool, false, buttonPressure));
                }
            }
        }

        /// <summary>
        /// 比较vectorA和vectorB是否一样，通过转换为字符串比对
        /// </summary>
        /// <param name="vectorA"></param>
        /// <param name="vectorB"></param>
        /// <returns></returns>
        private bool Vector2ShallowEquals(Vector2 vectorA, Vector2 vectorB)
        {
            return (vectorA.x.ToString("F" + axisFidelity) == vectorB.x.ToString("F" + axisFidelity) &&
                    vectorA.y.ToString("F" + axisFidelity) == vectorB.y.ToString("F" + axisFidelity));
        }

        private void OnDisable()
        {
            //在0.1s内调用DisableEvents()，禁用所有事件发送
            Invoke("DisableEvents", 0.1f);
        }

        /// <summary>
        /// 禁用，还原，但是保存touchpad和trigger的坐标等
        /// </summary>
        private void DisableEvents()
        {
            if (triggerPressed)
            {
                OnTriggerReleased(SetButtonEvent(ref triggerPressed, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Press, false, 0f, ref triggerPressed);
            }

            if (triggerTouched)
            {
                OnTriggerTouchEnd(SetButtonEvent(ref triggerTouched, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Touch, false, 0f, ref triggerTouched);
            }

            if (triggerHairlinePressed)
            {
                OnTriggerHairlineEnd(SetButtonEvent(ref triggerHairlinePressed, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Hairline, false, 0f, ref triggerHairlinePressed);
            }

            if (triggerClicked)
            {
                OnTriggerUnclicked(SetButtonEvent(ref triggerClicked, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Click, false, 0f, ref triggerClicked);
            }

            if (applicationMenuPressed)
            {
                OnApplicationMenuReleased(SetButtonEvent(ref applicationMenuPressed, false, 0f));
                EmitAlias(ButtonAlias.Application_Menu, false, 0f, ref applicationMenuPressed);
            }

            if (gripPressed)
            {
                OnGripReleased(SetButtonEvent(ref gripPressed, false, 0f));
                EmitAlias(ButtonAlias.Grip, false, 0f, ref gripPressed);
            }

            if (touchpadPressed)
            {
                OnTouchpadReleased(SetButtonEvent(ref touchpadPressed, false, 0f));
                EmitAlias(ButtonAlias.Touchpad_Press, false, 0f, ref touchpadPressed);
            }

            if (touchpadTouched)
            {
                OnTouchpadTouchEnd(SetButtonEvent(ref touchpadTouched, false, 0f));
                EmitAlias(ButtonAlias.Touchpad_Touch, false, 0f, ref touchpadTouched);
            }

            triggerAxisChanged = false;
            touchpadAxisChanged = false;

            controllerIndex = (uint)trackedController.index;
            if (controllerIndex < uint.MaxValue)
            {
                device = SteamVR_Controller.Input((int)controllerIndex);

                Vector2 currentTriggerAxis = device.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
                Vector2 currentTouchpadAxis = device.GetAxis();

                // 保存当前的touchpad和trigger的设置.
                touchpadAxis = new Vector2(currentTouchpadAxis.x, currentTouchpadAxis.y);
                triggerAxis = new Vector2(currentTriggerAxis.x, currentTriggerAxis.y);
                hairTriggerDelta = device.hairTriggerDelta;
            }
        }

        private void Update()
        {
            controllerIndex = (uint)trackedController.index;
            //Only continue if the controller index has been set to a sensible number
            //SteamVR seems to put the index to the uint max value if it can't find the controller
            if (controllerIndex >= uint.MaxValue)
            {
                return;
            }

            device = SteamVR_Controller.Input((int)controllerIndex);

            Vector2 currentTriggerAxis = device.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            Vector2 currentTouchpadAxis = device.GetAxis();

            //Trigger Pressed
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerPressed(SetButtonEvent(ref triggerPressed, true, currentTriggerAxis.x));
                EmitAlias(ButtonAlias.Trigger_Press, true, currentTriggerAxis.x, ref triggerPressed);
            }
            else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerReleased(SetButtonEvent(ref triggerPressed, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Press, false, 0f, ref triggerPressed);
            }

            //Trigger Touched
            if (device.GetTouchDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerTouchStart(SetButtonEvent(ref triggerTouched, true, currentTriggerAxis.x));
                EmitAlias(ButtonAlias.Trigger_Touch, true, currentTriggerAxis.x, ref triggerTouched);
            }
            else if (device.GetTouchUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerTouchEnd(SetButtonEvent(ref triggerTouched, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Touch, false, 0f, ref triggerTouched);
            }

            //Trigger Hairline
            if (device.GetHairTriggerDown())
            {
                OnTriggerHairlineStart(SetButtonEvent(ref triggerHairlinePressed, true, currentTriggerAxis.x));
                EmitAlias(ButtonAlias.Trigger_Hairline, true, currentTriggerAxis.x, ref triggerHairlinePressed);
            }
            else if (device.GetHairTriggerUp())
            {
                OnTriggerHairlineEnd(SetButtonEvent(ref triggerHairlinePressed, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Hairline, false, 0f, ref triggerHairlinePressed);
            }

            //Trigger Clicked
            if (!triggerClicked && currentTriggerAxis.x == 1f)
            {
                OnTriggerClicked(SetButtonEvent(ref triggerClicked, true, currentTriggerAxis.x));
                EmitAlias(ButtonAlias.Trigger_Click, true, currentTriggerAxis.x, ref triggerClicked);
            }
            else if (triggerClicked && currentTriggerAxis.x < 1f)
            {
                OnTriggerUnclicked(SetButtonEvent(ref triggerClicked, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Click, false, 0f, ref triggerClicked);
            }

            //Trigger Axis
            if (Vector2ShallowEquals(triggerAxis, currentTriggerAxis))
            {
                triggerAxisChanged = false;
            }
            else
            {
                OnTriggerAxisChanged(SetButtonEvent(ref triggerAxisChanged, true, currentTriggerAxis.x));
            }

            //ApplicationMenu
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu))
            {
                OnApplicationMenuPressed(SetButtonEvent(ref applicationMenuPressed, true, 1f));
                EmitAlias(ButtonAlias.Application_Menu, true, 1f, ref applicationMenuPressed);
            }
            else if (device.GetPressUp(SteamVR_Controller.ButtonMask.ApplicationMenu))
            {

                OnApplicationMenuReleased(SetButtonEvent(ref applicationMenuPressed, false, 0f));
                EmitAlias(ButtonAlias.Application_Menu, false, 0f, ref applicationMenuPressed);
            }

            //Grip
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
            {
                OnGripPressed(SetButtonEvent(ref gripPressed, true, 1f));
                EmitAlias(ButtonAlias.Grip, true, 1f, ref gripPressed);
            }
            else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Grip))
            {
                OnGripReleased(SetButtonEvent(ref gripPressed, false, 0f));
                EmitAlias(ButtonAlias.Grip, false, 0f, ref gripPressed);
            }

            //Touchpad Pressed
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
            {
                OnTouchpadPressed(SetButtonEvent(ref touchpadPressed, true, 1f));
                EmitAlias(ButtonAlias.Touchpad_Press, true, 1f, ref touchpadPressed);
            }
            else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                OnTouchpadReleased(SetButtonEvent(ref touchpadPressed, false, 0f));
                EmitAlias(ButtonAlias.Touchpad_Press, false, 0f, ref touchpadPressed);
            }

            //Touchpad Touched
            if (device.GetTouchDown(SteamVR_Controller.ButtonMask.Touchpad))
            {
                OnTouchpadTouchStart(SetButtonEvent(ref touchpadTouched, true, 1f));
                EmitAlias(ButtonAlias.Touchpad_Touch, true, 1f, ref touchpadTouched);
            }
            else if (device.GetTouchUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                OnTouchpadTouchEnd(SetButtonEvent(ref touchpadTouched, false, 0f));
                EmitAlias(ButtonAlias.Touchpad_Touch, false, 0f, ref touchpadTouched);
            }

            if (Vector2ShallowEquals(touchpadAxis, currentTouchpadAxis))
            {
                touchpadAxisChanged = false;
            }
            else
            {
                OnTouchpadAxisChanged(SetButtonEvent(ref touchpadTouched, true, 1f));
                touchpadAxisChanged = true;
            }

            // 保存当前trigger和touchpad状态.
            touchpadAxis = new Vector2(currentTouchpadAxis.x, currentTouchpadAxis.y);
            triggerAxis = new Vector2(currentTriggerAxis.x, currentTriggerAxis.y);
            hairTriggerDelta = device.hairTriggerDelta;
        }


        /// <summary>
        /// 设置Controller的速度和角速度
        /// </summary>
        private void SetVelocity()
        {
            var origin = trackedController.origin ? trackedController.origin : trackedController.transform.parent;
            if (origin != null)
            {
                controllerVelocity = origin.TransformDirection(device.velocity);
                controllerAngularVelocity = origin.TransformDirection(device.angularVelocity);
            }
            else
            {
                controllerVelocity = device.velocity;
                controllerAngularVelocity = device.angularVelocity;
            }
        }
    }
}