﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Pdf;
using Wacom.Kiosk.Pdf.Shared;
using Path = System.IO.Path;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for ConfigureDocumentWindow.xaml
    /// </summary>
    public partial class ConfigureDocumentWindow : Window
    {
        private string clientName = "";
        private string ClientIpAddress = "";
        private ILogger logger;

        public ConfigureDocumentWindow(string client, string clientIpAddress, ILogger logger)
        {
            InitializeComponent();
            clientName = client;
            ClientIpAddress = clientIpAddress;
            this.logger = logger;
        }

        /// <summary>Handles the Click event of the button_open control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            string documentPath = textbox_document_path.Text;
            string documentDefinitionPath = textbox_view_definition_path.Text;
            int.TryParse(textbox_page_index.Text, out int pageNumber);
            int.TryParse(textbox_thumbnails_from.Text, out int thumbnailsFrom);
            int.TryParse(textbox_thumbnails_to.Text, out int thumbnailsTo);

            KioskServer.Mq.UpdateClientContext(clientName, pageNumber, documentPath);

            var msg = GenerateOpenDocumentMessage(
                            documentPath,
                            documentDefinitionPath,
                            pageNumber,
                            thumbnailsFrom,
                            thumbnailsTo);

            if (msg == null)
            {
                System.Windows.MessageBox.Show("Wrong data provided. Please check your parameters.");
                Close();
                return;
            }

            if (clientName.Equals("Everyone"))
                KioskServer.Mq.BroadcastMessage(msg.ToByteArray());
            else
                KioskServer.Mq.SendMessage(ClientIpAddress, msg.ToByteArray());

            Close();
        }

        /// <summary>Generates the open document message.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="definitionFilePath">The definition file path.</param>
        /// <param name="pageToParse">The page to parse.</param>
        /// <param name="thumbnailsFrom">The thumbnails from.</param>
        /// <param name="thumbnailsTo">The thumbnails to.</param>
        /// <returns></returns>
        private KioskMessage<OpenDocumentPageMessage> GenerateOpenDocumentMessage(string filePath, string definitionFilePath, int pageToParse, int thumbnailsFrom, int thumbnailsTo)
        {
            try
            {
                using var pdfHelper = new PdfHelper(logger);
                string pageJson = pdfHelper.ParsePage(filePath, pageToParse);
                var page = JsonPdfSerializer.DeserializePage(pageJson, logger);
                using var docViewFileStream = File.OpenText(definitionFilePath);
                var docViewDefinitionString = docViewFileStream.ReadToEnd();
                var thumbsStr = pdfHelper.GetThumbnails(thumbnailsFrom, thumbnailsTo);

                return new OpenDocumentPageMessage(KioskServer.Sender)
                                .WithDefinition(docViewDefinitionString)
                                .WithThumbnails(thumbsStr, thumbnailsTo - thumbnailsFrom + 1)
                                .ForDocumentPage(page, pdfHelper.DocumentPagesCount)
                                .Build();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>Handles the Click event of the button_filepicker_document_path control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void button_filepicker_document_path_Click(object sender, RoutedEventArgs e)
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
                textbox_document_path.Text = openFileDlg.FileName;
                textbox_document_path.CaretIndex = textbox_document_path.Text.Length;
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
                textbox_view_definition_path.Text = openFileDlg.FileName;
                textbox_view_definition_path.CaretIndex = textbox_view_definition_path.Text.Length;
            }
        }
    }
}
