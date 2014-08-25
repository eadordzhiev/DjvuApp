using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuApp.ViewModel.Messages
{
    public sealed class OnNavigatedFromMessage
    {
        public object Parameter { get; private set; }

        public OnNavigatedFromMessage(object parameter)
        {
            Parameter = parameter;
        }
    }
}
