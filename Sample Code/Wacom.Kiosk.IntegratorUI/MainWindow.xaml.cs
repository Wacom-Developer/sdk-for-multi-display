﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;



using Wacom.Kiosk.Integrator;
using Wacom.Kiosk.Message.Handler;
using Wacom.Kiosk.Message.Shared;
using Wacom.Kiosk.Message.Shared.SDKMessage.Integrator.Messages;
using Wacom.Kiosk.Message.Shared.SDKMessage.Tablet;
using Wacom.Kiosk.Pdf;
using Wacom.Kiosk.Pdf.Shared;
using Wacom.Kiosk.UI.Parsers.Shared;
using Wacom.Kiosk.UI.Parsers;

using Page = Wacom.Kiosk.Pdf.Shared.Page;
using Path = System.IO.Path;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.AcroForms;
using Patagames.Pdf.Net.BasicTypes;



namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        /// <summary>The selected client</summary>
        private string selectedClient = "";

        private readonly ILogger logger;

        private enum IdleMode
        {
            Image,
            Video
        };
        /// <summary>The idle mode image</summary>
        private IdleMode idleMode = IdleMode.Image;

        private bool bMirroring = false;
        private bool bPrivacy = false;

        private int PageCount = 0;

        /// <summary>Saved field values for each page of current document</summary>
        private readonly Dictionary<int, Dictionary<string, object>> InputValues = new Dictionary<int, Dictionary<string, object>>();

        /// <summary>Name of field being signed</summary>
        private string SignatureFieldName;
        /// <summary>
        /// Requested resolution for signature image
        /// </summary>
        /// <remarks>Should be at least 72 (PDF units). Higher values give a better looking image</remarks>
        private const int SignatureDPI = 300;
        /// <summary>XAML definition for simple signature capture screen</summary>
        private const string defaultSignatureDefinition = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""     xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""     xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""     xmlns:local=""clr-namespace:Wacom.Kiosk.App"" mc:Ignorable=""d"" x:Name=""DocumentView"" Title=""DocumentView"" WindowStyle=""None"" Width=""1920"" Height=""1080"">   <Grid Background=""White"" >       <Grid Name=""SignatureContainer""></Grid>        <Grid Panel.ZIndex=""2"" HorizontalAlignment=""Right"" Width=""150"" Background=""Transparent"">            <StackPanel Panel.ZIndex=""2"" Orientation=""Vertical"">                <Button x:Name=""AcceptSignature"" Content=""AcceptSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>                <Button x:Name=""CancelSignature"" Content=""CancelSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>                <Button x:Name=""ClearSignature"" Content=""ClearSignature"" Width=""100"" Height=""100"" BorderThickness=""0"" HorizontalAlignment=""Right"" Margin=""25,25,25,0"" VerticalAlignment=""Top""></Button>            </StackPanel>        </Grid>    </Grid></Window>";

        private ActiveClient ActiveClient {
            get
            {
                return KioskServer.ServerInstance.ActiveClients.Where(el => el.ClientAddress.Equals(selectedClient)).FirstOrDefault();
            }
        }

        #endregion

        #region Construction

        public MainWindow(ILogger logger)
        {
            
            InitializeComponent();
            RegisterTabletMessageHandlers();

            
            this.logger = logger;
            cbxClients.SelectionChanged += ClientSelectionChange;
            cbxIdleMode.ItemsSource = new List<string> { "Image mode", "Video mode" };
            cbxIdleMode.SelectedItem = "Image mode";
            cbxIdleMode.SelectionChanged += IdleModeSelectionChange;

            KioskServer.ServerInstance.Logger = this.logger;
            KioskServer.ServerInstance.OnClientConnected += OnClientConnected;
            KioskServer.ServerInstance.OnClientDisconnected += OnClientDisconnected;
            KioskServer.ServerInstance.OnSubscriberMessageReceived += OnMessageReceived;

            /*
            string licenseFile = @"Resources\PdfiumLicense.txt";
            if (File.Exists(licenseFile))
            {
                using var pdfiumLicenseStream = File.OpenText(licenseFile);
                var pdfiumLicense = pdfiumLicenseStream.ReadToEnd();
                if (!string.IsNullOrEmpty(pdfiumLicense))
                {
                    PdfCommon.Initialize(pdfiumLicense);
                }
            }
            */
        }

        #endregion

        #region SDK Event Handlers
        private void OnClientDisconnected(object sender, string clientName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                cbxClients.Items.Remove(ActiveClient?.Name);
            });
        }

        private void OnClientConnected(object sender, byte[] e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cert = new X509Certificate2("MyServer.pfx", "password");
                var client = KioskServer.ServerInstance.ActiveClients.Last();
                string license = ConfigurationManager.AppSettings.Get("license");
                KioskServer.SendMessage(client.ClientAddress, new ClientAcceptedMessage(new KioskClient("Integrator"), client.Name)
                    .WithLicense(license)
                    .WithPublicKey(Convert.ToBase64String(cert.GetPublicKey()))
                    .Build().ToByteArray());
                cbxClients.Items.Add(client?.Name);
                selectedClient = client?.Name;
                cbxClients.SelectedItem = selectedClient;
                AppendLog($"Client '{client?.Name}' connected");
            });
        }

        /// <summary>Handles message received from client</summary>
        private void OnMessageReceived(object sender, byte[] messageBytes)
        {
            if (messageBytes != null)
            {
                object msg = KioskMessageFactory.FromByteArray(messageBytes);
                MessageHandlers.HandleMessage(msg); 
            }
        }

        #endregion

        #region Control Event Handlers

        /// <summary>Handles change to Idle Mode selection</summary>
        private void IdleModeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            idleMode = (sender as ComboBox).SelectedItem.ToString().Equals("Image mode") ? IdleMode.Image : IdleMode.Video;
        }

        /// <summary>Handles change to Client selection</summary>
        private void ClientSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            string selectedClientName = (sender as ComboBox)?.SelectedItem?.ToString();
            selectedClient = KioskServer.ServerInstance.ActiveClients.Where(el => el.Name.Equals(selectedClientName)).FirstOrDefault()?.ClientAddress;
        }

        /// <summary>Clears logged events from the window</summary>
        private void ClearLogClick(object sender, RoutedEventArgs e)
        {
            txtLog.Text = String.Empty;
        }

        /// <summary>Sets the seleced Idle Mode</summary>
        private void IdleModeClick(object sender, RoutedEventArgs e)
        {
            SendMessage(
                    new OpenIdleMessage(KioskServer.Sender)
                                .WithImagesOrVideos(idleMode == IdleMode.Image ? "images" : "videos")
                                .WithDefaultOrCustomGroup(IdleCustDef.Text)
                                .WithSlideShowInterval(3)
                                .Build()
                                .ToByteArray());
        }

        
        private void ShowDialogClick(object sender, RoutedEventArgs e)
        {
            ConfigureDialogWindow dlgConfigurationWindow = new ConfigureDialogWindow();

            if ((bool)dlgConfigurationWindow.ShowDialog() && !string.IsNullOrEmpty(dlgConfigurationWindow.Filename))
            {
                using var xamlStream = File.OpenText(dlgConfigurationWindow.Filename);
                var xaml = xamlStream.ReadToEnd();

                SendMessage(new ShowDialogMessage(KioskServer.Sender)
                                .WithXAML(xaml)
                                .Build()
                                .ToByteArray());
            }
            else
            {
                SendMessage(new DismissDialogMessage(KioskServer.Sender)
                                .Build()
                                .ToByteArray());
            }
        }

        /// <summary>Initiates display of a web page</summary>
        private void OpenWebClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                ConfigureWebWindow webConfigurationWindow = new ConfigureWebWindow(selectedClient)
                {
                    Owner = this
                };
                webConfigurationWindow.Activate();
                webConfigurationWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigurePdfWindow for input of PDF to display</summary>
        private void OpenPdfClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                ConfigurePdfWindow pdfConfigurationWindow = new ConfigurePdfWindow(selectedClient)
                {
                    Owner = this
                };
                pdfConfigurationWindow.Activate();
                pdfConfigurationWindow.ShowDialog(); 
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigureIdleWindow for input of update parameters</summary>
        private void UpdateMediaClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                ConfigureIdleWindow idleConfigWindow = new ConfigureIdleWindow(selectedClient)
                {
                    Owner = this
                };
                idleConfigWindow.Activate();
                idleConfigWindow.ShowDialog(); 
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigureThumbnailsWindow for input of parameters</summary>
        private void UpdateThumbnailsClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                ConfigureThumbnailsWindow thumbnailsConfigurationWindow = new ConfigureThumbnailsWindow(selectedClient, logger)
                {
                    Owner = this
                };
                thumbnailsConfigurationWindow.Activate();
                thumbnailsConfigurationWindow.ShowDialog(); 
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigureSignatureWindow for input of signature capture parameters</summary>
        private void OpenSignatureClick(object sender, RoutedEventArgs e)
        {
            ActiveClient activeClient = ActiveClient;

            if (activeClient != null)
            {
                var signatureConfigurationWindow = new ConfigureSignatureWindow(activeClient.ClientAddress)
                {
                    Owner = this
                };
                signatureConfigurationWindow.Activate();
                signatureConfigurationWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigureDocumentWindow for input of document display paraters</summary>
        private void OpenDocClick(object sender, RoutedEventArgs e)
        {
            ActiveClient activeClient = ActiveClient;
            if (activeClient != null)
            {
                ConfigureDocumentWindow documentConfigurationWindow = new ConfigureDocumentWindow(activeClient.Name, activeClient.ClientAddress, logger);


                if ((bool)documentConfigurationWindow.ShowDialog())
                {
                    InputValues.Clear();
                    var comboBoxItem = cbxZoom.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == "100%");
                    cbxZoom.SelectedIndex = cbxZoom.Items.IndexOf(comboBoxItem);
                    PageCount = documentConfigurationWindow.PageCount;
                }
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }

        /// <summary>Displays ConfigureKeyboardLayoutWindow for input of layout parameters</summary>
        private void UpdateLayoutClick(object sender, RoutedEventArgs e)
        {
            ConfigureKeyboardLayoutWindow keyboardLayoutConfigurationWindow = new ConfigureKeyboardLayoutWindow(selectedClient)
            {
                Owner = this
            };
            keyboardLayoutConfigurationWindow.Activate();
            keyboardLayoutConfigurationWindow.ShowDialog();
        }

        /// <summary>Initiates enabling/disabling a named element</summary>
        private void SetStateClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                string elName = txtElementName.Text;
                bool isEnabled = chkIsEnabled.IsChecked ?? false;
                SendMessage(new SetElementEnabledMessage(KioskServer.Sender)
                    .WithName(elName)
                    .WithState(isEnabled)
                    .Build()
                    .ToByteArray()); 
            }
        }

        /// <summary>Initiates setting value (Content) of a named element</summary>
        private void SetValueClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                string eltName = txtElementName.Text;
                string eltValue = txtElementValue.Text;
                SendMessage(new SetElementValueMessage(KioskServer.Sender)
                    .WithName(eltName)
                    .WithValue(eltValue)
                    .Build()
                    .ToByteArray()); 
            }
        }

        /// <summary>Scrolls to and selects a named element</summary>
        private void GoToField_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                string fieldName = txtFieldName.Text;
                SendMessage(new SelectAcroFieldMessage(KioskServer.Sender)
                    .WithAcroFieldName(fieldName)
                    .Build()
                    .ToByteArray());
            }
        }

        private void ZoomSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveClient != null)
            {
                if (double.TryParse(((ComboBoxItem)cbxZoom.SelectedValue).Tag.ToString(), out double zoom))
                {
                    SendMessage(new SetDocumentZoomMessage(KioskServer.Sender)
                        .WithZoom(zoom)
                        .Build()
                        .ToByteArray());
                } 
            }
        }

        /// <summary>Displays ConfigureAppConfig for input of config file name</summary>
        private void UpdateConfigClick(object sender, RoutedEventArgs e)
        {
            ConfigureAppConfig appConfigUpdateWindow = new ConfigureAppConfig(selectedClient)
            {
                Owner = this
            };
            appConfigUpdateWindow.Activate();
            appConfigUpdateWindow.ShowDialog();
        }

        /// <summary>Initiates toggling of Mirroring</summary>
        private void MirrorClick(object sender, RoutedEventArgs e)
        {
            ActiveClient activeClient = ActiveClient;
            if (activeClient != null)
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
        }

        /// <summary>Initiates toggling of Privacy Mode</summary>
        private void PrivacyClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
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

        #endregion

        #region SDK Message Handling
        /// <summary>Registers the tablet message handlers.</summary>
        private void RegisterTabletMessageHandlers()
        {
            MessageHandlers.RegisterHandler(new MessageHandler<ApplicationExitMessage>((msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    cbxClients.Items.Remove(msg.Sender.Name);
                });

                AppendLog(msg.ToString());
            }), logger);

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
                // Initial connection or could be kiosk restarted, either way 
                // assume mirroring and privacy are therefore both off
                bMirroring = false;
                bPrivacy = false;
                if (msg.AesPasswordCypher != null) ((NamedPipeServer)KioskServer.ServerInstance).AesPasswordCipher = msg.AesPasswordCypher;
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<InputChangedMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureClickedMessage>((msg) =>
            {
                HandleSignatureClicked(msg);
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<ButtonClickedMessage>((msg) =>
            {
                HandleButtonClicked(msg);
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<ErrorTabletMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<ThumbnailClickedMessage>((msg) =>
            {
                HandleThumbnailClicked(msg);
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureAcceptedMessage>((msg) =>
            {
                HandleSignatureAccepted(msg);
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureCancelledMessage>((msg) =>
            {
                HandleSinatureCancelled(msg);
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<SignatureWrongFormatMessage>((msg) =>
            {
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<StartMirroringMessage>((msg) =>
            {
                bMirroring = true;
                AppendLog(msg.ToString());
            }), logger);

            MessageHandlers.RegisterHandler(new MessageHandler<StopMirroringMessage>((msg) =>
            {
                bMirroring = false;
                AppendLog(msg.ToString());
            }), logger);
        }

        /// <summary>
        /// Respond to signautre capture cancellation. 
        /// </summary>
        /// <param name="msg"></param>
        private void HandleSinatureCancelled(SignatureCancelledMessage msg)
        {
            AppendLog(msg.ToString());

            // Return to document or revert to idle mode
            var activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
            if (msg.FromDocument && !string.IsNullOrEmpty(activeClient.DocumentContext.DocumentPath))
            {
                ChangeDocumentPage(activeClient, activeClient.DocumentContext.DocumentPageNumber, true);
            }
            else
            {
                SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
            }
        }

        /// <summary>
        /// Respond to signature captured
        /// </summary>
        /// <param name="msg"></param>
        private void HandleSignatureAccepted(SignatureAcceptedMessage msg)
        {
            AppendLog(msg.ToString());

            var activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
            if (msg.FromDocument && !string.IsNullOrEmpty(activeClient.DocumentContext.DocumentPath))
            {
                // We are signing a PDF document
                string saveAs = activeClient.DocumentContext.DocumentPath;

                // If document was loaded from this app's resources, prompt to save it elsewhere
                if (saveAs.StartsWith(AppContext.BaseDirectory))
                {
                    saveAs = GetSaveAs(activeClient.DocumentContext.DocumentPath);
                }

                if (saveAs != null)
                {
                    try
                    {
                        // Logic for applying a Digital Signature to a Pdf should be called from here

                        activeClient.DocumentContext.DocumentPath = saveAs;
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        });
                    }
                }
                // Return to document display
                ChangeDocumentPage(activeClient, activeClient.DocumentContext.DocumentPageNumber, true);
            }
            else
            {
                // Signature was standalone

                // Display the signautre image in a separate window
                var sigImage = JsonPdfSerializer.ByteArrayToBitmap(msg.SignaturePictureBytes);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var signaturePopupWindow = new SignatureImagePopupWindow(sigImage)
                    {
                        Owner = this
                    };
                    signaturePopupWindow.Activate();
                    signaturePopupWindow.Show();
                });

                // Revert to idle mode
                SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
            }
        }

        /// <summary>
        /// Respond to click on thumbnail
        /// Respond to click on thumbnail
        /// </summary>
        /// <param name="msg"></param>
        private void HandleThumbnailClicked(ThumbnailClickedMessage msg)
        {
            var activeClient = KioskServer.GetActiveClient(msg.Sender.Name);
            if (activeClient.DocumentContext.DocumentPageNumber == msg.PageNumber)
            {
                // Thumbnail is current page - do nothing
                return;
            }

            ChangeDocumentPage(activeClient, msg.PageNumber);
            AppendLog(msg.ToString());
        }

        /// <summary>
        /// Respond to click of a document viewer button
        /// </summary>
        /// <param name="msg"></param>
        private void HandleButtonClicked(ButtonClickedMessage msg)
        {
            AppendLog(msg.ToString());

            switch (msg.ButtonName)
            {
                case "PageNext":
                    StepToNextPage(true, msg.Sender.Name);
                    break;

                case "PageBack":
                    StepToNextPage(false, msg.Sender.Name);
                    break;

                case "OpenMirroring":
                    SendMessage(new StartMirroringMessage(KioskServer.Sender).Build().ToByteArray());
                    break;

                case "CloseMirroring":
                    SendMessage(new StopMirroringMessage(KioskServer.Sender).Build().ToByteArray());
                    break;

                case "DocumentAccepted":
                    AcceptDocument(msg.Sender.Name);
                    break;

                case "DocumentRejected":
                    RejectDocument();
                    break;

                case "MessageBox_Cancel":
                    SendMessage(new DismissDialogMessage(KioskServer.Sender).Build().ToByteArray());
                    break;

                case "MessageBox_Dismiss":
                    SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
                    break;
            }
        }

        /// <summary>
        /// Respond to click of signature field
        /// </summary>
        /// <param name="msg"></param>
        private void HandleSignatureClicked(SignatureClickedMessage msg)
        {
            ActiveClient activeClient = KioskServer.GetActiveClient(msg.Sender.Name);

            try
            {
                AppendLog($"{msg.SignatureFieldName} clicked");
                using PdfHelper pdfHelper = new PdfHelper(logger);
                var sig = pdfHelper.GetSignatureInfo(ActiveClient.DocumentContext.DocumentPath, msg.SignatureFieldName);
                // Check if field has been signed
                if (string.IsNullOrEmpty(sig))
                {
                    StartSignatureCapture(msg, activeClient);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(sig, "Signature", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Exception: {ex}");
                throw;
            }
        }

        #endregion

        #region PDF Document Support

        private void StartSignatureCapture(SignatureClickedMessage msg, ActiveClient activeClient)
        {
            SignatureFieldName = msg.SignatureFieldName;
            // Register new handler to save PDF, when we've received field data
            // for the current page
            MessageHandlers.RegisterHandler(new MessageHandler<PageDataMessage>((msg) =>
            {
                AppendLog(msg.ToString());

                // Save field values from current page
                InputValues[activeClient.DocumentContext.DocumentPageNumber] = msg.PageData;

                //var fieldRect = forms.InterForm.Fields.Find(fld => fld.FullName == SignatureFieldName).Controls[0].BoundRect;

                // Open Signature page
                SignatureConfig signatureConfig = new SignatureConfig()
                {
                    X = 0,
                    Y = 0,
                    Width = 1200,
                    Height = 700,
                    AreaPercent = 0.8,
                    PenTrackingType = PenTrackingType.Limited,
                    SignatureViewType = SignatureViewType.External,
                    SignatureFormat = SignatureFormat.Fss,
                    Image = new SignatureImageConfig()
                    {
                        DPI = SignatureDPI
                        //Width = (int)fieldRect.Width,
                        //Height = (int)fieldRect.Height
                    }
                };
                StringBuilder sb = new StringBuilder();
                foreach (var page in InputValues.Values)
                {
                    foreach (var field in page)
                    {
                        sb.Append($"{field.Key}:{field.Value}");
                    }
                }
                //var hash = HashingUtility.GetHash(sb.ToString());
                //var extraData = new Dictionary<string, string>
                //{
                //    { "testData1", "Data" },
                //    { "testData2", "Data" },
                //    { "testData3", "Data" },
                //};
                //SendMessage(new OpenSignatureMessage(KioskServer.Sender).WithDefinition(defaultSignatureDefinition).WithExtraData(extraData).WithConfig(signatureConfig).WithFromDocument(true).Build().ToByteArray());
                SendMessage(new OpenSignatureMessage(KioskServer.Sender).WithDefinition(defaultSignatureDefinition).WithConfig(signatureConfig).WithFromDocument(true).Build().ToByteArray());

                Debug.WriteLine("PdfDocument going out of scope");

            }), logger);

            SendMessage(new GetPageDataMessage(KioskServer.Sender).Build().ToByteArray());
        }


        /// <summary>
        /// Handles click of DocumentAccepted button
        /// </summary>
        /// <param name="clientName"></param>
        private void AcceptDocument(string clientName)
        {
            ActiveClient activeClient = KioskServer.GetActiveClient(clientName);

            // Register new handler to save PDF, when we've received field data
            // for the current page
            MessageHandlers.RegisterHandler(new MessageHandler<PageDataMessage>((msg) =>
            {
                AppendLog(msg.ToString());

                
                SendMessage(new OpenIdleMessage(KioskServer.Sender).Build().ToByteArray());
            }), logger);

            // Request field data for current page
            SendMessage(new GetPageDataMessage(KioskServer.Sender).Build().ToByteArray());
        }

        private void RejectDocument()
        {
            using var xamlStream = File.OpenText(@"Resources\Definitions\MessageBox.xaml");
            var xaml = xamlStream.ReadToEnd();

            SendMessage(new ShowDialogMessage(KioskServer.Sender).WithXAML(xaml).Build().ToByteArray());
        }
       

        /// <summary>
        /// Handles PageNext/PageBack button click
        /// </summary>
        /// <param name="moveForward"></param>
        /// <param name="clientName"></param>
        private void StepToNextPage(bool moveForward, string clientName)
        {
            ActiveClient activeClient = KioskServer.GetActiveClient(clientName);

            int pageToParse = moveForward
                ? activeClient.DocumentContext.DocumentPageNumber + 1
                : activeClient.DocumentContext.DocumentPageNumber - 1;

            if (pageToParse < 1 || pageToParse > PageCount) 
                return;

            ChangeDocumentPage(activeClient, pageToParse);
        }

        /// <summary>
        /// Go to a specific page in the current document
        /// </summary>
        /// <param name="activeClient"></param>
        /// <param name="pageNumber"></param>
        /// <param name="afterSigning">True if returning to the document/page after signature capture</param>
        private void ChangeDocumentPage(ActiveClient activeClient, int pageNumber, bool afterSigning = false)
        {
            try
            {
                if (!afterSigning)
                {
                    if (pageNumber == activeClient.DocumentContext.DocumentPageNumber)
                    {
                        // Same document, same page - nothing to do
                        return;
                    }

                    // Switching between pages. Request field data for the current page 1st
                    // so we can save it and restore it later

                    // Register new handler to display new page, when we've received field data
                    // for the current page
                    MessageHandlers.RegisterHandler(new MessageHandler<PageDataMessage>((msg) =>
                    {
                        AppendLog(msg.ToString());

                        // Save field values from current page
                        InputValues[activeClient.DocumentContext.DocumentPageNumber] = msg.PageData;

                        LoadDocumentPage(activeClient, pageNumber);

                    }), logger);

                    // Request field values for the current page
                    SendMessage(new GetPageDataMessage(KioskServer.Sender).Build().ToByteArray());
                }
                else
                {
                    // In this example, we've already retrieved and saved field data during signing process
                    // so we only have to reload the page
                    LoadDocumentPage(activeClient, pageNumber);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Load a specific page in the current document
        /// </summary>
        /// <param name="activeClient"></param>
        /// <param name="pageNumber"></param>
        private void LoadDocumentPage(ActiveClient activeClient, int pageNumber)
        {
            using var pdfHelper = new PdfHelper(logger);
            string pageJson = pdfHelper.ParsePage(activeClient.DocumentContext.DocumentPath, pageNumber);
            Page page = JsonPdfSerializer.DeserializePage(pageJson, logger);

            // Restore field values if previously saved
            if (InputValues.ContainsKey(pageNumber) && InputValues[pageNumber] != null)
            {
                foreach (var value in InputValues[pageNumber])
                {
                    var acroFields = page.AcroFields.FindAll(field => field.Name == value.Key);
                    foreach (var fld in acroFields)
                        fld.Value = value.Value.ToString();
                }
            }

            // Open new page
            activeClient.UpdateDocumentContext(pageNumber, activeClient.DocumentContext.DocumentPath);
            SendMessage(new OpenDocumentPageMessage(KioskServer.Sender)
                                .ForDocumentPage(page)
                                .Build()
                                .ToByteArray());
        }

        /// <summary>
        /// Displays SaveFileDialog (on UI thread) to get filename to save as
        /// </summary>
        /// <param name="docPath">Full path for current filename</param>
        /// <returns>Full path for filename to save as</returns>
        private string GetSaveAs(string docPath)
        {
            string saveAs = null;
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog()
                {
                    FileName = Path.GetFileName(docPath),
                    Filter = "PDF File (*.pdf)|*.pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    OverwritePrompt = true
                };
                saveAs = ((bool)saveFileDialog.ShowDialog(this)) ? saveFileDialog.FileName : null;

            })).Wait();

            return saveAs;
        }

        #endregion

        #region Utility Methods

        /// <summary>Appends to the log.</summary>
        /// <param name="str">The string.</param>
        private void AppendLog(string msg)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                string now = DateTimeOffset.UtcNow.ToString();
                txtLog.Text += $"{msg}";

                txtLog.Text += Environment.NewLine;
                scroll_log.ScrollToBottom();
            });
        }

        /// <summary>Sends the message.</summary>
        /// <param name="bytes">The bytes.</param>
        private void SendMessage(byte[] bytes)
        {
            if (KioskServer.ServerInstance.ActiveClients.Count > 0)
            {
                KioskServer.SendMessage(selectedClient, bytes);
            }
        }

        #endregion

        private void ClearBrowserDataClick(object sender, RoutedEventArgs e)
        {
            if (ActiveClient != null)
            {
                ConfigureBrowserDataWindow browserDataConfigurationWindow = new ConfigureBrowserDataWindow(selectedClient)
                {
                    Owner = this
                };
                browserDataConfigurationWindow.Activate();
                browserDataConfigurationWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No Client Connected");
            }
        }
    }
}
                    