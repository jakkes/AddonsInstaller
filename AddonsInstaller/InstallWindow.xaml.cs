using CloudFlareUtilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using HtmlAgilityPack;
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
    /// Interaction logic for InstallWindow.xaml
    /// </summary>
    public partial class InstallWindow : Window
    {

        private static Random random = new Random();

        private string Name;
        private string Url;
        private string AddonDir;

        private ObservableCollection<string> Names = new ObservableCollection<string>();
        private ObservableCollection<string> Ids = new ObservableCollection<string>();

        public InstallWindow(string name, string url, string addonDir)
        {
            Name = name; Url = url; AddonDir = addonDir;

            InitializeComponent();
            AddonNameTxt.Text = "Loading " + name;
            VersionSelect.ItemsSource = Names;
            LoadVersions();
        }

        private async Task LoadVersions()
        {
            try
            {
                var res = await Global.HTTP.GetAsync("https://curseforge.com" + Url + "/files");
                if (!res.IsSuccessStatusCode)
                {
                    Global.ShowError("Failed searching for AddOns, error code: 0xfd");
                }
                await PopulateVersions(await res.Content.ReadAsStringAsync());

            }
            catch (AggregateException ex) when(ex.InnerException is CloudFlareClearanceException)
            {
                // After all retries, clearance still failed.
                Global.ShowError("Failed retrieving versions, error code: 0xfe");
            }
            catch (AggregateException ex) when(ex.InnerException is TaskCanceledException)
            {
                // Looks like we ran into a timeout. Too many clearance attempts?
                // Maybe you should increase client.Timeout as each attempt will take about five seconds.
                Global.ShowError("Failed retrieving versions, error code: 0xff");
            }
            finally
            {
                AddonNameTxt.Text = "Choose version of " + Name;
            }
        }

        private async Task PopulateVersions(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            Names.Clear();
            Ids.Clear();

            var nav = doc.CreateNavigator();
            var res = nav.Select("/html/body/div[1]/main/div[1]/div[2]/section/div/div/div/section/div[2]/div[2]/div/div/table/tbody/tr[*]/td[2]/a");
            var res2 = nav.Select("/html/body/div[1]/main/div[1]/div[2]/section/div/div/div/section/div[2]/div[2]/div/div/table/tbody/tr[*]/td[5]/div/div");

            if (res.Count == 0)
            {
                res = nav.Select("/html/body/div[1]/main/div[1]/div[2]/section/div/div/div/section/div[3]/div[2]/div/div/table/tbody/tr[*]/td[2]/a[1]");
                res2 = nav.Select("/html/body/div[1]/main/div[1]/div[2]/section/div/div/div/section/div[3]/div[2]/div/div/table/tbody/tr[*]/td[5]/div/div");
            }

            if (res.Count == 0)
            {
                Global.ShowError("Failed to load versions...");
            }

            foreach (HtmlNodeNavigator node in res)
            {
                var urlsplit = node.CurrentNode.GetAttributeValue("href", "").Split('/');
                res2.MoveNext();
                HtmlNodeNavigator versionNode = (HtmlNodeNavigator)res2.Current;

                Names.Add(node.CurrentNode.InnerText + " - " + versionNode.CurrentNode.InnerText.Replace("\r\n", "").Trim());
                Ids.Add(urlsplit[urlsplit.Length - 1]);
            }
        }

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VersionSelect.SelectedItem == null)
                return;
            InstallBtn.IsEnabled = false;
            Progress.Value = 25;
            string fileName = await Download();
            if (string.IsNullOrEmpty(fileName))
            {
                Global.ShowError("Failed downloading the file");
                return;
            }
            Progress.Value = 75;
            Install(fileName);
            Progress.Value = 100;
            InstallBtn.IsEnabled = true;
            InstallBtn.Content = "Close";
            InstallBtn.Click -= InstallBtn_Click;
            InstallBtn.Click += (o, args) =>
            {
                this.Close();
            };
        }

        private async Task<string> Download()
        {
            var req = await Global.HTTP.GetAsync("https://curseforge.com" + Url + "/download/" + Ids[VersionSelect.SelectedIndex] + "/file");
            if (!req.IsSuccessStatusCode)
                return null;

            var tmpName = random.Next(9999999).ToString() + ".tmp";

            await req.Content.ReadAsFileAsync(tmpName, true);
            return tmpName;
        }

        private void Install(string tmpName)
        {
            try
            {
                ZipFile.ExtractToDirectory(tmpName, AddonDir);
            } catch (IOException)
            {
                var tmpDir = tmpName.Substring(0, tmpName.Length - 4);
                var res = MessageBox.Show("AddOn seems to be installed already, do you wish to overwrite it?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Directory.CreateDirectory(tmpDir);
                    ZipFile.ExtractToDirectory(tmpName, tmpDir);
                    foreach (var dir in Directory.EnumerateDirectories(tmpDir))
                    {
                        var dirname = dir.Split('\\')[1];
                        if (Directory.Exists(AddonDir + "/" + dirname))
                            Directory.Delete(AddonDir + "/" + dirname, true);
                        Directory.Move(dir, AddonDir + "/" + dirname);
                    }
                    Directory.Delete(tmpDir, true);
                }
            }
            finally {
                File.Delete(tmpName);
            }
        }
    }
}
