using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    internal class FrameProcessor
    {
        public int OutputWidth
        {
            get {
                if (Enabled)
                    return GetOutputWidth();
                else
                    return
                        InputSequence.width;
            }
        }
        public int OutputHeight
        {
            get
            {
                if (Enabled)
                    return GetOutputHeight();
                else
                    return
                        InputSequence.width;
            }
        }

        public int NumU
        {
            get {
                if (Enabled)
                    return GetNumU();
                else
                    return InputSequence.numU;
            }
        }
        public int NumV
        {
            get {
                if (Enabled)
                    return GetNumV();
                else
                    return InputSequence.numV;
            }
        }

        public bool GenerateMipMaps;
        public bool Linear;

        public bool Enabled { get{ return m_bEnabled; } set {SetEnabled(value); } }

        public ProcessingFrameSequence InputSequence
        {
            get { return m_ProcessorStack.GetInputSequence(this); }
        }
        public ProcessingFrameSequence OutputSequence
        {
            get { if (m_bEnabled) return m_OutputSequence; else return InputSequence; }
        }

        public ProcessorInfo ProcessorInfo
        {
            get { return m_ProcessorInfo; }
        }

        protected FrameProcessorStack m_ProcessorStack;
        protected ProcessingFrameSequence m_OutputSequence;

        protected bool m_bEnabled;

        protected int m_OutputWidth;
        protected int m_OutputHeight;

        protected ProcessorInfo m_ProcessorInfo;

        public ProcessorSettingsBase settings { get { return m_Settings; } private set { m_Settings = value; m_SerializedObject = new SerializedObject(m_Settings); } }

        private ProcessorSettingsBase m_Settings;

        protected SerializedObject m_SerializedObject;

        public FrameProcessor(string shaderPath, FrameProcessorStack processorStack, ProcessorInfo info)
            : this(AssetDatabase.LoadAssetAtPath<Shader>(shaderPath), processorStack, info)
        { }

        public FrameProcessor(Shader shader, FrameProcessorStack processorStack, ProcessorInfo info)
        {
            m_ProcessorInfo = info;
            m_ProcessorInfo.ProcessorName = info.Settings.name;
            m_bEnabled = m_ProcessorInfo.Enabled;
            m_ProcessorStack = processorStack;
            m_OutputSequence = new ProcessingFrameSequence(this);
            Linear = true;
            GenerateMipMaps = true;

            m_ProcessorInfo = info;
            settings = m_ProcessorInfo.Settings;
            m_Shader = shader;
            m_Material = new Material(m_Shader) { hideFlags = HideFlags.DontSave };
            m_Material.hideFlags = HideFlags.DontSave;
        }

        public void SetEnabled(bool value)
        {
            m_bEnabled = value;
            var info = new SerializedObject(m_ProcessorInfo);
            info.Update();
            info.FindProperty("Enabled").boolValue = value;
            info.ApplyModifiedProperties();
        }

        public void Refresh()
        {
            if(Enabled != m_ProcessorInfo.Enabled)
                Enabled = m_ProcessorInfo.Enabled;
            UpdateSequenceLength();
            UpdateOutputSize();
        }

        protected virtual void UpdateOutputSize()
        {
            SetOutputSize(InputSequence.width, InputSequence.height);
        }

        protected virtual int GetOutputWidth()
        {
            UpdateOutputSize();
            return m_OutputWidth;
        }
        protected virtual int GetOutputHeight()
        {
            UpdateOutputSize();
            return m_OutputHeight;
        }

        public void SetOutputSize(int width, int height)
        {
            if(m_OutputWidth != width || m_OutputHeight != height)
            {
                m_OutputWidth = Mathf.Clamp(width,1,8192);
                m_OutputHeight = Mathf.Clamp(height,1,8192);
            }
        }

        protected bool DrawSidePanelHeader()
        {
            bool bHasChanged = false;
            bool previousEnabled = Enabled;
            Enabled = VFXToolboxGUIUtility.ToggleableHeader(Enabled, false, settings.name);

            if(previousEnabled != Enabled)
            {
                SerializedObject o = new SerializedObject(m_ProcessorInfo);
                o.FindProperty("Enabled").boolValue = Enabled;
                o.ApplyModifiedProperties();
                m_ProcessorStack.Invalidate(this);
                bHasChanged = true;
            }
            return bHasChanged;
        }

        public bool OnSidePanelGUI(ImageSequence asset, int ProcessorIndex)
        {
            bool bHasChanged = DrawSidePanelHeader();

            using (new EditorGUI.DisabledScope(!Enabled))
            {
                m_SerializedObject.Update();
                bHasChanged = m_Settings.OnInspectorGUI(m_SerializedObject, bHasChanged);
                m_SerializedObject.ApplyModifiedProperties();
            }

            return bHasChanged;
        }

        public virtual void RequestProcessOneFrame(int currentFrame)
        {
            int length = OutputSequence.length;

            int i = (currentFrame + 1) % length;

            while (i != currentFrame)
            {
                bool advance = false;
                if(OutputSequence.frames[i].dirty)
                {
                    advance = OutputSequence.Process(i);
                    if(advance) return;
                }

                i = (i + 1);
                i %= length;
            }
        }

        public virtual int GetProcessorSequenceLength()
        {
            return InputSequence.length;
        }

        public bool Process(int frame)
        { 
            return settings.Process(GetSequenceInfo(), m_ProcessorInfo, frame);
        }

        public SequenceInfo GetSequenceInfo()
        {
            return new SequenceInfo()
            {
                currentSequence = InputSequence
            };
        }

        public bool Process(ProcessingFrame frame)
        {
            return Process(OutputSequence.frames.IndexOf(frame));
        }

        public void UpdateSequenceLength()
        {
            int currentCount = m_OutputSequence.frames.Count;
            int requiredCount = GetProcessorSequenceLength();

            if (currentCount == requiredCount)
                return;

            if(currentCount > requiredCount)
            {
                for(int i = requiredCount - 1; i < currentCount - 1; i++)
                {
                    m_OutputSequence.frames[i].Dispose();
                }

                m_OutputSequence.frames.RemoveRange(requiredCount - 1, currentCount - requiredCount);
            }
            else
            {
                for(int i = 0; i < requiredCount - currentCount; i++)
                {
                    m_OutputSequence.frames.Add(new ProcessingFrame(this));
                }
            }
        }

        public virtual void Invalidate()
        {
            UpdateSequenceLength();
            SetOutputSize(GetOutputWidth(), GetOutputHeight());
            m_OutputSequence.InvalidateAll();

            FrameProcessor next = m_ProcessorStack.GetNextProcessor(this);
            if(next != null)
                next.Invalidate();
        }

        public override string ToString()
        {
            return settings.label + (Enabled ? "" : " (Disabled)");
        }

        public Material material { get { return m_Material; } }

        protected Shader m_Shader;
        protected Material m_Material;

        public void ExecuteShaderAndDump(int outputframe, Texture mainTex)
        {
            ExecuteShaderAndDump(outputframe, mainTex, m_Material);
        }

        public void ExecuteShaderAndDump(int outputframe, Texture mainTex, Material material)
        {
            RenderTexture backup = RenderTexture.active;
            Graphics.Blit(mainTex, (RenderTexture)m_OutputSequence.frames[outputframe].texture, material);
            RenderTexture.active = backup;
        }

        public void Dispose()
        {
            Material.DestroyImmediate(m_Material);
            m_OutputSequence.Dispose();
        }

        protected int GetNumU()
        {
            if (InputSequence.processor == null)
                return 1;
            return InputSequence.numU;
        }

        protected int GetNumV()
        {
            if (InputSequence.processor == null)
                return 1;
            return InputSequence.numV;
        }


    }

}
