using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using CloudFlareUtilities;
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
using System.Collections.ObjectModel;

namespace AddonsInstaller
{
    /// <summary>
    /// Interaction logic for SuperWindow.xaml
    /// </summary>
    public partial class SuperWindow : Window
    {
        private string path;

        private static readonly string searchPath = "https://www.curseforge.com/wow/addons/search?search=";
        
        private ObservableCollection<string> InstalledAddons = new ObservableCollection<string>();
        private ObservableCollection<string> SearchNames = new ObservableCollection<string>();
        private List<string> SearchUrls = new List<string>();

        private static Regex nameRegex = new Regex("<h3.+>(.*)</h3>");

        public SuperWindow(string path)
        {
            InitializeComponent();

            this.path = path + "/Interface/AddOns";
            if (!Directory.Exists(this.path))
            {
                Directory.CreateDirectory(this.path);
            }
            PathTxt.Text = "Working in: " + this.path;

            InstalledAddonsList.ItemsSource = InstalledAddons;
            AvailableAddonsList.ItemsSource = SearchNames;
            PopulateInstalledAddons();
        }

        private void PopulateInstalledAddons()
        {
            InstalledAddons.Clear();
            foreach (var dir in Directory.GetDirectories(this.path))
            {
                var splits = dir.Split(new char[] { '\\', '/' });
                InstalledAddons.Add(splits[splits.Length - 1]);
            }
        }
        private void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (InstalledAddonsList.SelectedItem == null)
                return;

            string name = InstalledAddonsList.SelectedItem.ToString();
            var result = MessageBox.Show("Are you sure that you want to UNINSTALL the AddOn: \n" + name, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Directory.Delete(this.path + "/" + name, true);
                PopulateInstalledAddons();
            }
        }
        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            PopulateInstalledAddons();
        }
        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }
        private async Task Search()
        {
            try
            {
                SearchBtn.IsEnabled = false;
                var res = await Global.HTTP.GetAsync(searchPath + this.SearchQuery.Text);
                if (!res.IsSuccessStatusCode)
                {
                    Global.ShowError("Failed searching for AddOns, error code: 0xfd");
                }
                await PopulateSearchList(await res.Content.ReadAsStringAsync());
            }
            catch (AggregateException ex) when (ex.InnerException is CloudFlareClearanceException)
            {
                // After all retries, clearance still failed.
                Global.ShowError("Failed searching for AddOns, error code: 0xfe");
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                // Looks like we ran into a timeout. Too many clearance attempts?
                // Maybe you should increase client.Timeout as each attempt will take about five seconds.
                Global.ShowError("Failed searching for AddOns, error code: 0xff");
            }
            finally
            {
                SearchBtn.IsEnabled = true;
            }
        }

        private async Task PopulateSearchList(string htmlResponse)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlResponse);
            var nav = doc.CreateNavigator();
            var res = nav.Select("/html/body/div[1]/main/div[1]/div[3]/ul/div/div[*]/div/div[2]/div[1]/a[1]");

            SearchNames.Clear();
            SearchUrls.Clear();

            foreach (HtmlNodeNavigator node in res)
            {
                var match = nameRegex.Match(node.CurrentNode.InnerHtml);
                if (!match.Success)
                    continue;

                string name = match.Groups[1].ToString();
                string url = node.CurrentNode.GetAttributeValue("href", "");

                SearchNames.Add(name);
                SearchUrls.Add(url);
            }
        }

        private void SearchQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableAddonsList.SelectedItem == null)
                return;

            var installWindow = new InstallWindow(SearchNames[AvailableAddonsList.SelectedIndex], SearchUrls[AvailableAddonsList.SelectedIndex], path);
            installWindow.Owner = this;
            installWindow.ShowDialog();
            PopulateInstalledAddons();
        }
    }
}
