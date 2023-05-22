namespace Dragonfly.Graphics.Test
{
    partial class FrmTriangleTest
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
            this.pnlTarget = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // pnlTarget
            // 
            this.pnlTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlTarget.Location = new System.Drawing.Point(9, 9);
            this.pnlTarget.Margin = new System.Windows.Forms.Padding(0);
            this.pnlTarget.Name = "pnlTarget";
            this.pnlTarget.Size = new System.Drawing.Size(466, 398);
            this.pnlTarget.TabIndex = 0;
            // 
            // FrmTriangleTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 416);
            this.Controls.Add(this.pnlTarget);
            this.Name = "FrmTriangleTest";
            this.Text = "FrmTriangleTest";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.ResizeEnd += new System.EventHandler(this.OnResizeEnd);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTarget;
    }
}