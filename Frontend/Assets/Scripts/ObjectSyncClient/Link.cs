using System;
using System.Collections.Generic;

public class AvoidRecursion : IDisposable
{
    static readonly List<object> locked;
    public static bool IsLocked(object obj)
    {
        return locked.Contains(obj);
    }

    object obj;
    public AvoidRecursion(object obj)
    {
        if (IsLocked(obj))
        {
            throw new Exception("Recursion detected!");
        }
        locked.Add(obj);
        this.obj = obj;
    }
    public void Dispose()
    {
        locked.Remove(obj);
    }
}

namespace ObjectSync
{
    class Link<T>
    {
        readonly List<Attribute<T>> attributes = new();
        public bool locked = false;
        public Link(IEnumerable<Attribute<T>> attrs)
        {
            foreach (var attr in attrs)
            {
                Add(attr);
            }
        }
        public void Add(Attribute<T> attribute)
        {
            attributes.Add(attribute);
            attribute.OnSet += ValueChangeListener;
        }
        public void Remove(Attribute<T> attribute)
        {
            attributes.Remove(attribute);
            attribute.OnSet -= ValueChangeListener;
        }
        public void Close()
        {
            foreach(Attribute<T> attribute in attributes)
            {
                attribute.OnSet -= ValueChangeListener;
            }
        }

        void ValueChangeListener(T value)
        {
            if (AvoidRecursion.IsLocked(this)) return;
            using (new AvoidRecursion(this))
            {
                foreach (Attribute<T> attribute in attributes)
                {
                    attribute.Set(value);
                }
            }
        }
        ~Link(){
            Close();
        }
    }
}