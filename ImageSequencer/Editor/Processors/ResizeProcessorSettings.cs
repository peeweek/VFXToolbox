using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("Common","Resize")]
    class ResizeProcessorSettings : ProcessorSettingsBase
    {
        public ushort Width;
        public ushort Height;

        public override string shaderPath => "Packages/com.unity.vfx-toolbox/ImageSequencer/Editor/Shaders/Resize.shader";

        public override string processorName => "Resize";

        public override string label
        {
            get
            {
                return string.Format("{0} ({1}x{2})", processorName, Width, Height);
            }
        }

        public override bool OnCanvasGUI(CanvasInfo canvas, SequenceInfo sequence, int currentFrameIndex)
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            Vector2 center = canvas.CanvasToScreen(Vector2.zero);

            Vector2 topRight;
            Vector2 bottomLeft;

            Texture frame = sequence.RequestFrame(currentFrameIndex);

            topRight = canvas.CanvasToScreen(new Vector2(-frame.width / 2, frame.height / 2));
            bottomLeft = canvas.CanvasToScreen(new Vector2(frame.width / 2, -frame.height / 2));

            // Arrows
            Handles.color = canvas.styles.green;
            Handles.DrawLine(new Vector3(topRight.x, topRight.y - 16), new Vector3(bottomLeft.x, topRight.y - 16));
            Handles.DrawLine(new Vector3(bottomLeft.x - 16, topRight.y), new Vector3(bottomLeft.x - 16, bottomLeft.y));
            Handles.color = Color.white;

            // Texts
            GUI.color = Color.green;
            GUI.Label(new Rect(center.x - 32, topRight.y - 32, 64, 16), Width.ToString(), canvas.styles.miniLabelCenter);
            VFXToolboxGUIUtility.GUIRotatedLabel(new Rect(bottomLeft.x - 48, center.y - 8, 64, 16), Height.ToString(), -90.0f, canvas.styles.miniLabelCenter);
            GUI.color = Color.white;
            return false;
        }

        public override bool OnInspectorGUI(SerializedObject serializedObject, bool hasChanged)
        {
            var width = serializedObject.FindProperty("Width");
            var height = serializedObject.FindProperty("Height");

            EditorGUI.BeginChangeCheck();

            using (new GUILayout.HorizontalScope())
            {
                int w = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Width"), width.intValue), 1, 8192);

                if (GUILayout.Button("", EditorStyles.popup, GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int s = 8192; s >= 16; s /= 2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, MenuSetWidth, s);
                    }
                    menu.ShowAsContext();
                }

                if (w != width.intValue)
                {
                    width.intValue = w;
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                int h = Mathf.Clamp(EditorGUILayout.IntField(VFXToolboxGUIUtility.Get("Height"), height.intValue), 1, 8192);

                if (GUILayout.Button("", EditorStyles.popup, GUILayout.Width(16)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int s = 8192; s >= 16; s /= 2)
                    {
                        menu.AddItem(VFXToolboxGUIUtility.Get(s.ToString()), false, MenuSetHeight, s);
                    }
                    menu.ShowAsContext();
                }
                if (h != height.intValue)
                {
                    height.intValue = h;
                }
            }

            if (Mathf.Log(height.intValue, 2) % 1.0f != 0 || Mathf.Log(width.intValue, 2) % 1.0f != 0)
            {
                EditorGUILayout.HelpBox("Warning: your resize resolution is not a power of two.", MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                UpdateOutputSize();
                Invalidate();
                hasChanged = true;
            }

            return hasChanged;
        }

        public override bool Process(SequenceInfo sequence, ProcessorInfo processorInfo, int index)
        {
            Texture texture = sequence.RequestFrame(index);
            Vector4 kernelAndSize = new Vector4((float)texture.width / (float)Width, (float)texture.height / (float)Height, (float)Width, (float)Height);
            processorInfo.SetTexture("_MainTex", texture);
            processorInfo.SetVector("_KernelAndSize", kernelAndSize);
            processorInfo.ExecuteShaderAndDump(index, texture);
            return true;
        }

        private void MenuSetWidth(object o)
        {
            m_SerializedObject.Update();
            var width = m_SerializedObject.FindProperty("Width");
            width.intValue = (int)o;
            m_SerializedObject.ApplyModifiedProperties();
            Invalidate();
            UpdateOutputSize();
        }

        private void MenuSetHeight(object o)
        {
            m_SerializedObject.Update();
            var height = m_SerializedObject.FindProperty("Height");
            height.intValue = (int)o;
            m_SerializedObject.ApplyModifiedProperties();
            Invalidate();
            UpdateOutputSize();
        }


        public override void Reset()
        {
            Width = 256;
            Height = 256;
        }

    }
}
