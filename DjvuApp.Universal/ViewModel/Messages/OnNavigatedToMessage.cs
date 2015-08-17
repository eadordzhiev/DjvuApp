namespace DjvuApp.ViewModel.Messages
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