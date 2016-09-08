//====================================================================================
//
// 这个类继承自VRTK_InteractableObject，和一般的交互物体一样，可以用手柄触碰，抓取和使用
// 在VRTK_InteractableObject中，只是为使用逻辑设置了一些相关的参数，并没有具体到场景中来实现功能
// 这个方法重写了父类的StartUsing方法，设置了自己的一些旋转参数，并在Update中每帧根据参数的值，来实现door对象的旋转
// 场景中的door对象，box collider比显示的门要大一点，这样手柄只要在门的附近，就可以使用它
//
//====================================================================================

using UnityEngine;
using VRTK;

public class Openable_Door : VRTK_InteractableObject
{
    public bool flipped = false;
    public bool rotated = false;

    // 用sideFilip与side相乘来判断开门的方向，得到1则拉开门，得到-1则推开门
    private float sideFlip = -1;// 翻转标志
    private float side = -1;// 方位标志
    private float smooth = 270.0f;// 平滑旋转门，每秒270度
    private float doorOpenAngle = -90f;// 开门的角度是围绕door的y轴旋转-90度
    private bool open = false;// 如果当前手柄的使用目的是开门，为true，否则为false

    private Vector3 defaultRotation;// 门初始(关门)的世界旋转角度
    private Vector3 openRotation;// 门的旋转角度，三维形式

    /// <summary>
    /// 重写StartUsing方法
    /// </summary>
    /// <param name="usingObject"></param>
    public override void StartUsing(GameObject usingObject)
    {
        base.StartUsing(usingObject);
        // 设置开门方向
        SetDoorRotation(usingObject.transform.position);
        // 设置开门旋转角度的四元数
        SetRotation();

        // 开始使用，这次使用行为的open和上次使用的必然相反，上次是开，那么这次就是关
        open = !open;
    }

    /// <summary>
    /// 重写Start()方法
    /// </summary>
    protected override void Start () {
        base.Start();
        defaultRotation = transform.eulerAngles;
        SetRotation();
        sideFlip = (flipped ? 1 : -1);
    }

    /// <summary>
    /// 重写Update()方法
    /// 根据open标志的值，判断要开门还是关门
    /// 如果open为true，代表要开门，那么平滑旋转门从当前角度到开门角度
    /// 如果open为false，代码要关门，那么平缓旋转门从当前角度到关门角度
    /// </summary>
    protected override void Update()
    {
        if (open)
        {
            Debug.Log("open");
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(openRotation), Time.deltaTime * smooth);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(defaultRotation), Time.deltaTime * smooth);
        }
    }

    /// <summary>
    /// 设置门的旋转，只围绕y轴旋转
    /// 角度是设置好的，主要是判断旋转的方向，根据sideFlip*side的结果来判断是拉门还是推门
    /// sideFlidp和side的值只会从1和-1中取，所以只要两者值正负一致，门都会围绕自己的y轴旋转doorOpenAngle，两者不一致时就会旋转-doorOpenAngle
    /// </summary>
    private void SetRotation()
    {
        openRotation = new Vector3(defaultRotation.x, defaultRotation.y + (doorOpenAngle * (sideFlip * side)), defaultRotation.z);
    }

    /// <summary>
    /// 根据手柄的位置和rotated来设置side值
    /// </summary>
    /// <param name="interacterPosition">手柄的位置</param>
    private void SetDoorRotation(Vector3 interacterPosition)
    {
        side = ((rotated == false && interacterPosition.z > transform.position.z) || (rotated == true && interacterPosition.x > transform.position.x) ? -1 : 1);
    }
}