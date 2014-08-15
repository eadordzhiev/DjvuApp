namespace DjvuApp.Common
{
    public class OnNavigatedToMessage
    {
        public object Parameter { get; private set; }

        public OnNavigatedToMessage(object parameter)
        {
            Parameter = parameter;
        }
    }
}