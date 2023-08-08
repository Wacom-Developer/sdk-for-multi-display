using System;
using System.ComponentModel;
using System.Windows;

using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureWebWindow.xaml
    /// </summary>
    public partial class ConfigureBrowserDataWindow : Window
    {
        private readonly string _clientName = "";
        private static string _cookieUrl = string.Empty;
        private static string _cookieName = string.Empty;

        public ConfigureBrowserDataWindow(string client)
        {
            InitializeComponent();

            _clientName = client;
        }

        private void OnClearCookiesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] msgBytes = new ClearBrowserCookiesMessage(KioskServer.Sender)
                                        .WithUrl(txtCookieUrl.Text)
                                        .WithName(txtCookieName.Text)
                                        .Build().ToByteArray();

                if (_clientName.Equals("Everyone"))
                    KioskServer.Mq.BroadcastMessage(msgBytes);
                else
                    KioskServer.Mq.SendMessage(_clientName, msgBytes);

                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating ClearBrowserCookiesMessage:\n{ex.Message}");
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _cookieUrl = txtCookieUrl.Text;
            _cookieName = txtCookieName.Text;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            txtCookieUrl.Text = _cookieUrl;
            txtCookieName.Text = _cookieName;
        }
    }
}
