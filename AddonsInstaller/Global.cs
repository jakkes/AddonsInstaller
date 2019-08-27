using CloudFlareUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AddonsInstaller
{
    public static class Global
    {
        public static HttpClient HTTP;
        static Global()
        {
            setupCurse();
        }
        public static void ShowError(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void setupCurse()
        {
            ClearanceHandler handler = new ClearanceHandler();
            HTTP = new HttpClient(handler);
        }
    }
}
