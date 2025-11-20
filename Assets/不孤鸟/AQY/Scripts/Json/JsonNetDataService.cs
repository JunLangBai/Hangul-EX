using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;

/// <summary>
/// 使用 Newtonsoft.Json 进行数据持久化存储的静态服务类
/// </summary>
public static class JsonNetDataService
{
    /// <summary>
    /// 根据运行环境获取存档的基础目录路径
    /// </summary>
    /// <returns>返回存档目录的完整路径</returns>
    private static string GetSaveDirectoryPath()
    {
#if UNITY_EDITOR
        // 在Unity编辑器中，我们希望路径是 "Assets/SaveData"
        // Application.dataPath 返回的是 Assets 目录的完整路径
        return Path.Combine(Application.dataPath, "SaveData");
#else
        // 在构建后的游戏中，使用标准的持久化数据路径
        return Application.persistentDataPath;
#endif
    }
    
    /// <summary>
    /// 将数据保存到文件中（使用Newtonsoft.Json）
    /// </summary>
    /// <typeparam name="T">要保存的数据类型</typeparam>
    /// <param name="data">要保存的数据实例</param>
    /// <param name="fileName">文件名（无需扩展名）</param>
    public static void SaveData<T>(T data, string fileName)
    {
        // 1. 获取特定于环境的基础目录
        string directoryPath = GetSaveDirectoryPath();
        
        // 2. 组合成完整的文件路径
        string filePath = Path.Combine(directoryPath, fileName + ".json");

        try
        {
            // 3. 在写入前，确保目录存在。如果不存在，则创建它。
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 4. 将对象序列化为JSON字符串并写入文件
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            Debug.Log($"成功将数据保存到: {filePath}");

#if UNITY_EDITOR
            // 5. (编辑器专用) 刷新AssetDatabase，以便新文件能立即显示在Project窗口中
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"保存数据时发生错误: {e.Message}");
        }
    }

    /// <summary>
    /// 从文件中读取数据（使用Newtonsoft.Json）
    /// </summary>
    /// <typeparam name="T">要读取的数据类型</typeparam>
    /// <param name="fileName">文件名（无需扩展名）</param>
    /// <returns>读取到的数据实例；如果文件不存在或读取失败，则返回该类型的默认值</returns>
    public static T LoadData<T>(string fileName)
    {
        // 1. 获取特定于环境的基础目录
        string directoryPath = GetSaveDirectoryPath();
        
        // 2. 组合成完整的文件路径
        string filePath = Path.Combine(directoryPath, fileName + ".json");

        if (!File.Exists(filePath))
        {
            return default(T);
        }

        try
        {
            // 3. 从文件读取JSON字符串并反序列化
            string json = File.ReadAllText(filePath);
            T data = JsonConvert.DeserializeObject<T>(json);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取数据时发生错误: {e.Message}");
            return default(T);
        }
    }
}