using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace DriftPlayer
{
    class ViewModel : DependencyObject
    {
        private Player player;
        public ObservableCollection<IPlayable> Playlist { get; private set; }
        private Dispatcher uiDispatcher;

        public ViewModel(Player p, Dispatcher UIDispatcher)
        {
            this.uiDispatcher = UIDispatcher;
            this.player = p;
            this.Playlist = new ObservableCollection<IPlayable>();
            this.player.PlayableAdded += (s, e) =>
            {
                this.uiDispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                {
                    IPlayable playable = s as IPlayable;
                    Playlist.Add(playable);
                }));
            };
        }
    }
}
