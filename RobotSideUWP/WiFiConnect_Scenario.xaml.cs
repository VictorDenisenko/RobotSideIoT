using RobotSideUWP;
using System;
using System.Collections.ObjectModel;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RobotSideUWP
{
    public sealed partial class WiFiConnect_Scenario : Page
    {
        MainPage rootPage;
        private WiFiAdapter firstAdapter;
        public ObservableCollection<WiFiNetworkDisplay> ResultCollection
        {
            get;
            private set;
        }

        public WiFiConnect_Scenario()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                ResultCollection = new ObservableCollection<WiFiNetworkDisplay>();
                rootPage = MainPage.Current;
                var access = await WiFiAdapter.RequestAccessAsync();
                if (access != WiFiAccessStatus.Allowed)
                {
                    rootPage.NotifyUser("Access denied", NotifyType.ErrorMessage);
                }
                else
                {
                    DataContext = this;

                    var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                    if (result.Count >= 1)
                    {
                        firstAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);

                        var button = new Button();
                        button.Content = string.Format("Scan Available Wifi Networks");
                        button.Click += Button_Click;
                        Buttons.Children.Add(button);
                    }
                    else
                    {
                        rootPage.NotifyUser("No WiFi Adapters detected on this machine.", NotifyType.ErrorMessage);
                    }
                }
            }
            catch(Exception ex)
            {
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await firstAdapter.ScanAsync();
                ConnectionBar.Visibility = Visibility.Collapsed;
                DisplayNetworkReport(firstAdapter.NetworkReport);
            }
            catch(Exception ex)
            { }
        }

        private void DisplayNetworkReport(WiFiNetworkReport report)
        {
            try
            {
                rootPage.NotifyUser(string.Format("Network Report Timestamp: {0}", report.Timestamp), NotifyType.StatusMessage);
                ResultCollection.Clear();
                foreach (var network in report.AvailableNetworks)
                {
                    ResultCollection.Add(new WiFiNetworkDisplay(network, firstAdapter));
                }
            }
            catch(Exception ex)
            { }
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedNetwork = ResultsListView.SelectedItem as WiFiNetworkDisplay;
                if (selectedNetwork == null)
                {
                    return;
                }
                ConnectionBar.Visibility = Visibility.Visible;

                if (selectedNetwork.AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211 &&
                        selectedNetwork.AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None)
                {
                    NetworkKeyInfo.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NetworkKeyInfo.Visibility = Visibility.Visible;
                }
            }
            catch(Exception ex)
            { }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedNetwork = ResultsListView.SelectedItem as WiFiNetworkDisplay;
                if (selectedNetwork == null || firstAdapter == null)
                {
                    rootPage.NotifyUser("Network not selected", NotifyType.ErrorMessage);
                    return;
                }
                WiFiReconnectionKind reconnectionKind = WiFiReconnectionKind.Manual;
                reconnectionKind = WiFiReconnectionKind.Automatic;

                WiFiConnectionResult result;
                if (selectedNetwork.AvailableNetwork.SecuritySettings.NetworkAuthenticationType == Windows.Networking.Connectivity.NetworkAuthenticationType.Open80211 &&
                        selectedNetwork.AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None)
                {
                    result = await firstAdapter.ConnectAsync(selectedNetwork.AvailableNetwork, reconnectionKind);
                }
                else
                {
                    var credential = new PasswordCredential();
                    credential.Password = NetworkKey.Password;

                    result = await firstAdapter.ConnectAsync(selectedNetwork.AvailableNetwork, reconnectionKind, credential);
                }

                if (result.ConnectionStatus == WiFiConnectionStatus.Success)
                {
                    rootPage.NotifyUser(string.Format("Successfully connected to {0}.", selectedNetwork.Ssid), NotifyType.StatusMessage);
                    webViewGrid.Visibility = Visibility.Visible;
                    toggleBrowserButton.Content = "Hide Browser Control";
                    refreshBrowserButton.Visibility = Visibility.Visible;

                }
                else
                {
                    rootPage.NotifyUser(string.Format("Could not connect to {0}. Error: {1}", selectedNetwork.Ssid, result.ConnectionStatus), NotifyType.ErrorMessage);
                }
                foreach (var network in ResultCollection)
                {
                    network.UpdateConnectivityLevel();
                }
            }
            catch(Exception ex)
            { }
        }

        private void Browser_Toggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (webViewGrid.Visibility == Visibility.Visible)
                {
                    webViewGrid.Visibility = Visibility.Collapsed;
                    refreshBrowserButton.Visibility = Visibility.Collapsed;
                    toggleBrowserButton.Content = "Show Browser Control";
                }
                else
                {
                    webViewGrid.Visibility = Visibility.Visible;
                    refreshBrowserButton.Visibility = Visibility.Visible;
                    toggleBrowserButton.Content = "Hide Browser Control";
                }
            }
            catch(Exception ex)
            { }
        }
        private void Browser_Refresh(object sender, RoutedEventArgs e)
        {
            try
            {
                webView.Refresh();
            }
            catch(Exception ex)
            { }
        }
    }
}

