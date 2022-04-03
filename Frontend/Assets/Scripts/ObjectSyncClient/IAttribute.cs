using Newtonsoft.Json.Linq;

namespace ObjectSync
{
    public interface IAttribute
    {
        object OValue { get; } // Not needed?

        void Recieve(JToken message);
        void Send();
        void Set(JToken value = null, bool send = true);
        void Update();
    }
}