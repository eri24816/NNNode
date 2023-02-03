using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObjectSync;

namespace NNNode.Inspector
{
    public class StringField: InspectorField<string>
    {

        [SerializeField]
        TMPro.CodeEditor inputFieldUI;  

        private void Start()
        {
            inputFieldUI.onEndEdit.AddListener((value) => { attribute.Set(value); });
        }
        protected override void OnChange(string value)
        {
            inputFieldUI.SetTextWithoutNotify(value); 
        }
    }
}
