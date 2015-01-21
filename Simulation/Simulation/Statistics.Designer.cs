namespace Simulation
{
    partial class Statistics
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
            this.lat_view = new System.Windows.Forms.DataGridView();
            this.update_btn = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.containerBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.lat_view)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.containerBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // lat_view
            // 
            this.lat_view.AllowUserToAddRows = false;
            this.lat_view.AllowUserToDeleteRows = false;
            this.lat_view.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.lat_view.Location = new System.Drawing.Point(12, 12);
            this.lat_view.Name = "lat_view";
            this.lat_view.ReadOnly = true;
            this.lat_view.Size = new System.Drawing.Size(362, 588);
            this.lat_view.TabIndex = 0;
            this.lat_view.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.lat_view_CellContentClick);
            // 
            // update_btn
            // 
            this.update_btn.Location = new System.Drawing.Point(384, 12);
            this.update_btn.Name = "update_btn";
            this.update_btn.Size = new System.Drawing.Size(75, 61);
            this.update_btn.TabIndex = 1;
            this.update_btn.Text = "Update";
            this.update_btn.UseVisualStyleBackColor = true;
            this.update_btn.Click += new System.EventHandler(this.update_btn_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(384, 539);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 61);
            this.button1.TabIndex = 2;
            this.button1.Text = "Update";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // containerBindingSource
            // 
            //this.containerBindingSource.DataSource = typeof(Simulation.Container); //maybe wrong
            // 
            // Statistics
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 608);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.update_btn);
            this.Controls.Add(this.lat_view);
            this.Name = "Statistics";
            this.Text = "Latency Statistics";
            ((System.ComponentModel.ISupportInitialize)(this.lat_view)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.containerBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView lat_view;
        private System.Windows.Forms.BindingSource containerBindingSource;
        private System.Windows.Forms.Button update_btn;
        private System.Windows.Forms.Button button1;


    }
}