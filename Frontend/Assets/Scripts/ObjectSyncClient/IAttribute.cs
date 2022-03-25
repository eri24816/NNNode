using Newtonsoft.Json.Linq;
using System;

namespace ObjectSync
{
    public interface IAttribute
    {
        object OValue { get; } // Not needed?

        void Recieve(JToken message);
        void Send();
        void Set(object value = null, bool send = true);
        void Update();
    }
}