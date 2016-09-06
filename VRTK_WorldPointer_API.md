###概述

这个抽象类可以让任何游戏指示器获取实现指示器的状态. 它继承自 `VRTK_DestinationMarker` ，可以在指示器光标碰撞其他objects的时候发送destination事件.

World Pointer也为所有继承子类的光标提供了一个play area范围显示. play area范围类似标准player area，可以用于显示可能要传送去的新play area. 它同样可以处理新play area空间处理的物体碰撞，当目的地play area与物体碰撞的时候，play area会显示红色或其他指定颜色，表示这个地方是不能传送的.

当使用地形的时候，play area collider的效果不是很好，因为地形是不均匀的，因此建议在使用地形的时候关闭play area collisions.

###Inspector Parameters

- **Enable Teleport:** 如果勾选此项那么Destination Set event中的teleport会被置为真，teleport脚本就能知道是否该移动到新的位置. 可以既启用controller光束又不让它触发传送(如果不勾选此项).
- **Controller:** controller用于打开指示器. 如果Controller已经绑定了VRTK_COntrollerEvents脚本那么这个参数可以留空，运行的时候会被自动赋值.
- **Pointer Material:** 指示器渲染的材质. 默认使用 `WorldPointer` 材质.
- **Pointer Hit Color:** 当光束碰撞到有效目标时的颜色. 可以为不同的Controller设置不同的颜色.
- **Pointer Miss Color:** 当光束没有碰撞到有效目标时的颜色. 可以为不同的Controller设置不同的颜色.
- **Show Play Area Cursor:** 如果勾选此项那么play area的边界会显示在指示光束的顶端，颜色为当前指示光束的颜色.
- **Play Area Cursor Dimensions:** play area cursor和collider的大小. 若设值为0那么Play Area Cursor的大小就是标准play area的大小.
- **Handle Play Area Cursor Collisions:** 若勾选此项，当play area cursor碰撞到其他object那么指示线的颜色会变成`Pointer Miss Color`并且`WorldPointerDestinationSet`事件不会被触发, 避免传送至play area会碰撞的区域.
- **Ignore Target With Tag Or Class:** 一个字符串，代表一个object tag或者一个object上面绑定的脚本名字，告知play area cursor忽略与它们的碰撞.
- **Pointer Visibility:** 是否显示指示线:
 - `On_When_Active` 当Pointer按钮被按下时才显示.
 - `Always On` 总是显示，但只有按下Pointer button时才触发destination set事件.
 - `Always Off` 从不显示，但是仍可以设置目标点，按下Pointer按钮仍然触发destination set事件.
- **Hold Button To Activate:** 在两次按下按钮的间隔内指示线会一直可见.destination set事件在第二次按钮按下前第一束光束释放时才触发.
- **Activate Delay:** 两束射线之间的时间间隔. 避免频繁传送.
##Class Methods

###Virtual方法（虚方法）

virtual 关键字用于在基类中修饰方法。virtual的使用会有两种情况：

- 情况1：在基类中定义了virtual方法，但在派生类中没有重写该虚方法。那么在对派生类实例的调用中，该虚方法使用的是基类定义的方法。
- 情况2：在基类中定义了virtual方法，然后在派生类中使用override重写该方法。那么在对派生类实例的调用中，该虚方法使用的是派生重写的方法。
###setPlayAreaCursorCollision/1

&gt;`public virtual void setPlayAreaCursorCollision(bool state)`

- Parameters
 - `bool state` - play area cursor是否与其他物体碰撞.
- Returns
 - *none*

setPlayAreaCursorCollision方法用于设置playAreaCursorCollided的碰撞状态，前提是handlePlayAreaCursorCollisions为真，即考虑检测碰撞.
###IsActive/0


&gt;`public virtual bool IsActive()`

- Parameters
 - *none*
- Returns
 - `bool` - 当前射线为active时为真.

IsActive用于查看当前指示射线是否active.

###CanActivate/0

&gt;`public virtual bool CanActivate()`

- Parameters
 - *none*
- Returns
 - `bool` - 如果为真代表时间间隔已过，可以使下一束射线可用.

CanActivate方法确认是否计时完毕，使得下一束射线可用.

###ToggleBeam/1

&gt;`public virtual void ToggleBeam(bool state)`

- Parameters
 - `bool state` - 是否打开射线.
- Returns
 - *none*

ToggleBeam方法可以在脚本运行时动态的控制射线的开关. 参数传入真则射线会打开,参数传入假那么射线将会关闭.


----------
###PointerIn()、PointerOut()、PointerSet()
这三个方法分别用于处理射线停留物体，离开物体，传送时的物体交互，以及回调委托给自己的方法

###TogglePointer
这个方法用于处理目标传送区域的可见状态，以及某些情况下的物体交互，根据传入的参数true or false决定

        protected virtual void TogglePointer(bool state)
        {
            // 如果play area cursor可用，那么根据射线开，area显示，射线关，area不显示；如果不可用，那么一直为false
            var playAreaState = (showPlayAreaCursor ? state : false);
            if (playAreaCursor)
            {
                playAreaCursor.gameObject.SetActive(playAreaState);
            }
            if (!state && interactableObject && interactableObject.pointerActivatesUseAction && interactableObject.holdButtonToUse && interactableObject.IsUsing())
            {
                //如果射线关闭，停止物体交互
                interactableObject.StopUsing(this.gameObject);
            }
        }

###SetPointerMaterial() && UpdatePointerMaterial(Color color)

- SetPointerMaterial()方法中将play area cursor矩形框的材质设为和Pointer射线一致

- UpdatePointerMaterial(Color color)则根据碰撞检测等，设置射线的颜色，并调用SetPointerMaterial()同步play area cursor颜色

###DrawPlayAreaCursorBoundary(int index, float left, float right, float top, float bottom, float thickness, Vector3 localPosition)

