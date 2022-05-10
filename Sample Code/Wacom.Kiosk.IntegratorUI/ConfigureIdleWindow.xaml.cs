using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureIdleWindow.xaml
    /// </summary>
    public partial class ConfigureIdleWindow : Window
    {
        private readonly string clientName = "";
        private List<string> fileNames;

        public ConfigureIdleWindow(string client)
        {
            InitializeComponent();
            clientName = client;

            List<string> updateOptionsList = new List<string>()
            {
                "images",
                "videos"
            };

            combobox_update_type.ItemsSource = updateOptionsList;
            combobox_update_type.SelectedItem = updateOptionsList.First();
        }

        private void button_filepicker_media_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                Multiselect = true,
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "media", "images", "default")
            };

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                fileNames = openFileDlg.FileNames.ToList();
            }
            button_update.IsEnabled = fileNames.Count > 0;
        }

        private void button_update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string mediaSelection = combobox_update_type.SelectedItem.ToString().ToLowerInvariant();
                string mediaGroup = text_box_media_group.Text;

                using var updateIdleMediaMessage = new UpdateIdleMediaMessage(KioskServer.Sender)
                                            .WithMediaType(mediaSelection)
                                            .WithGroupName(mediaGroup)
                                            .WithFileNames(fileNames);

                KioskMessage<UpdateIdleMediaMessage> msg = updateIdleMediaMessage.Build();

                if (clientName.Equals("Everyone"))
                    KioskServer.Mq.BroadcastMessage(msg.ToByteArray());
                else
                    KioskServer.Mq.SendMessage(clientName, msg.ToByteArray());

                Close();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating UpdateIdleMediaMessage:\n{ex.Message}");
            }
        }
    }
}
