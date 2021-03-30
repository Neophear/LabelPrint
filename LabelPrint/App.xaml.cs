using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelPrint
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            // Setup Quick Converter.
            // Add the System namespace so we can use primitive types (i.e. int, etc.).
            QuickConverter.EquationTokenizer.AddNamespace(typeof(object));
        }
    }
}
