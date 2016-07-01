using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;

namespace DjvuApp.ViewModel
{
    public sealed class MainViewModel : ViewModelBase
    {
        public bool HasBooks
        {
            get { return _hasBooks; }

            private set
            {
                if (_hasBooks == value)
                {
                    return;
                }

                _hasBooks = value;
                RaisePropertyChanged();
            }
        }

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

        public ICommand RenameBookCommand { get; private set; }

        public ICommand RemoveBookCommand { get; private set; }

        public ICommand AddBookCommand { get; private set; }

        public ICommand ShareBookCommand { get; private set; }

        private ObservableCollection<IBook> _books;
        private bool _hasBooks;

        private readonly IBookProvider _bookProvider;
        private readonly ResourceLoader _resourceLoader;

        [UsedImplicitly]
        public MainViewModel(IBookProvider bookProvider)
        {
            _bookProvider = bookProvider;
            _resourceLoader = ResourceLoader.GetForCurrentView();

            RenameBookCommand = new RelayCommand<IBook>(async book => await RenameBookAsync(book));
            RemoveBookCommand = new RelayCommand<IBook>(async book => await RemoveBookAsync(book));
            AddBookCommand = new RelayCommand(async () =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".djvu");
                picker.FileTypeFilter.Add(".djv");

                var files = await picker.PickMultipleFilesAsync();
                foreach (var file in files)
                {
                    await AddBookFromFileAsync(file);
                }
            });
            ShareBookCommand = new RelayCommand<IBook>(ShareBook);
            
#pragma warning disable 4014
            RefreshBooksAsync();
#pragma warning restore 4014
        }

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                var file = e.Parameter as IStorageFile;
                if (file != null)
                {
                    await AddBookFromFileAsync(file);
                }
            }
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
        
        private async Task AddBookFromFileAsync(IStorageFile file)
        {
            var dialog = new BusyIndicator();
            var taskDescription = _resourceLoader.GetString("Application_Opening");
            dialog.TaskDescription = string.Format(taskDescription, file.Name);
            dialog.Show();

            IBook book;
            try
            {
                book = await _bookProvider.AddBookAsync(file);
            }
            catch (NotImplementedException)
            {
                await ShowDocumentTypeIsNotSupportedMessageAsync();
                return;
            }
            catch (Exception)
            {
                await ShowDocumentOpeningErrorMessageAsync();
                return;
            }
            finally
            {
                dialog.Hide();
            }
            
            Books.Insert(0, book);
        }

        private async Task RenameBookAsync(IBook book)
        {
            var name = await RenameDialog.ShowAsync(book.Title);

            if (name != book.Title)
            {
                await _bookProvider.ChangeTitleAsync(book, name);
            }
        }

        private async Task RemoveBookAsync(IBook book)
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
        
        private async Task RefreshBooksAsync()
        {
            var books = 
                from book in await _bookProvider.GetBooksAsync()
                orderby book.LastOpeningTime descending 
                select book;

            if (books.Any(book => book.ThumbnailPath == null))
            {
                var dialog = new BusyIndicator();
                dialog.Show();

                var booksArray = books.ToArray();
                for (int i = 0; i < booksArray.Length; i++)
                {
                    var book = booksArray[i];

                    var progressFormat = _resourceLoader.GetString("Application_MigrationProgress");
                    dialog.TaskDescription = string.Format(progressFormat, i + 1, booksArray.Length);

                    await _bookProvider.UpdateThumbnail(book);
                }
                
                dialog.Hide();
                
                await RefreshBooksAsync();
                return;
            }

            Books = new ObservableCollection<IBook>(books);
            Books.CollectionChanged += (sender, args) => UpdateHasBooks();
            UpdateHasBooks();
        }

        private void UpdateHasBooks()
        {
            HasBooks = Books.Any();
        }

        private async Task ShowDocumentOpeningErrorMessageAsync()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentOpeningErrorDialog_Title");
            var content = resourceLoader.GetString("DocumentOpeningErrorDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentOpeningErrorDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
        }

        private async Task ShowDocumentTypeIsNotSupportedMessageAsync()
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