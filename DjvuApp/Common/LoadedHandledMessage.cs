using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuApp.Common
{
    public sealed class LoadedHandledMessage<T>
    {
        public T Parameter { get; private set; }

        public LoadedHandledMessage(T parameter)
        {
            Parameter = parameter;
        }
    }
}
