## 可交互物体 (VRTK_InteractableObject)

### Overview

这个脚本可以绑定在任何需要交互的gameobject上 (例如通过手柄交互).

这个脚本为游戏世界里的物体提供了一种简单的途径，使得它们可以被抓取或是使用，但是更好的是，将这个脚本作为一个基类，继承它的脚本可以实现更多更丰富的功能.

### Inspector可见参数

#### 触碰交互
  * **Highlight On Touch:** 如果勾选，那么物体只有在手柄碰到它的时候才高亮.
  * **Touch Highlight Color:** 物体被碰到高亮时的颜色. 这个颜色会覆盖任何其他的颜色设置 (例如`VRTK_InteractTouch` 脚本).
  * **Rumble On Touch:** 当手柄碰到物体时触发触觉反馈, `x`表示持续时间, `y`表示脉冲强度. (在编辑器里可以修改其值)
  * **Allowed Touch Controllers:** 决定哪个手柄可以触碰物体.可用选项有:
   * `Both` 两个手柄都可以.
   * `Left_Only` 只有左边手柄可以.
   * `Right_Only` 只有右边手柄可以.
  * **Hide Controller On Touch:** 是否覆盖手柄相关设置 (触碰物体时隐藏手柄):
   * `Default` 使用手柄的设置.
   * `Override Hide` 无论手柄设置为何，隐藏.
   * `Override Dont Hide` 无论手柄设置为何，不隐藏.

#### 抓取交互
  * **Is Grabbable:** 物体是否可被抓取.
  * **Is Droppable:** 使用grab按钮是否可以把已被抓取的物体放下. 如果未勾选此项那么一旦物体被抓取就不能被放下. 但是当很大的力施加在连接处的时候，连接断裂，物体也会放下. 为了避免这种情况最好使用`child Of Controller`方式.
  * **Is Swappable:** 物体是否可以在两个手柄之间传递. 如果未勾选此项，那么物体被另一个手柄抓取之前必须先从当前手柄上放下.
  * **Hold Button To Grab:** 如果此项被勾选，那么要一直按着按钮才能保持物体抓取，松开按钮物体会掉落. 如果未勾选此项那么按一下抓取按钮物体会被抓取且在按第二下之前它不会掉落.
  * **Rumble On Grab:** 当手柄抓取物体时触发触觉反馈, `x`表示持续时间, `y`表示脉冲强度. (在编辑器里可以修改其值).
  * **Allowed Grab Controllers:** 决定哪个手柄可以抓取物体.可用选项有:
   * `Both` 两个手柄都可以.
   * `Left_Only` 只有左边手柄可以.
   * `Right_Only` 只有右边手柄可以.
  * **Precision_Snap:** 如果此项勾选那么当手柄抓取物体的时候, 它会在手柄触碰点精确地抓取物体.
  * **Right Snap Handle:** 一个空物体的Transform，它必须是被抓取物体的子对象，并且是该物体相对于右侧手柄旋转定位的基准点.如果没有提供Right Snap Handle但是提供了Left Snap Handle,那么就代而使用Left Snap Handle. 如果没有提供任何Snap Handle那么该物体被抓取的位置就是它的中心点.
  * **Left Snap Handle:** 一个空物体的Transform，它必须是被抓取物体的子对象，并且是该物体相对于左侧手柄旋转定位的基准点.如果没有提供Left Snap Handle但是提供了Right Snap Handle,那么就代而使用Right Snap Handle. 如果没有提供任何Snap Handle那么该物体被抓取的位置就是它的中心点.
  * **Hide Controller On Grab:** 是否覆盖手柄相关设置 (抓取物体时隐藏手柄):
   * `Default` 使用手柄的设置.
   * `Override Hide` 无论手柄设置为何，隐藏.
   * `Override Dont Hide` 无论手柄设置为何，不隐藏.

#### 抓取方式
  * **Grab Attach Type:** 当物体被抓取的时候，以什么样的形式附着在手柄上面.
   * `Fixed Joint` 把物体和手柄在固定的节点连接，物体和手柄的旋转移动完全同步.
   * `Spring Joint` 用弹簧关节连接物体和手柄意味着物体与手柄施加给它的力之间有一定的灵活性. 如果不想物体直接吸附到手柄上而是希望有一种拉扯的效果就可以使用这种. 它可以给人一种物体移动有阻力的错觉.
   * `Track Object` 物体不会吸附在手柄上, 但是它会随着手柄的方向移动, 铰链关节的物体可以使用这种.
   * `Rotator Track` 跟随手柄的动作旋转. 例如手柄控制门的开关.
   * `Child Of Controller` 让该物体直接变成手柄的一个子对象.
   * `Climbable` 用来攀爬的非刚体结构可交互物体.
  * **Detach Threshold:** 把物体和手柄分离时需要的力的大小.如果手柄意图给物体施加一个比这个值要大的力(from pulling it through another object or pushing it into another object)那么物体与手柄之间的连接就会断开，这样物体便不再被抓取.如果物体是以`Track Object`方式被抓取，没有吸附在手柄上的话，那就根据距离来判断是否分离.
  * **Spring Joint Strength:** 物体与手柄连接弹簧的弹力.数值越小弹簧越松，那么需要更大的力才能移动物体,数值越大弹簧越紧，一点点力就会让物体移动.
  * **Spring Joint Damper:** 使用弹簧方式连接时，使得弹力衰减的量.数值较大时可以减少移动连接的可交互物体时的震荡效应.
  * **Throw Multiplier:** 当抛出物体时需要给速度乘上一个这个值. This can also be used in conjunction with the Interact Grab Throw Multiplier to have certain objects be thrown even further than normal (or thrown a shorter distance if a number below 1 is entered).
  * **On Grab Collision Delay:** 当物体第一次被抓取时，给碰撞效果一个延时.这个效果在物体卡在别的东西里面时很有用.

#### 使用交互
  * **Is Usable:** 物体是否可以被使用.
  * **Use Only If Grabbed:** 如果此项勾选那么物体使用之前必须先被抓取.
  * **Hold Button To Use:** 如果勾选此项那么要一直按着使用按钮物体才能被持续使用.如果此项未勾选那么在按第二次按钮之前物体会持续使用.
  * **Pointer Activates Use Action:** 如果勾选此项那么手柄发出的射线击中可交互物体以后，, 如果这个物体的`Hold Button To Use`选项没有勾选那么射线消失的同时会触发它的`Using` 方法.如果`Hold Button To Use` 未勾选那么当射线消失的时候`Using`会调用. 此项勾选时，当射线击中可交互物体的时候，world pointer 不会抛出`Destination Set`事件，避免当使用物体时发生不必要的传送.
  * **Rumble On Use:** 当手柄使用物体时触发触觉反馈, `x`表示持续时间, `y`表示脉冲强度. (在编辑器里可以修改其值).
  * **Allowed Use Controllers:** 决定哪个手柄可以使用物体.可用选项有:
   * `Both` 两个手柄都可以.
   * `Left_Only` 只有左边手柄可以.
   * `Right_Only` 只有右边手柄可以.
  * **Hide Controller On Use:** 是否覆盖手柄相关设置 (使用物体时隐藏手柄):
   * `Default` 使用手柄的设置.
   * `Override Hide` 无论手柄设置为何，隐藏.
   * `Override Dont Hide` 无论手柄设置为何，不隐藏.

### 事件类

  * `InteractableObjectTouched` - 其他物体触碰当前物体时发送事件.
  * `InteractableObjectUntouched` - 其他物体停止触碰当前物体时发送事件.
  * `InteractableObjectGrabbed` - 其他物体(例如手柄)抓取当前物体时发送事件.
  * `InteractableObjectUngrabbed` - 其他物体停止抓取当前物体时发送事件.
  * `InteractableObjectUsed` - 其他物体(例如手柄)使用当前物体时发送事件.
  * `InteractableObjectUnused` - 其他物体停止使用当前物体时发送事件.

#### 事件参数

  * `GameObject interactingObject` - 发起交互行为的gameobject(例如手柄)

### 类方法

#### CheckHideMode/2

  > `public bool CheckHideMode(bool defaultMode, ControllerHideMode overrideMode)`

  * Parameters
   * `bool defaultMode` - 手柄上的默认设置(true=隐藏, false=不隐藏).
   * `ControllerHideMode overrideMode` - 物体的设置，将覆盖手柄默认设置.
     * `Default` 使用手柄的设置.(returns `overrideMode`).
     * `OverrideHide` 无论手柄设置为何，隐藏.(even if `defaultMode`is `true`).
     * `OverrideDontHide` 无论手柄设置为何，不隐藏.(even if `defaultMode`is `false`).
  * Returns
   * `bool` - 返回`true`，如果综合`defaultMode`和`overrideMode`考虑后依旧要隐藏手柄 .

CheckHideMode方法是一个供其他脚本(例如InteractTouch InteractGrab InteractUse)使用的简单的方法，它可以用来计算手柄隐藏与否，通过同时考虑手柄默认设置和可交互物体的设置.

#### IsTouched/0

  > `public bool IsTouched()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 返回`true`如果物体正在被触碰.

IsTouched方法用于检查物体当前是不是被触碰.

#### IsGrabbed/0

  > `public bool IsGrabbed()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 返回`true`如果物体正在被抓取.

IsGrabbed方法用于检查物体当前是不是被抓取.

#### IsUsing/0

  > `public bool IsUsing()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 返回`true`如果物体正在被使用.

IsUsing方法用于检查物体当前是不是被使用.

#### StartTouching/1

  > `public virtual void StartTouching(GameObject currentTouchingObject)`

  * Parameters
   * `GameObject currentTouchingObject` - 正在触碰该物体的游戏对象(例如手柄).
  * Returns
   * _none_

StartTouching方法会自动地被调用，当物体开始被触碰时.调用处是手柄绑定的VRTK_InteractTouch脚本的OntriggerStay.它是一个虚方法，可以被子类重写.

#### StopTouching/1

  > `public virtual void StopTouching(GameObject previousTouchingObject)`

  * Parameters
   * `GameObject previousTouchingObject` - 先前触及此物体的游戏对象(例如手柄).
  * Returns
   * _none_

StopTouching方法会自动地被调用，当物体停止被触碰时.调用处是手柄绑定的VRTK_InteractTouch脚本的StopTouching,然后在OnTriggerExit处最后调用.它是一个虚方法，可以被子类重写.
#### Grabbed/1

  > `public virtual void Grabbed(GameObject currentGrabbingObject)`

  * Parameters
   * `GameObject currentGrabbingObject` - 正在抓取当前物体的游戏对象(例如手柄).
  * Returns
   * _none_

Grabbed方法会自动地被调用，当物体开始被抓取时.它是一个虚方法，可以被子类重写.

#### Ungrabbed/1

  > `public virtual void Ungrabbed(GameObject previousGrabbingObject)`

  * Parameters
   * `GameObject previousGrabbingObject` - 先前抓取此物体的游戏对象(例如手柄).
  * Returns
   * _none_

UnGrabbed方法会自动地被调用，当物体停止被抓取时.它是一个虚方法，可以被子类重写.

#### StartUsing/1

  > `public virtual void StartUsing(GameObject currentUsingObject)`

  * Parameters
   * `GameObject currentUsingObject` - 正在使用当前物体的游戏对象(例如手柄).
  * Returns
   * _none_

StartUsing方法会自动地被调用，当物体开始被使用时.它是一个虚方法，可以被子类重写.

#### StopUsing/1

  > `public virtual void StopUsing(GameObject previousUsingObject)`

  * Parameters
   * `GameObject previousUsingObject` - 先前使用                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      此物体的游戏对象(例如手柄).
  * Returns
   * _none_

StopUsing方法会自动地被调用，当物体停止被使用时.它是一个虚方法，可以被子类重写.
#### ToggleHighlight/1

  > `public virtual void ToggleHighlight(bool toggle)`

  * Parameters
   * `bool toggle` - 为`true`时启用高亮，为`false`时禁用高亮.
  * Returns
   * _none_

ToggleHighlight/1方法是一个关闭高亮的快捷方法，它的方法签名和实际控制的高亮开关方法一样. 它传入的参数必须永远是`false`，并且调用`ToggleHighlight(toggle,Color.clear)`实现关闭高亮.

#### ToggleHighlight/2

  > `public virtual void ToggleHighlight(bool toggle, Color globalHighlightColor)`

  * Parameters
   * `bool toggle` - 为`true`时启用高亮，为`false`时禁用高亮.
   * `Color globalHighlightColor` - 高亮物体时使用的颜色.
  * Returns
   * _none_

ToggleHighlight/2方法用于开启/关闭物体的高亮.

#### PauseCollisions/0

  > `public void PauseCollisions()`

  * Parameters
   * _none_
  * Returns
   * _none_

PauseCollisions方法在物体被抓取的时候通过移除物体上刚体组件的检测碰撞功能来暂停物体上的所有碰撞.这个方法可以在第一次抓取物体的时候避免手柄将物体弹开.

#### AttachIsTrackObject/0

  > `public bool AttachIsTrackObject()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 当抓取方式为追踪连接时为真，例如`Track Object`或者`Rotator Track`.

AttachIsTrackObject方法用于确认物体是否使用了追踪连接的方式被抓取.

#### AttachIsClimbObject/0

  > `public bool AttachIsClimbObject()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 抓取方式为`Climbable`时为真.

AttachIsClimbObject方法用于确认物体是否使用了`Climbable`的抓取连接方式.

#### AttachIsStaticObject/0

  > `public bool AttachIsStaticObject()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 抓取方式为类似`Climbable`的静态连接方式时为真.

AttachIsStaticObject方法用于确认物体是否使用了静态的抓取连接方式.

#### ZeroVelocity/0

  > `public void ZeroVelocity()`

  * Parameters
   * _none_
  * Returns
   * _none_

ZeroVelocity方法重置物体的刚体组件的速度和角速度都为0.

#### SaveCurrentState/0

  > `public void SaveCurrentState()`

  * Parameters
   * _none_
  * Returns
   * _none_

SaveCurrentState方法将物体当前的父对象以及刚体运动学设置保存下来.

#### ToggleKinematic/1

  > `public void ToggleKinematic(bool state)`

  * Parameters
   * `bool state` - 物体的刚体运动学状态.
  * Returns
   * _none_

ToggleKinematic方法用于设置物体的刚体运动学状态.

#### GetGrabbingObject/0

  > `public GameObject GetGrabbingObject()`

  * Parameters
   * _none_
  * Returns
   * `GameObject` - 正在抓取当前物体的游戏对象.

GetGrabbingObject方法用于找到正在抓取当前物体的游戏对象.

#### IsValidInteractableController/2

  > `public bool IsValidInteractableController(GameObject actualController, AllowedController controllerCheck)`

  * Parameters
   * `GameObject actualController` - 接受检测的手柄游戏对象.
   * `AllowedController controllerCheck` - 哪个手柄被允许与当前物体交互.
  * Returns
   * `bool` - 如果此手柄被允许与当前物体交互.

IsValidInteractableController方法由于检测一个手柄游戏对象是否被允许与当前物体交互，因为有的情况下(根据使用需求)手柄是禁止与物体交互的.

#### ForceStopInteracting/0

  > `public void ForceStopInteracting()`

  * Parameters
   * _none_
  * Returns
   * _none_

ForceStopInteracting方法强行停止物体的交互行为，手柄会放下物体并停止触碰它.当手柄要和另外的物体交互时这个方法很有用.

#### SetGrabbedSnapHandle/1

  > `public void SetGrabbedSnapHandle(Transform handle)`

  * Parameters
   * `Transform handle` - 物体被抓取的时候，抓取的位置，例如一个杯子，可以设置把手为SnapHandle.
  * Returns
   * _none_

SetGrabbedSnapHandle方法用于在运行的时候设置物体抓取的位置.

#### RegisterTeleporters/0

  > `public void RegisterTeleporters()`

  * Parameters
   * _none_
  * Returns
   * _none_

RegisterTeleporters方法用于找到绑定了传送脚本的游戏对象，把`OnTeleported`委托给传送脚本里的Teleported事件.这个方法可以让物体随着传送移动.

### 私有/保护方法
#### LoadPreviousState/0

  >`protected virtual void LoadPreviousState()`

  * Parameters
   * _none_
  * Returns
   * _none_

LoadPreviousState方法会恢复物体被抓取之前的一些重要状态。这个方法非常重要，当物体的抓取方式是作为手柄的子对象时，释放物体后，物体的parent要改变为初始的状态，否则就不如我们预想了。

### Example

`SteamVR_Unity_Toolkit/Examples/005_Controller_BasicObjectGrabbing` 使用`VRTK_InteractTouch`和 `VRTK_InteractGrab`脚本 scripts on the controllers to show how an interactable object can be grabbed and snapped to the controller and thrown around the game world.

`SteamVR_Unity_Toolkit/Examples/013_Controller_UsingAndGrabbingMultipleObjects` shows mutltiple objects that can be grabbed by holding the buttons or grabbed by toggling the button click and also has objects that can have their Using state toggled to show how mutliple items can be turned on at the same time.