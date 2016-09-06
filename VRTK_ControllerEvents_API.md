###概述

Controller Events 脚本用于统一处理Vive手柄发送的事件.

Controller Events 脚本挂载在[CameraRig] 预置件下的Controller 子对象上，为手柄上每个按钮的触发提供事件监听 (包括menu按钮).

当手柄上某个按钮被按下, 脚本会发送一个事件， 通知其他脚本这个点击事件，而具体的点击逻辑并不需要写在Controller Events脚本里面. 当一个按钮被释放, 脚本同样会发送一个事件通知其他脚本这个按钮被释放了.

这个脚本也有各个按钮相应的public boolean属性以便其他脚本获取到所需按钮的当前状态，例如是否释放，是否点击等.

###Inspector可见参数

在unity中，可以通过下拉列表，指定这些行为的对应按钮是什么

- **Pointer Toggle Button:** 这个按钮用于控制一束激光指示线开/关.
- **Pointer Set Button:** 这个按钮用于设置指示线的目标标记.
- **Grab Toggle Button:** 这个按钮用于控制抓取游戏中的物体.
- **Use Toggle Button:** 这个按钮用于使用游戏中的物体.
- **UI Click Button:** 这个按钮用于点击UI元素.
- **Menu Toggle Button:**这个按钮用于点击弹出游戏内置按钮.
- **Axis Fidelity:** 坐标变化的精度, 默认为1. 大于2的数字将会导致过于灵敏的结果.

###变量

- **public bool triggerPressed** - 当trigger被扣下一半左右时为真.
- **public bool triggerTouched** - 当trigger被扣下一点点时为真.
- **public bool triggerHairlinePressed** - 当trigger比任何之前扣下的程度多时为真.
- **public bool triggerClicked** - 当trigger完全扣下时为真.
- **public bool triggerAxisChanged** - 当trigger位置改变时为真.
- **public bool applicationMenuPressed** - 当application menu被按下时为真.
- **public bool touchpadPressed** - 当touchpad被按下时为真.
- **public bool touchpadTouched** - 当touchpad被触碰时为真.
- **public bool touchpadAxisChanged** - 当touchpad触碰位置改变时为真.
- **public bool gripPressed** - 当grip被按下时为真.
- **public bool pointerPressed** - 当别名为pointer的按钮被按下时为真.
- **public bool grabPressed** - 当别名为grab的按钮被按下时为真.
- **public bool usePressed** - 当别名为use的按钮被按下时为真.
- **public bool uiClickPressed** - 当别名为UI click的按钮被按下时为真.
- **public bool menuPressed** -  当别名为menu的按钮被按下时为真.

![](http://image99.360doc.com/DownloadImg/2016/07/1211/78360182_1.jpg)
###事件

- **TriggerPressed** - 当trigger被扣下一半左右时发送事件.
- **TriggerReleased** - 当Trigger从扣下一半的状态释放后发送事件.
- **TriggerTouchStart** - 当trigger被扣下一点点时发送事件.
- **TriggerTouchEnd** - 当trigger完全没有被扣下时发送事件.
- **TriggerHairlineStart** - 当trigger扣下的程度超过了当前的hairline阈值时发送事件.
- **TriggerHairlineEnd** - 当tringger释放程度超过了当前的hairline阈值时发送事件.
- **TriggerClicked** - 当trigger在clicked之前扣下的过程中发送事件.
- **TriggerUnclicked** - 当trigger不再一直处于clicked状态时发送事件.
- **TriggerAxisChanged** - 当trigger扣下的量发生变化时发送事件.
- **ApplicationMenuPressed** - 当application menu被按下时发送事件.
- **ApplicationMenuReleased** - 当application menu被释放时发送事件.
- **GripPressed** - 当grip被按下时发送事件.
- **GripReleased** - 当grip被释放时发送事件.
- **TouchpadPressed** - 当touchpad被按下的时候发送事件(比触摸的按压程度大).
- **TouchpadReleased** - 当touchpad从被按下(非触碰)的状态下释放时发送事件.
- **TouchpadTouchStart** - 当touchpad被触摸时发送事件 (不是点击或者摁下).
- **TouchpadTouchEnd** - 当touchpad不再被触摸时发送事件.
- **TouchpadAxisChanged** - 当touchpad被触摸的点改变时发送事件.
- **AliasPointerOn** - 当pointer toggle(别名)被按下的时候发送事件.
- **AliasPointerOff** - 当pointer toggle(别名)被释放的时候发送事件.
- **AliasPointerSet** - 当pointer set(别名)被释放时发送事件.
- **AliasGrabOn** - 当grab toggle(别名)被按下的时候发送事件.
- **AliasGrabOff** - 当grab toggle(别名)被释放的时候发送事件.
- **AliasUseOn** - 当use toggle(别名)被按下的时候发送事件.
- **AliasUseOff** - 当use toggle(别名)被释放时发送事件.
- **AliasMenuOn** - 当menu toggle(别名)被按下时发送事件.
- **AliasMenuOff** - 当menu toggle(别名)被释放时发送事件.
- **AliasUIClickOn** - 当UI click(别名)被按下时发送事件.
- **AliasUIClickOff** - 当UI click(别名)被释放时发送事件.

事件和bool状态变量有着对应的关系，通常一个bool状态变量会对应至少两个按钮事件

![](http://images.kalloctech.com/forum/Vive_1.jpg)

#####事件装载参数

    public struct ControllerInteractionEventArgs
    {
        public uint controllerIndex;
        public float buttonPressure;
        public Vector2 touchpadAxis;
        public float touchpadAngle;
    }


- uint controllerIndex - 当前使用设备的索引.
- float buttonPressure - 按钮的按压数值. 0f 到 1f.
- Vector2 touchpadAxis - touchpad被触摸的坐标. (0,0) 到 (1,1).
- float touchpadAngle - touchpad触摸时滑动的角度, top为0, bottom为180，以此类推其他 . 0f 到 360f.

#####委托类型
`public delegate void ControllerInteractionEventHandler(object sender, ControllerInteractionEventArgs e);`

声明一个委托类型，参数为object和ControllerInteractionEventArgs,绑定事件时一定要传入这两个参数，按钮被按下时会通过`SetButtonEvent()`方法来给`ControllerInteractionEventArgs e`分配值.

#####按钮别名
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
这个工具类给Vive手柄一些常用的操作取一些别名，和实际的按钮建立映射，例如：

    public ButtonAlias menuToggleButton = ButtonAlias.Application_Menu;
这个menuToggleButton与SteamVR中的

    SteamVR_Controller.ButtonMask.ApplicationMenu
对应，当这个按钮被按下时，别名按钮对应的事件(如果有绑定)也会发送

#####和SteamVR相关的全局变量

        private uint controllerIndex;
        private SteamVR_TrackedObject trackedController;
        private SteamVR_Controller.Device device;

        private Vector2 touchpadAxis = Vector2.zero;
        private Vector2 triggerAxis = Vector2.zero;
        private float hairTriggerDelta;

        private Vector3 controllerVelocity = Vector3.zero;
        private Vector3 controllerAngularVelocity = Vector3.zero;

- controllerIndex - 手柄的索引值，通过`trackedController.index`获取
- trackedController - gameobject绑定的SteamVR_TrackedObject脚本
- device - 设备类，通过此类获取实际中手柄的各种数据
- touchpadAxis - 全局变量，touchpad的坐标
- triggerAxis - 全局变量，trigger的坐标
- hairTriggerDelta - 
- controllerVelocity - 手柄运动的速度
- controllerAngularVelocity - 手柄旋转的角速度

#####事件发送方法
以`OnTriggerPressed`方法为例，其他都和这个差不多

        public virtual void OnTriggerPressed(ControllerInteractionEventArgs e)
        {
            if (TriggerPressed != null)
            {
                TriggerPressed(this, e);//发送事件，通知绑定此事件的脚本，执行具体的逻辑，但是此处是真正最后调用的地方
            }
        }

#####装载参数
        private ControllerInteractionEventArgs SetButtonEvent(ref bool buttonBool, bool value, float buttonPressure)
        {
            buttonBool = value;
            ControllerInteractionEventArgs e;
            e.controllerIndex = controllerIndex;
            e.buttonPressure = buttonPressure;
            e.touchpadAxis = device.GetAxis();//调用SteamVR API获取当前的touchpad二维坐标
            e.touchpadAngle = CalculateTouchpadAxisAngle(e.touchpadAxis);//计算二维坐标在圆形表盘上对应的角度
            return e;
        }
通过传入`ref bool buttonBool`，可以在对`ControllerInteractionEventArgs`进行装填的同时，把事件对应的按钮bool状态进行更新，
例如`TriggerPressed`和`TriggerReleased`事件对应的按钮bool状态是`triggerPressed`，当发送`TriggerPressed`事件时要同时更新`triggerPressed`为`true`；发送`TriggerReleased`事件时要同时更新`triggerPressed`为`false`

#####初始化
        private void Awake()
        {
            trackedController = GetComponent<SteamVR_TrackedObject>();
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        private void Start()
        {
            //获取当前脚本attach的Controller的index
            controllerIndex = (uint)trackedController.index;
            if (controllerIndex < uint.MaxValue)
            {
                //获取设备
                device = SteamVR_Controller.Input((int)controllerIndex);
            }
        }
一般头显对应的index为0，两个手柄分别为0和1

#####别名按钮事件发送


    private void EmitAlias(ButtonAlias type, bool touchDown, float buttonPressure, ref bool buttonBool)

...

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
...

根据type判断是哪个别名按钮，最后一个参数buttonBool对应的是非别名的按钮bool状态，例如这个pointerToggleButton，发送事件时要把`touchpadPressed`状态更新，而更新为true还是false要根据`touchDown`的值来判断，上面的`OnAliasPointerOn`等方法和`OnTriggerPressed`

值得注意的是，不同的别名对应的可能是相同的按钮，例如`pointerToggleButton`和`pointerSetButton`都是`ButtonAlias.Touchpad_Press`.

#####禁用事件
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

            ...

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

这个方法应该就是将所有事件对应的按钮bool状态置为false，同时保存touchpad和trigger上的坐标信息，但是为什么要重新获取一次device呢？

#####Update()方法
        private void Update()
        {
            controllerIndex = (uint)trackedController.index;
            //Only continue if the controller index has been set to a sensible number
            //SteamVR 在未找到Controller时会把index置为uint最大的值
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
                //发送事件，设triggerPressed为true,同时发送Trigger_Press对应的别名按钮事件
                OnTriggerPressed(SetButtonEvent(ref triggerPressed, true, currentTriggerAxis.x));
                EmitAlias(ButtonAlias.Trigger_Press, true, currentTriggerAxis.x, ref triggerPressed);
            }
            else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerReleased(SetButtonEvent(ref triggerPressed, false, 0f));
                EmitAlias(ButtonAlias.Trigger_Press, false, 0f, ref triggerPressed);
            }
            ...
            // 保存当前trigger和touchpad状态.
            touchpadAxis = new Vector2(currentTouchpadAxis.x, currentTouchpadAxis.y);
            triggerAxis = new Vector2(currentTriggerAxis.x, currentTriggerAxis.y);
            hairTriggerDelta = device.hairTriggerDelta;
        }


----------
###使用实例
例如在`VRTK_ControllerEvents_ListenerExample`中

    GetComponent<VRTK_ControllerEvents>().TriggerPressed += new ControllerInteractionEventHandler(DoTriggerPressed);
    private void DoTriggerPressed(object sender, ControllerInteractionEventArgs e)
    {
        DebugLogger(e.controllerIndex, "TRIGGER", "pressed", e);
    }

获取到当前Controller绑定的`VRTK_ControllerEvents`脚本，为它的`TriggerPressed`绑定`DoTriggerPressed`方法，在`VRTK_ControllerEvents`脚本中，每一帧会检测trigger是否被按下，如果按下，则发送事件

    OnTriggerPressed(SetButtonEvent(ref triggerPressed, true, currentTriggerAxis.x));

然后在`OnTriggerPressed`方法里执行`TriggerPressed(this, e);`

   此时`DoTriggerPressed(this,e)`被真正调用，而example脚本中无需在update中写代码，只需要在初始化的时候绑定事件就可以了.