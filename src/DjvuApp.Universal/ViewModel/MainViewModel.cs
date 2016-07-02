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
using CollectionView;
using DjvuApp.Dialogs;
using DjvuApp.Model;
using DjvuApp.Pages;
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

        public ListCollectionView BooksCollectionView
        {
            get { return _booksCollectionView; }

            private set
            {
                if (_booksCollectionView == value)
                {
                    return;
                }

                _booksCollectionView = value;
                RaisePropertyChanged();
            }
        }

        public ICommand RenameBookCommand { get; }

        public ICommand RemoveBookCommand { get; }

        public ICommand AddBookCommand { get; }

        public ICommand ShareBookCommand { get; }

        private ListCollectionView _booksCollectionView;
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

            BooksCollectionView = new ListCollectionView(_bookProvider.Books);
            BooksCollectionView.SortDescriptions.Add(new SortDescription("LastOpeningTime", ListSortDirection.Descending));
            BooksCollectionView.VectorChanged += (sender, args) => UpdateHasBooks();
            UpdateHasBooks();

            MigrateAsync();
        }

        private async Task MigrateAsync()
        {
            var migrator = await LegacyDbMigrator.CreateAsync();
            if (migrator.IsMigrationNeeded)
            {
                var dialog = new BusyIndicator();
                dialog.TaskDescription = _resourceLoader.GetString("Application_MigrationMessage");
                dialog.Show();
                try
                {
                    await migrator.MigrateAsync();
                    await _bookProvider.RefreshAsync();
                }
                finally
                {
                    dialog.Hide();
                }
            }
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

        private static void ShareBook(IBook book)
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            TypedEventHandler<DataTransferManager, DataRequestedEventArgs> dataRequestedHandler = null;

            dataRequestedHandler = async (sender, e) =>
            {
                dataTransferManager.DataRequested -= dataRequestedHandler;

                var deferral = e.Request.GetDeferral();

                e.Request.Data.Properties.Title = book.Title;
                var file = await StorageFile.GetFileFromPathAsync(book.BookPath);
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
            
            try
            {
                await _bookProvider.AddBookAsync(file);
            }
            catch (NotImplementedException)
            {
                await ShowDocumentTypeIsNotSupportedMessageAsync();
            }
            catch
            {
                await ShowDocumentOpeningErrorMessageAsync();
            }
            finally
            {
                dialog.Hide();
            }
        }

        private static async Task RenameBookAsync(IBook book)
        {
            book.Title = await RenameDialog.ShowAsync(book.Title);
            await book.SaveChangesAsync();
        }

        private async Task RemoveBookAsync(IBook book)
        {
            var titleFormat = _resourceLoader.GetString("RemoveBookDialog_Title");
            var title = string.Format(titleFormat, book.Title);
            var content = _resourceLoader.GetString("RemoveBookDialog_Content");
            var removeButtonCaption = _resourceLoader.GetString("RemoveBookDialog_RemoveButton_Caption");
            var cancelButtonCaption = _resourceLoader.GetString("RemoveBookDialog_CancelButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(removeButtonCaption, async command => await book.RemoveAsync()));
            dialog.Commands.Add(new UICommand(cancelButtonCaption));

            await dialog.ShowAsync();
        }
        
        private void UpdateHasBooks()
        {
            HasBooks = BooksCollectionView.Any();
        }

        private static async Task ShowDocumentOpeningErrorMessageAsync()
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
            var title = resourceLoader.GetString("DocumentOpeningErrorDialog_Title");
            var content = resourceLoader.GetString("DocumentOpeningErrorDialog_Content");
            var okButtonCaption = resourceLoader.GetString("DocumentOpeningErrorDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(okButtonCaption));
            await dialog.ShowAsync();
        }

        private static async Task ShowDocumentTypeIsNotSupportedMessageAsync()
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