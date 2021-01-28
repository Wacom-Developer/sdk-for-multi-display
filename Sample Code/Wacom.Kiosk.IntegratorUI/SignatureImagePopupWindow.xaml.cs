using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wacom.Kiosk.IntegratorUI
{
    /// <summary>
    /// Interaction logic for SignatureImagePopupWindow.xaml
    /// </summary>
    public partial class SignatureImagePopupWindow : Window
    {
        public SignatureImagePopupWindow(BitmapImage signatureImage)
        {
            InitializeComponent();

            Background = new ImageBrush(signatureImage);
        }
    }
}
