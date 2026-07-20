using UnityEngine;
using UnityEngine.SceneManagement; // 用于获取当前场景名
using System.Collections.Generic; // 用于使用Dictionary
// 假设你有一个UI Text来显示最佳记录
using UnityEngine.UI; 

public class SettlementScreen : MonoBehaviour
{

    // 定义一个常量作为存档文件名，方便管理
    private const string AccuracyDataFileName = "level_accuracies";

    /// <summary>
    /// 在关卡结束时调用此方法来保存正确率
    /// 注意：这里可以增加一个逻辑，只保存比历史记录更高的分数
    /// </summary>
    /// <param name="currentAccuracy">本次关卡的正确率 (例如: 95.5f 代表 95.5%)</param>
    public void SaveLevelAccuracy(float currentAccuracy)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        var accuracies = JsonNetDataService.LoadData<Dictionary<string, float>>(AccuracyDataFileName) ?? new Dictionary<string, float>();

        // (可选逻辑) 检查是否需要更新记录：只有当新分数更高时才保存
        if (accuracies.TryGetValue(currentSceneName, out float savedAccuracy))
        {
            if (currentAccuracy <= savedAccuracy)
            {
                Debug.Log($"新分数 {currentAccuracy}% 不高于历史记录 {savedAccuracy}%，无需更新。");
                return; // 分数没有更高，直接返回
            }
        }

        // 添加或更新当前关卡的正确率
        accuracies[currentSceneName] = currentAccuracy;
        
        // 将更新后的整个字典保存回文件
        JsonNetDataService.SaveData(accuracies, AccuracyDataFileName);

        Debug.Log($"已更新场景 '{currentSceneName}' 的最高正确率: {currentAccuracy}%");
    }

    /// <summary>
    /// 获取当前场景已经保存过的正确率
    /// </summary>
    /// <returns>返回一个可空的float。如果找到记录，返回正确率；否则返回null。</returns>
    public float? GetSavedAccuracyForCurrentScene()
    {
        // 1. 加载所有存档数据
        var accuracies = JsonNetDataService.LoadData<Dictionary<string, float>>(AccuracyDataFileName);

        // 2. 如果存档文件不存在或为空，直接返回null
        if (accuracies == null)
        {
            return null;
        }

        // 3. 获取当前场景名
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 4. 尝试从字典中获取当前场景的记录
        if (accuracies.TryGetValue(currentSceneName, out float savedAccuracy))
        {
            // 如果找到了，返回对应的正确率
            return savedAccuracy;
        }
        else
        {
            // 如果字典里没有这个场景的key，说明这个场景还没存过，返回null
            return null;
        }
    }

    /// <summary>
    /// 读取并显示当前关卡的历史最佳正确率
    /// </summary>
   

    /// <summary>
    /// (用于调试) 打印所有已保存的正确率记录
    /// </summary>
    public void DisplayAllAccuracies()
    {
        var accuracies = JsonNetDataService.LoadData<Dictionary<string, float>>(AccuracyDataFileName);
        
        if (accuracies != null && accuracies.Count > 0)
        {
            Debug.Log("--- 所有已保存的关卡正确率 ---");
            foreach (var entry in accuracies)
            {
                Debug.Log($"场景: {entry.Key}, 正确率: {entry.Value}%");
            }
        }
        else
        {
            Debug.Log("未找到任何正确率存档。");
        }
    }
}