## 抓取交互物体 (VRTK_InteractGrab)

### 概述

Interact Grab脚本绑定在[CameraRig]下的手柄游戏对象上.并且手柄游戏对象必须绑定`VRTK_ControllerEvents`脚本，来监听手柄上按钮的事件，抓取/释放可交互物体等.它监听的是`AliasGrabOn`和`AliasGrabOff`事件，当这两个事件被触发，那么本脚本内委托给这两个事件的方法就会被回调.

该手柄游戏对象也需要绑定`VRTK_InteractTouch`脚本，来确定可交互物体是否被触碰.可交互物体只有先被触碰才能进行抓取行为.

当可交互物体绑定了`VRTK_InteractableObject`脚本并且`isGrabbable`为`true`，手柄触碰它以后就可以抓取它了.

如果一个可交互的物体处于可抓取的状态，此时按下设定的抓取按钮(默认是`Grip`,可以在`VRTK_ControllerEvents`编辑器修改)，就可以抓取物体，并且将物体连接到手柄上，再次按下抓取按钮前，物体不会被释放.

当手柄上的抓取按钮被释放，那么被抓取的物体会被施加一个沿着当前手柄运动方向，大小为手柄当前速度的初速度，加速度为重力加速度，这样就可以模拟出物体被丢出的效果.

可交互物体需要一个碰撞器来处理trigger事件以及一个刚体组件来做物理方面的事情.

### Inspector可见参数

  * **Controller Attach Point:** 手柄模型上的刚体圆点，被抓取的物体通过这个点连接手柄(默认是手柄对象下的model下的tip).
  * **Hide Controller On Grab:** 当抓取物体时是否隐藏手柄模型.
  * **Hide Controller Delay:** 当手柄抓取物体到隐藏手柄之间的延迟时间.
  * **Grab Precognition:** 预抓取时间.当物体以非常快的速度运动时，由于身体反应的速度，很难及时抓到物体.这个参数的值设置的足够大的时候，可以在手柄碰到物体之前按下抓取按钮，当碰撞发生时，如果抓取按钮还按着，整个抓取行为就算成功完成.
  * **Throw Multiplier:** 丢出速度乘子.被丢出的物体的速度要乘上这个乘子，可以控制物体被丢出的程度.
  * **Create Rigid Body When Not Touching:** 如果此项勾选，当手柄按下抓取按钮时没有碰到一个可交互的物体，那么为手柄添加一个刚体组件，让手柄可以推开其他刚体对象.

### 事件类

  * `ControllerGrabInteractableObject` - 当物体被抓取时发送事件，调用委托给自己的方法.
  * `ControllerUngrabInteractableObject` - 当物体被释放时发送事件，调用委托给自己的方法.

#### 事件参数

  * `uint controllerIndex` - 执行交互的手柄索引.
  * `GameObject target` - 正在与手柄交互的可交互物体游戏对象.

### 公共方法

#### ForceRelease/0

  > `public void ForceRelease()`

  * Parameters
   * _none_
  * Returns
   * _none_

ForceRelease方法会强制手柄停止抓取当前的可交互物体.

#### AttemptGrab/0

  > `public void AttemptGrab()`

  * Parameters
   * _none_
  * Returns
   * _none_

AttemptGrab方法会尝试抓取当前被触碰的物体，无需按下手柄上的抓取按钮.
目前看来好像是为外部脚本提供直接抓取的功能(ArrowSpawner,VRTK_ObjectAutoGrab等).

#### GetGrabbedObject/0

  > `public GameObject GetGrabbedObject()`

  * Parameters
   * _none_
  * Returns
   * `GameObject` - 正在被当前手柄抓取的游戏对象.

GetGrabbedObject方法返回正在被当前手柄抓取的游戏对象.

### 私有方法

#### SetControllerAttachPoint/0

  > `private void SetControllerAttachPoint()`

  * Parameters
   * _none_ 
  * Returns
   * _none_ 

SetControllerAttachPoint方法为手柄的连接点controllerAttachPoint赋值，默认情况下这个连接点是controller预置体下的一个子对象，如果有特别指定，就使用特定的对象。

#### IsObjectGrabbable/1

  > `private bool IsObjectGrabbable()`

  * Parameters
   * `GameObject` - 被抓取物体的游戏对象
  * Returns
   * `bool` - 游戏对象是否可抓取

IsObjectGrabbable方法分别从手柄上的touch脚本和要判断的游戏对象的交互脚本来判断这个游戏对象是不是可以被抓取。

#### IsObjectHoldOnGrab/1

  > `private bool IsObjectHoldOnGrab()`

  * Parameters
   * `GameObject` - 被抓取物体的游戏对象
  * Returns
   * `bool` - 游戏对象是需要一直按键来保持抓取

IsObjectHoldOnGrab方法判断一个游戏对象是否需要一直按着按钮来保持抓取状态，通过要被抓取的游戏对象的交互脚本来判断。

#### GetSnapHandle/1

  > `private Transform GetSnapHandle()`

  * Parameters
   * `VRTK_InteractableObject` - 要抓取的游戏对象的交互脚本.
  * Returns
   * `Transform` - 抓取部位的Transform

GetSnapHandle方法返回当前手柄抓取物体的部位，这个部位由物体的设置决定。物体会为左右手柄设置抓取自己的部位，如果只指定了一侧手柄的抓取部位，那么在此方法中，将另外一个手柄的抓取部位也设为这个部位。

#### SetSnappedObjectPosition/1

  > `private void SetSnappedObjectPosition()`

  * Parameters
   * `GameObject` - 被抓取物体的游戏对象
  * Returns
   * _none_

SetSnappedObjectPosition方法为被抓取对象设置位置和旋转，如果该物体没有指定抓取部位,手柄连接点和物体重合就可以；如果指定了抓取部位，手柄连接点的位置应该与物体抓取点重合，根据抓取点的位置和手柄连接点的位置计算物体的位置。

#### SnapObjectToGrabToController/1

  > `private void SnapObjectToGrabToController()`

  * Parameters
   * `GameObject` - 被抓取物体的游戏对象
  * Returns
   * _none_

SnapObjectToGrabToController方法把要抓取物体的游戏对象连接到手柄上，根据不同的抓取方式，连接的方式也不同。

#### CreateJoint/1

  > `private void CreateJoint()`

  * Parameters
   * `GameObject` - 被抓取物体的游戏对象
  * Returns
   * _none_

CreateJoint方法为被抓取物体的游戏对象创建关节，连接到手柄上，关节的属性值来自于物体的交互脚本。

#### ReleaseGrabbedObjectFromController/1
#### ReleaseAttachedObjectFromController/1
#### ReleaseParentedObjectFromController/0

这三个方法连在一起，除了销毁关节，其他都是返回刚体组件，不知道有什么用
返回的刚体组件在丢东西的方法里有用

#### ThrowReleasedObject/3

  > `private void ThrowReleasedObject()`

  * Parameters
   * `Rigidbody` - 被释放物体的刚体组件
   * `uint` - 执行交互的手柄索引
   * `float` - 初速度乘子
  * Returns
   * _none_

ThrowReleasedObject方法，在物体被手柄丢出去的时候，为其刚体组件添加一个沿手柄运动方向的初速度。

#### GrabInteractedObject/0
#### GrabTrackedObject/0
#### GrabClimbObject/0

这三个方法分别为可交互物体，追踪连接物体，可攀爬物体初始化。初始化后`grabbedObject`应不为空，返回`true`；若仍为空，说明初始化失败。

#### InitGrabbedObject/0

  > `private void InitGrabbedObject()`

  * Parameters
   * _none_
  * Returns
   * _none_

InitGrabbedObject方法通过手柄上的touch脚本初始化当前抓取物体grabbedObject

#### HideController/0

  >`HideController()`
  
  * Parameters
   * _none_
  * Returns
   * _none_

HideController方法使用`VRTK_ControllerActions`脚本关闭手柄模型

#### UngrabInteractedObject/2

  >`UngrabInteractedObject()`
  
  * Parameters
   * `uint` - 执行操作的手柄索引
   * `bool` - 是否扔出物体
  * Returns
   * _none_

UngrabInteractedObject方法用于取消抓取当前物体，根据参数来控制是否在释放的同时丢出物体

#### UngrabTrackedObject/0
#### UngrabClimbObject/0

这两个方法和UngrabInteractedObject不同的地方是只调用了InitGrabbedObject，没有做其他事情，根据参数来控制是否在释放的同时丢出物体

#### ReleaseObject/2

  >`ReleaseObject()`
  
  * Parameters
   * `uint` - 执行操作的手柄索引
   * `bool` - 是否扔出物体
  * Returns
   * _none_

ReleaseObject方法只是调用了UngrabInteractedObject方法

#### GetGrabbableObject/0

  >`private GameObject GetGrabbableObject()`
  
  * Parameters
   * _none_ 
  * Returns
   * `GameObject` - 可以抓取的游戏对象

GetGrabbableObject方法返回一个可以抓取的游戏对象，调取手柄上的touch脚本，如果正在被触碰的物体游戏对象是可以抓取的，就返回这个游戏对象，否则返回本grab脚本中的`grabbedObject`。

#### IncrementGrabState/0

  >`private void IncrementGrabState()`
  
  * Parameters
   * _none_
  * Returns
   * _none_

如果手柄正在触碰的物体对象不需要一直按着按钮来保持抓取状态，就增加grabEnabledState标记的值。

#### AttemptGrabObject/0

  >`private void AttemptGrabObject()`
  
  * Parameters
   * _none_
  * Returns
   * _none_

AttemptGrabObject方法尝试抓取物体，首先获取一个可抓取的游戏对象，然后根据此对象的连接设置，调用不同的方法初始化grabbedObject。根据该对象的内部参数，来添加手柄的震动反馈。如果没有可以抓取的游戏对象就更新计时器

#### CanRelease/0

  >`private bool CanRelease()`
  
  * Parameters
   * _none_
  * Returns
   * `bool` 当前被抓取的物体grabbedObject是否可以被释放

CanRelease方法调取被抓取的物体grabbedObject的交互脚本，根据脚本中`isDroppable`参数(使用grab按钮是否可以把已被抓取的物体放下. 如果为false那么一旦物体被抓取就不能被放下)来判断该物体是不是能被释放。

#### AttemptReleaseObject/1

  >`private void AttemptReleaseObject(uint controllerIndex)`
  
  * Parameters
   * `uint` - 执行操作的手柄索引
  * Returns
   * _none_

AttemptReleaseObject方法尝试释放物体，但是要判断物体的可释放状态，首先该物体被抓取后是可以被释放的，并且

1. 该物体需要一直按着按钮保持抓取`IsObjectHoldOnGrab(grabbedObject) == true` 
2. 或者该物体不需要一直按着按钮来保持抓取，但是抓取按钮被再次按下`grabEnabledState >= 2`

如果上述条件都成立，就根据物体与手柄的连接方式来调用不同的释放物体的方法

### 委托方法

#### DoGrabObject/2
        private void DoGrabObject(object sender, ControllerInteractionEventArgs e)
        {
            AttemptGrabObject();
        }
#### DoReleaseObject/2 
        private void DoReleaseObject(object sender, ControllerInteractionEventArgs e)
        {
            AttemptReleaseObject(e.controllerIndex);
        }

上述两个方法用来委托给(监听)VRTK_ControllerEvents脚本中的AliasGrabOn/AliasGrabOff事件，抓取按钮被按下时，调用`AttemptGrabObject()`，抓取按钮被释放时，调用`AttemptReleaseObject(e.controllerIndex)`
        
			GetComponent<VRTK_ControllerEvents>().AliasGrabOn += new ControllerInteractionEventHandler(DoGrabObject);
            GetComponent<VRTK_ControllerEvents>().AliasGrabOff += new ControllerInteractionEventHandler(DoReleaseObject);

### MonoBehaviour类方法

#### Awake

脚本一加载就要初始化需要的脚本

#### OnEnable

脚本可用时，绑定事件，开始监听，为手柄设置连接点

#### OnDisable

脚本禁用时，解绑事件，停止监听，强制手柄释放抓取

#### Update

每帧更新，用于在连接点为空的时候来设置连接点；

获取按钮的按下/释放状态，当

1. 抓取按钮的状态是按下时，但是没有可交互的物体，设置手柄刚体组件的`isKinematic = false`，把此时的手柄作为一个普通的刚体，可以与场景中别的刚体碰撞。
2. 当抓取按钮松开时，设置手柄刚体组件的`isKinematic = true`，手柄会穿过其他物体。

未按下按钮时，根据参数的设置，尝试直接抓取物体

### Example
  
`SteamVR_Unity_Toolkit/Examples/005_Controller/BasicObjectGrabbing` demonstrates the grabbing of interactable objects that have the `VRTK_InteractableObject` script attached to them. The objects can be picked up and thrown around.

`SteamVR_Unity_Toolkit/Examples/013_Controller_UsingAndGrabbingMultipleObjects` demonstrates that each controller can grab and use objects independently and objects can also be toggled to their use state simultaneously.

`SteamVR_Unity_Toolkit/Examples/014_Controller_SnappingObjectsOnGrab` demonstrates the different mechanisms for snapping a grabbed object to the controller.

---
