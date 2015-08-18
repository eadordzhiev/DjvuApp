using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JetBrains.Annotations;
using DjvuApp.Controls;
using DjvuApp.Djvu;
using DjvuApp.ViewModel.Messages;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DjvuApp.ViewModel
{
    [UsedImplicitly]
    public sealed class ViewerViewModel : ViewModelBase
    {
        public bool IsProgressVisible
        {
            get
            {
                return _isProgressVisible;
            }

            private set
            {
                if (_isProgressVisible != value)
                {
                    _isProgressVisible = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCurrentPageBookmarked
        {
            get
            {
                return _isCurrentPageBookmarked;
            }

            private set
            {
                if (_isCurrentPageBookmarked != value)
                {
                    _isCurrentPageBookmarked = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DjvuDocument CurrentDocument
        {
            get
            {
                return _currentDocument;
            }

            private set
            {
                if (_currentDocument != value)
                {
                    _currentDocument = value;
                    RaisePropertyChanged();
                }
            }
        }

        public uint CurrentPageNumber
        {
            get
            {
                return _currentPageNumber;
            }

            set
            {
                if (_currentPageNumber != value)
                {
                    _currentPageNumber = value;
                    OnCurrentPageNumberChanged();
                    RaisePropertyChanged();
                }
            }
        }

        public IReadOnlyList<DjvuOutlineItem> Outline
        {
            get
            {
                return _outline;
            }

            private set
            {
                if (_outline != value)
                {
                    _outline = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ICommand ShowOutlineCommand { get; }

        public ICommand JumpToPageCommand { get; }

        public ICommand AddBookmarkCommand { get; }

        public ICommand RemoveBookmarkCommand { get; }

        public ICommand ShowBookmarksCommand { get; }

        public RelayCommand GoToNextPageCommand { get; }

        public RelayCommand GoToPreviousPageCommand { get; }

        public ICommand ShareCommand { get; }

        private bool _isProgressVisible;
        private bool _isCurrentPageBookmarked;
        private DjvuDocument _currentDocument;
        private uint _currentPageNumber;
        private IReadOnlyList<DjvuOutlineItem> _outline;

        private readonly DataTransferManager _dataTransferManager;
        private readonly IBookProvider _provider;
        private readonly INavigationService _navigationService;
        private readonly ResourceLoader _resourceLoader;
        private ObservableCollection<IBookmark> _bookmarks;
        private IBook _book;

        public ViewerViewModel(IBookProvider provider, INavigationService navigationService)
        {
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _provider = provider;
            _navigationService = navigationService;
            _resourceLoader = ResourceLoader.GetForCurrentView();

            ShowOutlineCommand = new RelayCommand(ShowOutline);
            JumpToPageCommand = new RelayCommand(ShowJumpToPageDialog);
            AddBookmarkCommand = new RelayCommand(AddBookmark);
            RemoveBookmarkCommand = new RelayCommand(RemoveBookmark);
            ShowBookmarksCommand = new RelayCommand(ShowBookmarks);
            ShareCommand = new RelayCommand(DataTransferManager.ShowShareUI);

            GoToNextPageCommand = new RelayCommand(
                () => CurrentPageNumber++, 
                () => CurrentDocument != null && CurrentPageNumber < CurrentDocument.PageCount);
            GoToPreviousPageCommand = new RelayCommand(
                () => CurrentPageNumber--, 
                () => CurrentPageNumber > 1);

            MessengerInstance.Register<LoadedHandledMessage<IBook>>(this, message => LoadedHandler(message.Parameter));
            MessengerInstance.Register<OnNavigatedFromMessage>(this,
                async message =>
                {
                    _dataTransferManager.DataRequested -= DataRequestedHandler;
                    Application.Current.Suspending -= ApplicationSuspendingHandler;
                    await SaveLastOpenedPageAsync();
                });
            MessengerInstance.Register<OnNavigatedToMessage>(this,
                message =>
                {
                    _dataTransferManager.DataRequested += DataRequestedHandler;
                    Application.Current.Suspending += ApplicationSuspendingHandler;
                });
        }

        private async void ApplicationSuspendingHandler(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SaveLastOpenedPageAsync();
            deferral.Complete();
        }

        private async Task SaveLastOpenedPageAsync()
        {
            if (CurrentPageNumber == 0)
                return;

            await _provider.UpdateLastOpenedPageAsync(_book, CurrentPageNumber);
        }

        private async void DataRequestedHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            e.Request.Data.Properties.Title = _book.Title;
            var file = await StorageFile.GetFileFromPathAsync(_book.Path);
            e.Request.Data.SetStorageItems(new IStorageItem[] {file}, true);

            deferral.Complete();
        }
        
        private async void RemoveBookmark()
        {
            if (!IsCurrentPageBookmarked)
                return;

            var bookmark = _bookmarks.First(item => item.PageNumber == CurrentPageNumber);
            await _provider.RemoveBookmarkAsync(bookmark);
            _bookmarks.Remove(bookmark);
            IsCurrentPageBookmarked = false;
        }

        private async void AddBookmark()
        {
            if (IsCurrentPageBookmarked) 
                return;

            var title = await CreateBookmarkDialog.ShowAsync();
            if (title == null)
                return;
            var bookmark = await _provider.CreateBookmarkAsync(_book, title, CurrentPageNumber);
            _bookmarks.Add(bookmark);
            IsCurrentPageBookmarked = true;
        }

        private async void ShowBookmarks()
        {
            var bookmark = await SelectBookmarkDialog.ShowAsync(_bookmarks);
            if (bookmark == null)
                return;

            CurrentPageNumber = bookmark.PageNumber;
        }

        private async void ShowOutline()
        {
            var pageNumber = await OutlineDialog.ShowAsync(Outline);
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void ShowJumpToPageDialog()
        {
            var pageNumber = await JumpToPageDialog.ShowAsync(CurrentPageNumber, CurrentDocument.PageCount);
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void ShowFileOpeningError(IBook book)
        {
            var title = _resourceLoader.GetString("FileDamagedDialog_Title");
            var content = _resourceLoader.GetString("FileDamagedDialog_Content");
            var removeButtonCaption = _resourceLoader.GetString("FileDamagedDialog_RemoveButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(removeButtonCaption));
            await dialog.ShowAsync();
            await _provider.RemoveBookAsync(book);
            _navigationService.GoBack();
        }

        private async void LoadedHandler(IBook book)
        {
            IsProgressVisible = true;

            _book = book;
            DjvuDocument document;

            try
            {
                document = await DjvuDocument.LoadAsync(book.Path);
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                IsProgressVisible = false;
                ShowFileOpeningError(book);
                //App.TelemetryClient.TrackException(ex);
                return;
            }

            await _provider.UpdateLastOpeningTimeAsync(book);
            
            Outline = document.GetOutline();

            _bookmarks = new ObservableCollection<IBookmark>(await _provider.GetBookmarksAsync(book));
            _bookmarks.CollectionChanged += (sender, e) => UpdateIsCurrentPageBookmarked();

            CurrentDocument = document;
            var lastOpenedPage = _book.LastOpenedPage;
            if (lastOpenedPage != null)
            {
                CurrentPageNumber = (uint)lastOpenedPage;
            }

            IsProgressVisible = false;
        }

        private void OnCurrentPageNumberChanged()
        {
            UpdateIsCurrentPageBookmarked();
            GoToNextPageCommand.RaiseCanExecuteChanged();
            GoToPreviousPageCommand.RaiseCanExecuteChanged();
        }

        private void UpdateIsCurrentPageBookmarked()
        {
            IsCurrentPageBookmarked = _bookmarks.Any(bookmark => bookmark.PageNumber == CurrentPageNumber);
        }
    }
}
