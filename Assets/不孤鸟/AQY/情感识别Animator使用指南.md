# 情感识别场景 - 3D模型Animator使用指南

## 📋 当前实现分析

### 现有代码（EmotionTestController.cs）

```csharp
[Header("模型与动画")]
public GameObject characterModel; // 3D模型对象
public Animator modelAnimator;    // Animator组件

// 情绪列表（对应动画状态名称）
private List<string> emotions = new List<string> { 
    "smiling",  // 开心
    "sad",      // 伤心
    "angry",    // 生气
    "fear"      // 害怕
};

// 播放情绪动画
IEnumerator ShowEmotion()
{
    // 1. 随机选择一个情绪
    currentEmotion = emotions[Random.Range(0, emotions.Count)];

    // 2. 激活模型
    characterModel.SetActive(true);
    
    // 3. 播放对应的动画状态
    modelAnimator.Play(currentEmotion);

    // 4. 等待显示时间
    yield return new WaitForSeconds(displayTime);

    // 5. 触发回到待机状态
    modelAnimator.SetTrigger("DoIdle");
    
    yield return new WaitForSeconds(0.5f);
}
```

---

## 🎮 Animator使用方法详解

### 方法1：使用 `Animator.Play()` 直接播放状态

```csharp
// 直接播放指定名称的动画状态
modelAnimator.Play("smiling");

// 完整参数版本
modelAnimator.Play(
    "smiling",  // 状态名称
    0,          // Layer层级（0是默认层）
    0f          // 归一化时间（0是开始，1是结束）
);
```

**优点：**
- 简单直接，立即切换到目标状态
- 适合需要强制切换动画的场景

**缺点：**
- 忽略过渡（Transition），可能导致动画切换不流畅

---

### 方法2：使用 `Animator.SetTrigger()` 触发过渡

```csharp
// 触发一个Trigger参数，让Animator根据状态机自动过渡
modelAnimator.SetTrigger("DoIdle");
```

**优点：**
- 遵循Animator Controller中设置的过渡规则
- 动画切换更流畅，有混合效果

**缺点：**
- 需要在Animator Controller中预先设置好Trigger参数和过渡

---

### 方法3：使用 `Animator.SetBool/SetInt/SetFloat()` 控制参数

```csharp
// 设置布尔参数
modelAnimator.SetBool("IsHappy", true);

// 设置整数参数（可用于切换不同情绪）
modelAnimator.SetInteger("EmotionIndex", 1);

// 设置浮点参数（可用于混合动画）
modelAnimator.SetFloat("EmotionIntensity", 0.8f);
```

**优点：**
- 更灵活，可以根据参数值自动选择状态
- 适合复杂的状态机逻辑

---

## 🛠️ 在Unity中配置Animator Controller

### 步骤1：创建Animator Controller

1. 在Project窗口右键 → Create → Animator Controller
2. 命名为 `EmotionAnimatorController`
3. 双击打开Animator窗口

### 步骤2：添加动画状态

在Animator窗口中：

1. **创建状态**：
   - 右键空白处 → Create State → Empty
   - 命名为对应的情绪名称：`smiling`, `sad`, `angry`, `fear`

2. **分配动画片段**：
   - 选中状态
   - 在Inspector中的Motion字段拖入对应的动画片段（Animation Clip）

3. **创建Idle状态**：
   - 创建一个 `Idle` 状态作为默认待机动画
   - 设置为橙色（默认状态）

### 步骤3：设置过渡（Transitions）

#### 方案A：使用Trigger（推荐）

```
创建Trigger参数：
- DoSmiling
- DoSad
- DoAngry
- DoFear
- DoIdle

设置过渡：
Idle → smiling  (条件: DoSmiling)
Idle → sad      (条件: DoSad)
Idle → angry    (条件: DoAngry)
Idle → fear     (条件: DoFear)

smiling → Idle  (条件: DoIdle)
sad → Idle      (条件: DoIdle)
angry → Idle    (条件: DoIdle)
fear → Idle     (条件: DoIdle)
```

**对应代码：**
```csharp
// 播放情绪动画
modelAnimator.SetTrigger("DoSmiling");

// 回到待机
modelAnimator.SetTrigger("DoIdle");
```

#### 方案B：使用Integer参数

```
创建Integer参数：
- EmotionState (0=Idle, 1=Smiling, 2=Sad, 3=Angry, 4=Fear)

设置过渡：
Any State → smiling  (条件: EmotionState == 1)
Any State → sad      (条件: EmotionState == 2)
Any State → angry    (条件: EmotionState == 3)
Any State → fear     (条件: EmotionState == 4)
Any State → Idle     (条件: EmotionState == 0)
```

**对应代码：**
```csharp
// 播放情绪动画
modelAnimator.SetInteger("EmotionState", 1); // Smiling

// 回到待机
modelAnimator.SetInteger("EmotionState", 0); // Idle
```

---

## 📝 推荐的代码实现

### 改进版EmotionTestController（使用Trigger）

```csharp
// 在类中定义情绪到Trigger的映射
private Dictionary<string, string> emotionTriggers = new Dictionary<string, string>
{
    { "smiling", "DoSmiling" },
    { "sad", "DoSad" },
    { "angry", "DoAngry" },
    { "fear", "DoFear" }
};

IEnumerator ShowEmotion()
{
    // 随机选择情绪
    currentEmotion = emotions[Random.Range(0, emotions.Count)];

    // 激活模型
    characterModel.SetActive(true);
    
    // 使用Trigger触发动画（更流畅）
    if (emotionTriggers.ContainsKey(currentEmotion))
    {
        modelAnimator.SetTrigger(emotionTriggers[currentEmotion]);
    }

    // 等待显示时间
    yield return new WaitForSeconds(displayTime);

    // 回到待机状态
    modelAnimator.SetTrigger("DoIdle");
    
    yield return new WaitForSeconds(0.5f);
}
```

### 改进版EmotionTestController（使用Integer）

```csharp
// 定义情绪枚举
private enum EmotionState
{
    Idle = 0,
    Smiling = 1,
    Sad = 2,
    Angry = 3,
    Fear = 4
}

IEnumerator ShowEmotion()
{
    // 随机选择情绪（1-4）
    int randomEmotion = Random.Range(1, 5);
    
    // 激活模型
    characterModel.SetActive(true);
    
    // 设置情绪状态
    modelAnimator.SetInteger("EmotionState", randomEmotion);

    // 等待显示时间
    yield return new WaitForSeconds(displayTime);

    // 回到待机状态
    modelAnimator.SetInteger("EmotionState", 0);
    
    yield return new WaitForSeconds(0.5f);
}
```

---

## 🎯 常见问题与解决方案

### 问题1：动画切换不流畅

**原因：** 使用了 `Play()` 方法，跳过了过渡

**解决：** 使用 `SetTrigger()` 或参数控制，并在Animator中设置合适的过渡时间

```csharp
// 在Animator窗口中：
// 选中Transition → Inspector → Settings
// 调整 Transition Duration（建议0.1-0.3秒）
```

### 问题2：动画没有播放

**检查清单：**
1. ✅ Animator组件是否已添加到GameObject
2. ✅ Animator Controller是否已分配
3. ✅ 动画状态名称是否正确（区分大小写）
4. ✅ 动画片段是否已分配到状态
5. ✅ 过渡条件是否正确设置

### 问题3：动画播放后卡住

**原因：** 动画状态没有设置循环，或者没有过渡回Idle

**解决：**
```csharp
// 方案1：设置动画片段为循环
// 选中Animation Clip → Inspector → Loop Time ✓

// 方案2：确保有回到Idle的过渡
modelAnimator.SetTrigger("DoIdle");
```

### 问题4：如何检测动画播放完成

```csharp
// 方法1：使用协程等待
IEnumerator WaitForAnimation(string stateName)
{
    // 等待进入目标状态
    while (!modelAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
    {
        yield return null;
    }
    
    // 等待动画播放完成
    while (modelAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
    {
        yield return null;
    }
    
    Debug.Log("动画播放完成！");
}

// 使用
StartCoroutine(WaitForAnimation("smiling"));
```

---

## 🔧 调试技巧

### 1. 实时查看当前状态

```csharp
void Update()
{
    // 获取当前动画状态信息
    AnimatorStateInfo stateInfo = modelAnimator.GetCurrentAnimatorStateInfo(0);
    
    // 打印状态名称
    Debug.Log($"当前状态: {stateInfo.shortNameHash}");
    
    // 打印播放进度（0-1）
    Debug.Log($"播放进度: {stateInfo.normalizedTime}");
}
```

### 2. 检查参数值

```csharp
// 检查Trigger是否被触发
bool isTriggerSet = modelAnimator.GetBool("DoSmiling");

// 检查Integer参数
int emotionState = modelAnimator.GetInteger("EmotionState");

Debug.Log($"EmotionState = {emotionState}");
```

### 3. 在Animator窗口中测试

1. 进入Play模式
2. 打开Animator窗口
3. 观察状态切换和参数变化
4. 可以手动调整参数值测试

---

## 📚 总结

### 当前代码使用的方法：
- `modelAnimator.Play(currentEmotion)` - 直接播放
- `modelAnimator.SetTrigger("DoIdle")` - 触发回到待机

### 推荐改进：
1. 统一使用Trigger或Integer参数控制
2. 在Animator Controller中设置好所有过渡
3. 添加动画播放完成的检测
4. 考虑添加音频反馈（情绪变化时播放音效）

### 下一步：
1. 检查3D模型的动画片段是否正确导入
2. 创建或检查Animator Controller的配置
3. 测试所有情绪动画的切换效果
4. 根据需要调整过渡时间和混合效果
