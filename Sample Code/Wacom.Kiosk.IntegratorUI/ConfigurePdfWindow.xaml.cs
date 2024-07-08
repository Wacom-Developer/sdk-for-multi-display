using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Interaction logic for ConfigurePdfWindow.xaml
    /// </summary>
    public partial class ConfigurePdfWindow : Window
    {
        private List<string> selectionOptions = new List<string> { "External link", "Local file" };

        private string clientName = "";

        public ConfigurePdfWindow(string client)
        {
            InitializeComponent();
            clientName = client;
            combobox_pdf_mode.ItemsSource = selectionOptions;
            combobox_pdf_mode.SelectedItem = selectionOptions.First();
            combobox_pdf_mode.SelectionChanged += PdfModeSelectionChange;
            button_filepicker_pdf.IsEnabled = false;
        }

        /// <summary>PDF mode selection change.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void PdfModeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem.ToString().Equals("External link"))
            {
                button_filepicker_pdf.IsEnabled = false;
                textbox_pdf_file_path.Text = "";
            }
            else
            {
                button_filepicker_pdf.IsEnabled = true;
                textbox_pdf_file_path.Text = "";
            }
        }

        /// <summary>Handles the Click event of the button_filepicker_document_definition_path control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void button_filepicker_document_definition_path_Click(object sender, RoutedEventArgs e)
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
                textbox_pdf_file_path.Text = openFileDlg.FileName;
                textbox_pdf_file_path.CaretIndex = textbox_pdf_file_path.Text.Length;
            }
        }

        /// <summary>Handles the Click event of the button_open control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var msg = new OpenPdfMessage(KioskServer.Sender).WithUrl(textbox_pdf_file_path.Text).Build();

                if (clientName.Equals("Everyone"))
                    KioskServer.ServerInstance.BroadcastMessage(msg.ToByteArray());
                else
                    KioskServer.ServerInstance.SendMessage(clientName, msg.ToByteArray());

                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating OpenPdfMessage:\n{ex.Message}");
            }
        }
    }
}
