using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Djvu;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DjvuApp.ViewModel
{
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

        public uint TotalPageNumber
        {
            get
            {
                return _totalPageNumber;
            }

            private set
            {
                if (_totalPageNumber != value)
                {
                    _totalPageNumber = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand ShowOutlineCommand { get; }

        public ICommand JumpToPageCommand { get; }

        public RelayCommand AddBookmarkCommand { get; }

        public RelayCommand RemoveBookmarkCommand { get; }

        public RelayCommand ShowBookmarksCommand { get; }

        public RelayCommand GoToNextPageCommand { get; }

        public RelayCommand GoToPreviousPageCommand { get; }

        public ICommand ShareCommand { get; }

        private bool _isProgressVisible;
        private bool _isCurrentPageBookmarked;
        private DjvuDocument _currentDocument;
        private uint _currentPageNumber;
        private uint _totalPageNumber;
        private IReadOnlyList<DjvuOutlineItem> _outline;

        private readonly DataTransferManager _dataTransferManager;
        private readonly IBookProvider _provider;
        private readonly INavigationService _navigationService;
        private readonly ResourceLoader _resourceLoader;
        private ObservableCollection<IBookmark> _bookmarks;
        private IBook _book;
        private IStorageFile _file;

        public ViewerViewModel(IBookProvider provider, INavigationService navigationService)
        {
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _provider = provider;
            _navigationService = navigationService;
            _resourceLoader = ResourceLoader.GetForCurrentView();

            ShowOutlineCommand = new RelayCommand(ShowOutline, () => _outline != null);
            JumpToPageCommand = new RelayCommand(ShowJumpToPageDialog);
            AddBookmarkCommand = new RelayCommand(AddBookmark, () => _book != null);
            RemoveBookmarkCommand = new RelayCommand(RemoveBookmark, () => _book != null);
            ShowBookmarksCommand = new RelayCommand(ShowBookmarks, () => _book != null);
            ShareCommand = new RelayCommand(DataTransferManager.ShowShareUI);

            GoToNextPageCommand = new RelayCommand(
                () => CurrentPageNumber++, 
                () => CurrentDocument != null && CurrentPageNumber < CurrentDocument.PageCount);
            GoToPreviousPageCommand = new RelayCommand(
                () => CurrentPageNumber--, 
                () => CurrentPageNumber > 1);
        }

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoadedHandler(e.Parameter);

            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.Title = _book?.Title ?? _file.Name;

            _dataTransferManager.DataRequested += DataRequestedHandler;
            CoreApplication.Suspending += ApplicationSuspendingHandler;
        }

        public async void OnNavigatedFrom(NavigationEventArgs e)
        {
            var applicationView = ApplicationView.GetForCurrentView();
            applicationView.Title = string.Empty;

            _dataTransferManager.DataRequested -= DataRequestedHandler;
            CoreApplication.Suspending -= ApplicationSuspendingHandler;
            await SaveLastOpenedPageAsync();
        }

        private async void ApplicationSuspendingHandler(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SaveLastOpenedPageAsync();
            deferral.Complete();
        }

        private async Task SaveLastOpenedPageAsync()
        {
            if (_book == null || CurrentPageNumber == 0)
                return;

            await _provider.UpdateLastOpenedPageAsync(_book, CurrentPageNumber);
        }

        private void DataRequestedHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            e.Request.Data.Properties.Title = _book?.Title ?? _file.Name;
            e.Request.Data.SetStorageItems(new IStorageItem[] {_file}, true);

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
            var pageNumber = await OutlineDialog.ShowAsync(_outline);
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

        private async void ShowFileOpeningError()
        {
            var title = _resourceLoader.GetString("DocumentOpeningErrorDialog_Title");
            var content = _resourceLoader.GetString("DocumentOpeningErrorDialog_Content");
            var buttonCaption = _resourceLoader.GetString("DocumentOpeningErrorDialog_OkButton_Caption");

            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand(buttonCaption));
            await dialog.ShowAsync();

            _navigationService.GoBack();
        }

        private async Task LoadedHandler(object navigationParameter)
        {
            IsProgressVisible = true;

            _book = navigationParameter as IBook;
            if (_book != null)
            {
                _file = await StorageFile.GetFileFromPathAsync(_book.Path);
            }
            else if (navigationParameter is IStorageFile)
            {
                _file = (IStorageFile) navigationParameter;
            }
            else
            {
                throw new Exception("Invalid parameter.");
            }
            
            DjvuDocument document;

            try
            {
                document = await DjvuDocument.LoadAsync(_file);
            }
            catch (Exception)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                IsProgressVisible = false;
                ShowFileOpeningError();
                return;
            }
            
            CurrentDocument = document;
            CurrentPageNumber = _book?.LastOpenedPage ?? 1;
            TotalPageNumber = document.PageCount;

            if (_book != null)
            {
                await _provider.UpdateLastOpeningTimeAsync(_book);

                _bookmarks = new ObservableCollection<IBookmark>(await _provider.GetBookmarksAsync(_book));
                _bookmarks.CollectionChanged += (sender, e) => UpdateIsCurrentPageBookmarked();
            }

            _outline = await document.GetOutlineAsync();

            IsProgressVisible = false;
            
            ShowOutlineCommand.RaiseCanExecuteChanged();
            AddBookmarkCommand.RaiseCanExecuteChanged();
            RemoveBookmarkCommand.RaiseCanExecuteChanged();
            ShowBookmarksCommand.RaiseCanExecuteChanged();
        }

        private void OnCurrentPageNumberChanged()
        {
            UpdateIsCurrentPageBookmarked();
            GoToNextPageCommand.RaiseCanExecuteChanged();
            GoToPreviousPageCommand.RaiseCanExecuteChanged();
        }

        private void UpdateIsCurrentPageBookmarked()
        {
            if (_bookmarks != null)
            {
                IsCurrentPageBookmarked = _bookmarks.Any(bookmark => bookmark.PageNumber == CurrentPageNumber);
            }
        }
    }
}
