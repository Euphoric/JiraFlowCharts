using Jira.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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

namespace Jira.FlowCharts.JiraUpdate
{
    /// <summary>
    /// Interaction logic for JiraUpdateView.xaml
    /// </summary>
    public partial class JiraUpdateView : UserControl, IJiraUpdateView
    {
        public JiraUpdateView()
        {
            InitializeComponent();
        }

        public SecureString GetLoginPassword()
        {
            return JiraPasswordBox.SecurePassword;
        }
    }
}
