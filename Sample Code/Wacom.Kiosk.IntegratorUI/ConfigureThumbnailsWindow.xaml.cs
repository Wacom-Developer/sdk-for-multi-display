using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;
using Wacom.Kiosk.Pdf;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureThumbnailsWindow.xaml
    /// </summary>
    public partial class ConfigureThumbnailsWindow : Window
    {
        private readonly string clientName = string.Empty;
        private List<string> thumbnails = new List<string>();

        private ILogger logger;

        public ConfigureThumbnailsWindow(string client, ILogger logger)
        {
            InitializeComponent();
            this.logger = logger;
            clientName = client;
            combobox_thumbs_from.Visibility = Visibility.Hidden;
            combobox_thumbs_to.Visibility = Visibility.Hidden;
            label_from.Visibility = Visibility.Hidden;
            label_to.Visibility = Visibility.Hidden;
            button_update_thumbnails.Visibility = Visibility.Hidden;
        }

        private void button_update_thumbnails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.TryParse(combobox_thumbs_from.SelectedItem?.ToString(), out int thumbnailsFrom);
                int.TryParse(combobox_thumbs_to.SelectedItem?.ToString(), out int thumbnailsTo);

                if (thumbnailsFrom == 0)
                {
                    MessageBox.Show("Invalid start index.");
                    return;
                }

                if (thumbnailsTo != 0 && thumbnailsFrom > thumbnailsTo)
                {
                    MessageBox.Show("Starting index should be lesser than the end index.");
                    return;
                }

                if (thumbnails == null || thumbnails.Count == 0)
                {
                    MessageBox.Show("No thumbnails parsed from document");
                    return;
                }

                if (thumbnailsTo == 0)
                {
                    KioskMessage<UpdateThumbnailsMessage> msgUpdateSingle = new UpdateThumbnailsMessage(KioskServer.Sender).WithData(thumbnailsFrom, thumbnails.ElementAt(thumbnailsFrom - 1)).Build();
                    if (clientName.Equals("Everyone"))
                        KioskServer.Mq.BroadcastMessage(msgUpdateSingle.ToByteArray());
                    else
                        KioskServer.Mq.SendMessage(clientName, msgUpdateSingle.ToByteArray());

                    Close();
                    return;
                }


                Dictionary<int, string> thumbnailsData = new Dictionary<int, string>();

                for (int i = thumbnailsFrom; i < thumbnailsTo + 1; i++)
                {
                    thumbnailsData.Add(i, thumbnails[i - 1]);
                }

                KioskMessage<UpdateThumbnailsMessage> msgUpdateMultiple = new UpdateThumbnailsMessage(KioskServer.Sender).WithData(thumbnailsData).Build();

                if (clientName.Equals("Everyone"))
                    KioskServer.Mq.BroadcastMessage(msgUpdateMultiple.ToByteArray());
                else
                    KioskServer.Mq.SendMessage(clientName, msgUpdateMultiple.ToByteArray());

                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception generating UpdateThumbnailsMessage:\n{ex.Message}");
            }
        }

        private void button_filepicker_thumbnail_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog()
            {
                InitialDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Documents")
            };

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                ExtractDocumentThumbnails(openFileDlg.FileName);
            }

        }

        private void ExtractDocumentThumbnails(string filePath)
        {
            PdfHelper pdfHelper = new PdfHelper(logger).Initialize(filePath);

            thumbnails = JsonConvert.DeserializeObject<List<string>>(pdfHelper.GetThumbnails(1, pdfHelper.DocumentPagesCount));

            List<string> availablePagesListFrom = Enumerable.Range(1, pdfHelper.DocumentPagesCount).Select(idx => idx.ToString()).ToList();
            List<string> availablePagesListTo = Enumerable.Range(1, pdfHelper.DocumentPagesCount).Select(idx => idx.ToString()).ToList();

            combobox_thumbs_from.ItemsSource = availablePagesListFrom;
            combobox_thumbs_from.SelectedItem = availablePagesListFrom.First();
            availablePagesListTo.Insert(0, "NONE");
            combobox_thumbs_to.ItemsSource = availablePagesListTo;
            combobox_thumbs_to.SelectedItem = availablePagesListTo.First();

            combobox_thumbs_from.Visibility = Visibility.Visible;
            combobox_thumbs_to.Visibility = Visibility.Visible;
            label_from.Visibility = Visibility.Visible;
            label_to.Visibility = Visibility.Visible;
            button_update_thumbnails.Visibility = Visibility.Visible;
        }
    }
}
