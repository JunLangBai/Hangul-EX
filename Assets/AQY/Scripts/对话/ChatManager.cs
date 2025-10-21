using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
   public GameObject rightBubble;
   public GameObject leftBubble;
   
   public RectTransform contentParent;
   
   public ScrollRect scrollRect;

   public void AddUserChat(string text)
   {
      GameObject newBubbleObj = Instantiate(leftBubble, contentParent);
      
      TextMeshProUGUI textComponent = newBubbleObj.GetComponentInChildren<TextMeshProUGUI>();
      
      if (textComponent != null)
      {
         textComponent.text = text;
      }

      // 2. 获取气泡上的动画脚本
      BubblePopEffect popEffect = newBubbleObj.GetComponent<BubblePopEffect>();

      // 3. 触发弹出动画
      if (popEffect != null)
      {
         popEffect.PlayPopAnimation();
      }
      else
      {
         Debug.LogError("新气泡预制体上没有找到 BubblePopEffect 脚本！");
      }

      // 4. (可选) 确保 ScrollView 自动滚动到最新添加的气泡
      ScrollToBottom();
   }
   
   public void AddLLMChat(string text)
   {
      GameObject newBubbleObj = Instantiate(rightBubble, contentParent);
      
      TextMeshProUGUI textComponent = newBubbleObj.GetComponentInChildren<TextMeshProUGUI>();
      
      if (textComponent != null)
      {
         textComponent.text = text;
      }

      // 2. 获取气泡上的动画脚本
      BubblePopEffect popEffect = newBubbleObj.GetComponent<BubblePopEffect>();

      // 3. 触发弹出动画
      if (popEffect != null)
      {
         popEffect.PlayPopAnimation();
      }
      else
      {
         Debug.LogError("新气泡预制体上没有找到 BubblePopEffect 脚本！");
      }
      
      // 4. (可选) 确保 ScrollView 自动滚动到最新添加的气泡
      ScrollToBottom();
   }
   
   private void ScrollToBottom()
   {
      if (scrollRect != null)
      {
         // 在下一帧执行滚动，确保Content的布局更新完毕
         LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
         scrollRect.verticalNormalizedPosition = 0f;
      }
   }
}
