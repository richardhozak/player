using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for ProgressSlider.xaml
    /// </summary>
    public partial class ProgressSlider : UserControl
    {
        private bool mouseCaptured = false;

        public event EventHandler<TimeEventArgs> TimeChanged;

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(double), typeof(ProgressSlider));

        public TimeSpan MaximumTime
        {
            get { return (TimeSpan)GetValue(MaximumTimeProperty); }
            set { SetValue(MaximumTimeProperty, value); }
        }

        public static readonly DependencyProperty MaximumTimeProperty = DependencyProperty.Register("MaximumTime", typeof(TimeSpan), typeof(ProgressSlider));

        public ProgressSlider()
        {
            InitializeComponent();
            DataContext = this;
        }

        private new void MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && mouseCaptured)
            {
                var x = e.GetPosition(volumeBar).X;
                var ratio = x / volumeBar.ActualWidth;
                Volume = ratio * volumeBar.Maximum;
                OnTimeChanged();
            }
        }

        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseCaptured = true;
            var x = e.GetPosition(volumeBar).X;
            var ratio = x / volumeBar.ActualWidth;
            Volume = ratio * volumeBar.Maximum;
            OnTimeChanged();
        }

        private new void MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseCaptured = false;
        }

        public TimeSpan CurrentTime
        {
            get
            {
                return (TimeSpan)GetValue(CurrentTimeProperty);
            }
            set
            {
                //if (this.MaximumTime.Subtract(value) < TimeSpan.Zero - TimeSpan.FromSeconds(5))
                //    throw new ArgumentException("value");

                SetValue(CurrentTimeProperty, value);

                Volume = (value.TotalMilliseconds / this.MaximumTime.TotalMilliseconds) * volumeBar.Maximum;
            }
        }

        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register("CurrentTime", typeof(TimeSpan), typeof(ProgressSlider));

        [Obsolete("Use CurrentTime property")]
        public void SetTime(TimeSpan time)
        {
            if (this.MaximumTime.Subtract(time) < TimeSpan.Zero)
                throw new ArgumentException("time");

            Volume = (time.TotalMilliseconds / this.MaximumTime.TotalMilliseconds) * volumeBar.Maximum;
        }

        private void OnTimeChanged()
        {
            if (this.TimeChanged != null)
            {
                this.TimeChanged(this, new TimeEventArgs(TimeSpan.FromMilliseconds(this.MaximumTime.TotalMilliseconds * (Volume / 100))));
            }
        }
    }
}
