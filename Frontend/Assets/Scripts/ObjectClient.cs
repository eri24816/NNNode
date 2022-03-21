using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;

namespace GraphUI
{
    public abstract class ObjectClient : MonoBehaviour, ObjectSync.IObjectClient
    {
        ObjectSync.Object syncObject;
        // Use this for initialization
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
        public virtual void OnCreate(JToken message, ObjectSync.Object obj)
        {
            syncObject = obj;
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void RecieveMessage(JToken message)
        {
        }
    }
}