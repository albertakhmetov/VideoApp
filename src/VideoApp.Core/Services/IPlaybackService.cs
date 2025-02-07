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
namespace VideoApp.Core.Services;

using System;
using System.Collections.Immutable;
using VideoApp.Core.Models;

public interface IPlaybackService : IDisposable
{
    bool IsInitialized { get; }

    IObservable<string?> MediaFileName { get; }

    IObservable<long> Duration { get; }

    IObservable<long> Position { get; }

    IObservable<int> Volume { get; }

    IObservable<PlaybackState> State { get; }

    IObservable<ImmutableArray<TrackInfo>> AudioTracks { get; }

    IObservable<ImmutableArray<TrackInfo>> SubtitleTracks { get; }

    IObservable<int> AudioTrack { get; }

    IObservable<int> SubtitleTrack { get; }

    void Initialize(object sender, string[] options);

    Task<bool> Load(string fileName);

    void Play();

    void Pause();

    void TooglePlaying();

    void Stop();

    bool SetPosition(double position);

    bool SetPosition(long position);

    bool SkipBack(TimeSpan timeSpan);

    bool SkipForward(TimeSpan timeSpan);

    bool SetVolume(int volume);

    bool SetAudioTrack(int trackId);

    bool SetSubtitleTrack(int trackId);
}
