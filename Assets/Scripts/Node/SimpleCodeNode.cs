using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class SimpleCodeNode : CodeNode
    {


        public override void Init(APIMessage.NewNode.Info info)
        {
            base.Init(info);
            Code = info.code;
        }
    }
}