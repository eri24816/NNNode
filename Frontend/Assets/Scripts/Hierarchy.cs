using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectSync.ObjectClients
{
    [AddComponentMenu("ObjectClient/Hierarchy")]
    public class Hierarchy : ObjectClient
    {
        [SerializeField]
        RectTransform rootPanel;

        public override void RecieveMessage(JToken message)
        {
            base.RecieveMessage(message);
            switch ((string)message["command"])
            {
                case "add item":
                    var item = spaceClient[(string)message["item_id"]];
                    //AddItem()
                    break;
            }
        }

}