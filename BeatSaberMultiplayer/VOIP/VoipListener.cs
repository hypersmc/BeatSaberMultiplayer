﻿using NSpeex;
using System;
using System.IO;
using UnityEngine;

namespace BeatSaberMultiplayer.VOIP
{
    //https://github.com/DwayneBull/UnityVOIP/blob/master/VoipListener.cs
    public class VoipListener : MonoBehaviour
    {

        private AudioClip recording;
        private float[] recordingBuffer;
        private float[] resampleBuffer;

        private SpeexCodex encoder;

        private int lastPos = 0;
        private int index;

        public event Action<VoipFragment> OnAudioGenerated;

        public BandMode max = BandMode.Wide;
        public int inputFreq;

        public bool isListening
        {
            get
            {
                return _isListening;
            }
            set
            {
                if (!_isListening && value && recordingBuffer != null)
                {
                    index += 3;
                    lastPos = Math.Max(Microphone.GetPosition(Config.Instance.VoiceChatMicrophone) - recordingBuffer.Length, 0);
                }
                _isListening = value;
            }
        }

        private bool _isListening;

        void Awake()
        {
           
        }

        public void StartRecording()
        {
            if (Microphone.devices.Length == 0) return;

            inputFreq = AudioUtils.GetFreqForMic();
            
            encoder = SpeexCodex.Create(BandMode.Wide);

            var ratio = inputFreq / (float)AudioUtils.GetFrequency(encoder.mode);
            int sizeRequired = (int)(ratio * encoder.dataSize);
            recordingBuffer = new float[sizeRequired];
            resampleBuffer = new float[encoder.dataSize];
            
            if (AudioUtils.GetFrequency(encoder.mode) == inputFreq)
            {
                recordingBuffer = resampleBuffer;
            }            

            recording = Microphone.Start(Config.Instance.VoiceChatMicrophone, true, 20, inputFreq);
            Plugin.log.Debug("Used microphone: " + (Config.Instance.VoiceChatMicrophone == null ? "DEFAULT" : Config.Instance.VoiceChatMicrophone));
            Plugin.log.Debug("Used mic sample rate: " + inputFreq + "Hz");
            Plugin.log.Debug("Used buffer size for recording: " + sizeRequired + " floats");
        }

        public void StopRecording()
        {
            Microphone.End(Config.Instance.VoiceChatMicrophone);
            Destroy(recording);
            recording = null;
        }

        void Update()
        {
            if ( recording == null)
            {
                return;
            }

            var now = Microphone.GetPosition(Config.Instance.VoiceChatMicrophone);

            var length = now - lastPos;

            if (now < lastPos)
            {
                lastPos = 0;
                length = now;
            }
            
            while (length >= recordingBuffer.Length)
            {
                if (_isListening && recording.GetData(recordingBuffer, lastPos))
                {
                    //Send..
                    index++;
                    if (OnAudioGenerated != null )
                    {
                        //Downsample if needed.
                        if (recordingBuffer != resampleBuffer)
                        {
                            AudioUtils.Resample(recordingBuffer, resampleBuffer, inputFreq, AudioUtils.GetFrequency(encoder.mode));
                        }

                        var data = encoder.Encode(resampleBuffer);

                        VoipFragment frag = new VoipFragment(0, index, data, encoder.mode);

                        OnAudioGenerated?.Invoke(frag);
                    }
                }
                length -= recordingBuffer.Length;
                lastPos += recordingBuffer.Length;
            }            
        }

        void OnDestroy()
        {
            StopRecording();
        }
    }
}