using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObjectSync;
using Newtonsoft.Json.Linq;

namespace NNNode
{
    public class TextEditorNode : Node
    {
        Attribute<string> Content;
        public string dataType = "string";

        [SerializeField]
        TMPro.CodeEditor inputFieldUI;
        [SerializeField]
        UnityEngine.UI.Image lineNumberPanel;
        [SerializeField]
        TMPro.TMP_Text lineNumber;

        public override void OnCreate(JToken message, ObjectSync.Object obj)
        {
            base.OnCreate(message, obj);

            // link the inputField to the content attribute
            Content = syncObject.RegisterAttribute<string>("content", (value) => { inputFieldUI.SetTextWithoutNotify(value); });
            inputFieldUI.onEndEdit.AddListener((value) => { Content.Set(value); });
        }
        public override void SetColor(Color color)
        {
            base.SetColor(color); 
            lineNumber.color = color;
            color.a = 0.3f;
            lineNumberPanel.color = color;
        }
    }
}