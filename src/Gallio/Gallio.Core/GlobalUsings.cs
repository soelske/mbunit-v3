// Explicit global using directives for replacement files in Gallio.Core.
// ImplicitUsings=disable is intentional to prevent Timer ambiguity (System.Threading.Timer
// vs System.Windows.Forms.Timer). These are manually curated to provide the same
// common namespaces without SDK auto-import.

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Threading;
global using System.Threading.Tasks;
