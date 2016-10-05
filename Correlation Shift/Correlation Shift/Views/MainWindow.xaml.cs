using Correlation_Shift.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Correlation_Shift.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null) return null;
            if (element.GetType() == type) return element;
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        private void ListBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                ListBox s = sender as ListBox;
                MainWindowViewModel vm = this.DataContext as MainWindowViewModel;

                if (s != null && vm != null)
                {
                    switch (Convert.ToInt32(s.Tag))
                    {
                        case 1:
                            foreach(string item in s.SelectedItems.OfType<string>().ToList())
                            {
                                vm.ChannelOneFiles.Remove(item);
                            }
                            break;
                        case 2:
                            foreach (string item in s.SelectedItems.OfType<string>().ToList())
                            {
                                vm.ChannelTwoFiles.Remove(item);
                            }
                            break;
                    }

                    vm.CalculateImageShift.RaiseCanExecuteChanged();
                }
            }
        }

        private void Channel1Listbox_ScrollChanged(object sender, RoutedEventArgs e)
        {
            ScrollViewer _listboxScrollViewer1 = GetDescendantByType(Channel1Listbox, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer _listboxScrollViewer2 = GetDescendantByType(Channel2Listbox, typeof(ScrollViewer)) as ScrollViewer;
            _listboxScrollViewer2.ScrollToVerticalOffset(_listboxScrollViewer1.VerticalOffset);
        }

        private void Channel2Listbox_ScrollChanged(object sender, RoutedEventArgs e)
        {
            ScrollViewer _listboxScrollViewer1 = GetDescendantByType(Channel2Listbox, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer _listboxScrollViewer2 = GetDescendantByType(Channel1Listbox, typeof(ScrollViewer)) as ScrollViewer;
            _listboxScrollViewer2.ScrollToVerticalOffset(_listboxScrollViewer1.VerticalOffset);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
