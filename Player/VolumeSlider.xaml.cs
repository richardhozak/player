using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for ProgressSlider.xaml
    /// </summary>
    public partial class VolumeSlider : UserControl
    {
        private bool mouseCaptured;

        public event EventHandler<VolumeEventArgs> VolumeChanged;

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(double), typeof(VolumeSlider));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(VolumeSlider), new PropertyMetadata(Target));

        private static void Target(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            VolumeSlider slider = (VolumeSlider)dependencyObject;
            slider.volumeBar.Maximum = (double)dependencyPropertyChangedEventArgs.NewValue;
        }

        public VolumeSlider()
        {
            InitializeComponent();
            DataContext = this;
            this.PreviewMouseWheel += VolumeSlider_PreviewMouseWheel;
        }

        void VolumeSlider_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && this.Volume < this.Maximum)
                this.Volume += 0.01;
            else if (e.Delta < 0 && this.Volume > 0)
                this.Volume -= 0.01;
            this.OnVolumeChanged();
        }

        private new void MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && mouseCaptured)
            {
                var x = e.GetPosition(volumeBar).X;
                var ratio = x / volumeBar.ActualWidth;
                Volume = ratio * volumeBar.Maximum;
                OnVolumeChanged();
            }
        }

        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseCaptured = true;
            var x = e.GetPosition(volumeBar).X;
            var ratio = x / volumeBar.ActualWidth;
            Volume = ratio * volumeBar.Maximum;
            OnVolumeChanged();
        }

        private new void MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseCaptured = false;
        }
        
        private void OnVolumeChanged()
        {
            if (this.VolumeChanged != null)
            {
                this.VolumeChanged(this, new VolumeEventArgs(this.Volume));
            }
        }
    }
}
