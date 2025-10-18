using UnityEngine.UI;
using UnityEngine;
namespace Rokid.UXR.Interaction
{
    public class WatchGes : MonoBehaviour
    {
        [SerializeField]
        private HandType hand;
        [SerializeField]
        private Text logText;

        // === 修改部分 ===
        [SerializeField]
        private GameObject[] watchModels; // 在Inspector中拖入你的模型
        private int modelIndex = 0;
        // === 修改结束 ===

        private Vector3 oriScale;
        // private Material watchMat; // 不再需要
        private bool active;
        private bool stateChange;

        void Awake()
        {
            RKHandWatch.OnActiveWatch += OnActiveWatch;
            RKHandWatch.OnWatchPoseUpdate += OnWatchPoseUpdate;
        }

        private void Start()
        {
            this.gameObject.SetActive(false); // 保持原样，开始时隐藏
            // watchMat = GetComponent<MeshRenderer>()?.material; // 不再需要
            oriScale = transform.localScale;
            
        }

        private void OnDestroy()
        {
            RKHandWatch.OnActiveWatch -= OnActiveWatch;
            RKHandWatch.OnWatchPoseUpdate -= OnWatchPoseUpdate;
        }
        // 添加 OnEnable() 方法
        // OnEnable 在 gameObject.SetActive(true) 时被调用
        private void OnEnable()
        {
            // 每次激活时（即手势让它出现时），都重置到第一个模型
            modelIndex = 0;
            SwitchModel(modelIndex);
        }
        // === 关键修改结束 ===

        private void OnWatchPoseUpdate(HandType hand, Pose pose)
        {
            if (hand == this.hand)
                transform.SetPose(pose); // 移动父物体
        }

        private void OnActiveWatch(HandType hand, bool active)
        {
            if (hand == this.hand)
            {
                if (this.active != active && active == true)
                {
                    stateChange = true;
                }
                this.active = active;
                // 这句代码会触发 OnEnable() (如果 active=true)
                // 或触发 OnDisable() (如果 active=false)
                this.gameObject.SetActive(active);
            }
        }

        private Pose GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
        {
            return GesEventInput.Instance.GetSkeletonPose(index, hand);
        }

        // === 新增的辅助方法 ===
        /// <summary>
        /// 激活指定索引的模型，并禁用其他所有模型
        /// </summary>
        private void SwitchModel(int index)
        {
            if (watchModels == null || watchModels.Length == 0) return;

            // 确保索引在有效范围内
            modelIndex = index % watchModels.Length;

            for (int i = 0; i < watchModels.Length; i++)
            {
                if (watchModels[i] != null)
                {
                    // 只激活索引匹配的模型
                    watchModels[i].SetActive(i == modelIndex);
                }
            }
        }

        private void Update()
        {
            if (active == true && stateChange == true)
            {
                stateChange = false;
                return;
            }

            // 当物体处于激活状态时，才检测切换手势
            if (active && GesEventInput.Instance.GetHandDown(hand, false))
            {
                if (watchModels == null || watchModels.Length == 0)
                {
                    return;
                }

                modelIndex++; // 切换到下一个模型
                
                // 处理切换逻辑
                SwitchModel(modelIndex); // 调用切换方法
            }
        }
    }
}