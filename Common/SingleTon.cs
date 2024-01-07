using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SingleTon<T> where T : class, new()
{
    public static class InternalClass<T> where T : class, new()
    {
        public static T instance = new T();
    }

    public static T Instance
    {
        get
        {
            return InternalClass<T>.instance;
        }
    }
}
