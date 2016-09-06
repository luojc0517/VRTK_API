##VRTK_BasicTeleport
### 概述

基础传送类用于实时更新`[CameraRig]`的x/z位置为WorldPointer射线tip的位置(我们看到的是指示线末端的小圆球),这一过程是通过`WorldPointerDestinationSet`事件实现的.在此设定y的值是不改变的，也就是说基础传送类不能处理上下的传送，我们只能在一个平面上进行水平传送.

这个脚本绑定在`[CameraRig]` 预置体上，同时要在射线发送对象上绑定一个实现VRTK_WordPointer的子类(例如为Controller绑定VRTK_SimplePointer).

### Inspector Parameters

  * **Blink Transition Speed:** basic teleport脚本可以为传送设置一个屏幕亮度渐变的速度，传送的时候屏幕变黑，传送完成的时候变亮.设置速度为0意味着当前传送没有闪烁渐变的效果.这个效果通过`SteamVR_Fade.cs` 脚本实现.
  * **Distance Blink Delay:** 一个0-32的范围，屏幕渐变暗过程的时长，由传送距离决定. 为0表示无论距离多远都不变暗, 为32表示即使距离很短，例如传送至原地，转变的过程也很长. 可以模拟玩家从一个地点传送到另外一个地点花费的时间. 设为16为佳.
  * **Headset Position Compensation:** 如果勾选此项那么传送地点相对于传送区域的位置和传送前Camera相对于play area的位置一致. 如果未勾选那么无论现在站在play area的什么地方，传送后都会站在传送区域的正中央.
  * **Ignore Target With Tag Or Class:** 如果一个gameobject的tag是这个字符串或者绑定了名为这个字符串的脚本，那么玩家不能传送到那里. 射线和传送区域的颜色会变成miss color.
  * **Nav Mesh Limit Distance:** nav mesh采集有效传送位置范围的最大值. 如果为`0`那么nav mesh约束可被忽略.

### 事件

  * `Teleporting` - 当传送开始时调用委托给它的方法.
  * `Teleported` - 当传送全部完成时调用委托给它的方法.

#### Event Payload

  > 参数装载的结构体和[VRTK_DestinationMarker Event Payload](#vrtk_destinationmarker)一样.

### Class Methods

#### InitDestinationSetListener/1

  > `public void InitDestinationSetListener(GameObject markerMaker)`

  * Parameters
   * `GameObject markerMaker` - 实现destination marker的组件, 例如一个controller.
  * Returns
   * _none_

这个方法可以为markerMaker下所有绑定了VRTK_DestinationMarker的组件绑定DoTeleport方法，这些组件会在合适的时候回调它，触发传送. 

### Example

`SteamVR_Unity_Toolkit/Examples/004_CameraRig_BasicTeleport` uses the `VRTK_SimplePointer` script on the Controllers to initiate a laser pointer by pressing the `Touchpad` on the controller and when the laser pointer is deactivated (release the `Touchpad`) then the user is teleported to the location of the laser pointer tip as this is where the pointer destination marker position is set to.
