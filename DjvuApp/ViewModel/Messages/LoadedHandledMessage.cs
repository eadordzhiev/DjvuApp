namespace DjvuApp.ViewModel.Messages
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
