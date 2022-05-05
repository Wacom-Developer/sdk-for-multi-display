using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureKeyboardLayout.xaml
    /// </summary>
    public partial class ConfigureKeyboardLayoutWindow : Window
    {
        private string clientName = "";
        public ConfigureKeyboardLayoutWindow(string client)
        {
            InitializeComponent();
            clientName = client;
        }

        private void JsonFilePickerBtn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                JsonFileTextBox.Text = openFileDlg.FileName;
            }
        }

        private void LayoutImageFilePickerBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                LayoutImageTextBox.Text = openFileDlg.FileName;
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var jsonFileStream = File.OpenText(JsonFileTextBox.Text);
                string layotJsonContent = jsonFileStream.ReadToEnd();
                var picBase64 = Convert.ToBase64String(File.ReadAllBytes(LayoutImageTextBox.Text));


                var msg = new UpdateKeyboardLayoutMessage(KioskServer.Sender)
                    .WithName(LayoutNameTextBox.Text)
                    .WithLayout(layotJsonContent)
                    .WithPicture(picBase64)
                    .Build();

                if (clientName.Equals("Everyone"))
                    KioskServer.Mq.BroadcastMessage(msg.ToByteArray());
                else
                    KioskServer.Mq.SendMessage(clientName, msg.ToByteArray());

                this.Close();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating UpdateKeyboardLayoutMessage:\n{ex.Message}");
            }
        }
    }
}
