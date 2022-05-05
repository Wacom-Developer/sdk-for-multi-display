using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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
using Wacom.Kiosk.Pdf.Shared;
using Wacom.Kiosk.UI.Parsers.Shared;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureSignatureWindow.xaml
    /// </summary>
    public partial class ConfigureSignatureWindow : Window
    {
        private string clientAddress = "";

        public ConfigureSignatureWindow(string client)
        {
            InitializeComponent();
            clientAddress = client;
        }

        private void button_filepicker_document_path_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Definitions")
            };

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                textbox_signature_definition.Text = openFileDlg.FileName;
            }
        }

        private void button_filepicker_signature_config_path_Click(object sender, RoutedEventArgs e)
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
                textbox_signature_config.Text = openFileDlg.FileName;
            }
        }

        private void button_filepicker_signature_backrgound_path_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "media", "images", "default")
            };

            openFileDlg.Filter = "PNG files (*.png)|*.png";

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                textbox_signature_background.Text = openFileDlg.FileName;
            }
        }

        private void button_open_signature_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var signatureDefinitionStream = File.OpenText(textbox_signature_definition.Text);
                string definition = signatureDefinitionStream.ReadToEnd();

                using var signatureConfigStream = File.OpenText(textbox_signature_config.Text);
                string config = signatureConfigStream.ReadToEnd();

                SignatureConfig signatureConfig = JsonConvert.DeserializeObject<SignatureConfig>(config);
                if (!string.IsNullOrEmpty(textbox_Encryption_Certificate.Text))
                {
                    // V1.1
                    //signatureConfig.EncryptionCertificate = new X509Certificate2(textbox_Encryption_Certificate.Text);
                    signatureConfig.EncryptionCertificate = Convert.ToBase64String(File.ReadAllBytes(textbox_Encryption_Certificate.Text));
                }

                var hash = HashingUtility.GetHash("This string will be hashed");
                var msg = new OpenSignatureMessage(KioskServer.Sender);
                msg.WithDefinition(definition);
                msg.WithConfig(signatureConfig);
                msg.WithHash(hash);
                msg.WithFromDocument(false);

                if (!string.IsNullOrEmpty(textbox_signature_background.Text))
                {
                    using var signatureImageStream = File.OpenRead(textbox_signature_background.Text);
                    byte[] bgImgBytes = new byte[signatureImageStream.Length];
                    signatureImageStream.Read(bgImgBytes, 0, bgImgBytes.Length);
                    var imgString = Convert.ToBase64String(bgImgBytes);
                    msg.WithBackgroundImage(imgString);
                }
                if (clientAddress.Equals("Everyone"))
                    KioskServer.Mq.BroadcastMessage(msg.Build().ToByteArray());
                else
                    KioskServer.Mq.SendMessage(clientAddress, msg.Build().ToByteArray());

                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating OpenSignatureMessage:\n{ex.Message}");
            }
        }

        private void button_filepicker_Encryption_Certificate_path_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources")
            };


            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                textbox_Encryption_Certificate.Text = openFileDlg.FileName;
            }
        }
    }
}
