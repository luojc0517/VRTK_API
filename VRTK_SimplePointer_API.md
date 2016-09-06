
&gt;`extends VRTK_WorldPointer`

###概述

Simple Pointer从controller发送一条彩色的射线来模拟激光射线. 可以用来指示场景中的物体，计算controller和物体之间的距离等.

渲染出来的射线是一条直线，末端有一个小小的实心圆来标记传送的目的地，或者击中物体的位置，也可以勾选显示一个传送目的地的play area(一个长方体矩形框).

默认是按下TouchPad使得射线active. 它监听的是AliasPointer事件，因此可以在编辑器改变Pointer Toggle button对应的按钮，改变VRTK_ControllerEvents脚本参数，用户可以自定义射线发送的按键.

Simple Pointer脚本可以绑定在[CameraRig] prefab下的Controller对象， Controller对象同时也要绑定VRTK_ControllerEvents 脚本，这样才能监听到按钮相关的操作事件. 

也可以把Simple Pointer script绑定在其他对象上(比如[CameraRig]/Camera (head))使得其他物体也能发送射线. controller 参数不能为空，一定要指定是哪个controller.

目前观察，绑定在eye上面的话是看不见射线的，只能看见点和play area 矩形框

###Inspector 可见参数(继承基类的public变量，一起显示)

- Enable Teleport: 是否允许传送.
- Controller: 控制射线开关的controller. 如果脚本本来就绑定在controller上，那么这个参数可以为空，默认使用当前绑定的controller作为发射器，如果脚本绑定在其他对象上，就一定要指定一个controller来控制射线的开关.
- Pointer Material: 指示器渲染的材质. 默认使用 `WorldPointer` 材质.
- Pointer Hit Color: 当光束碰撞到有效目标时的颜色. 可以为不同的Controller设置不同的颜色.
- Pointer Miss Color: 当光束没有碰撞到有效目标，禁止传送等情况时显示的颜色. 可以为不同的Controller设置不同的颜色.
- Show Play Area Cursor: 如果勾选此项那么play area的边界会显示在指示光束的顶端，颜色为当前指示光束的颜色.
- Play Area Cursor Dimensions: play area cursor和collider的大小. 若设值为0那么Play Area Cursor的大小就是标准play area的大小.
- Handle Play Area Cursor Collisions: 若勾选此项，当play area cursor碰撞到其他object那么指示线的颜色会变成`Pointer Miss Color`并且`WorldPointerDestinationSet`事件不会被触发, 避免传送至不合理的区域.
- Ignore Target With Tag Or Class: 如果一个object的tag为此字符串或者object的脚本名为这个字符串，告知play area cursor忽略与它们的碰撞，可以传送.
- Pointer Visibility: 是否显示指示线:
 - On_When_Active 只有当Pointer button按下时才显示.
 - Always On 一直显示，但是只有按下Pointer button才能触发传送.
 - Always Off 不显示，但是按下Pointer button也能触发传送.
- Hold Button To Activate: 在两次按下按钮的间隔内指示线会一直可见.destination set事件在第二次按钮按下前第一束光束释放时才触发传送.
- Activate Delay: 两束射线之间的时间间隔. 避免频繁传送.
- Pointer Thickness: The thickness and length of the beam can also be set on the script as well as the ability to toggle the sphere beam tip that is displayed at the end of the beam (to represent a cursor).
- Pointer Length: 射线发送的长度.
- Show Pointer Tip: 是否显示射线末端的标记，默认是圆形.
- Custom Pointer Cursor: 射线末端默认是显示一个圆形，可以把别的东西拖放进来.
- Layers To Ignore: 发送射线时忽略的LayerMask.

##方法
###Update()方法
update方法首先调用父类VRTK_WorldPointer的

        protected override void Update()
        {
            base.Update();// 调用父类Update()更新射线末端的目标传送区域矩形框
            if (pointer.gameObject.activeSelf)
            {
                Ray pointerRaycast = new Ray(transform.position, transform.forward);// 生成Ray对象，起点为脚本绑定对象的位置，方向为它的朝向
                RaycastHit pointerCollidedWith;// 生成RaycaseHit对象，存储击中点
                var rayHit = Physics.Raycast(pointerRaycast, out pointerCollidedWith, pointerLength, ~layersToIgnore);// layerMask为除了layersToIgnore的其他layerMask
                var pointerBeamLength = GetPointerBeamLength(rayHit, pointerCollidedWith);// 得到击中物体到发射点之间的距离
                SetPointerTransform(pointerBeamLength, pointerThickness);
            }
        }

