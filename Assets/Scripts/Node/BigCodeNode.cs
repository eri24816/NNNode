using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class BigCodeNode : CodeNode
    { 
        public bool expanded = false;

        [SerializeField]
        TMPro.TMP_InputField nameInput;

        [SerializeField]
        Transform left, right, top, buttom;
        [SerializeField]
        TMPro.TextMeshProUGUI outputText;
        [SerializeField]
        UnityEngine.UI.LayoutElement outputTextLayoutElement;
        [SerializeField]
        List<GameObject> toHideOnMinimize;
        [SerializeField]
        float expandedWidth, minimizedWidth;
        public float maxOutputHeight = 150;

        readonly CoolDown recvCodeCD = new CoolDown(hz: 10);


        public override void Init(string infoJSON)
        {
            base.Init(infoJSON);

            nameInput.text = name;
            
        }

        public override void Update()
        {
            base.Update();
            if (recvCodeCD.Update())
            {
                Code = recievedCode;
            }
        }
        public override void Start()
        {
            base.Start();
            nameInput.enabled = false;
            nameInput.text = Name;
        }


        public override void AddOutput(string output)
        {
            base.AddOutput(output);
            if (expanded)
                UpdateOutputText();
        }

        public override void ClearOutput()
        {
            base.ClearOutput();
            if (expanded)
                UpdateOutputText();
        }

        void UpdateOutputText()
        {
            outputText.overflowMode = TMPro.TextOverflowModes.Overflow;
            outputText.text = output;
            outputText.ForceMeshUpdate();
            outputText.overflowMode = TMPro.TextOverflowModes.Truncate;
            outputTextLayoutElement.minHeight = Mathf.Min(maxOutputHeight, outputText.textBounds.size.y);
        }
        public void Rename()
        {
            nameInput.enabled = true;
            nameInput.Select();
        }
        public void RenameEnd()
        {
            nameInput.enabled = false;
        }
        public void CollapseOrExpand()
        { 
            expanded ^= true;
            foreach (GameObject g in toHideOnMinimize)
                g.SetActive(expanded);
            ((RectTransform)transform).sizeDelta = new Vector2(expanded ? expandedWidth : minimizedWidth, ((RectTransform)transform).sizeDelta.y);
            if (expanded)
            {
                UpdateOutputText();
            }
            if (isDemo)
            {
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
