//=====================================================================================
//
// Purpose: Provide a mechanism for determining if a game world object is interactable
//
// This script should be attached to any object that needs touch, use or grab
//
// An optional highlight color can be set to change the object's appearance if it is
// invoked.
//
//=====================================================================================
namespace VRTK
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 事件传递参数
    /// </summary>
    public struct InteractableObjectEventArgs
    {
        public GameObject interactingObject;// 发起交互行为的gameobject(例如手柄)
    }

    public delegate void InteractableObjectEventHandler(object sender, InteractableObjectEventArgs e);

    public class VRTK_InteractableObject : MonoBehaviour
    {

        /// <summary>
        /// 抓取物体时物体与手柄之间的连接方式
        /// </summary>
        public enum GrabAttachType
        {
            Fixed_Joint,// 物体和手柄以固定关节连接，抓取时为物体添加FixedJoint组件，连接至手柄上的刚体组件
            Spring_Joint,// 物体和手柄以弹簧关节连接，抓取时为物体添加SpringJoint组件，连接至手柄上的刚体组件，让它们像被弹簧连接着一样联动
            Track_Object,// 物体不会吸附在手柄上, 但是它会随着手柄的方向移动, 铰链关节的物体可以使用这种
            Rotator_Track,// 跟随手柄的动作旋转.。例如手柄控制门的开关
            Child_Of_Controller,// 让该物体直接变成手柄的一个子对象
            Climbable// 用来攀爬的静态物体，不随手柄运动
        }

        /// <summary>
        /// 允许交互的手柄
        /// </summary>
        public enum AllowedController
        {
            Both,// 两个都可以
            Left_Only,// 只有左
            Right_Only// 只有右
        }

        /// <summary>
        /// 手柄隐藏方式
        /// </summary>
        public enum ControllerHideMode
        {
            Default,// 使用手柄设置
            OverrideHide,// 隐藏
            OverrideDontHide,// 不隐藏
        }

        [Header("Touch Interactions", order = 1)]
        public bool highlightOnTouch = false;// 如果勾选，那么物体只有在手柄碰到它的时候才高亮.
        public Color touchHighlightColor = Color.clear;// 物体被碰到高亮时的颜色. 这个颜色会覆盖任何其他的颜色设置 (例如`VRTK_InteractTouch` 脚本).
        public Vector2 rumbleOnTouch = Vector2.zero;// 当手柄碰到物体时触发触觉反馈, `x`表示持续时间, `y`表示脉冲强度. (在编辑器里可以修改其值)
        public AllowedController allowedTouchControllers = AllowedController.Both;// 决定哪个手柄可以触碰物体.
        public ControllerHideMode hideControllerOnTouch = ControllerHideMode.Default;// 触碰物体时是否隐藏手柄:

        [Header("Grab Interactions", order = 2)]
        public bool isGrabbable = false;// 物体是否可被抓取.
        public bool isDroppable = true;// 使用grab按钮是否可以把已被抓取的物体放下. 如果为false那么一旦物体被抓取就不能被放下
        public bool isSwappable = true;// 物体是否可以在两个手柄之间传递. 如果为false，那么物体被另一个手柄抓取之前必须先从当前手柄上放下
        public bool holdButtonToGrab = true;// 如果为true，那么要一直按着按钮才能保持物体抓取，松开按钮物体会掉落. 如果为false那么按一下抓取按钮物体会被抓取且在按第二下之前它不会掉落.
        public Vector2 rumbleOnGrab = Vector2.zero;// 当手柄抓取物体时触发触觉反馈, x分量表示持续时间,y分量表示脉冲强度. (默认为(0，0)在编辑器里可以修改其值)
        public AllowedController allowedGrabControllers = AllowedController.Both;// 决定哪个手柄可以抓取物体
        public bool precisionSnap;// 如果为true那么当手柄抓取物体的时候, 它会在手柄触碰点精确地抓取物体
        public Transform rightSnapHandle;// 一个空物体的Transform，它必须是被抓取物体的子对象，并且是该物体相对于右侧手柄旋转定位的基准点
        public Transform leftSnapHandle;// 一个空物体的Transform，它必须是被抓取物体的子对象，并且是该物体相对于左侧手柄旋转定位的基准点
        public ControllerHideMode hideControllerOnGrab = ControllerHideMode.Default;// 抓取时是否隐藏手柄

        [Header("Grab Mechanics", order = 3)]
        public GrabAttachType grabAttachMechanic = GrabAttachType.Fixed_Joint;// 默认附着形式为固定连接点
        public float detachThreshold = 500f;// 把物体和手柄分离时需要的力的大小
        public float springJointStrength = 500f;// 物体与手柄连接弹簧的弹力.数值越小弹簧越松，那么需要更大的力才能移动物体,数值越大弹簧越紧，一点点力就会让物体移动
        public float springJointDamper = 50f;// 使用弹簧方式连接时，使得弹力衰减的量.数值较大时可以减少移动连接的可交互物体时的震荡效应
        public float throwMultiplier = 1f;// 当抛出物体时需要给速度乘上一个这个值
        public float onGrabCollisionDelay = 0f;// 当物体第一次被抓取时，给碰撞效果一个延时.这个效果在物体卡在别的东西里面时很有用

        [Header("Use Interactions", order = 4)]
        public bool isUsable = false;// 物体是否可以被使用
        public bool useOnlyIfGrabbed = false;// 如果为true那么物体使用之前必须先被抓取
        public bool holdButtonToUse = true;// 如果为true那么要一直按着使用按钮物体才能被持续使用.如果为false那么在按第二次按钮之前物体会持续使用
        // 如果为true那么手柄发出的射线击中可交互物体以后, 
        // 如果这个物体的`Hold Button To Use`选项没有勾选那么射线消失的同时会触发它的`Using` 方法.
        // 如果`Hold Button To Use` 未勾选那么当射线消失的时候`Using`会调用. 
        // 此项勾选时，当射线击中可交互物体的时候，world pointer 不会抛出`Destination Set`事件，避免当使用物体时发生不必要的传送.
        public bool pointerActivatesUseAction = false;
        public Vector2 rumbleOnUse = Vector2.zero;// 当手柄抓取物体时触发触觉反馈, x分量表示持续时间,y分量表示脉冲强度. (默认为(0，0)在编辑器里可以修改其值)
        public AllowedController allowedUseControllers = AllowedController.Both;// 决定哪个手柄可以使用物体
        public ControllerHideMode hideControllerOnUse = ControllerHideMode.Default;// 使用时是否隐藏手柄

        public event InteractableObjectEventHandler InteractableObjectTouched;// 其他物体触碰当前物体时发送事件
        public event InteractableObjectEventHandler InteractableObjectUntouched;// 其他物体停止触碰当前物体时发送事件
        public event InteractableObjectEventHandler InteractableObjectGrabbed;// 其他物体(例如手柄)抓取当前物体时发送事件
        public event InteractableObjectEventHandler InteractableObjectUngrabbed;// 其他物体停止抓取当前物体时发送事件
        public event InteractableObjectEventHandler InteractableObjectUsed;// 其他物体(例如手柄)使用当前物体时发送事件
        public event InteractableObjectEventHandler InteractableObjectUnused;// 其他物体停止使用当前物体时发送事件

        protected Rigidbody rb;// 物体上的刚体组件
        protected GameObject touchingObject = null;// 正在触碰物体的游戏对象(例如手柄)
        protected GameObject grabbingObject = null;// 正在抓取物体的游戏对象(例如手柄)
        protected GameObject usingObject = null;// 正在使用物体的游戏对象(例如手柄)

        private int usingState = 0;// 手柄对物体按下使用按钮的次数，如果物体不勾选`holdButtonToUse`，一个使用周期内第二次按下使用按钮，即`usingState>=2`，物体才会停止使用
        private Dictionary<string, Color[]> originalObjectColours;// 物体本身的材质颜色字典

        private Transform grabbedSnapHandle;// 被当前手柄抓取的部位，根据自己的设置和手柄的处理动态生成
        private Transform trackPoint;// 追踪连接的连接点,手柄下有一个对应的默认子对象，也可以另外自己指定
        private bool customTrackPoint = false;// 如果`trackPoint`就是手柄默认的那个连接点子对象，就为`false`,否则为`true`
        private Transform originalControllerAttachPoint;// 物体的一个子对象，与`trackPoint`对应构成两个追踪连接点

        private Transform previousParent;// 物体初始的父对象
        private bool previousKinematicState;// 物体初始的`Kinematic`状态
        private bool previousIsGrabbable;// 物体初始的可抓取属性
        private bool forcedDropped;// 默认为`false`，在强制停止物体的方法执行后会置其为`true`，如果强制停止物体交互时脚本被禁用，那么下次脚本启用的时候看见这个参数为`true`就知道应该加载脚本被禁用之前的状态

        /// <summary>
        /// CheckHideMode方法是一个供其他脚本(例如InteractTouch InteractGrab InteractUse)使用的简单的方法，
        /// 它可以用来计算手柄隐藏与否，通过同时考虑手柄默认设置和可交互物体的设置.
        /// </summary>
        /// <param name="defaultMode"></param>
        /// <param name="overrideMode"></param>
        /// <returns>true隐藏，false不隐藏</returns>
        public bool CheckHideMode(bool defaultMode, ControllerHideMode overrideMode)
        {
            switch (overrideMode)
            {
                case VRTK_InteractableObject.ControllerHideMode.OverrideDontHide:
                    return false;
                case VRTK_InteractableObject.ControllerHideMode.OverrideHide:
                    return true;
            }
            // VRTK_InteractableObject.ControllerHideMode.Default
            return defaultMode;
        }
        // 下面方法用于发送事件，调用委托给这些事件的方法
        public virtual void OnInteractableObjectTouched(InteractableObjectEventArgs e)
        {
            if (InteractableObjectTouched != null)
            {
                InteractableObjectTouched(this, e);
            }
        }

        public virtual void OnInteractableObjectUntouched(InteractableObjectEventArgs e)
        {
            if (InteractableObjectUntouched != null)
            {
                InteractableObjectUntouched(this, e);
            }
        }

        public virtual void OnInteractableObjectGrabbed(InteractableObjectEventArgs e)
        {
            if (InteractableObjectGrabbed != null)
            {
                InteractableObjectGrabbed(this, e);
            }
        }

        public virtual void OnInteractableObjectUngrabbed(InteractableObjectEventArgs e)
        {
            if (InteractableObjectUngrabbed != null)
            {
                InteractableObjectUngrabbed(this, e);
            }
        }

        public virtual void OnInteractableObjectUsed(InteractableObjectEventArgs e)
        {
            if (InteractableObjectUsed != null)
            {
                InteractableObjectUsed(this, e);
            }
        }

        public virtual void OnInteractableObjectUnused(InteractableObjectEventArgs e)
        {
            if (InteractableObjectUnused != null)
            {
                InteractableObjectUnused(this, e);
            }
        }

        /// <summary>
        /// 设置事件传递参数
        /// </summary>
        /// <param name="interactingObject"></param>
        /// <returns></returns>
        public InteractableObjectEventArgs SetInteractableObjectEvent(GameObject interactingObject)
        {
            InteractableObjectEventArgs e;
            e.interactingObject = interactingObject;
            return e;
        }

        /// <summary>
        /// IsTouched方法用于检查物体当前是不是被触碰
        /// </summary>
        /// <returns>如果touchingObject不为空返回true</returns>
        public bool IsTouched()
        {
            return (touchingObject != null);
        }

        /// <summary>
        /// IsGrabbed方法用于检查物体当前是不是被抓取
        /// </summary>
        /// <returns>如果grabbingObject不为空返回true</returns>
        public bool IsGrabbed()
        {
            return (grabbingObject != null);
        }

        /// <summary>
        /// IsUsing方法用于检查物体当前是不是被使用
        /// </summary>
        /// <returns>如果usingObject不为空返回true</returns>
        public bool IsUsing()
        {
            return (usingObject != null);
        }

        /// <summary>
        /// StartTouching方法会自动地被调用，当物体开始被触碰时. 
        /// 调用处是手柄绑定的VRTK_InteractTouch脚本的OntriggerStay
        /// 它是一个虚方法，可以被子类重写 .
        /// </summary>
        /// <param name="currentTouchingObject">正在触碰该物体的游戏对象(例如手柄)</param>
        public virtual void StartTouching(GameObject currentTouchingObject)
        {
            // 将currentTouchingObject作为参数，回调委托给在InteractableObjectTouched事件上的方法
            OnInteractableObjectTouched(SetInteractableObjectEvent(currentTouchingObject));

            // 保存正在触碰物体的游戏对象
            touchingObject = currentTouchingObject;
        }

        /// <summary>
        /// StopTouching方法会自动地被调用，当物体停止被触碰时. 
        /// 调用处是手柄绑定的VRTK_InteractTouch脚本的StopTouching,然后在OnTriggerExit处最后调用
        /// 它是一个虚方法，可以被子类重写.
        /// </summary>
        /// <param name="previousTouchingObject">先前触及此物体的游戏对象</param>
        public virtual void StopTouching(GameObject previousTouchingObject)
        {
            // 将previousTouchingObject作为参数，回调委托给InteractableObjectUntouched事件上的方法
            OnInteractableObjectUntouched(SetInteractableObjectEvent(previousTouchingObject));
            // 将touchingObject设为空
            touchingObject = null;
            if(gameObject.activeInHierarchy)
            {
                // 停止触碰时也要停止使用
                StartCoroutine(StopUsingOnControllerChange(previousTouchingObject));
            }
        }

        /// <summary>
        /// Grabbed方法会自动地被调用，当物体开始被抓取时.
        /// 调用处是手柄绑定的VRTK_InteractGrab中的InitGrabbedObject以及子类调用基类方法
        /// 它是一个虚方法，可以被子类重写(如Lamp、Sword).
        /// </summary>
        /// <param name="currentGrabbingObject">正在抓取物体的游戏对象(例如手柄)</param>
        public virtual void Grabbed(GameObject currentGrabbingObject)
        {
            // 将currentGrabbingObject作为参数，回调委托给InteractableObjectGrabbed的方法
            OnInteractableObjectGrabbed(SetInteractableObjectEvent(currentGrabbingObject));

            // 如果之前有别的手柄抓取该物体，要强行释放抓取
            ForceReleaseGrab();
            // 移除原来的追踪连接点
            RemoveTrackPoint();
            // 将当前的抓取手柄设置为传进来的新的手柄游戏对象
            grabbingObject = currentGrabbingObject;
            // 设置新的追踪连接点
            SetTrackPoint(grabbingObject);
            if (!isSwappable)
            {
                // 如果不可交换,那么物体如果已经被抓取，就不能被其他手柄抓取了
                // 保存物体之前的isGrabble选项(物体可以被手柄抓取只是不能被第二个手柄抓取，所以状态要保存)，然后把物体的isGrabbable设为false
                previousIsGrabbable = isGrabbable;
                isGrabbable = false;
            }
        }

        /// <summary>
        /// Ungrabbed方法会自动地被调用，当物体停止被抓取时.
        /// 调用处是手柄绑定的VRTK_InteractGrab中的InitUngrabbedObject以及子类调用基类方法
        /// 它是一个虚方法，可以被子类重写(如Lamp、Sword).
        /// </summary>
        /// <param name="previousGrabbingObject"></param>
        public virtual void Ungrabbed(GameObject previousGrabbingObject)
        {
            // 将previousGrabbingObject作为参数，回调委托给InteractableObjectUngrabbed事件的方法
            OnInteractableObjectUngrabbed(SetInteractableObjectEvent(previousGrabbingObject));
            RemoveTrackPoint();
            grabbedSnapHandle = null;
            grabbingObject = null;

            // 回到被抓取前的状态
            LoadPreviousState();
            if (gameObject.activeInHierarchy)
            {
                // 既然停止抓取，也要停止使用
                StartCoroutine(StopUsingOnControllerChange(previousGrabbingObject));
            }
        }

        /// <summary>
        /// StartUsing方法会自动地被调用，当物体开始被使用时.它是一个虚方法，可以被子类重写.
        /// </summary>
        /// <param name="currentUsingObject"></param>
        public virtual void StartUsing(GameObject currentUsingObject)
        {
            // 将currentUsingObject作为参数，回调委托给InteractableObjectUsed事件的方法
            OnInteractableObjectUsed(SetInteractableObjectEvent(currentUsingObject));
            usingObject = currentUsingObject;
        }

        /// <summary>
        /// StopUsing方法会自动地被调用，当物体停止被使用时.它是一个虚方法，可以被子类重写.
        /// </summary>
        /// <param name="previousUsingObject"></param>
        public virtual void StopUsing(GameObject previousUsingObject)
        {
            // 将previousUsingObject作为参数，回调委托给InteractableObjectUnused的方法
            OnInteractableObjectUnused(SetInteractableObjectEvent(previousUsingObject));
            usingObject = null;
        }

        /// <summary>
        /// 关闭高亮的快捷方法，参数只能传入false，什么鬼
        /// </summary>
        /// <param name="toggle"></param>
        public virtual void ToggleHighlight(bool toggle)
        {
            // 调用ToggleHighlight(false,Color.clear)
            ToggleHighlight(toggle, Color.clear);
        }

        /// <summary>
        /// 用于开启/关闭物体的高亮.
        /// </summary>
        /// <param name="toggle">true-开/false-关</param>
        /// <param name="globalHighlightColor">高亮显示的颜色</param>
        public virtual void ToggleHighlight(bool toggle, Color globalHighlightColor)
        {
            if (highlightOnTouch)
            {
                // 如果设置了物体被触碰的时候要高亮
                if (toggle && !IsGrabbed() && !IsUsing())
                {
                    // 如果开启高亮，且没有正在被抓取，没有正在被使用

                    // 如果当前脚本的touchHighlightColor有设置，就使用这个颜色，如果设置为Color.clear那就使用传入的参数globalHighlightColor
                    Color color = (touchHighlightColor != Color.clear ? touchHighlightColor : globalHighlightColor);
                    if (color != Color.clear)
                    {
                        // 如果颜色不是Color.clear，建立该颜色的颜色字典
                        var colorArray = BuildHighlightColorArray(color);
                        // 改变当前物体的材质颜色
                        ChangeColor(colorArray);
                    }
                }
                else
                {
                    if (originalObjectColours == null)
                    {
                        Debug.LogError("VRTK_InteractableObject has not had the Start() method called, if you are inheriting this class then call base.Start() in your Start() method.");
                        return;
                    }
                    // 关闭高亮，设置物体的材质颜色为原本的颜色
                    ChangeColor(originalObjectColours);
                }
            }
        }

        /// <summary>
        /// 参数，使用状态
        /// </summary>
        public int UsingState
        {
            get { return usingState; }
            set { usingState = value; }
        }

        /// <summary>
        /// PauseCollisions方法在物体被抓取的时候，通过移除物体上刚体组件的检测碰撞功能来暂停物体上的所有碰撞.
        /// 暂停的时间只持续onGrabCollisionDelay秒，之后会重新开启碰撞检测
        /// </summary>
        public void PauseCollisions()
        {
            if (onGrabCollisionDelay > 0f)
            {
                // 暂时关闭刚体组件的碰撞检测
                if (GetComponent<Rigidbody>())
                {
                    GetComponent<Rigidbody>().detectCollisions = false;
                }
                foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
                {
                    rb.detectCollisions = false;
                }
                // onGrabCollisionDelay秒后开启刚体组件的碰撞检测
                Invoke("UnpauseCollisions", onGrabCollisionDelay);
            }
        }

        /// <summary>
        /// AttachIsTrackObject方法用于确认物体是否使用了追踪连接的方式被抓取.
        /// </summary>
        /// <returns>当抓取方式为追踪连接时为真，例如Track Object或者Rotator Track</returns>
        public bool AttachIsTrackObject()
        {
            return (grabAttachMechanic == GrabAttachType.Track_Object || grabAttachMechanic == GrabAttachType.Rotator_Track);
        }

        /// <summary>
        /// AttachIsClimbObject方法用于确认物体是否使用了`Climbable`的抓取连接方式
        /// </summary>
        /// <returns>抓取方式为Climbable时为真</returns>
        public bool AttachIsClimbObject()
        {
            return (grabAttachMechanic == GrabAttachType.Climbable);
        }

        /// <summary>
        /// AttachIsStaticObject方法用于确认物体是否使用了静态的抓取连接方式
        /// </summary>
        /// <returns>抓取方式为类似`Climbable`的静态连接方式时为真</returns>
        public bool AttachIsStaticObject()
        {
            return AttachIsClimbObject(); // 目前工具包里只有Climbable是静态连接方式
        }

        /// <summary>
        /// ZeroVelocity方法重置物体的刚体组件的速度和角速度都为0
        /// </summary>
        public void ZeroVelocity()
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// SaveCurrentState方法将物体当前的父对象以及刚体运动学设置保存下来
        /// </summary>
        public void SaveCurrentState()
        {
            if (grabbingObject == null)
            {
                // 如果抓取物体的对象是空
                // 设previousParent为当前物体的父对象
                previousParent = transform.parent;

                if (rb)
                {
                    // 设previousKinematicState为刚体组件现在的运动学状态
                    previousKinematicState = rb.isKinematic;
                }
            }
        }

        /// <summary>
        /// ToggleKinematic方法用于设置物体的刚体运动学状态
        /// </summary>
        /// <param name="state">物体的刚体运动学状态</param>
        public void ToggleKinematic(bool state)
        {
            if (rb)
            {
                rb.isKinematic = state;
            }
        }

        /// <summary>
        /// GetGrabbingObject方法用于找到正在抓取当前物体的游戏对象
        /// </summary>
        /// <returns>正在抓取当前物体的游戏对象</returns>
        public GameObject GetGrabbingObject()
        {
            return grabbingObject;
        }

        /// <summary>
        /// IsValidInteractableController方法由于检测一个手柄游戏对象是否被允许与当前物体交互，因为有的情况下(根据使用需求)手柄是禁止与物体交互的
        /// </summary>
        /// <param name="actualController">接受检测的手柄游戏对象</param>
        /// <param name="controllerCheck">哪个手柄被允许与当前物体交互</param>
        /// <returns></returns>
        public bool IsValidInteractableController(GameObject actualController, AllowedController controllerCheck)
        {
            if (controllerCheck == AllowedController.Both)
            {
                // 如果物体被允许与任何手柄交互，那么始终返回真
                return true;
            }
            // controllerCheck.ToString().Replace("_Only", "")这句代码是把Left_Only和Right_Only里面的_Only去掉，留下Left或者Right
            // 然后用VRTK_DeviceFinder去找到对应的手柄
            var controllerHand = VRTK_DeviceFinder.GetControllerHandType(controllerCheck.ToString().Replace("_Only", ""));
            // 对比，里面要依赖SteamVR_ControllerManager
            return (VRTK_DeviceFinder.IsControllerOfHand(actualController, controllerHand));
        }

        /// <summary>
        /// ForceStopInteracting方法强行停止物体的交互行为，手柄会放下物体并停止触碰它.当手柄要和另外的物体交互时这个方法很有用.
        /// </summary>
        public void ForceStopInteracting()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ForceStopInteractingAtEndOfFrame());
            }
        }

        /// <summary>
        /// SetGrabbedSnapHandle方法用于在运行的时候设置物体抓取的位置
        /// </summary>
        /// <param name="handle">物体被抓取的时候，抓取的位置，例如一个杯子，可以设置把手为SnapHandle</param>
        public void SetGrabbedSnapHandle(Transform handle)
        {
            grabbedSnapHandle = handle;
        }

        /// <summary>
        /// RegisterTeleporters方法用于找到绑定了传送脚本的游戏对象，把`OnTeleported`委托给传送脚本里的Teleported事件.这个方法可以让物体随着传送移动
        /// </summary>
        public void RegisterTeleporters()
        {
            foreach (var teleporter in FindObjectsOfType<VRTK_BasicTeleport>())
            {
                // 把此脚本中的OnTeleported方法委托给传送脚本执行
                teleporter.Teleported += new TeleportEventHandler(OnTeleported);
            }
        }

        protected virtual void Awake()
        {
            // 初始化刚体组件
            rb = GetComponent<Rigidbody>();

            if (!AttachIsStaticObject())
            {
                // 如果该物体不是静态类型的
                // 如果该物体没有刚体组件，就添加一个，并且设置它的运动学状态为'kinematic'
                if (!rb)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                }
                rb.maxAngularVelocity = float.MaxValue;
            }
            forcedDropped = false;
        }

        protected virtual void Start()
        {
            // 把物体的所有材质颜色以字典的形式存储下来
            originalObjectColours = StoreOriginalColors();
        }

        protected virtual void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                // 如果物体不再可用，强行停止手柄与它的交互
                ForceStopInteracting();
            }

            if (AttachIsTrackObject())
            {
                // 如果物体是以追踪的方式与物体连接，每帧要进行检查追踪点和物体之间的距离，大于一定值就要断开连接
                CheckBreakDistance();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (trackPoint)
            {
                // 若trackPoint不为空
                switch (grabAttachMechanic)
                {
                    case GrabAttachType.Rotator_Track:
                        FixedUpdateRotatorTrack();
                        break;
                    case GrabAttachType.Track_Object:
                        FixedUpdateTrackObject();
                        break;
                }
            }
        }

        protected virtual void OnEnable()
        {
            RegisterTeleporters();
            if (forcedDropped)
            {
                LoadPreviousState();
            }
        }

        protected virtual void OnDisable()
        {
            foreach (var teleporter in FindObjectsOfType<VRTK_BasicTeleport>())
            {
                // 取消事件委托
                teleporter.Teleported -= new TeleportEventHandler(OnTeleported);
            }
            // 停止交互
            ForceStopInteracting();
        }

        /// <summary>
        /// 当附在同一对象上的关节被断开时调用。
        /// 当一个力大于这个关节的承受力时，关节将被断开。此时OnJointBreak将被调用，应用到关节的力将被传入。之后这个关节将自动从游戏对象中移除并删除。
        /// </summary>
        /// <param name="force"></param>
        protected virtual void OnJointBreak(float force)
        {
            // 强制释放抓取
            ForceReleaseGrab();
        }

        /// <summary>
        /// 加载之前保存的状态
        /// </summary>
        protected virtual void LoadPreviousState()
        {
            if (gameObject.activeInHierarchy)
            {
                transform.parent = previousParent;
                forcedDropped = false;
            }
            if (rb)
            {
                rb.isKinematic = previousKinematicState;
            }
            if (!isSwappable)
            {
                isGrabbable = previousIsGrabbable;
            }
        }

        /// <summary>
        /// ForceReleaseGrab方法用于强行让已经抓取物体的手柄释放对该物体的抓取
        /// </summary>
        private void ForceReleaseGrab()
        {
            if (grabbingObject)
            {
                grabbingObject.GetComponent<VRTK_InteractGrab>().ForceRelease();
            }
        }

        /// <summary>
        /// 启用刚体组件的碰撞检测
        /// </summary>
        private void UnpauseCollisions()
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().detectCollisions = true;
            }
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.detectCollisions = true;
            }
        }

        /// <summary>
        /// 获取该物体或者子对象的所有Renderer
        /// </summary>
        /// <returns>Renerer[]数组</returns>
        private Renderer[] GetRendererArray()
        {
            return (GetComponents<Renderer>().Length > 0 ? GetComponents<Renderer>() : GetComponentsInChildren<Renderer>());
        }

        /// <summary>
        /// 将物体的渲染器的颜色按渲染器游戏对象分类，存储它下面所有材质的颜色
        /// </summary>
        /// <returns>Dictionary<string, Color[]></returns>
        private Dictionary<string, Color[]> StoreOriginalColors()
        {
            var colors = new Dictionary<string, Color[]>();
            foreach (Renderer renderer in GetRendererArray())
            {
                // 遍历物体下面的的渲染器
                // 颜色字典键值为渲染器的游戏对象的名字，为它新建一个Color数组，长度是渲染器的材质的数量
                colors[renderer.gameObject.name] = new Color[renderer.materials.Length];

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    // 遍历该渲染器下的材质，保存每一个材质的颜色
                    var material = renderer.materials[i];
                    if (material.HasProperty("_Color"))
                    {
                        // 如果材质的shader有_Color这个属性
                        colors[renderer.gameObject.name][i] = material.color;
                    }
                }
            }
            return colors;
        }

        /// <summary>
        /// 根据传入的颜色参数，建立一个高亮颜色字典，键值为渲染器的游戏对象，值为材质颜色的数组，数组里的颜色都设为传入的颜色
        /// </summary>
        /// <param name="color">高亮目标颜色</param>
        /// <returns>Dictionary<string, Color[]></returns>
        private Dictionary<string, Color[]> BuildHighlightColorArray(Color color)
        {
            var colors = new Dictionary<string, Color[]>();
            foreach (Renderer renderer in GetRendererArray())
            {
                colors[renderer.gameObject.name] = new Color[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var material = renderer.materials[i];
                    if (material.HasProperty("_Color"))
                    {
                        colors[renderer.gameObject.name][i] = color;
                    }
                }
            }
            return colors;
        }
        
        /// <summary>
        /// 改变物体的渲染器的材质颜色
        /// </summary>
        /// <param name="colors">存有物体材质颜色的字典</param>
        private void ChangeColor(Dictionary<string, Color[]> colors)
        {
            foreach (Renderer renderer in GetRendererArray())
            {
                if (!colors.ContainsKey(renderer.gameObject.name))
                {
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var material = renderer.materials[i];
                    if (material.HasProperty("_Color"))
                    {
                        // 一 一比对渲染器的游戏对象和材质顺序，修改颜色
                        material.color = colors[renderer.gameObject.name][i];
                    }
                }
            }
        }

        /// <summary>
        /// 检查物体与追踪点之间的距离，如果大于设定值就要断开抓取连接
        /// </summary>
        private void CheckBreakDistance()
        {
            if (trackPoint)
            {
                float distance = Vector3.Distance(trackPoint.position, transform.position);
                if (distance > (detachThreshold / 1000))
                {
                    ForceReleaseGrab();
                }
            }
        }

        /// <summary>
        /// 设置追踪点
        /// </summary>
        /// <param name="point">追踪点游戏对象(例如手柄)</param>
        private void SetTrackPoint(GameObject point)
        {
            var controllerPoint = point.transform;
            var grabScript = point.GetComponent<VRTK_InteractGrab>();

            if (grabScript && grabScript.controllerAttachPoint)
            {
                controllerPoint = grabScript.controllerAttachPoint.transform;
            }

            if (AttachIsTrackObject() && precisionSnap)
            {
                // 在point下面新建一个名为"[当前物体名字]TrackObject_PrecisionSnap_AttachPoint"的游戏对象
                trackPoint = new GameObject(string.Format("[{0}]TrackObject_PrecisionSnap_AttachPoint", gameObject.name)).transform;
                trackPoint.parent = point.transform;
                customTrackPoint = true;
                if (grabAttachMechanic == GrabAttachType.Track_Object)
                {
                    // 设置追踪点的位置和旋转与物体一致
                    trackPoint.position = transform.position;
                    trackPoint.rotation = transform.rotation;
                }
                else
                {
                    // 设置追踪点的位置和选择与手柄一致
                    trackPoint.position = controllerPoint.position;
                    trackPoint.rotation = controllerPoint.rotation;
                }
            }
            else
            {
                // 追踪点就是手柄上的`controllerPoint`
                trackPoint = controllerPoint;
                customTrackPoint = false;
            }

            // 在物体的下面新建一个名为"[手柄名字]Original_Controller_AttachPoint"的游戏对象，位置和旋转设置为当前追踪点的位置
            originalControllerAttachPoint = new GameObject(string.Format("[{0}]Original_Controller_AttachPoint", grabbingObject.name)).transform;
            originalControllerAttachPoint.parent = transform;
            originalControllerAttachPoint.position = trackPoint.position;
            originalControllerAttachPoint.rotation = trackPoint.rotation;
        }

        /// <summary>
        /// 移除追踪点
        /// </summary>
        private void RemoveTrackPoint()
        {
            if (customTrackPoint && trackPoint)
            {
                // 如果追踪点是定制的，就表示生成了新的游戏对象，此时要销毁
                Destroy(trackPoint.gameObject);
            }
            else
            {
                // 不然只要取消追踪点对别的游戏对象的引用就可以
                trackPoint = null;
            }
            if (originalControllerAttachPoint)
            {
                // 从物体下面移除初始的手柄抓取点游戏对象
                Destroy(originalControllerAttachPoint.gameObject);
            }
        }

        /// <summary>
        /// Rotator_Track连接方式时，在FixedUpdate方法里更新物体的旋转
        /// </summary>
        private void FixedUpdateRotatorTrack()
        {
            // 方向是从初始抓取位置开始，指向当前的追踪点位置
            var rotateForce = trackPoint.position - originalControllerAttachPoint.position;
            // 在物体的初始抓取位置施加一个指向当前追踪点的力
            rb.AddForceAtPosition(rotateForce, originalControllerAttachPoint.position, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Track_Object连接方式时，在FixedUpdate方法中更新物体的位置和旋转
        /// </summary>
        private void FixedUpdateTrackObject()
        {
            float maxDistanceDelta = 10f;

            Quaternion rotationDelta;// 单位旋转量
            Vector3 positionDelta;// 单位移动量

            float angle;// 旋转角度
            Vector3 axis;// 旋转轴

            if (grabbedSnapHandle != null)
            {
                // 如果有把手，用把手处理单位旋转和移动
                rotationDelta = trackPoint.rotation * Quaternion.Inverse(grabbedSnapHandle.rotation);
                positionDelta = trackPoint.position - grabbedSnapHandle.position;
            }
            else
            {
                // 直接用物体旋转和位置处理
                rotationDelta = trackPoint.rotation * Quaternion.Inverse(transform.rotation);
                positionDelta = trackPoint.position - transform.position;
            }

            // 把四元数形式的旋转变成围绕什么轴旋转多少度的形式
            rotationDelta.ToAngleAxis(out angle, out axis);

            angle = (angle > 180 ? angle -= 360 : angle);

            // 作用是将当前值current移向目标target。（对Vector3是沿两点间直线）
            // maxDistanceDelta就是每次移动的最大长度。
            // 返回值是当current值加上maxDistanceDelta的值，如果这个值超过了target，返回的就是target的值。


            if (angle != 0)
            {
                Vector3 angularTarget = angle * axis;
                rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, angularTarget, maxDistanceDelta);
            }

            Vector3 velocityTarget = positionDelta / Time.fixedDeltaTime;
            rb.velocity = Vector3.MoveTowards(rb.velocity, velocityTarget, maxDistanceDelta);
        }

        /// <summary>
        /// 同步物体和手柄的位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTeleported(object sender, DestinationMarkerEventArgs e)
        {
            if (AttachIsTrackObject() && trackPoint)
            {
                // 物体的位置就是抓取物体的手柄的位置
                transform.position = grabbingObject.transform.position;
            }
        }

        /// <summary>
        /// 等待一帧后停止手柄对物体使用
        /// </summary>
        /// <param name="previousController">要停止使用的手柄游戏对象</param>
        /// <returns></returns>
        private IEnumerator StopUsingOnControllerChange(GameObject previousController)
        {
            // 等待直到所有的摄像机和GUI被渲染完成后，在该帧显示在屏幕之前
            yield return new WaitForEndOfFrame();

            // 获取现在正在使用物体的手柄上的VRTK_InteractUse脚本
            var usingObject = previousController.GetComponent<VRTK_InteractUse>();
            if (usingObject)
            {
                if (holdButtonToUse)
                {
                    // 如果设置了要一直按着按钮才能使用，调用ForceStopUsing
                    usingObject.ForceStopUsing();
                }
                else
                {
                    // 否则调用ForceStopUsing
                    usingObject.ForceResetUsing();
                }
            }
        }

        /// <summary>
        /// 等待一帧后停止所有手柄与物体的交互(触碰、抓取、使用)
        /// </summary>
        /// <returns></returns>
        private IEnumerator ForceStopInteractingAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            if (touchingObject != null && touchingObject.activeInHierarchy)
            {
                touchingObject.GetComponent<VRTK_InteractTouch>().ForceStopTouching();
                forcedDropped = true;
            }

            if (grabbingObject != null && grabbingObject.activeInHierarchy)
            {
                grabbingObject.GetComponent<VRTK_InteractTouch>().ForceStopTouching();
                grabbingObject.GetComponent<VRTK_InteractGrab>().ForceRelease();
                forcedDropped = true;
            }

            if (usingObject != null && usingObject.activeInHierarchy)
            {
                usingObject.GetComponent<VRTK_InteractTouch>().ForceStopTouching();
                usingObject.GetComponent<VRTK_InteractUse>().ForceStopUsing();
                forcedDropped = true;
            }
        }
    }
}
