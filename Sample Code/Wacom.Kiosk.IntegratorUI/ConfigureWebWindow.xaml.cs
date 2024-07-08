using System;
using System.Windows;

using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureWebWindow.xaml
    /// </summary>
    public partial class ConfigureWebWindow : Window
    {
        private readonly string _clientName = "";
        private static string _url = string.Empty;
        private static bool? _incognitoMode = null;

        public ConfigureWebWindow(string client)
        {
            InitializeComponent();

            _clientName = client;
        }

        /// <summary>Handles the Click event of the button_open control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var msg = new OpenWebMessage(KioskServer.Sender)
                                .WithUrl(txtUrl.Text);

                _url = txtUrl.Text;
                _incognitoMode = chkIncognitoMode.IsChecked;

                if (_incognitoMode.HasValue) 
                { 
                    msg.InIncognitoMode(chkIncognitoMode.IsChecked.Value);
                }

                byte[] msgBytes = msg.Build().ToByteArray();

                if (_clientName.Equals("Everyone"))
                    KioskServer.ServerInstance.BroadcastMessage(msgBytes);
                else
                    KioskServer.ServerInstance.SendMessage(_clientName, msgBytes);

                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating OpenWebMessage:\n{ex.Message}");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            txtUrl.Text = _url;
            chkIncognitoMode.IsChecked = _incognitoMode;
        }
    }
}
