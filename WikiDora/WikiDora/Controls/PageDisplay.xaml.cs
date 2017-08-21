using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WikiDora.Controls
{
    /// <summary>
    /// Interaction logic for PageDisplay.xaml
    /// </summary>
    public partial class PageDisplay : UserControl
    {
        public PageDisplay()
        {
            InitializeComponent();
        }

        private void btnLikePage_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }
        private void btnDislikePage_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
