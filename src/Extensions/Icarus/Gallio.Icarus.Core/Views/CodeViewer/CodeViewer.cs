// Copyright 2005-2025 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// Portions Copyright 2018-2025 Bart Suelze
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// .NET 8 replacement: ICSharpCode.TextEditor not available; uses System.Windows.Forms.RichTextBox.

using System;
using System.IO;
using System.Windows.Forms;
using Gallio.Common.Reflection;

namespace Gallio.Icarus.Views.CodeViewer
{
    public partial class CodeViewer : UserControl
    {
        public CodeViewer(CodeLocation codeLocation)
        {
            InitializeComponent();

            if (codeLocation != CodeLocation.Unknown && codeLocation.Path != null)
            {
                try
                {
                    richTextBox.Text = File.ReadAllText(codeLocation.Path);
                    richTextBox.Font = new System.Drawing.Font("Courier New", 9.5f);
                    richTextBox.WordWrap = false;
                    richTextBox.ReadOnly = true;

                    if (codeLocation.Line > 0)
                        JumpTo(codeLocation.Line, codeLocation.Column);
                }
                catch (Exception)
                {
                    // If file cannot be read, show nothing
                }
            }
        }

        public void JumpTo(int line, int column)
        {
            if (line <= 0) return;
            int lineIndex = richTextBox.GetFirstCharIndexFromLine(line - 1);
            if (lineIndex < 0) return;
            int charIndex = lineIndex + Math.Max(0, column - 1);
            if (charIndex > richTextBox.TextLength) charIndex = lineIndex;
            richTextBox.Select(charIndex, 0);
            richTextBox.ScrollToCaret();
        }
    }
}
