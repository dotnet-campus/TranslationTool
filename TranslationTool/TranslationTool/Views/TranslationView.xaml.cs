﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TranslationTool.ViewModels;

namespace TranslationTool.Views
{
    /// <summary>
    /// TranslationView.xaml 的交互逻辑
    /// </summary>
    public partial class TranslationView : UserControl
    {
        public TranslationView()
        {
            InitializeComponent();
            //#if DEBUG
            //            DetailCheckBox.Visibility = Visibility.Visible;
            //            ApiComboBox.Visibility = Visibility.Visible;
            //#endif
        }

        private void SearchingTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                (this.DataContext as TranslationViewModel)?.SearchCommand.Execute(null);
            }
        }

        private void ApiComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (this.DataContext as TranslationViewModel)?.SearchCommand.Execute(null);
        }
    }
}
