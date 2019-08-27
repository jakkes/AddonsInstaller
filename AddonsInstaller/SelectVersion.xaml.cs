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
using System.Windows.Shapes;

namespace AddonsInstaller
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SelectVersion : Window
    {
        public SelectVersion(bool classic, bool retail)
        {
            
            List<string> source = new List<string>(2);
            if (classic)
                source.Add("Classic");
            if (retail)
                source.Add("Retail");

            InitializeComponent();

            Selector.ItemsSource = source.ToArray();
            if (source.Count > 0)
                Selector.SelectedIndex = 0;
        }

        private void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Selector.SelectedItem != null)
                this.Close();
        }
    }
}
