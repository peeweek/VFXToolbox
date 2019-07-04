using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    public abstract class ProcessorSettingsBase : ScriptableObject
    {
        public abstract void Reset();

        public abstract string shaderPath { get; }

        public abstract string processorName { get; }

        public abstract string label { get; }

        public abstract bool OnInspectorGUI(SerializedObject serializedObject, bool hasChanged);

        public abstract bool OnCanvasGUI(CanvasInfo canvas, SequenceInfo sequence, int index);

        public abstract bool Process(SequenceInfo sequence, ProcessorInfo processorInfo, int index);

        public virtual Vector2Int GetOutputSize(Vector2Int InputSize)
        {
            return InputSize;
        }

    }
}


