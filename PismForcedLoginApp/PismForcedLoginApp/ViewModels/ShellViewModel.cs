﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PismForcedLoginApp.Core.Services;
using PismForcedLoginApp.Helpers;
using PismForcedLoginApp.Services;
using PismForcedLoginApp.Views;

using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using WinUI = Microsoft.UI.Xaml.Controls;

namespace PismForcedLoginApp.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isLoggedIn;
        private static INavigationService _navigationService;
        private IIdentityService _identityService;
        private UserDataService _userDataService;
        private WinUI.NavigationView _navigationView;
        private bool _isBackEnabled;
        private WinUI.NavigationViewItem _selected;
        private UserViewModel _user;

        public ICommand ItemInvokedCommand { get; }

        public ICommand LoginCommand { get; }

        public ICommand UserProfileCommand { get; }

        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set { SetProperty(ref _isLoggedIn, value); }
        }

        public bool IsBackEnabled
        {
            get { return _isBackEnabled; }
            set { SetProperty(ref _isBackEnabled, value); }
        }

        public WinUI.NavigationViewItem Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        public UserViewModel User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        public ShellViewModel(INavigationService navigationServiceInstance, UserDataService userDataService, IIdentityService identityService)
        {
            _navigationService = navigationServiceInstance;
            _userDataService = userDataService;
            _identityService = identityService;
            ItemInvokedCommand = new DelegateCommand<WinUI.NavigationViewItemInvokedEventArgs>(OnItemInvoked);
            LoginCommand = new DelegateCommand(OnLogin);
            UserProfileCommand = new DelegateCommand(OnUserProfile);
        }

        public async void Initialize(Frame frame, WinUI.NavigationView navigationView)
        {
            _navigationView = navigationView;
            frame.NavigationFailed += (sender, e) =>
            {
                throw e.Exception;
            };
            frame.Navigated += Frame_Navigated;
            _navigationView.BackRequested += OnBackRequested;
            _identityService.LoggedIn += OnLoggedIn;
            _identityService.LoggedOut += OnLoggedOut;
            IsLoggedIn = _identityService.IsLoggedIn();
            await GetUserData();
        }

        private async void OnLoggedIn(object sender, EventArgs e)
        {
            IsLoggedIn = true;
            await GetUserData();
        }

        private async void OnLoggedOut(object sender, EventArgs e)
        {
            await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                IsLoggedIn = false;
                _navigationService.Navigate(PageTokens.MainPage, null);
                _navigationService.ClearHistory();
            });
        }

        private async Task GetUserData()
        {
            if (IsLoggedIn)
            {
                User = await _userDataService.GetUserFromCacheAsync();
                User = await _userDataService.GetUserFromGraphApiAsync();
            }
        }

        private void OnItemInvoked(WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                _navigationService.Navigate("Settings", null);
                return;
            }

            var item = _navigationView.MenuItems
                            .OfType<WinUI.NavigationViewItem>()
                            .First(menuItem => (string)menuItem.Content == (string)args.InvokedItem);
            var pageKey = item.GetValue(NavHelper.NavigateToProperty) as string;
            _navigationService.Navigate(pageKey, null);
        }

        private async void OnLogin()
        {
            await _identityService.LoginAsync();
        }

        private void OnUserProfile()
        {
            _navigationService.Navigate(PageTokens.SettingsPage, null);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            IsBackEnabled = _navigationService.CanGoBack();
            if (e.SourcePageType == typeof(SettingsPage))
            {
                Selected = _navigationView.SettingsItem as WinUI.NavigationViewItem;
                return;
            }

            Selected = _navigationView.MenuItems
                            .OfType<WinUI.NavigationViewItem>()
                            .FirstOrDefault(menuItem => IsMenuItemForPageType(menuItem, e.SourcePageType));
        }

        private void OnBackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            _navigationService.GoBack();
        }

        private bool IsMenuItemForPageType(WinUI.NavigationViewItem menuItem, Type sourcePageType)
        {
            var sourcePageKey = sourcePageType.Name;
            sourcePageKey = sourcePageKey.Substring(0, sourcePageKey.Length - 4);
            var pageKey = menuItem.GetValue(NavHelper.NavigateToProperty) as string;
            return pageKey == sourcePageKey;
        }
    }
}
