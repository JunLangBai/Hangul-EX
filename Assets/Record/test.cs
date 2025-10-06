using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicrophoneController : MonoBehaviour
{
    bool micConnected = false;
    int minFreq;
    int maxFreq;
    
    AudioSource goAudioSource;

    void Start()
    {
        //查询麦克风
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("No Microphone devices found");
        }
        else
        {
            micConnected = true;
            
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
            if (minFreq == 0 && maxFreq == 0)
            {

                maxFreq = 44100;
            }
            
            goAudioSource = GetComponent<AudioSource>();
        }
    }

    void OnGUI()
    {
        if (micConnected)
        {
            if (!Microphone.IsRecording(null))
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Record"))
                {
                    goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);
                } 
            }

            else
            {
                if(GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Stop and play"))
                {
                    
                    Microphone.End(null);
                    
                    goAudioSource.Play();
                }
                
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 25, 200, 50), "Microphone is recording");
            }
        }
       
    }
}
