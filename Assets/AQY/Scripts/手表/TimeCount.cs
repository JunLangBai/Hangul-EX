using UnityEngine;
using TMPro; // 导入 TextMeshPro
using System.Collections; // 导入 System.Collections 以使用协程
using System; // 导入 System 以使用 DateTime

public class TimeCount : MonoBehaviour
{
    [Header("显示格式")]
    [Tooltip("日期的显示格式")]
    [SerializeField] private string dateFormat = "yyyy-MM-dd";
    [Tooltip("时间的显示格式")]
    [SerializeField] private string timeFormat = "HH:mm";

    [Header("显示组件")]
    [Tooltip("用于显示日期的TextMeshPro组件")]
    [SerializeField] private TextMeshPro dateText;
    [Tooltip("用于显示时间的TextMeshPro组件")]
    [SerializeField] private TextMeshPro timeText;

    // --- 性能优化变量 ---
    
    // 缓存日期字符串，因为日期一天才变一次
    private string cachedDateString = "";
    private int lastCheckedDay = -1;

    void Start()
    {
        // 添加空引用检查，确保组件已在Inspector中分配
        if (dateText == null)
        {
            Debug.LogError("dateText 尚未在Inspector中分配！", this);
            return; // 停止执行以防出错
        }
        if (timeText == null)
        {
            Debug.LogError("timeText 尚未在Inspector中分配！", this);
            return; // 停止执行以防出错
        }

        // 启动协程，让它在后台每秒运行一次
        StartCoroutine(UpdateClockCoroutine());
    }

    // 使用协程，而不是Update()
    // 这样代码只在每秒执行一次，而不是每帧
    private IEnumerator UpdateClockCoroutine()
    {
        // 无限循环
        while (true)
        {
            UpdateClockText();
            
            // 等待1秒钟，然后再继续循环
            // 这极大地降低了CPU开销
            yield return new WaitForSeconds(1.0f);
        }
    }

    /// <summary>
    /// 更新文本的核心方法
    /// </summary>
    private void UpdateClockText()
    {
        DateTime now = DateTime.Now;

        // --- 日期优化 ---
        // 检查是否是新的一天
        if (now.Day != lastCheckedDay)
        {
            // 只有在天数变化时，才重新生成日期字符串
            cachedDateString = now.ToString(dateFormat);
            lastCheckedDay = now.Day;
            
            // 立即更新日期文本
            // 这个SetText调用每天只会执行一次
            dateText.SetText(cachedDateString);
        }

        // --- 时间字符串 ---
        // 时间每秒都在变，所以这里不可避免地需要每秒生成一次
        string timeString = now.ToString(timeFormat); // 每秒1次分配

        // --- 更新TextMeshPro ---
        // 每秒更新时间文本
        timeText.SetText(timeString);
    }
}