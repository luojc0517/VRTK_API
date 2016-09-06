### 概述

这个抽象类提供在游戏世界里发送目的标记事件的功能. 它可以用于实现一些特定功能的地点标记需求，例如传送功能.

它的派生子类 `VRTK_WorldPointer` 用于处理pointer events，当pointer cursor接触到游戏中的区域.

### Inspector可见参数

  * **Enable Teleport:** 如果勾选此项那么Destination Set event中的teleport会被置为真，teleport脚本就能知道是否该移动到新的位置.

## 事件类

  * `DestinationMarkerEnter` - 当与其他gameobject发生collision时发送事件.
  * `DestinationMarkerExit` - 当与其他gameobject结束collision时发送事件.
  * `DestinationMarkerSet` - 当destination marker是active时发送事件，确定最新的目标点 (可用于选择和传送).

### 事件装载参数
    public struct DestinationMarkerEventArgs
    {
        public float distance;
        public Transform target;
        public Vector3 destinationPosition;
        public bool enableTeleport;
        public uint controllerIndex;
    }

  * `float distance` - 原点和碰撞点之间的距离.
  * `Transform target` - collided destination的Transform对象.
  * `Vector3 destinationPosition` - destination marker的世界坐标.
  * `bool enableTeleport` - destination set event是否应该触发传送.
  * `uint controllerIndex` 发送光束的手柄索引.
  
## 方法

### SetInvalidTarget/1

  &gt; `public virtual void SetInvalidTarget(string name)`

  * Parameters
   * `string name` - 无效target的tag或class的name.
  * Returns
   * _none_

SetInvalidTarget方法用于将包含匹配name的给定tag或class设为invalid destination targets.

### SetNavMeshCheckDistance/1

  &gt; `public virtual void SetNavMeshCheckDistance(float distance)`

  * Parameters
   * `float distance` - The max distance the nav mesh can be from the sample point to be valid.
  * Returns
   * _none_

The SetNavMeshCheckDistance method sets the max distance the destination marker position can be from the edge of a nav mesh to be considered a valid destination.

### SetHeadsetPositionCompensation/1

  &gt; `public virtual void SetHeadsetPositionCompensation(bool state)`

  * Parameters
   * `bool state` - 当设置destination marker时是否要考虑头显在play area中的位移改变.
  * Returns
   * _none_

SetHeadsetPositionCompensation方法决定当设置destination marker时，是否要将头显关于play area中心的偏移量计入考虑. 如果为 `true` 那么位置的偏移会被考虑影响.

----------
### 发送事件代码
        //发送事件，实际调用
        public virtual void OnDestinationMarkerEnter(DestinationMarkerEventArgs e)
        {
            if (DestinationMarkerEnter != null)
            {
                DestinationMarkerEnter(this, e);
            }
        }

        //发送事件，实际调用
        public virtual void OnDestinationMarkerExit(DestinationMarkerEventArgs e)
        {
            if (DestinationMarkerExit != null)
            {
                DestinationMarkerExit(this, e);
            }
        }

        //发送事件，实际调用
        public virtual void OnDestinationMarkerSet(DestinationMarkerEventArgs e)
        {
            if (DestinationMarkerSet != null)
            {
                DestinationMarkerSet(this, e);
            }
        }
### 事件装载代码
        protected DestinationMarkerEventArgs SetDestinationMarkerEvent(float distance, Transform target, Vector3 position, uint controllerIndex)
        {
            DestinationMarkerEventArgs e;
            e.controllerIndex = controllerIndex;
            e.distance = distance;
            e.target = target;
            e.destinationPosition = position;
            e.enableTeleport = enableTeleport;
            return e;
        }


----------
这个类主要就是一些事件的声明和装载，所有继承它的子类都可以使用里面的事件机制，根据内容看来应该是一些与发送射线然后碰撞目标的逻辑。