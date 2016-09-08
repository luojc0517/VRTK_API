//====================================================================================
//
// Purpose: Provide abstraction into setting a destination position in the scene
// As this is an abstract class, it should never be used on it's own.
//
// Events Emitted:
//
// DestinationMarkerEnter - is emitted when an object is collided with
// DestinationMarkerExit - is emitted when the object is no longer collided
// DestinationMarkerSet - is emmited when the destination is set
//
// Event Payload:
//
// distance - The distance between the origin and the collided destination
// target - The Transform of the destination object
// destiationPosition - The world position of the destination marker
// enableTeleport - Determine if the DestinationSet event should allow teleporting
// controllerIndex - The optional index of the controller the pointer is attached to
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;

    //委托事件的参数结构体
    public struct DestinationMarkerEventArgs
    {
        public float distance;// 原点和碰撞点之间的距离.
        public Transform target;// collided destination的Transform对象.
        public Vector3 destinationPosition;// destination marker的世界坐标.
        public bool enableTeleport;// destination set event是否应该触发传送.
        public uint controllerIndex;// 发送光束的手柄索引.
    }

    //委托声明
    public delegate void DestinationMarkerEventHandler(object sender, DestinationMarkerEventArgs e);

    public abstract class VRTK_DestinationMarker : MonoBehaviour
    {
        public bool enableTeleport = true;

        public event DestinationMarkerEventHandler DestinationMarkerEnter;// 当与其他gameobject发生collision时发送事件.
        public event DestinationMarkerEventHandler DestinationMarkerExit;// 当与其他gameobject结束collision时发送事件.
        public event DestinationMarkerEventHandler DestinationMarkerSet;// 当destination marker是active时发送事件，确定最新的目标点 (可用于选择和传送).

        protected string invalidTargetWithTagOrClass;//如果一个target的tag或者class为这个字符串则为无效target
        protected float navMeshCheckDistance;// nav mesh采样距离
        protected bool headsetPositionCompensation;//是否考虑头显位移

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

        /// <summary>
        /// SetInvalidTarget方法用于将包含匹配name的给定tag或class设为invalid destination targets.
        /// </summary>
        /// <param name="name"></param>
        public virtual void SetInvalidTarget(string name)
        {
            invalidTargetWithTagOrClass = name;
        }

        /// <summary>
        /// SetNavMeshCheckDistance方法返回从标记点开始进行nav mesh采样的最大距离
        /// </summary>
        /// <param name="distance">nav mesh采样的最大距离.</param>
        public virtual void SetNavMeshCheckDistance(float distance)
        {
            navMeshCheckDistance = distance;
        }

        /// <summary>
        /// SetHeadsetPositionCompensation方法决定当设置destination marker时，是否要将头显关于play area中心的偏移量计入考虑. 如果为 `true` 那么位置的偏移会被考虑影响.
        /// </summary>
        /// <param name="state">当设置destination marker时是否要考虑头显在play area中的位移改变.</param>
        public virtual void SetHeadsetPositionCompensation(bool state)
        {
            headsetPositionCompensation = state;
        }

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
    }
}