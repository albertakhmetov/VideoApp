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
    private readonly BehaviorSubject<long> durationSubject, positionSubject;
    private readonly BehaviorSubject<int> volumeSubject;

    private readonly Subject<int> volumeSetSubject;
    private readonly Subject<long> positionSetSubject;

    private readonly BehaviorSubject<ImmutableArray<TrackInfo>> audioTrackInfoSubject, subtitleTrackInfoSubject;
    private readonly BehaviorSubject<int> audioTrackSubject, subtitleTrackSubject;

    private IDisposable? trackLoadingStatus;

    public PlaybackService()
    {
        mediaFileNameSubject = new BehaviorSubject<string?>(null);
        stateSubject = new BehaviorSubject<PlaybackState>(PlaybackState.NotInitialized);
        durationSubject = new BehaviorSubject<long>(0);
        positionSubject = new BehaviorSubject<long>(0);
        volumeSubject = new BehaviorSubject<int>(100);

        audioTrackInfoSubject = new BehaviorSubject<ImmutableArray<TrackInfo>>([]);
        subtitleTrackInfoSubject = new BehaviorSubject<ImmutableArray<TrackInfo>>([]);

        audioTrackSubject = new BehaviorSubject<int>(-1);
        subtitleTrackSubject = new BehaviorSubject<int>(-1);

        volumeSetSubject = new Subject<int>();
        positionSetSubject = new Subject<long>();

        MediaFileName = mediaFileNameSubject.AsObservable();
        State = stateSubject.AsObservable();
        Duration = durationSubject.AsObservable();
        Position = positionSubject.AsObservable();
        Volume = volumeSubject.AsObservable();
        AudioTrackInfo = audioTrackInfoSubject.AsObservable();
        SubtitleTrackInfo = subtitleTrackInfoSubject.AsObservable();
        AudioTrack = audioTrackSubject.AsObservable();
        SubtitleTrack = subtitleTrackSubject.AsObservable();
    }

    public bool IsInitialized => libVCL != null;

    public IObservable<string?> MediaFileName { get; }

    public IObservable<PlaybackState> State { get; }

    public IObservable<long> Duration { get; }

    public IObservable<long> Position { get; }

    public IObservable<int> Volume { get; }

    public IObservable<ImmutableArray<TrackInfo>> AudioTrackInfo { get; }

    public IObservable<ImmutableArray<TrackInfo>> SubtitleTrackInfo { get; }

    public IObservable<int> AudioTrack { get; }

    public IObservable<int> SubtitleTrack { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;

        trackLoadingStatus?.Dispose();
    }

    public void Initialize(object sender, string[] options)
    {
        if (sender is VideoView videoView && SynchronizationContext.Current != null)
        {
            disposable = new CompositeDisposable();

            libVCL = new LibVLC(options).DisposeWith(disposable);
            mediaPlayer = new MediaPlayer(libVCL).DisposeWith(disposable);

            volumeSetSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(volume => mediaPlayer.Volume = volume)
                .DisposeWith(disposable);

            positionSetSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(position => mediaPlayer.Time = position)
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<MediaPlayerMediaChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.MediaChanged))
                .Select(x => x.EventArgs.Media)
                .Subscribe(x => Load(x))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<MediaPlayerLengthChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.LengthChanged))
                .Select(x => x.EventArgs.Length)
                .Subscribe(x => durationSubject.OnNext(x))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<MediaPlayerTimeChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.TimeChanged))
                .Select(x => x.EventArgs.Time)
                .Subscribe(x => positionSubject.OnNext(x))
                .DisposeWith(disposable);

            Observable
                .FromEventPattern<MediaPlayerVolumeChangedEventArgs>(mediaPlayer, nameof(MediaPlayer.VolumeChanged))
                .Select(x => Convert.ToInt32(x.EventArgs.Volume))
                .Subscribe(x => volumeSubject.OnNext(mediaPlayer.Volume))
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

            videoView.MediaPlayer = mediaPlayer;
            stateSubject.OnNext(PlaybackState.Closed);
        }
    }

    private void Load(Media m)
    {
        if (disposable == null)
        {
            throw new InvalidOperationException();
        }

        if (libVCL == null || mediaPlayer == null)
        {
            return;
        }

        trackLoadingStatus?.Dispose();

        trackLoadingStatus = Observable
           .FromEventPattern<MediaParsedChangedEventArgs>(m, nameof(Media.ParsedChanged))
           .Select(x => x.EventArgs.ParsedStatus == MediaParsedStatus.Done)
           .Subscribe(x => UpdateTracks())
           .DisposeWith(disposable);
    }

    private void UpdateTracks()
    {
        if (libVCL == null || mediaPlayer == null)
        {
            return;
        }

        var audios = mediaPlayer.AudioTrackDescription
            .Select(x => new TrackInfo(x.Id, x.Name))
            .ToArray();

        var subtitles = mediaPlayer.SpuDescription
            .Select(x => new TrackInfo(x.Id, x.Name))
            .ToArray();

        audioTrackInfoSubject.OnNext(ImmutableArray.Create(audios));
        subtitleTrackInfoSubject.OnNext(ImmutableArray.Create(subtitles));

        audioTrackSubject.OnNext(mediaPlayer.AudioTrack);
        subtitleTrackSubject.OnNext(mediaPlayer.Spu);

        mediaFileNameSubject.OnNext(mediaPlayer.Media.Meta(MetadataType.Title));

        trackLoadingStatus = null;
    }

    public bool Load(string fileName)
    {
        if (libVCL == null || mediaPlayer == null)
        {
            return false;
        }

        if (fileName != null && File.Exists(fileName))
        {
            using var media = new Media(libVCL, new Uri(fileName));

            return mediaPlayer.Play(media) == true;
        }
        else
        {
            return false;
        }
    }

    public void Play()
    {
        mediaPlayer?.Play();
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
        return SetPosition(Convert.ToInt64(position));
    }

    public bool SetPosition(long position)
    {
        if (!IsInitialized || mediaPlayer == null)
        {
            return false;
        }

        if (Math.Abs(position - positionSubject.Value) < 1000)
        {
            return false;
        }

        positionSetSubject.OnNext(position);
        return true;
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

        volumeSetSubject.OnNext(newVolume);
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
}
