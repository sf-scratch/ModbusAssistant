using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using ModbusAssistant.Utils;

namespace ModbusAssistant.CustomControls
{
    public class ShowLastListBox : ListBox
    {
        /// <summary>
        /// 获取和设置最大容量
        /// </summary>
        public int MaxCount
        {
            get { return (int)GetValue(MaxCountProperty); }
            set { SetValue(MaxCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxCountProperty =
            DependencyProperty.Register("MaxCount", typeof(int), typeof(ShowLastListBox), new PropertyMetadata(1000));

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null) { return; }
            //维持数据量在MaxCount内
            if (this.Items.Count >= MaxCount)
            {
                ObservableCollection<string> list = this.ItemsSource as ObservableCollection<string>;
                if (list != null && list.Count > 0)
                {
                    for (int i = 0; i < this.Items.Count - MaxCount; i++)
                    {
                        list.RemoveAt(0);
                    }
                }
            }
            //滚动到最后一行
            ScrollViewer scroll = CustomVisualTreeHelper.FindVisualChild<ScrollViewer>(this);
            scroll.ScrollToBottom();

            base.OnItemsChanged(e);
        }


    }
}
