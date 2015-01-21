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
    public partial class simulation_gui : Form
    {
        private bool show_stats;
        private bool is_south;
        private Statistics stat;
        private Simulation sim;
        private string change;


        public simulation_gui()
        {
            InitializeComponent();
            show_stats = false;
            is_south = false;
        }

        private void start_button_Click(object sender, EventArgs e)
        {
            status_label.Text = "Setting Up...";
            try
            {
                string addr = ip_addr.Text;
                int port = Int32.Parse(port_addr.Text);

                int humans = Int32.Parse(human_size.Text);
                int zombies = Int32.Parse(zombie_size.Text);
                human_size.Text = "";
                zombie_size.Text = "";
                human_ctr.Text = humans.ToString();
                zombie_ctr.Text = zombies.ToString();

                Simulation sim = new Simulation(humans, zombies, this,addr,port,is_south);
                this.sim = sim;
                
                
                Thread thread = new Thread(new ThreadStart(sim.start));
                thread.Start();
                
                status_label.Text = "Simulation Initialized...";
                if (show_stats)
                {
                    status_label.Text = "Creating Table...";
                    Thread stat = new Thread(new ThreadStart(launch_table));
                    stat.Start();
                }
                
                status_label.Text = "Simulating";
                
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void update(string str)
        {
            change = str;
            if (InvokeRequired)
            {
                MethodInvoker method = new MethodInvoker(change_text_box);
                Invoke(method);
                return;
            }
            else
            {
                status_label.Text = str;
            }
            
        }

        public void redraw_table()
        {
            if(stat != null)
            {
                stat.redraw_gui();
            }
        }

        private void change_text_box()
        {
            status_label.Text = change;
        }

        private void close_button_Click(object sender, EventArgs e)
        {
            status_label.Text = "Tearing down...";
            human_ctr.Text = "0";
            zombie_ctr.Text = "0";

            
            if(sim != null)
                sim.finish();
            status_label.Text = "TCP connection closed";
            if (stat != null)
                stat.force_close();
            status_label.Text = "Inactive";
        }





        private void launch_table()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            stat = new Statistics(sim.get_stats());
            Application.Run(stat);
        }

        private void statistics_report_CheckedChanged(object sender, EventArgs e)
        {
            show_stats = !show_stats;
        }

        public bool get_stats_displayed()
        {
            return show_stats;
        }

        private void south_bool_CheckedChanged(object sender, EventArgs e)
        {
            is_south = !is_south;
        }
    }
}
