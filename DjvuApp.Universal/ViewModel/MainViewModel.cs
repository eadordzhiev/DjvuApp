using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;

namespace DjvuApp.ViewModel
{
    public sealed class MainViewModel : ViewModelBase
    {
        public ObservableCollection<IBook> Books
        {
            get { return _books; }

            private set
            {
                if (_books == value)
                {
                    return;
                }

                _books = value;
                RaisePropertyChanged();
            }
        }

        public bool IsProgressVisible
        {
            get { return _isProgressVisible; }

            private set
            {
                if (_isProgressVisible == value)
                {
                    return;
                }

                _isProgressVisible = value;
                RaisePropertyChanged();
            }
        }

        public ICommand RenameBookCommand { get; private set; }

        public ICommand RemoveBookCommand { get; private set; }

        public ICommand AddBookCommand { get; private set; }

        public ICommand ShareBookCommand { get; private set; }

        private ObservableCollection<IBook> _books;
        private bool _isProgressVisible;

        private readonly IBookProvider _bookProvider;
        private readonly ResourceLoader _resourceLoader;

        [UsedImplicitly]
        public MainViewModel(IBookProvider bookProvider)
        {
            _bookProvider = bookProvider;
            _resourceLoader = ResourceLoader.GetForCurrentView();

            RenameBookCommand = new RelayCommand<IBook>(RenameBook);
            RemoveBookCommand = new RelayCommand<IBook>(RemoveBook);
            AddBookCommand = new RelayCommand(async () =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".djvu");

                var file = await picker.PickSingleFileAsync();
                AddBookFromFile(file);
            });
            ShareBookCommand = new RelayCommand<IBook>(ShareBook);

            MessengerInstance.Register<OnNavigatedToMessage<MainViewModel>>(this, message =>
            {
                if (message.EventArgs.NavigationMode == NavigationMode.New)
                {
                    var file = message.EventArgs.Parameter as IStorageFile;
                    if (file != null)
                    {
                        AddBookFromFile(file);
                    }
                }
            });
            
            RefreshBooks();
        }

        public override void Cleanup()
        {
            MessengerInstance.Unregister<OnNavigatedToMessage<MainViewModel>>(this);
        }

        private void ShareBook(IBook book)
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            TypedEventHandler<DataTransferManager, DataRequestedEventArgs> dataRequestedHandler = null;

            dataRequestedHandler = async (sender, e) =>
            {
                dataTransferManager.DataRequested -= dataRequestedHandler;

                var deferral = e.Request.GetDeferral();

                e.Request.Data.Properties.Title = book.Title;
                var file = await StorageFile.GetFileFromPathAsync(book.Path);
                e.Request.Data.SetStorageItems(new IStorageItem[] { file }, true);

                deferral.Complete();
            };
            
            dataTransferManager.DataRequested += dataRequestedHandler;
            DataTransferManager.ShowShareUI();
        }
        
        private async void AddBookFromFile(IStorageFile file)
        {
            var dialog = new BusyIndicator();
            dialog.Show($"Opening: {file.Name}");

            IBook book;
            try
            {
                book = await _bookProvider.AddBookAsync(file);
            }
            catch (NotImplementedException)
            {
                ShowDocumentTypeIsNotSupportedMessage();
                return;
            }
            catch (Exception)
            {
                ShowDocumentOpeningErrorMessage();
                return;
            }

            dialog.Hide();

            Books.Insert(0, book);
        }

        private async void RenameBook(IBook book)
        {
            var name = await RenameDialog.ShowAsync(book.Title);

            if (name != book.Title)
            {
                await _bookProvider.ChangeTitleAsync(book, name);
            }
        }

        private async void RemoveBook(IBook book)
        {
            var titleFormat = _resourceLoader.GetString("RemoveBookDialog_Title");
            var title = string.Format(titleFormat, book.Title);
            var content = _resourceLoader.GetString("RemoveBookDialog_Content");
            var removeButtonCaption = _resourceLoader.GetString("RemoveBookDialog_RemoveButton_Caption");
            var cancelButtonCaption = _resourceLoader.GetString("RemoveBookDialog_CancelButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(removeButtonCaption, async command =>
            {
                Books.Remove(book);
                await _bookProvider.RemoveBookAsync(book);
            }));
            dialog.Commands.Add(new UICommand(cancelButtonCaption));

            await dialog.ShowAsync();
        }

        private async void RefreshBooks()
        {
            IsProgressVisible = true;

            var books = 
                from book in await _bookProvider.GetBooksAsync()
                orderby book.LastOpeningTime descending 
                select book;

            Books = new ObservableCollection<IBook>(books);

            IsProgressVisible = false;
        }

        private async void ShowDocumentOpeningErrorMessage()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentOpeningErrorDialog_Title");
            var content = resourceLoader.GetString("DocumentOpeningErrorDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentOpeningErrorDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
        }

        private async void ShowDocumentTypeIsNotSupportedMessage()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_Title");
            var content = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentTypeIsNotSupportedDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
        }
    }
}