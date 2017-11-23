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

namespace WhatsUpOnAntyradioWPF
{
    /// <summary>
    /// Interaction logic for History.xaml
    /// </summary>
    public partial class History : Window
    {
        public History()
        {
            InitializeComponent();


            for (int x = 1; x <= 50; x++)
            {
                listBox.Items.Add("Item " + x.ToString());
            }
        }
        
    }
}
