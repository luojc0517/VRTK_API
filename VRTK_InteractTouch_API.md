## 触碰可交互物体 (VRTK_InteractTouch)

### 概述

Interact Touch脚本绑定在[CameraRig]下的手柄游戏对象上.

### Inspector可见参数

  * **Hide Controller On Touch**: 当发生有效触碰时隐藏手柄模型
  * **Hide Controller Delay:** 当手柄触碰物体到隐藏手柄之间的延迟时间
  * **Global Touch Highlight Color:** 如果可交互物体被触碰的时候可以被高亮，但是物体没有设置高亮颜色，那么使用这个全局高亮颜色
  * **Custom Rigidbody Object:** 如果需要额外定制刚体和碰撞体，那么可以通过这个参数来传递.如果为空，那么运行时系统会自动适配HTC Vive的默认手柄

### 内置参数
* **defaultColliderPrefab:** 手柄上面的碰撞器的预置体(如果未来要支持oculus的手柄的话可以自己做一个对应的collider组合)

![](http://i.imgur.com/Zf64ZFZ.png)

### 事件类

  * `ControllerTouchInteractableObject` -  触碰有效物体时发送事件，调用委托给自己的方法
  * `ControllerUntouchInteractableObject` - 不再触碰有效物体时发送事件，调用委托给自己的方法

#### 事件参数

  * `uint controllerIndex` - 正在交互的手柄索引
  * `GameObject target` - 正在和手柄交互的物体的游戏对象

### 方法

#### ForceTouch/1

  > `public void ForceTouch(GameObject obj)`

  * Parameters
   * `GameObject obj` - 试图强行触碰的游戏对象
  * Returns
   * _none_

ForceTouch方法会试图强制手柄去触碰给定的游戏对象. 当一个物体没有接触手柄，但是又需要被抓取或使用时，这个方法很有用.手柄无需接触物体，但是可以强制与它交互.

#### GetTouchedObject/0

  > `public GameObject GetTouchedObject()`

  * Parameters
   * _none_
  * Returns
   * `GameObject` - 正在被当前手柄触碰的游戏对象

GetTouchedObject方法返回正在被当前手柄触碰的游戏对象

#### IsObjectInteractable/1

  > `public bool IsObjectInteractable(GameObject obj)`

  * Parameters
   * `GameObject obj` - 需要判断是否可交互的游戏对象
  * Returns
   * `bool` - 如果游戏对象绑定了 `VRTK_InteractableObject`且脚本可用返回`true`

IsObjectInteractable方法用来检查一个给定的游戏对象是否是可交互的对象，以及交互脚本是不是可用

#### ToggleControllerRigidBody/1

  > `public void ToggleControllerRigidBody(bool state)`

  * Parameters
   * `bool state` - 手柄刚体碰撞能力开/关的状态. `true`打开刚体组件，`false`关闭
  * Returns
   * _none_

ToggleControllerRigidBody方法控制手柄上面刚体组件的碰撞检测能力.如果它为真，那么手柄会与其他物体发生碰撞.

#### IsRigidBodyActive/0

  > `public bool IsRigidBodyActive()`

  * Parameters
   * _none_
  * Returns
   * `bool` - 如果当前手柄的刚体组件可用并且可以影响场景中其他的刚体时返回真

IsRigidBodyActive方法用于检查前手柄的刚体组件是否可用并且可以影响场景中其他的刚体

#### ForceStopTouching/0

  > `public void ForceStopTouching()`

  * Parameters
   * _none_
  * Returns
   * _none_

ForceStopTouching方法会停止手柄与物体的交互，即使在视觉上他们仍然是接触的

#### ControllerColliders/0

  > `public Collider[] ControllerColliders()`

  * Parameters
   * _none_
  * Returns
   * `Collider[]` - 与手柄关联的碰撞器数组

ControllerColliders方法检索返回所有与手柄相关的碰撞器

----------
###主要逻辑

#### OnTriggerEnter/1

>`OnTriggerEnter(Collider collider)`
        
		private void OnTriggerEnter(Collider collider)
        {
            // 如果进入手柄trigger范围的碰撞器是可交互的物体，并且当前没有触碰的物体或者当前触碰的物体没有被抓取
            if (IsObjectInteractable(collider.gameObject) && (touchedObject == null || !touchedObject.GetComponent<VRTK_InteractableObject>().IsGrabbed()))
            {
                // 将进入范围的碰撞器对应的可交互对象保存为lastTouchedObject
                lastTouchedObject = GetColliderInteractableObject(collider);
            }
        }
当有新的可交互的碰撞体进入手柄，那么就当它作为最新的触碰物体

#### OnTriggerStay/1
>`OnTriggerStay(Collider collider)`

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
				...// 处理手柄的隐藏
				...// 处理物体高亮
				...// 处理手柄震动反馈
            }
        }

在新的碰撞体停留在手柄的Trigger范围内时，就要检查，如果手柄当前正在触摸一个物体，但是没有抓取，并且这个新的碰撞体也是可以交互的，那么手柄应该停止对当前物体的触碰，去触碰新的碰撞体对应的游戏对象

----------


### Example

`SteamVR_Unity_Toolkit/Examples/005_Controller/BasicObjectGrabbing` demonstrates the highlighting of objects that have the `VRTK_InteractableObject` script added to them to show the ability to highlight interactable objects when they are touched by the controllers.

---

在unity中模拟的时候可以禁用一些代码。
例如，手柄的显示/隐藏，可以自己手动写成手柄下的模型的active的状态改变

        // jackie自己做的一个模拟HTCVive手柄的模型对象
        private GameObject HTCViveModel;


隐藏手柄模型

        private void HideController()
        {
            if (touchedObject != null)
            {
                //controllerActions.ToggleControllerModel(false, touchedObject);
                HTCViveModel.SetActive(false);
            }
        }


----------
