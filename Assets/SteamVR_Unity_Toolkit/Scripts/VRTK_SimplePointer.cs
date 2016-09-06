//====================================================================================
//
// Purpose: Provide basic laser pointer to VR Controller
//
// This script must be attached to a Controller within the [CameraRig] Prefab
//
// The VRTK_ControllerEvents script must also be attached to the Controller
//
// Press the default 'Grip' button on the controller to activate the beam
// Released the default 'Grip' button on the controller to deactivate the beam
//
// This script is an implementation of the VRTK_WorldPointer.
//
//====================================================================================
namespace VRTK
{
    using UnityEngine;

    public class VRTK_SimplePointer : VRTK_WorldPointer
    {
        public float pointerThickness = 0.002f;
        public float pointerLength = 100f;
        public bool showPointerTip = true;// 是否显示射线末端的Tip(默认是一个和射线同色的实心圆)
        public GameObject customPointerCursor;
        public LayerMask layersToIgnore = Physics.IgnoreRaycastLayer;

        private GameObject pointerHolder;// 发送射线的gameobject，如手柄和头显
        private GameObject pointer;// 射线
        private GameObject pointerTip;// 射线末端的标记
        private Vector3 pointerTipScale = new Vector3(0.05f, 0.05f, 0.05f);

        // material of customPointerCursor (if defined)
        private Material customPointerMaterial;

        protected override void OnEnable()
        {
            base.OnEnable();// 调用父类OnEnable()，监听事件，处理射线材质
            InitPointer();// 初始化射线
        }

        protected override void OnDisable()
        {
            base.OnDisable();// 调用父类OnDisable,解除事件绑定，清楚目标点信息，清楚play area矩形框
            if (pointerHolder != null)
            {
                Destroy(pointerHolder);// 销毁射线
            }
        }

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

        /// <summary>
        /// 初始化射线
        /// </summary>
        protected override void InitPointer()
        {
            pointerHolder = new GameObject(string.Format("[{0}]WorldPointer_SimplePointer_Holder", gameObject.name));
            Utilities.SetPlayerObject(pointerHolder, VRTK_PlayerObject.ObjectTypes.Pointer);
            pointerHolder.transform.parent = transform;// pointerHolder挂在发送射线的gameobject的下面
            pointerHolder.transform.localPosition = Vector3.zero;

            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);// pointer的类型是Cube
            pointer.transform.name = string.Format("[{0}]WorldPointer_SimplePointer_Pointer", gameObject.name);
            Utilities.SetPlayerObject(pointer, VRTK_PlayerObject.ObjectTypes.Pointer);
            pointer.transform.parent = pointerHolder.transform;// pointer挂在pointerHolder下面

            pointer.GetComponent<BoxCollider>().isTrigger = true;
            pointer.AddComponent<Rigidbody>().isKinematic = true;
            pointer.layer = LayerMask.NameToLayer("Ignore Raycast");

            if (customPointerCursor == null)
            {
                // 如果没有特别指定PointerTip是某种模型，就新建一个球体
                pointerTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);// 默认的末端标记是球
                pointerTip.transform.localScale = pointerTipScale;// 缩放
            }
            else
            {
                Renderer renderer = customPointerCursor.GetComponentInChildren<MeshRenderer>();
                if (renderer)
                {
                    customPointerMaterial = Material.Instantiate(renderer.sharedMaterial);
                }
                pointerTip = Instantiate(customPointerCursor);
                foreach (Renderer mr in pointerTip.GetComponentsInChildren<Renderer>())
                {
                    mr.material = customPointerMaterial;
                }
            }

            pointerTip.transform.name = string.Format("[{0}]WorldPointer_SimplePointer_PointerTip", gameObject.name);
            Utilities.SetPlayerObject(pointerTip, VRTK_PlayerObject.ObjectTypes.Pointer);
            pointerTip.transform.parent = pointerHolder.transform;// pointerTip挂在pointerHolder下面

            pointerTip.GetComponent<Collider>().isTrigger = true;
            pointerTip.AddComponent<Rigidbody>().isKinematic = true;
            pointerTip.layer = LayerMask.NameToLayer("Ignore Raycast");

            base.InitPointer();// 调用父类初始化传送区域

            SetPointerTransform(pointerLength, pointerThickness);// 设置初始射线的位置
            TogglePointer(false);
        }

        /// <summary>
        /// 重写SetPointerMaterial()
        /// </summary>
        protected override void SetPointerMaterial()
        {
            base.SetPointerMaterial();
            pointer.GetComponent<Renderer>().material = pointerMaterial;
            if (customPointerMaterial != null)
            {
                customPointerMaterial.color = pointerMaterial.color;
            }
            else
            {
                pointerTip.GetComponent<Renderer>().material = pointerMaterial;
            }
        }

        
        /// <summary>
        /// 重写父类的TogglePointer，若不声明是base.TogglePointer那么调用的是TogglePointer
        /// </summary>
        /// <param name="state"></param>
        protected override void TogglePointer(bool state)
        {
            // 如果射线的可见状态是Always_On，那么无论传入的state是什么，都为true；不然就根据state决定
            state = (pointerVisibility == pointerVisibilityStates.Always_On ? true : state);
            base.TogglePointer(state);
            pointer.gameObject.SetActive(state);// 设置为true则cube显示，否则不显示

            var tipState = (showPointerTip ? state : false);// 如果showPointerTip未勾选，那么无论state如何，都不显示PointerTip
            pointerTip.gameObject.SetActive(tipState);

            if (pointer.GetComponent<Renderer>() && pointerVisibility == pointerVisibilityStates.Always_Off)
            {
                pointer.GetComponent<Renderer>().enabled = false;
            }
        }

        
        /// <summary>
        /// 设置射线的位置，position是playerHolder坐标系的z轴方向，中点位置，playHolder的旋转和它的父对象(controller,headset)等保持一致
        /// </summary>
        /// <param name="setLength"></param>
        /// <param name="setThicknes"></param>
        private void SetPointerTransform(float setLength, float setThicknes)
        {
            // if the additional decimal isn't added then the beam position glitches
            // 这个0.00001f一定要加，不然会有问题
            var beamPosition = setLength / (2 + 0.00001f);

            pointer.transform.localScale = new Vector3(setThicknes, setThicknes, setLength);
            pointer.transform.localPosition = new Vector3(0f, 0f, beamPosition);
            pointerTip.transform.localPosition = new Vector3(0f, 0f, setLength - (pointerTip.transform.localScale.z / 2));
            pointerHolder.transform.localRotation = Quaternion.identity;
            base.SetPlayAreaCursorTransform(pointerTip.transform.position);// 根据末端tip的位置更新play area传送区域的位置
        }

        /// <summary>
        /// 返回射线起点到击中点的距离，最大100f
        /// </summary>
        /// <param name="hasRayHit">bool - 是否击中物体</param>
        /// <param name="collidedWith">RaycastHit - 击中信息</param>
        /// <returns></returns>
        private float GetPointerBeamLength(bool hasRayHit, RaycastHit collidedWith)
        {
            var actualLength = pointerLength;// 射线的长度，默认是100.0f

            // reset if beam not hitting or hitting new target
            // 当射线没有击中或者击中新的目标，首先把原来的击中信息清除重置
            if (!hasRayHit || (pointerContactTarget && pointerContactTarget != collidedWith.transform))
            {
                if (pointerContactTarget != null)
                {
                    // 调用父类方法，发送射线离开物体事件和停止使用物体
                    base.PointerOut();
                }

                // 清空pointerContactTarget相关的信息，更新射线材质为miss
                pointerContactDistance = 0f;
                pointerContactTarget = null;
                destinationPosition = Vector3.zero;

                UpdatePointerMaterial(pointerMissColor);
            }

            // check if beam has hit a new target
            // 处理新击中物体的信息
            if (hasRayHit)
            {
                pointerContactDistance = collidedWith.distance;// 从射线起点到击中点的距离
                pointerContactTarget = collidedWith.transform;// 击中物体的Transform
                destinationPosition = pointerTip.transform.position;// 射线末端实心圆标记的位置

                UpdatePointerMaterial(pointerHitColor);// 更新射线材质为击中的颜色

                base.PointerIn();// 调用父类方法，发送射线进入事件和开始物体使用
            }

            // adjust beam length if something is blocking it
            // 返回的长度不会超过射线最大值100.0f
            if (hasRayHit && pointerContactDistance < pointerLength)
            {
                actualLength = pointerContactDistance;
            }

            return actualLength;
        }
    }
}