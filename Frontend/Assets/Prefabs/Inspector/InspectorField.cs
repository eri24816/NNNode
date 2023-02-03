using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObjectSync;

namespace NNNode.Inspector
{
    public class InspectorField<T> : MonoBehaviour
    {
        protected Attribute<T> attribute;

        protected virtual void OnChange (T value)  {
        }
        public void Attach(Attribute<T> attribute)
        {
            attribute.OnSet += OnChange;
        }

        public void Dispose()
        {
            if(attribute != null)
                attribute.OnSet -= OnChange;
        }

        public virtual void SetColor(Color color)
        {
        }
        private void OnDestroy()
        {
            Dispose();
        }
    }
}
