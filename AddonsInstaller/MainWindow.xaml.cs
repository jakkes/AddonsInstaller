using System;
using System.IO;
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
using Microsoft.Win32;

namespace AddonsInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.FolderBrowserDialog folderDialog;
        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(".config"))
            {
                string path = File.ReadAllText(".config");
                PathTxt.Text = path;
            }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (folderDialog == null)
            {
                folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            }
            var res = folderDialog.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                PathTxt.Text = folderDialog.SelectedPath;
            }
        }

        private void FindBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFolder("C:/Program Files/World of Warcraft"))
            {
                PathTxt.Text = "C:/Program Files/World of Warcraft";
            } else if (CheckFolder("C:/Program/World of Warcraft"))
            {
                PathTxt.Text = "C:/Program/World of Warcraft";
            } else if (CheckFolder("C:/Program Files (x86)/World of Warcraft"))
            {
                PathTxt.Text = "C:/Program Files (x86)/World of Warcraft";
            } else
            {
                Global.ShowError("Could not find your game folder. Please specify it manually.");
            }
        }

        private bool CheckFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            if (!Directory.Exists(path + "/Data") || !File.Exists(path + "/.build.info"))
            {
                return false;
            }
            return true;
        }

        private void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            // Check if selected path makes sense
            string path = PathTxt.Text;
            if (path.EndsWith("/"))
                path.Substring(0, path.Length - 1);
            
            if (!CheckFolder(path))
            {
                Global.ShowError("Folder does not seem to be a valid World of Warcraft game directory.");
                return;
            }

            bool classic = Directory.Exists(path + "/_classic_");
            bool retail = Directory.Exists(path + "/_retail_");

            var versionSelection = new SelectVersion(classic, retail) { Owner = this };
            versionSelection.ShowDialog();

            if (versionSelection.Selector.SelectedValue.ToString() == "Classic")
                path += "/_classic_";
            else if (versionSelection.Selector.SelectedValue.ToString() == "Retail")
                path += "/_retail_";
            else
            {
                Global.ShowError("Something went wrong when choosing version...");
                return;
            }

            File.WriteAllText(".config", PathTxt.Text);

            var next = new SuperWindow(path);
            next.Show();

            this.Close();
        }
    }
}
