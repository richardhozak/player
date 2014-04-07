using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DriftPlayer
{
    class ApplicationMainViewModel : INotifyPropertyChanged
    {
        private Player player;
        private float volumeValue;
        public ObservableCollection<IPlayable> Playlist { get; set; }
        private IPlayable selected;
        private DispatcherTimer timer;


        public event EventHandler<TimeEventArgs> CurrentTimeChanged;
        public event EventHandler<TimeEventArgs> MaximumTimeChanged;

        private TimeSpan maximumTime;
        public TimeSpan MaximumTime 
        {
            get 
            {
                return this.maximumTime;
            }
            private set 
            {
                if (this.maximumTime.Subtract(value) == TimeSpan.Zero)
                    return;

                this.maximumTime = value;

                if (this.MaximumTimeChanged != null)
                    this.MaximumTimeChanged(this, new TimeEventArgs(value));

                this.OnPropertyChanged("MaximumTime");
            }
        }

        private TimeSpan currentTime;
        public TimeSpan CurrentTime
        {
            get 
            {
                return this.currentTime;
            }
            private set 
            {
                if (this.currentTime.Subtract(value) == TimeSpan.Zero)
                    return;

                this.currentTime = value;

                if (this.CurrentTimeChanged != null)
                    this.CurrentTimeChanged(this, new TimeEventArgs(value));

                this.OnPropertyChanged("CurrentTime");
            }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get
            {
                return this.isPaused;
            }
            set
            {
                this.isPaused = value;
                this.OnPropertyChanged("IsPaused");
            }
        }

        private SideWindow playlistWindow;

        public ApplicationMainViewModel(Player p)
        {
            this.player = p;
            this.volumeValue = 0.5f;
            this.Playlist = new ObservableCollection<IPlayable>(p.Playlist);
            this.player.PlayableAdded += (s, e) =>
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                {
                    IPlayable playable = s as IPlayable;
                    Playlist.Add(playable);
                }));
            };
            this.player.PlayableRemoved += (s, e) =>
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                {
                    IPlayable playable = s as IPlayable;
                    Playlist.Remove(playable);
                }));
                
            };
            registerCommands();

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(50);
            this.timer.Tick += timer_Tick;
            this.timer.IsEnabled = true;

            this.playlistWindow = new SideWindow();
            this.playlistWindow.DataContext = this;
        }


        void timer_Tick(object sender, EventArgs e)
        {
            if (this.player == null)
                return;

            this.MaximumTime = this.player.TotalTime;
            this.CurrentTime = this.player.CurrentTime;
            if (this.CurrentTime >= this.MaximumTime)
                this.player.Next();

            this.IsPaused = this.player.State == PlaybackState.Paused || this.player.State == PlaybackState.Stopped;
        }

        private void registerCommands()
        {
            this.PlayCommand = new RelayCommand(this.Play);
            this.PauseCommand = new RelayCommand(this.Pause);
            this.StopCommand = new RelayCommand(this.Stop);
            this.CloseCommand = new RelayCommand(this.Close);
            //this.AddCommand = new RelayCommand(new Action(() => this.Add()));
            this.ChangeAudioCommand = new RelayCommand(ChangeAudio);
            this.OpenClosePlaylistCommand = new RelayCommand(this.OpenClosePlaylist);
            this.AddFilesCommand = new RelayCommand(this.AddFiles);
            this.AddYoutubeCommand = new RelayCommand(this.AddYoutube);
            this.AddSoundcloudCommand = new RelayCommand(this.AddSoundcloud);
            this.NextCommand = new RelayCommand(this.player.Next);
            this.PreviousCommand = new RelayCommand(this.player.Previous);
            this.RemoveAudioCommand = new RelayCommand<IPlayable>(this.Remove);
        }

        private void AddSoundcloud()
        {
            AddToPlaylistDialogBox dialog = new AddToPlaylistDialogBox();
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                try
                {
                    this.player.Add(new SoundcloudAudio(dialog.URL));
                }
                catch (Exception)
                {
                    MessageBox.Show("Nepodařilo se přidat " + dialog.URL);
                }
            }
        }

        private void AddYoutube()
        {
            AddToPlaylistDialogBox dialog = new AddToPlaylistDialogBox();
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                try
                {
                    this.player.Add(new YoutubeAudio(dialog.URL));
                }
                catch (Exception)
                {
                    MessageBox.Show("Nepodařilo se přidat " + dialog.URL);
                }
            }
        }

        private void AddFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Audio files|*.wav;*.aiff;*.mp3;*.aac;*.flac;*.ogg;*.wma";
            if (dialog.ShowDialog() == true)
            {
                foreach (string fileName in dialog.FileNames)
                {
                    try
                    {
                        this.player.Add(new Mp3Audio(fileName)); 
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Nepodařilo se přidat " + fileName);
                    }
                }
            }
        }

        private void OpenClosePlaylist()
        {
            this.playlistWindow.ActivateAnimate();
            this.playlistWindow.Focus();
        }

        private void ChangeAudio()
        {
            Output output = OutputProvider.Output;
            switch (output)
            {
                case Output.ASIO:
                    OutputProvider.Output = Output.WAVE;
                    break;
                case Output.WAVE:
                    OutputProvider.Output = Output.ASIO;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Remove(IPlayable track)
        {
            this.player.Remove(track);
        }

        private void Play()
        {
            this.IsPaused = false;
            if (player.Volume == 0.0f)
                player.Volume = volumeValue;
            player.Play();
        }

        private void Pause()
        {
            this.IsPaused = true;
            player.Pause();
        }

        private void Stop()
        {
            this.IsPaused = false;
            player.Stop();
        }

        private void Close()
        {
            this.playlistWindow.Close();
            player.Dispose();
            Properties.Settings.Default.Save();
            Environment.Exit(0);
        }

        private void Add()
        {
            AddToPlaylistDialogBox atpdb = new AddToPlaylistDialogBox();
            if (atpdb.ShowDialog() == true)
            {
                try
                {
                    player.Add(new YoutubeAudio(atpdb.URL));
                }
                catch
                {
                    MessageBox.Show("Špatné URL nebo ID videa.");
                }
            }
        }

        public void ChangeVolume(float value)
        {
            this.volumeValue = value;
            if (player != null)
                player.Volume = this.volumeValue;
        }

        public void Select(IPlayable p)
        {
            this.selected = p;
        }

        public IPlayable Selected
        {
            get
            {
                return this.selected;
            }
            set
            {
                this.selected = value;
                this.OnPropertyChanged("Selected");
            }
        }

        public void PlaySelected()
        {
            this.IsPaused = false;
            if (this.selected != null)
                player.Play(this.selected);
        }

        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        //public ICommand AddCommand { get; private set; }
        public ICommand OpenPlaylistCommand { get; private set; }
        public ICommand ChangeAudioCommand { get; private set; }
        public ICommand OpenClosePlaylistCommand { get; private set; }
        public ICommand AddYoutubeCommand { get; private set; }
        public ICommand AddSoundcloudCommand { get; private set; }
        public ICommand AddFilesCommand { get; private set; }
        public ICommand NextCommand { get; private set; }
        public ICommand PreviousCommand { get; private set; }
        public ICommand RemoveAudioCommand { get; private set; }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null) 
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
