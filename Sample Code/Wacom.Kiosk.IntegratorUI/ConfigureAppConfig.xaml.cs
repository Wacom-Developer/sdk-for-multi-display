using System;
using System.IO;
using System.Windows;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureAppConfig.xaml
    /// </summary>
    public partial class ConfigureAppConfig : Window
    {
        private readonly string clientName = "";
        public ConfigureAppConfig(string client)
        {
            InitializeComponent();
            clientName = client;
        }
        private void AppConfigFilePicker_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Config")
            };

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                textbox_app_config_path.Text = openFileDlg.FileName;
                textbox_app_config_path.CaretIndex = textbox_app_config_path.Text.Length;
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            using StreamReader configFileStream = File.OpenText(textbox_app_config_path.Text);
            string jsonConfigFile = configFileStream.ReadToEnd();

            KioskMessage<UpdateConfigurationMessage> msg = new UpdateConfigurationMessage(KioskServer.Sender)
                .WithJsonString(jsonConfigFile)
                .Build();

            if (clientName.Equals("Everyone"))
                KioskServer.Mq.BroadcastMessage(msg.ToByteArray());
            else
                KioskServer.Mq.SendMessage(clientName, msg.ToByteArray());

            this.Close();
        }
    }
}
