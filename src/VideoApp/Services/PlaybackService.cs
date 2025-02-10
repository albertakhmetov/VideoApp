/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of VideoApp.
 *
 *  VideoApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  VideoApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with VideoApp. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace VideoApp.Services;

using System;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LibVLCSharp.Platforms.Windows;
using LibVLCSharp.Shared;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public sealed class PlaybackService : IPlaybackService
{
    private LibVLC? libVCL;
    private MediaPlayer? mediaPlayer;
    private CompositeDisposable? disposable;

    private readonly BehaviorSubject<string?> mediaFileNameSubject;
    private readonly BehaviorSubject<PlaybackState> stateSubject;
    private readonly BehaviorSubject<int> durationSubject, positionSubject;
    private readonly BehaviorSubject<int> volumeSubject;

    private readonly BehaviorSubject<ImmutableArray<TrackInfo>> audioTrackInfoSubject, subtitleTrackInfoSubject;
    private readonly BehaviorSubject<int> audioTrackSubject, subtitleTrackSubject;

    private long? lastSetPosition;

    public PlaybackService()
    {
        Core.Initialize("./LibVLC");

        if (!File.Exists("./LibVLC/plugins/plugins.dat"))
        {
            new LibVLC("--reset-plugins-cache");
        }

        mediaFileNameSubject = new BehaviorSubject<string?>(null);
        stateSubject = new BehaviorSubject<PlaybackState>(PlaybackState.NotInitialized);
        durationSubject = new BehaviorSubject<int>(0);
        positionSubject = new BehaviorSubject<int>(0);
        volumeSubject = new BehaviorSubject<int>(100);

        audioTrackInfoSubject = new BehaviorSubject<ImmutableArray<TrackInfo>>([]);
        subtitleTrackInfoSubject = new BehaviorSubject<ImmutableArray<TrackInfo>>([]);

        audioTrackSubject = new BehaviorSubject<int>(-1);
        subtitleTrackSubject = new BehaviorSubject<int>(-1);

        MediaFileName = mediaFileNameSubject.AsObservable();
        State = stateSubject.AsObservable();
        Duration = durationSubject.AsObservable();
        Position = positionSubject.AsObservable();
        Volume = volumeSubject.AsObservable();
        AudioTracks = audioTrackInfoSubject.AsObservable();
        SubtitleTracks = subtitleTrackInfoSubject.AsObservable();
        AudioTrack = audioTrackSubject.AsObservable();
        SubtitleTrack = subtitleTrackSubject.AsObservable();
    }

    public bool IsInitialized => libVCL != null;

    public IObservable<string?> MediaFileName { get; }

    public IObservable<PlaybackState> State { get; }

    public IObservable<int> Duration { get; }

    public IObservable<int> Position { get; }

    public IObservable<int> Volume { get; }

    public IObservable<ImmutableArray<TrackInfo>> AudioTracks { get; }

    public IObservable<ImmutableArray<TrackInfo>> SubtitleTracks { get; }

    public IObservable<int> AudioTrack { get; }

    public IObservable<int> SubtitleTrack { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;

        mediaPlayer?.Dispose();
        libVCL?.Dispose();

        mediaPlayer = null;
        libVCL = null;
    }

    public void Initialize(object sender, string[] options)
    {
        if (sender is VideoView videoView && SynchronizationContext.Current != null)
        {
            disposable = new CompositeDisposable();

            libVCL = new LibVLC(options);
            mediaPlayer = new MediaPlayer(libVCL);
            mediaPlayer.Volume = 100;

            Observable
                .FromEventPattern<MediaPlayerLengthChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.LengthChanged))
                .Select(x => Convert.ToInt32(x.EventArgs.Length / 1000))
                .Subscribe(x => durationSubject.OnNext(x))
                .DisposeWith(disposable);

            Observable
                .Interval(TimeSpan.FromMilliseconds(500))
                .Select(x => Convert.ToInt32(mediaPlayer.Time / 1000))
                .Where(x => lastSetPosition == null || lastSetPosition == x)
                .Distinct()
                .Subscribe(x =>
                {
                    lastSetPosition = null;
                    positionSubject.OnNext(x);
                })
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<MediaPlayerVolumeChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.VolumeChanged))
                .Select(x => Convert.ToInt32(x.EventArgs.Volume * 100))
                .Where(x => x >= 0)
                .Subscribe(x => volumeSubject.OnNext(x))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<EventArgs>(mediaPlayer, nameof(MediaPlayer.Opening))
                .Subscribe(x => stateSubject.OnNext(PlaybackState.Opening))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<EventArgs>(mediaPlayer, nameof(MediaPlayer.Playing))
                .Subscribe(x => stateSubject.OnNext(PlaybackState.Playing))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<EventArgs>(mediaPlayer, nameof(MediaPlayer.Paused))
                .Subscribe(x => stateSubject.OnNext(PlaybackState.Paused))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<EventArgs>(mediaPlayer, nameof(MediaPlayer.Stopped))
                .Subscribe(x => stateSubject.OnNext(PlaybackState.Stopped))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<EventArgs>(mediaPlayer, nameof(MediaPlayer.EndReached))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => mediaPlayer.Stop())
                .DisposeWith(disposable);

            stateSubject
                .Where(x => x == PlaybackState.Stopped)
                .Subscribe(_ => positionSubject.OnNext(0))
                .DisposeWith(disposable);

            stateSubject
                .Where(x => x == PlaybackState.Playing)
                .Subscribe(_ => NotifyCurrentState())
                .DisposeWith(disposable);

            videoView.MediaPlayer = mediaPlayer;
            stateSubject.OnNext(PlaybackState.Closed);
        }
    }

    public async Task<bool> Load(string fileName)
    {
        if (libVCL == null || mediaPlayer == null)
        {
            return false;
        }

        if (fileName != null && File.Exists(fileName))
        {
            using var media = new Media(libVCL, new Uri(fileName));

            var parseStatus = await media.Parse();

            if (parseStatus == MediaParsedStatus.Done)
            {
                var disabledTrack = new TrackInfo(-1, "Disabled", null);

                var audios = media.Tracks
                    .Where(x => x.TrackType == TrackType.Audio)
                    .Select((x, i) => new TrackInfo(x.Id, x.Description ?? $"Track {i + 1}", x.Language));

                var subtitles = media.Tracks
                    .Where(x => x.TrackType == TrackType.Text)
                    .Select((x, i) => new TrackInfo(x.Id, x.Description ?? $"Track {i + 1}", x.Language));

                if (audios.Any())
                {
                    audios = new TrackInfo[] { disabledTrack }.Union(audios);
                }

                if (subtitles.Any())
                {
                    subtitles = new TrackInfo[] { disabledTrack }.Union(subtitles);
                }

                mediaFileNameSubject.OnNext(media.Meta(MetadataType.Title));

                audioTrackInfoSubject.OnNext(ImmutableArray.Create(audios.ToArray()));
                subtitleTrackInfoSubject.OnNext(ImmutableArray.Create(subtitles.ToArray()));
            }
            else
            {
                mediaFileNameSubject.OnNext(Path.GetFileName(fileName));

                audioTrackInfoSubject.OnNext([]);
                subtitleTrackInfoSubject.OnNext([]);
            }

            mediaPlayer.Time = 0;
            positionSubject.OnNext(0);

            await Task.Delay(500);

            return mediaPlayer.Play(media) == true;
        }

        return false;
    }

    public void Play()
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return;
        }

        mediaPlayer.Play();
    }

    public void Pause()
    {
        mediaPlayer?.Pause();
    }

    public void TooglePlaying()
    {
        var state = stateSubject.Value;

        if (!IsInitialized || (state != PlaybackState.Playing && state != PlaybackState.Paused))
        {
            return;
        }

        if (state == PlaybackState.Paused)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    public void Stop()
    {
        mediaPlayer?.Stop();
    }

    public bool SetPosition(double position)
    {
        return SetPosition(Convert.ToInt32(position));
    }

    public bool SetPosition(int position)
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return false;
        }

        var newPosition = Math.Max(0, Math.Min(durationSubject.Value - 1, position));

        if (Math.Abs(newPosition - positionSubject.Value) < 1)
        {
            return false;
        }

        lastSetPosition = newPosition;
        mediaPlayer.Time = newPosition * 1000;

        positionSubject.OnNext(newPosition);

        return true;
    }

    public bool SkipBack(TimeSpan timeSpan)
    {
        return SetPosition(positionSubject.Value - timeSpan.TotalSeconds);
    }

    public bool SkipForward(TimeSpan timeSpan)
    {
        return SetPosition(positionSubject.Value + timeSpan.TotalSeconds);
    }

    public bool SetVolume(int volume)
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return false;
        }

        var newVolume = Math.Max(0, Math.Min(100, volume));

        if (volumeSubject.Value == newVolume)
        {
            return false;
        }

        mediaPlayer.Volume = newVolume;
        return true;
    }

    public bool SetAudioTrack(int trackId)
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return false;
        }

        if (mediaPlayer.AudioTrackDescription.Any(x => x.Id == trackId))
        {
            mediaPlayer.SetAudioTrack(trackId);
            audioTrackSubject.OnNext(trackId);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool SetSubtitleTrack(int trackId)
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return false;
        }

        if (mediaPlayer.SpuDescription.Any(x => x.Id == trackId))
        {
            mediaPlayer.SetSpu(trackId);
            subtitleTrackSubject.OnNext(trackId);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void NotifyCurrentState()
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return;
        }

        audioTrackSubject.OnNext(mediaPlayer.AudioTrack);
        subtitleTrackSubject.OnNext(mediaPlayer.Spu);
    }
}
