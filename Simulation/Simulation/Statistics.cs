using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Simulation
{

    public partial class Statistics : Form
    {
        private Info[] ctr;

        public Statistics(Info[] ctr)
        {
            // TODO: Complete member initialization
            this.ctr = ctr;
            InitializeComponent();
            lat_view.DataSource = ctr;
        }

        public void redraw_gui()
        {
            if (lat_view != null)
            {
                if (InvokeRequired)
                {
                    MethodInvoker method = new MethodInvoker(redraw_gui);
                    Invoke(method);
                    return;
                }
                lat_view.Refresh();
                lat_view.Update();
            }

        }

        private void update_btn_Click(object sender, EventArgs e)
        {
            redraw_gui();
        }

        private void lat_view_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            redraw_gui();
        }

        private void lat_view_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            redraw_gui();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            redraw_gui();
        }


        internal void force_close()
        {
            if (InvokeRequired)
            {
                MethodInvoker method = new MethodInvoker(force_close);
                Invoke(method);
                return;
            }
            Close();
        }
    }
}
