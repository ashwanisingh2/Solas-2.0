using System;
using System.Windows.Controls;

namespace Modules.DriverManagement.Infrastructure
{
    public class NavigationService
    {
        private readonly ContentControl _host;

        public NavigationService(ContentControl host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void Navigate(UserControl view)
        {
            _host.Content = view;
        }
    }
}
