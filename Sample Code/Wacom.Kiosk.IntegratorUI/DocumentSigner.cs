using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Patagames.Pdf.Net.BasicTypes;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;


namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Handles digital signing of a PDF document and very basic interrogation of signatures
    /// </summary>
    /// <remarks>
    /// PDFium.Net (Patagames), used elsewhere in the Kiosk SDK for PDF handling, does not support digital signing.
    /// In this sample, we use FreeSpire.PDF for .Net (https://www.e-iceblue.com/Introduce/free-pdf-component.html#.YIrJNrVKiUk)
    /// </remarks>
    class DocumentSigner
    {
        /// <summary>
        /// Digitally signs a document and inserts a handwritten signature image
        /// </summary>
        /// <param name="document">The document to sign</param>
        /// <param name="page">Number of page containing signature field</param>
        /// <param name="fieldName">Name of signature field</param>
        /// <param name="sigText">Biometric (FSS) signature data</param>
        /// <param name="sigImageBytes">Signature image data</param>
        /// <param name="SignatureDPI">DPI of signature image</param>
        /// <returns>Signed PdfDocument</returns>
        public PdfDocument SignPDF(
            PdfDocument document,
            int page,
            string fieldName,
            string sigText,
            byte[] sigImageBytes,
            int SignatureDPI
            )
        {
            try
            {
                if (document != null)
                {
                    PdfTypeDictionary fieldDict = GetFieldDictionary(document, fieldName);
                    if (fieldDict != null)
                    {
                        // If AcroForm dictionary contains a 'NeedAppearances' key with value 'true' then
                        // change to 'false' (since it can cause issues with signing).
                        PdfTypeDictionary formDict = document.Root["AcroForm"].As<PdfTypeDictionary>();
                        if (formDict != null && formDict.ContainsKey("NeedAppearances") && formDict.GetBooleanBy("NeedAppearances"))
                        {
                            formDict.SetBooleanAt("NeedAppearances", false);
                        }

                        var stream = new MemoryStream();
                        document.Save(stream, SaveFlags.NoIncremental);
                        document.Dispose();
                        return PdfDocument.Load(SignWithSpire(stream, page, fieldName, sigText, sigImageBytes, SignatureDPI));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex}");
                throw;
            }

            return null;
        }


        /// <summary>
        /// Returns string with signatory name & date and time of signature
        /// </summary>
        /// <param name="pdfDocPath">Filename of PDF document</param>
        /// <param name="fieldName">Name of signature field</param>
        /// <remarks>Can also used to determine if the given field has been signed</remarks>
        public static string GetSignatureInfo(string pdfDocPath, string fieldName)
        {
            using var doc = new Spire.Pdf.PdfDocument(pdfDocPath);

            var form = (Spire.Pdf.Widget.PdfFormWidget)doc.Form;
            for (int i = 0; i < form.FieldsWidget.Count; ++i)
            {
                var fldWidget = form.FieldsWidget[i] as Spire.Pdf.Widget.PdfSignatureFieldWidget;
                if (fldWidget != null && fldWidget.Name == fieldName)
                {
                    var sig = fldWidget.Signature;
                    if (sig != null)
                    {
                        string name = string.IsNullOrEmpty(sig.Name) ? "(unknown)" : sig.Name;
                        return $"Signed by: '{name}' at {sig.Date.ToShortTimeString()} on {sig.Date.ToLongDateString()}";
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        private Spire.Pdf.Graphics.PdfImage SigImage;
        private RectangleF SigImageRect;

        /// <summary>
        /// Get the field dictionary for the named field
        /// </summary>
        /// <param name="document"></param>
        /// <param name="fieldName"></param>
        /// <returns>PdfTypeDictionary</returns>
        private PdfTypeDictionary GetFieldDictionary(PdfDocument document, string fieldName)
        {
            PdfTypeDictionary fieldDict = null;
            if (document != null && document.Root.ContainsKey("AcroForm"))
            {
                // Get the 'AcroForm' dictionary.
                var formDict = document.Root["AcroForm"].As<PdfTypeDictionary>();
                // Search the fieldsArray for the named field.
                foreach (var obj in (formDict["Fields"]).As<PdfTypeArray>())
                {
                    // Get dictionary  of current field.
                    var dict = obj.As<PdfTypeDictionary>();
                    if (dict.ContainsKey("T") && dict.GetUnicodeBy("T") == fieldName)
                    {
                        // Found named field.
                        fieldDict = dict;

                        break;
                    }
                }
            }

            return fieldDict;
        }

        /// <summary>
        /// Signs a PDF document using FreeSpire.PDF library
        /// </summary>
        /// <param name="stream">Stream containing PDF document</param>
        /// <param name="pageNum">Number of page containing signature field</param>
        /// <param name="fieldName">Name of signature field</param>
        /// <param name="sigText">Biometric (FSS) signature data</param>
        /// <param name="sigImageBytes">Signature image data</param>
        /// <param name="SignatureDPI">DPI of signature image</param>
        /// <returns>Stream containing signed PDF</returns>
        /// <remarks>
        /// Biometric (FSS) signature data should ideally be saved as custom XMP metadata in the signature field
        /// but FreeSpire.PDF does not currently support this so an alternative (possibly paid-for) PDF library 
        /// would be required.
        /// </remarks>
        private Stream SignWithSpire(Stream stream, int pageNum, string fieldName, string sigText, byte[] sigImageBytes, int SignatureDPI)
        {
            Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument(stream);
            Spire.Pdf.PdfPageBase page = doc.Pages[pageNum - 1];

            // Load certificate from resources
            var cert = new Spire.Pdf.Security.PdfCertificate(@"Resources\WacomDemoSigningCert.pfx", "password");

            // Find signature field widget
            var formWidget = doc.Form as Spire.Pdf.Widget.PdfFormWidget;
            var fieldWidget = formWidget.FieldsWidget[fieldName] as Spire.Pdf.Widget.PdfSignatureFieldWidget;

            // Create and initialize signature object
            var signature = new Spire.Pdf.Security.PdfSignature(doc, page, cert, fieldWidget.Name, fieldWidget)
            {
                Certificated = true,
                DocumentPermissions = Spire.Pdf.Security.PdfCertificationFlags.AllowFormFill,
            };

            string url = "http://timestamp.wosign.com/rfc3161";
            signature.ConfigureTimestamp(url);
            signature.ConfigureCustomGraphics(DrawSigImage);

            ConfigureSigImage(sigImageBytes, fieldWidget.Bounds.Size, SignatureDPI);

            // Save (and thereby sign) the document to a stream 
            MemoryStream outStream = new MemoryStream();
            doc.SaveToStream(outStream);
            outStream.Seek(0, SeekOrigin.Begin);
            
            return outStream;
        }

        /// <summary>
        /// Callback method to draw the signature image in the target rectangle
        /// </summary>
        /// <param name="g"></param>
        private void DrawSigImage(Spire.Pdf.Graphics.PdfCanvas g)
        {
            g.DrawImage(SigImage, SigImageRect);
        }

        /// <summary>
        /// Converts signature image data into a PdfImage and calculates target area in which to draw it
        /// </summary>
        /// <param name="sigImageBytes">Signature image data</param>
        /// <param name="fieldSize">Size of signature field (PDF units)</param>
        /// <param name="SignatureDPI">DPI of signature image</param>
        private void ConfigureSigImage(byte[] sigImageBytes, SizeF fieldSize, int SignatureDPI)
        {
            var imageStream = new MemoryStream(sigImageBytes);
            var bitmap = Bitmap.FromStream(imageStream);

            // Convert image size to PDF units
            var sigSize = new SizeF(72.0f * bitmap.Width / SignatureDPI, 72.0f * bitmap.Height / SignatureDPI);
            // If too big, scale down to fit
            if (sigSize.Width > fieldSize.Width || sigSize.Height > fieldSize.Height)
            {
                float scale = Math.Min(fieldSize.Width / sigSize.Width, fieldSize.Height / sigSize.Height);
                sigSize.Width *= scale;
                sigSize.Height *= scale;
            }

            // Center image in rectangle
            float x = (fieldSize.Width - sigSize.Width) / 2;
            float y = (fieldSize.Height - sigSize.Height) / 2;

            SigImage = Spire.Pdf.Graphics.PdfImage.FromStream(imageStream);
            SigImageRect = new RectangleF(x, y, sigSize.Width, sigSize.Height);
        }


    }

}
