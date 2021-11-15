using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleModbusPLC
{
    public partial class Form1 : Form
    {
        private readonly InterfaceModbus interfaceModbus = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            string address = textBoxAddress.Text;
            if (address.Length <= 0)
            {
                MessageBox.Show("no input address");
            }
            
            if (!int.TryParse(textBoxPort.Text, out int port))
            {
                MessageBox.Show("failed: invaild port");
            }

            var task = Task.Run(() => interfaceModbus.Connect(address, port));
            task.ContinueWith(x =>
            {
                if (x.Result)
                {
                    MessageBox.Show("Success: Connect");
                    Task.Run(() => interfaceModbus.ShiftReadWrite());
                }
                else
                {
                    MessageBox.Show("Failed: Connect");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
