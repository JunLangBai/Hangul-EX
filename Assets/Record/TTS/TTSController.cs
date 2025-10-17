using System;

using System.Collections.Generic;

using System.IO;

using System.Threading.Tasks;

using UnityEngine;

using UnityEngine.Networking;

using UnityEngine.UI;

using TMPro; // 1. 引用TextMesh Pro的命名空间



namespace OpenapiDemo

{

    public class TtsController : MonoBehaviour

    {

        [Header("有道智云API凭证")]

        [SerializeField] private string appKey = "5a2f7e95ec88ff0d";

        [SerializeField] private string appSecret = "QagE0IVcZc8OfmCgtzQBi2BppIsesgP9";



        [Header("TTS参数")]

        [SerializeField] private string textToSynthesize = "안녕하세요. 유니티에서 실행되는 텍스트 음성 변환 기능입니다.";

        [SerializeField] private string voiceName = "piaozhiyou";

        [SerializeField] private string format = "mp3";



        [Tooltip("合成音频的语速，范围 0.5 ~ 2.0，默认为1.0")]

        [SerializeField] private string speed = "1.0";

        [Tooltip("合成音频的音量，范围 0.5 ~ 5.0，默认为1.0")]

        [SerializeField] private string volume = "1.0";



        [Header("场景组件关联")]

        [SerializeField] private Button ttsButton;

        // 2. 将变量类型从 Text 改为 TextMeshProUGUI

        [SerializeField] private TextMeshProUGUI buttonText;

        [SerializeField] private AudioSource audioSource;



        private string _originalButtonText;

        private string _audioSavePath;



        void Start()

        {

            if (ttsButton == null || audioSource == null)

            {

                Debug.LogError("请在Inspector面板中关联TTS按钮和AudioSource组件！");

                return;

            }



            if (buttonText != null)

            {

                _originalButtonText = buttonText.text;

            }



            _audioSavePath = Path.Combine(Application.persistentDataPath, "tts_output.mp3");

            ttsButton.onClick.AddListener(StartTtsProcess);

        }



        public void StartTtsProcess()

        {

            ttsButton.interactable = false;

            if (buttonText != null) buttonText.text = "正在合成...";



            StartCoroutine(RequestTtsCoroutine());

        }



        private System.Collections.IEnumerator RequestTtsCoroutine()

        {

            byte[] result = null;

            Exception taskException = null;



            Task ttsTask = Task.Run(() =>

            {

                try

                {

                    Dictionary<string, string[]> paramsMap = CreateRequestParams();

                    AuthV3Util.addAuthParams(appKey, appSecret, paramsMap);

                    Dictionary<string, string[]> header = new Dictionary<string, string[]>() { { "Content-Type", new string[] { "application/x-www-form-urlencoded" } } };



                    Debug.Log("正在请求TTS API...");

                    result = HttpUtil.doPost("https://openapi.youdao.com/ttsapi", header, paramsMap, "audio");

                }

                catch (Exception e)

                {

                    taskException = e;

                }

            });



            yield return new WaitUntil(() => ttsTask.IsCompleted);



            if (taskException != null)

            {

                Debug.LogError($"TTS Request Task failed: {taskException.Message}");

                ResetButton();

                yield break;

            }



            if (result == null)

            {

                Debug.LogError("获取音频数据失败，请检查网络、API凭证或参数是否正确。");

                ResetButton();

                yield break;

            }



            Debug.Log("音频数据获取成功，准备保存...");

            SaveFile(_audioSavePath, result);

            yield return StartCoroutine(PlayAudioCoroutine(_audioSavePath));



            ResetButton();

        }



        private Dictionary<string, string[]> CreateRequestParams()

        {

            var paramsDict = new Dictionary<string, string[]>() {

                { "q", new string[]{textToSynthesize}},

                {"voiceName", new string[]{voiceName}},

                {"format", new string[]{format}}

            };



            if (!string.IsNullOrEmpty(speed) && speed != "1.0")

            {

                paramsDict.Add("speed", new string[] { speed });

            }

            if (!string.IsNullOrEmpty(volume) && volume != "1.0")

            {

                paramsDict.Add("volume", new string[] { volume });

            }



            return paramsDict;

        }



        private void SaveFile(string path, byte[] data)

        {

            try

            {

                File.WriteAllBytes(path, data);

                Debug.Log("音频文件保存成功! 路径: " + path);

            }

            catch (Exception e)

            {

                Debug.LogError($"保存文件出错: {e.Message}");

            }

        }



        private System.Collections.IEnumerator PlayAudioCoroutine(string filePath)

        {

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))

            {

                yield return www.SendWebRequest();



                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)

                {

                    Debug.LogError(www.error);

                }

                else

                {

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                    if (clip != null)

                    {

                        audioSource.clip = clip;

                        audioSource.Play();

                        Debug.Log("开始播放音频...");

                    }

                    else

                    {

                        Debug.LogError("加载的AudioClip为空，请检查音频文件格式是否正确。");

                    }

                }

            }

        }



        private void ResetButton()

        {

            ttsButton.interactable = true;

            if (buttonText != null) buttonText.text = _originalButtonText;

        }

    }

}