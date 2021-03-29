using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wacom.Kiosk.Integrator;
using Wacom.Kiosk.Message.Handler;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;
using Wacom.Kiosk.Message.Shared.SDKMessage.Tablet;
using Wacom.Kiosk.Pdf;
using Wacom.Kiosk.Pdf.Shared;
using Wacom.Kiosk.UI.Parsers.Shared;
using Page = Wacom.Kiosk.Pdf.Shared.Page;
using Path = System.IO.Path;
using Wacom.Kiosk.UI.Parsers;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string defaultSignatureDefinition = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""     xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""     xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""     xmlns:local=""clr-namespace:Wacom.Kiosk.App"" mc:Ignorable=""d"" x:Name=""DocumentView"" Title=""DocumentView"" WindowStyle=""None"" Width=""1920"" Height=""1080"">    <Grid>        <Grid Name=""SignatureContainer""></Grid>        <Grid Panel.ZIndex=""2"" HorizontalAlignment=""Right"" Width=""150"" Background=""Transparent"">            <StackPanel Panel.ZIndex=""2"" Orientation=""Vertical"">                <Button x:Name=""AcceptSignature"" Content=""AcceptSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>                <Button x:Name=""CancelSignature"" Content=""CancelSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>                <Button x:Name=""ClearSignature"" Content=""ClearSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>            </StackPanel>        </Grid>    </Grid></Window>";

        /// <summary>The selected client</summary>
        private string selectedClient = "";
        //        private string selectedClient = "Everyone";
        /// <summary>The idle mode image</summary>
        private bool idleModeImage = true;

        private ILogger logger;
        bool bMirroring = false;
        bool bPrivacy = false;
        public MainWindow(ILogger logger)
        {
            InitializeComponent();
            RegisterTabletMessageHandlers();

            this.logger = logger;

            //combobox_clients.Items.Add(selectedClient);
            //combobox_clients.SelectedItem = selectedClient;
            combobox_clients.SelectionChanged += ClientSelectionChange;
            combobox_idlemode.ItemsSource = new List<string> { "Image mode", "Video mode" };
            combobox_idlemode.SelectedItem = "Image mode";
            combobox_idlemode.SelectionChanged += IdleModeSelectionChange;

            KioskServer.Mq.Logger = this.logger;
            KioskServer.Mq.OnClientConnected += OnClientConnected;
            KioskServer.Mq.OnClientDisconnected += OnClientDisconnected;
            KioskServer.Mq.OnSubscriberMessageReceived += OnMessageReceived;
        }

        private void OnClientDisconnected(object sender, string clientName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                combobox_clients.Items.Remove(KioskServer.Mq.ActiveClients.Where(el => el.Name.Equals(clientName)).FirstOrDefault()?.Name);
            });
        }

        private void OnClientConnected(object sender, byte[] e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cert = new X509Certificate2("MyServer.pfx", "password");
                var client = KioskServer.Mq.ActiveClients.Last();
                string license = ConfigurationManager.AppSettings.Get("license");
                KioskServer.SendMessage(client.ClientAddress, new ClientAcceptedMessage(new KioskClient("Integrator"), client.Name)
                    .WithLicense(license)
                    .WithCertificate(Convert.ToBase64String(cert.RawData))
                    .Build().ToByteArray());
                combobox_clients.Items.Add(client?.Name);
                selectedClient = client?.Name;
                combobox_clients.SelectedItem = selectedClient;
            });
        }

        /// <summary>Idle mode selection change.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void IdleModeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            idleModeImage = (sender as ComboBox).SelectedItem.ToString().Equals("Image mode");
        }

        /// <summary>Client selection change.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ClientSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            string selectedClientName = (sender as ComboBox)?.SelectedItem?.ToString();
            selectedClient = KioskServer.Mq.ActiveClients.Where(el => el.Name.Equals(selectedClientName)).FirstOrDefault()?.ClientAddress;
        }

        /// <summary>Handles the Click event of the btn_clearconsole control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_clearconsole_Click(object sender, RoutedEventArgs e)
        {
            textblock_log.Text = String.Empty;
        }

        /// <summary>Handles the Click event of the btn_idle control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_idle_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(
                    new OpenIdleMessage(KioskServer.Sender)
                                .WithImagesOrVideos(idleModeImage ? "images" : "videos")
                                .WithDefaultOrCustomGroup(IdleCustDef.Text)
                                .WithSlideShowInterval(3)
                                .Build()
                                .ToByteArray());
        }

        /// <summary>Handles the Click event of the btn_web control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_web_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(
                new OpenWebMessage(KioskServer.Sender)
                              .WithUrl(webbrowser_url.Text)
                              .Build()
                              .ToByteArray());
        }

        /// <summary>Handles the Click event of the btn_pdf control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_pdf_Click(object sender, RoutedEventArgs e)
        {
            ConfigurePdfWindow pdfConfigurationWindow = new ConfigurePdfWindow(selectedClient);
            pdfConfigurationWindow.Activate();
            pdfConfigurationWindow.Show();
        }


        private void btn_update_media_Click(object sender, RoutedEventArgs e)
        {
            ConfigureIdleWindow idleConfigWindow = new ConfigureIdleWindow(selectedClient);
            idleConfigWindow.Activate();
            idleConfigWindow.Show();
        }

        /// <summary>Handles the Click event of the btn_update_thumbnails control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_update_thumbnails_Click(object sender, RoutedEventArgs e)
        {
            ConfigureThumbnailsWindow thumbnailsConfigurationWindow = new ConfigureThumbnailsWindow(selectedClient, logger);
            thumbnailsConfigurationWindow.Activate();
            thumbnailsConfigurationWindow.Show();
        }

        private void btn_signature_Click(object sender, RoutedEventArgs e)
        {
            var signatureConfigurationWindow = new ConfigureSignatureWindow(selectedClient);
            signatureConfigurationWindow.Activate();
            signatureConfigurationWindow.Show();
        }

        /// <summary>Handles the Click event of the btn_doc control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btn_doc_Click(object sender, RoutedEventArgs e)
        {
            ActiveClient activeClient = KioskServer.Mq.ActiveClients.Where(el => el.ClientAddress.Equals(selectedClient)).FirstOrDefault();
            if (activeClient != null)
            {
                ConfigureDocumentWindow documentConfigurationWindow = new ConfigureDocumentWindow(activeClient.Name, activeClient.ClientAddress, logger);
                documentConfigurationWindow.Activate();
                documentConfigurationWindow.Show();
            }
            else
            {
                MessageBox.Show("No Client Connected Found");
            }
        }

        private void btnUpdateLayout_Click(object sender, RoutedEventArgs e)
        {
            ConfigureKeyboardLayoutWindow keyboardLayoutConfigurationWindow = new ConfigureKeyboardLayoutWindow(selectedClient);
            keyboardLayoutConfigurationWindow.Activate();
            keyboardLayoutConfigurationWindow.Show();
        }

        /// <summary>Called when [message received].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SubscriberMessageEventArgs"/> instance containing the event data.</param>
        private void OnMessageReceived(object sender, byte[] messageBytes)
        {
            object msg = KioskMessageFactory.FromByteArray(messageBytes);
            MessageHandlers.HandleMessage(msg);
        }

        /// <summary>Registers the tablet message handlers.</summary>
        private void RegisterTabletMessageHandlers()
        {
            var applicationExitHandler = new MessageHandler<ApplicationExitMessage>((msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    combobox_clients.Items.Remove(msg.Sender.Name);
                });

                AppendLog(msg.ToString());
            });
            MessageHandlers.RegisterHandler(applicationExitHandler, logger);

            MessageHandlers.RegisterHandler(new MessageHandler<NotLicensedMessage>((msg) =>
            {
                AppendLog("Feature not licensed");
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<OperationFailedMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<OperationSuccessMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<InitializeConnectionMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<InputChangedMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureClickedMessage>((msg) =>
            {
                SignatureConfig signatureConfig = new SignatureConfig()
                {
                    X = 0,
                    Y = 0,
                    Width = 1200,
                    Height = 700,
                    AreaPercent = 0.8,
                    PenTrackingType = PenTrackingType.Limited,
                    SignatureViewType = SignatureViewType.External,
                    SignatureFormat = SignatureFormat.Fss
                };
                var hash = HashingUtility.GetHash("This string will be hashed");
                SendMessage(new OpenSignatureMessage(KioskServer.Sender).WithDefinition(defaultSignatureDefinition).WithConfig(signatureConfig).WithHash(hash).WithFromDocument(true).Build().ToByteArray());
            }), logger);


            MessageHandlers.RegisterHandler(new MessageHandler<ButtonClickedMessage>((msg) =>
            {
                AppendLog(msg.ToString());

                if (msg.ButtonName.Equals("PageNext"))
                    ChangePage(true, msg.Sender.Name);

                if (msg.ButtonName.Equals("PageBack"))
                    ChangePage(false, msg.Sender.Name);

                if (msg.ButtonName.Equals("OpenMirroring"))
                {
                    SendMessage(new StartMirroringMessage(KioskServer.Sender).Build().ToByteArray());
                }

                if (msg.ButtonName.Equals("CloseMirroring"))
                    SendMessage(new StopMirroringMessage(KioskServer.Sender).Build().ToByteArray());

                if (msg.ButtonName.Equals("DocumentAccepted"))
                    SendMessage(new GetPageDataMessage(KioskServer.Sender).Build().ToByteArray());

                if (msg.ButtonName.Equals("DocumentRejected"))
                    SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<ErrorTabletMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);


            MessageHandlers.RegisterHandler(new MessageHandler<ThumbnailClickedMessage>((msg) =>
            {
                ActiveClient activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
                if (activeClient.DocumentContext.DocumentPageNumber == msg.PageNumber) return;

                ChangeDocumentPage(activeClient, msg.PageNumber);
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<PageDataMessage>((msg) =>
            {
                AppendLog(msg.ToString());
                SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
            }), logger);
                       
            MessageHandlers.RegisterHandler(new MessageHandler<SignatureAcceptedMessage>((msg) =>
            {
                AppendLog(msg.ToString());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var signaturePopupWindow = new SignatureImagePopupWindow(JsonPdfSerializer.ByteArrayToBitmap(msg.SignaturePictureBytes));
                    signaturePopupWindow.Activate();
                    signaturePopupWindow.Show();
                });

                var activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
                if (msg.FromDocument && !string.IsNullOrEmpty(activeClient.DocumentContext.DocumentPath))
                {
                    ChangeDocumentPage(activeClient, activeClient.DocumentContext.DocumentPageNumber);
                }
                else
                {
                    SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
                }
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureCancelledMessage>((msg) =>
            {
                AppendLog(msg.ToString());


                var activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
                if (msg.FromDocument && !string.IsNullOrEmpty(activeClient.DocumentContext.DocumentPath))
                {
                    ChangeDocumentPage(activeClient, activeClient.DocumentContext.DocumentPageNumber);
                }
                else
                {
                    SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
                }
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureWrongFormatMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<StopMirroringMessage>((msg) =>
            {
                bMirroring = false;
                AppendLog(msg.ToString());
            }), logger);
        }

        private void ChangePage(bool changeToNextPage, string clientName)
        {
            ActiveClient activeClient = KioskServer.GetActiveClient(clientName);

            int pageToParse = changeToNextPage
                ? activeClient.DocumentContext.DocumentPageNumber + 1
                : activeClient.DocumentContext.DocumentPageNumber - 1;

            if (pageToParse < 1) return;

            ChangeDocumentPage(activeClient, pageToParse);
        }

        private void ChangeDocumentPage(ActiveClient activeClient, int pageToParse)
        {
            try
            {
                var pdfHelper = new PdfHelper(logger);
                string pageJson = pdfHelper.ParsePage(activeClient.DocumentContext.DocumentPath, pageToParse);
                Page page = JsonPdfSerializer.DeserializePage(pageJson, logger);

                activeClient.UpdateDocumentContext(pageToParse, activeClient.DocumentContext.DocumentPath);

                SendMessage(new OpenDocumentPageMessage(KioskServer.Sender)
                                    .ForDocumentPage(page)
                                    .Build()
                                    .ToByteArray());
            }
            catch (Exception ex)
            {
                // TODO: LOG AND THROW EXCEPTION
                // pageToParse is bigger than the document page count.
                return;
            }
        }

        /// <summary>Appends the log.</summary>
        /// <param name="str">The string.</param>
        private void AppendLog(string msg)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                string now = DateTimeOffset.UtcNow.ToString();
                textblock_log.Text += $"{msg}";

                textblock_log.Text += Environment.NewLine;
                scroll_log.ScrollToBottom();
            });
        }

        /// <summary>Sends the message.</summary>
        /// <param name="bytes">The bytes.</param>
        private void SendMessage(byte[] bytes)
        {
            // If no clients are connected - don't try to send message.
            if (KioskServer.Mq.ActiveClients.Count < 1) return;

            //if (selectedClient.Equals("Everyone"))
            //{
            //    KioskServer.BroadcastMessage(bytes);
            //    return;
            //}

            KioskServer.SendMessage(selectedClient, bytes);
        }

        private void SetStateButton_Click(object sender, RoutedEventArgs e)
        {
            string elName = ElementName.Text;
            bool isEnabled = IsEnabled.IsChecked ?? false;
            SendMessage(new SetElementEnabledMessage(KioskServer.Sender)
                .WithName(elName)
                .WithState(isEnabled)
                .Build()
                .ToByteArray());
        }

        private void UpdateConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigureAppConfig appConfigUpdateWindow = new ConfigureAppConfig(selectedClient);
            appConfigUpdateWindow.Activate();
            appConfigUpdateWindow.Show();
        }

        private void btn_Mirror_Click(object sender, RoutedEventArgs e)
        {
            if (!bMirroring)
            {
                bMirroring = true;
                SendMessage(new StartMirroringMessage(KioskServer.Sender).Build().ToByteArray());
            }
            else
            {
                bMirroring = false;
                SendMessage(new StopMirroringMessage(KioskServer.Sender).Build().ToByteArray());
            }

        }

        private void btn_Privacy_Click(object sender, RoutedEventArgs e)
        {
            SetMirroringScreenBlackMessage mirroringBlack = new SetMirroringScreenBlackMessage(KioskServer.Sender);

            if (!bPrivacy)
            {
                mirroringBlack.IsActive(true);
                bPrivacy = true;
                // miss hide on sdk
            }
            else
            {
                mirroringBlack.IsActive(false);
                bPrivacy = false;
            }

            SendMessage(mirroringBlack.Build().ToByteArray());
        }
    }
}
