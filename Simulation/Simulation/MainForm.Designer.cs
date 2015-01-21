namespace Simulation
{
    partial class simulation_gui
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.control_box = new System.Windows.Forms.GroupBox();
            this.south_bool = new System.Windows.Forms.CheckBox();
            this.close_button = new System.Windows.Forms.Button();
            this.start_button = new System.Windows.Forms.Button();
            this.statistics_report = new System.Windows.Forms.CheckBox();
            this.zombie_label = new System.Windows.Forms.Label();
            this.human_label = new System.Windows.Forms.Label();
            this.zombie_size = new System.Windows.Forms.TextBox();
            this.human_size = new System.Windows.Forms.TextBox();
            this.ip_addr = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ip_addr_box = new System.Windows.Forms.GroupBox();
            this.port_addr = new System.Windows.Forms.TextBox();
            this.port_label = new System.Windows.Forms.Label();
            this.ip_addr_label = new System.Windows.Forms.Label();
            this.status_box = new System.Windows.Forms.GroupBox();
            this.latency_ctr = new System.Windows.Forms.Label();
            this.zombie_ctr = new System.Windows.Forms.Label();
            this.human_ctr = new System.Windows.Forms.Label();
            this.sim_lat_info = new System.Windows.Forms.Label();
            this.zombie_info = new System.Windows.Forms.Label();
            this.human_info = new System.Windows.Forms.Label();
            this.status_label = new System.Windows.Forms.Label();
            this.status_info = new System.Windows.Forms.Label();
            this.control_box.SuspendLayout();
            this.ip_addr_box.SuspendLayout();
            this.status_box.SuspendLayout();
            this.SuspendLayout();
            // 
            // control_box
            // 
            this.control_box.Controls.Add(this.south_bool);
            this.control_box.Controls.Add(this.close_button);
            this.control_box.Controls.Add(this.start_button);
            this.control_box.Controls.Add(this.statistics_report);
            this.control_box.Controls.Add(this.zombie_label);
            this.control_box.Controls.Add(this.human_label);
            this.control_box.Controls.Add(this.zombie_size);
            this.control_box.Controls.Add(this.human_size);
            this.control_box.Location = new System.Drawing.Point(12, 12);
            this.control_box.Name = "control_box";
            this.control_box.Size = new System.Drawing.Size(395, 130);
            this.control_box.TabIndex = 0;
            this.control_box.TabStop = false;
            this.control_box.Text = "Controller";
            // 
            // south_bool
            // 
            this.south_bool.AutoSize = true;
            this.south_bool.Location = new System.Drawing.Point(147, 77);
            this.south_bool.Name = "south_bool";
            this.south_bool.Size = new System.Drawing.Size(105, 17);
            this.south_bool.TabIndex = 7;
            this.south_bool.Text = "South Simulation";
            this.toolTip1.SetToolTip(this.south_bool, "Show the map of player movements");
            this.south_bool.UseVisualStyleBackColor = true;
            this.south_bool.CheckedChanged += new System.EventHandler(this.south_bool_CheckedChanged);
            // 
            // close_button
            // 
            this.close_button.Location = new System.Drawing.Point(286, 81);
            this.close_button.Name = "close_button";
            this.close_button.Size = new System.Drawing.Size(102, 30);
            this.close_button.TabIndex = 6;
            this.close_button.Text = "End Simulation";
            this.toolTip1.SetToolTip(this.close_button, "Stop the Simulation");
            this.close_button.UseVisualStyleBackColor = true;
            this.close_button.Click += new System.EventHandler(this.close_button_Click);
            // 
            // start_button
            // 
            this.start_button.Location = new System.Drawing.Point(287, 19);
            this.start_button.Name = "start_button";
            this.start_button.Size = new System.Drawing.Size(102, 38);
            this.start_button.TabIndex = 5;
            this.start_button.Text = "Start Simulation";
            this.toolTip1.SetToolTip(this.start_button, "Start a new Simulation");
            this.start_button.UseVisualStyleBackColor = true;
            this.start_button.Click += new System.EventHandler(this.start_button_Click);
            // 
            // statistics_report
            // 
            this.statistics_report.AutoSize = true;
            this.statistics_report.Location = new System.Drawing.Point(147, 51);
            this.statistics_report.Name = "statistics_report";
            this.statistics_report.Size = new System.Drawing.Size(94, 17);
            this.statistics_report.TabIndex = 4;
            this.statistics_report.Text = "View Statistics";
            this.toolTip1.SetToolTip(this.statistics_report, "Show the statistics of client connections");
            this.statistics_report.UseVisualStyleBackColor = true;
            this.statistics_report.CheckedChanged += new System.EventHandler(this.statistics_report_CheckedChanged);
            // 
            // zombie_label
            // 
            this.zombie_label.AutoSize = true;
            this.zombie_label.Location = new System.Drawing.Point(6, 71);
            this.zombie_label.Name = "zombie_label";
            this.zombie_label.Size = new System.Drawing.Size(50, 13);
            this.zombie_label.TabIndex = 3;
            this.zombie_label.Text = "Zombies:";
            // 
            // human_label
            // 
            this.human_label.AutoSize = true;
            this.human_label.Location = new System.Drawing.Point(6, 29);
            this.human_label.Name = "human_label";
            this.human_label.Size = new System.Drawing.Size(49, 13);
            this.human_label.TabIndex = 2;
            this.human_label.Text = "Humans:";
            // 
            // zombie_size
            // 
            this.zombie_size.Location = new System.Drawing.Point(16, 87);
            this.zombie_size.Name = "zombie_size";
            this.zombie_size.Size = new System.Drawing.Size(100, 20);
            this.zombie_size.TabIndex = 1;
            this.zombie_size.Text = "Num Zombies";
            this.toolTip1.SetToolTip(this.zombie_size, "Enter the number of Zombies you want in the simuation");
            // 
            // human_size
            // 
            this.human_size.Location = new System.Drawing.Point(16, 45);
            this.human_size.Name = "human_size";
            this.human_size.Size = new System.Drawing.Size(100, 20);
            this.human_size.TabIndex = 0;
            this.human_size.Text = "Num Humans";
            this.toolTip1.SetToolTip(this.human_size, "Enter the number of humans you want in the simulation");
            // 
            // ip_addr
            // 
            this.ip_addr.Location = new System.Drawing.Point(16, 42);
            this.ip_addr.Name = "ip_addr";
            this.ip_addr.Size = new System.Drawing.Size(121, 20);
            this.ip_addr.TabIndex = 8;
            // 
            // ip_addr_box
            // 
            this.ip_addr_box.Controls.Add(this.port_addr);
            this.ip_addr_box.Controls.Add(this.port_label);
            this.ip_addr_box.Controls.Add(this.ip_addr);
            this.ip_addr_box.Controls.Add(this.ip_addr_label);
            this.ip_addr_box.Location = new System.Drawing.Point(12, 148);
            this.ip_addr_box.Name = "ip_addr_box";
            this.ip_addr_box.Size = new System.Drawing.Size(167, 128);
            this.ip_addr_box.TabIndex = 1;
            this.ip_addr_box.TabStop = false;
            this.ip_addr_box.Text = "IP Address";
            // 
            // port_addr
            // 
            this.port_addr.Location = new System.Drawing.Point(16, 88);
            this.port_addr.Name = "port_addr";
            this.port_addr.Size = new System.Drawing.Size(121, 20);
            this.port_addr.TabIndex = 10;
            // 
            // port_label
            // 
            this.port_label.AutoSize = true;
            this.port_label.Location = new System.Drawing.Point(13, 72);
            this.port_label.Name = "port_label";
            this.port_label.Size = new System.Drawing.Size(29, 13);
            this.port_label.TabIndex = 9;
            this.port_label.Text = "Port:";
            // 
            // ip_addr_label
            // 
            this.ip_addr_label.AutoSize = true;
            this.ip_addr_label.Location = new System.Drawing.Point(13, 26);
            this.ip_addr_label.Name = "ip_addr_label";
            this.ip_addr_label.Size = new System.Drawing.Size(145, 13);
            this.ip_addr_label.TabIndex = 0;
            this.ip_addr_label.Text = "IP Address of Load Balancer:";
            // 
            // status_box
            // 
            this.status_box.Controls.Add(this.latency_ctr);
            this.status_box.Controls.Add(this.zombie_ctr);
            this.status_box.Controls.Add(this.human_ctr);
            this.status_box.Controls.Add(this.sim_lat_info);
            this.status_box.Controls.Add(this.zombie_info);
            this.status_box.Controls.Add(this.human_info);
            this.status_box.Controls.Add(this.status_label);
            this.status_box.Controls.Add(this.status_info);
            this.status_box.Location = new System.Drawing.Point(185, 148);
            this.status_box.Name = "status_box";
            this.status_box.Size = new System.Drawing.Size(222, 128);
            this.status_box.TabIndex = 2;
            this.status_box.TabStop = false;
            this.status_box.Text = "Status";
            // 
            // latency_ctr
            // 
            this.latency_ctr.AutoSize = true;
            this.latency_ctr.Location = new System.Drawing.Point(183, 95);
            this.latency_ctr.Name = "latency_ctr";
            this.latency_ctr.Size = new System.Drawing.Size(13, 13);
            this.latency_ctr.TabIndex = 7;
            this.latency_ctr.Text = "0";
            // 
            // zombie_ctr
            // 
            this.zombie_ctr.AutoSize = true;
            this.zombie_ctr.Location = new System.Drawing.Point(183, 72);
            this.zombie_ctr.Name = "zombie_ctr";
            this.zombie_ctr.Size = new System.Drawing.Size(13, 13);
            this.zombie_ctr.TabIndex = 6;
            this.zombie_ctr.Text = "0";
            // 
            // human_ctr
            // 
            this.human_ctr.AutoSize = true;
            this.human_ctr.Location = new System.Drawing.Point(183, 49);
            this.human_ctr.Name = "human_ctr";
            this.human_ctr.Size = new System.Drawing.Size(13, 13);
            this.human_ctr.TabIndex = 5;
            this.human_ctr.Text = "0";
            // 
            // sim_lat_info
            // 
            this.sim_lat_info.AutoSize = true;
            this.sim_lat_info.Location = new System.Drawing.Point(7, 95);
            this.sim_lat_info.Name = "sim_lat_info";
            this.sim_lat_info.Size = new System.Drawing.Size(114, 13);
            this.sim_lat_info.TabIndex = 4;
            this.sim_lat_info.Text = "Server Latency to Sim:";
            // 
            // zombie_info
            // 
            this.zombie_info.AutoSize = true;
            this.zombie_info.Location = new System.Drawing.Point(6, 72);
            this.zombie_info.Name = "zombie_info";
            this.zombie_info.Size = new System.Drawing.Size(102, 13);
            this.zombie_info.TabIndex = 3;
            this.zombie_info.Text = "Number of Zombies:";
            // 
            // human_info
            // 
            this.human_info.AutoSize = true;
            this.human_info.Location = new System.Drawing.Point(7, 49);
            this.human_info.Name = "human_info";
            this.human_info.Size = new System.Drawing.Size(101, 13);
            this.human_info.TabIndex = 2;
            this.human_info.Text = "Number of Humans:";
            // 
            // status_label
            // 
            this.status_label.AutoSize = true;
            this.status_label.Location = new System.Drawing.Point(151, 26);
            this.status_label.Name = "status_label";
            this.status_label.Size = new System.Drawing.Size(45, 13);
            this.status_label.TabIndex = 1;
            this.status_label.Text = "Inactive";
            // 
            // status_info
            // 
            this.status_info.AutoSize = true;
            this.status_info.Location = new System.Drawing.Point(6, 26);
            this.status_info.Name = "status_info";
            this.status_info.Size = new System.Drawing.Size(98, 13);
            this.status_info.TabIndex = 0;
            this.status_info.Text = "State of Simulation:";
            // 
            // simulation_gui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(419, 288);
            this.Controls.Add(this.status_box);
            this.Controls.Add(this.ip_addr_box);
            this.Controls.Add(this.control_box);
            this.Name = "simulation_gui";
            this.Text = "Maurader\'s Map Controller";
            this.control_box.ResumeLayout(false);
            this.control_box.PerformLayout();
            this.ip_addr_box.ResumeLayout(false);
            this.ip_addr_box.PerformLayout();
            this.status_box.ResumeLayout(false);
            this.status_box.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox control_box;
        private System.Windows.Forms.Label zombie_label;
        private System.Windows.Forms.Label human_label;
        private System.Windows.Forms.TextBox zombie_size;
        private System.Windows.Forms.TextBox human_size;
        private System.Windows.Forms.Button close_button;
        private System.Windows.Forms.Button start_button;
        private System.Windows.Forms.CheckBox statistics_report;
        private System.Windows.Forms.CheckBox south_bool;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox ip_addr;
        private System.Windows.Forms.GroupBox ip_addr_box;
        private System.Windows.Forms.Label port_label;
        private System.Windows.Forms.Label ip_addr_label;
        private System.Windows.Forms.TextBox port_addr;
        private System.Windows.Forms.GroupBox status_box;
        private System.Windows.Forms.Label status_label;
        private System.Windows.Forms.Label status_info;
        private System.Windows.Forms.Label human_ctr;
        private System.Windows.Forms.Label sim_lat_info;
        private System.Windows.Forms.Label zombie_info;
        private System.Windows.Forms.Label human_info;
        private System.Windows.Forms.Label zombie_ctr;
        private System.Windows.Forms.Label latency_ctr;
    }
}

