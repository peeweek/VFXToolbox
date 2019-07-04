using UnityEngine;
using System;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    public class ProcessorInfo : ScriptableObject
    {
        public string ProcessorName;
        public bool Enabled;
        public ProcessorSettingsBase Settings;

        internal FrameProcessor m_frameProcessor;

        internal static ProcessorInfo CreateDefault(string name, bool enabled, ProcessorSettingsBase settings, FrameProcessorStack processorStack)
        {
            ProcessorInfo p = CreateInstance<ProcessorInfo>();
            p.ProcessorName = name;
            p.Enabled = enabled;
            p.Settings = Instantiate(settings) as ProcessorSettingsBase;
            p.m_frameProcessor = new FrameProcessor(settings.shaderPath, processorStack, p);
            p.Settings.Reset();
            return p;
        }

        public override string ToString()
        {
            return ProcessorName + (Enabled ? "" : "Disabled") ;
        }

        public void SetTexture(string name, Texture texture)
        {
            m_frameProcessor.material.SetTexture(name, texture);
        }

        public void SetFloat(string name, float value)
        {
            m_frameProcessor.material.SetFloat(name, value);
        }

        public void SetVector(string name, Vector4 value)
        {
            m_frameProcessor.material.SetVector(name, value);
        }

        public void SetColor(string name, Color value)
        {
            m_frameProcessor.material.SetColor(name, value);
        }

        public void ExecuteShaderAndDump(int outputFrame, Texture mainTex)
        {
            m_frameProcessor.ExecuteShaderAndDump(outputFrame, mainTex);
        }

        public void ExecuteShaderAndDump(int outputFrame, Texture mainTex, Material material)
        {
            m_frameProcessor.ExecuteShaderAndDump(outputFrame, mainTex, material);
        }


    }
}

