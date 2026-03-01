// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// .NET 8 replacement: uses RichTextBox instead of ICSharpCode.TextEditor.

namespace Gallio.Icarus.Views.CodeViewer
{
    partial class CodeViewer
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // richTextBox
            //
            this.richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox.Location = new System.Drawing.Point(0, 0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.richTextBox.Size = new System.Drawing.Size(292, 273);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.Text = "";
            //
            // CodeViewer
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this.richTextBox);
            this.Name = "CodeViewer";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.RichTextBox richTextBox;
    }
}
