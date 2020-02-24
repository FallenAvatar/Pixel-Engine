namespace olc.PixelEngine.WinForms {
	partial class frmEngine {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;


		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.SuspendLayout();
			// 
			// frmEngine
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Name = "frmEngine";
			this.Text = "PixelEngine";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmEngine_FormClosing);
			this.Load += new System.EventHandler(this.frmEngine_Load);
			this.ResizeEnd += new System.EventHandler(this.frmEngine_ResizeEnd);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.frmEngine_Paint);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmEngine_KeyDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmEngine_KeyUp);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.frmEngine_MouseDown);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.frmEngine_MouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmEngine_MouseUp);
			this.ResumeLayout(false);

		}

		#endregion
	}
}