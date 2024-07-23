using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("互动相关")]
    public Button talkBtn;  //对话按钮
    public Button holoBtn;  //光环按钮 关闭背景音乐
    public Button touchBtn; //摸头按钮
    public Button lookBtn;  //

    SkeletonGraphic animator; //动画控制器

    public Spine.AnimationState animationState; //当前动画

    [SerializeField]
    private int currentIndex;   //当前第几个语音
    public bool enable; //是否允许说话
    public bool touching;   //是否正在摸头
    public bool looking;    //是否正在看

    [Header("音乐控制")]
    public AudioSource bgmSource;

    //三条轨道
    private Spine.TrackEntry trackEntry0;   //用来播放待机动画
    private Spine.TrackEntry trackEntry1;   //用来播放A结尾的动画
    private Spine.TrackEntry trackEntry2;   //用来播放M结尾的动画
    //委托
    private Spine.AnimationState.TrackEntryDelegate trackEntryDelegate0;    //第0轨道动画结束时做什么
    private Spine.AnimationState.TrackEntryDelegate trackEntryDelegate1;    //第一轨道动画结束时做什么
    private Spine.AnimationState.TrackEntryDelegate trackEntryDelegateTouch;    //摸头 动画结束时做什么
    private Spine.AnimationState.TrackEntryDelegate trackEntryDelegateEye;  //看 动画结束时做什么


    float scaleCoef;    //尺度系数、分辨率适配使用的

    //摸头相关
    public GameObject touchRoot;    
    private Vector3 touchPos;    //角色头部初始位置
    //看向鼠标 相关
    public GameObject eyeRoot;
    private Vector3 eyePos;  //角色目光初始位置

    private Vector3 mousePos;

    void Start()
    {
        //分辨率适配
        scaleCoef = Screen.width / 1920==0?1: Screen.width / 1920;
        Debug.Log(Screen.width);
        transform.parent.localScale = new Vector3(0.6f * scaleCoef, 0.6f * scaleCoef);

        //组件监听事件
        talkBtn.onClick.AddListener(Talk);
        holoBtn.onClick.AddListener(() => { if (bgmSource.isPlaying) bgmSource.Stop(); else bgmSource.Play(); });
        

        currentIndex = 0;
        enable = touching = looking = false;

        //获取、初始化组件
        animator = GetComponent<SkeletonGraphic>();
        animationState = animator.AnimationState;
        
        trackEntry0 = animationState.SetAnimation(0, "Start_Idle_01", false);
        trackEntryDelegate0 = SetToIdle0;
        trackEntry0.Complete += trackEntryDelegate0;

        trackEntryDelegateTouch+= TouchPost;
        trackEntryDelegateEye += LookPost;

        talkBtn.gameObject.SetActive(false);
        holoBtn.gameObject.SetActive(false);
        touchBtn.gameObject.SetActive(false);
        lookBtn.gameObject.SetActive(false);

        touchPos = touchRoot.transform.localPosition;
        eyePos = eyeRoot.transform.localPosition;
    }

    void Update()
    {
        if (touching)
        {
            mousePos = Input.mousePosition;
            Vector3 temp = mousePos - touchPos;
            if (temp.magnitude > 400)
                temp = temp.normalized * 400f;
            touchRoot.transform.localPosition = new Vector3(-temp.y / 30f, -temp.x / 5f, 0) * scaleCoef + touchPos;

        }
        if (looking)
        {
            mousePos = Input.mousePosition;
            Vector3 temp = mousePos - eyePos;
            if (temp.magnitude > 600f) temp = temp.normalized * 600f;
            eyeRoot.transform.localPosition = eyePos + new Vector3(temp.y, -temp.x, 0) * scaleCoef / 4f;

        }
    }
    /// <summary>
    /// 对话
    /// </summary>
    private void Talk()
    {
        //只有当上一个对话结束后，才可以进行新的对话
        if (enable)
        {
            enable = false;

            currentIndex = currentIndex % 5 + 1;
            trackEntry1 = animationState.SetAnimation(1, "Talk_0" + currentIndex + "_A", false);
            trackEntry2 = animationState.SetAnimation(2, "Talk_0" + currentIndex + "_M", false);

            trackEntryDelegate1 = SetToIdle1;
            trackEntry1.Complete += trackEntryDelegate1;
        }
    }

    /// <summary>
    /// 状态变化回调函数 
    /// 将状态从StartIdle变为Idle
    /// </summary>
    /// <param name="entry"></param>
    private void SetToIdle0(Spine.TrackEntry entry)
    {
        trackEntry0 = animationState.SetAnimation(0, "Idle_01",  true);
        enable = true;
        talkBtn.gameObject.SetActive(true);
        holoBtn.gameObject.SetActive(true);
        touchBtn.gameObject.SetActive(true);
        lookBtn.gameObject.SetActive(true);
    }

    /// <summary>
    /// 从对话状态转换为Idle
    /// </summary>
    /// <param name="entry"></param>
    private void SetToIdle1(Spine.TrackEntry entry)
    {
        enable = true;
    }

    #region 摸头

    /// <summary>
    /// 鼠标按下，开始摸头
    /// </summary>
    public void TouchBeginFunc()
    {
        //对话结束后才能摸头
        if(enable ==true)
        {
            enable = false; //摸头的时候不能对话
            trackEntry1 = animationState.SetAnimation(1, "Pat_01_A", true);
            trackEntry2 = animationState.SetAnimation(2, "Pat_01_M", true);

            trackEntry1.End += trackEntryDelegateTouch;
            touching  = true;
        }
    }

    /// <summary>
    /// 鼠标抬起 摸头结束
    /// </summary>
    public void TouchEndFunc()
    {
        if(touching ==true)
        {
            enable = true;
            touching = false;

            trackEntry1 = animationState.SetAnimation(1, "Pat_01_A", false);
            trackEntry2 = animationState.SetAnimation(2, "Pat_01_M", false);
            trackEntry1.Loop = false;
            trackEntry2.Loop = false;

        }
    }

    /// <summary>
    /// 摸头结束 清空两条轨道 播放结束动画
    /// </summary>
    /// <param name="entry"></param>
    private void TouchPost(Spine.TrackEntry entry)
    {
        trackEntry1 = animationState.SetAnimation(1, "PatEnd_01_A", false);
        trackEntry2 = animationState.SetAnimation(2, "PatEnd_01_M", false);

        touchRoot.transform.localPosition = touchPos;    //恢复位置
    }

    #endregion

    #region 人物看鼠标
    public void LookBeginFunc()
    {
        if(enable ==true)
        {
            enable=false;
            trackEntry1 = animationState.SetAnimation(1,"Look_01_M",true);
            trackEntry2 = animationState.SetAnimation(2,"Look_01_M",true) ;
            trackEntry1.End += trackEntryDelegateEye;
            looking = true;
        }
    }

    public void LookEndFunc()
    {
        if(looking == true)
        {
            enable = true ;
            looking = false;
            trackEntry1 = animationState.SetAnimation(1, "Look_01_M", false);
            trackEntry1.Loop = false;
            
        }
    }

    private void LookPost(Spine.TrackEntry entry)
    {
        trackEntry1 = animationState.SetAnimation(1, "LookEnd_01_A", false);
        trackEntry2 = animationState.SetAnimation(2, "LookEnd_01_M", false);

        eyeRoot.transform.localPosition = eyePos;    //恢复位置
    }
    #endregion
}
