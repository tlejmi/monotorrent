﻿//
// StreamProvider.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2020 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoTorrent.Client;
using MonoTorrent.Client.PiecePicking;

namespace MonoTorrent.Streaming
{
    /// <summary>
    /// Prepare the TorrentManager so individual files can be accessed while they are downloading.
    /// </summary>
    public class StreamProvider
    {
        LocalStream ActiveStream { get; set; }
        ClientEngine Engine { get; }
        StreamingPiecePicker Picker { get; }

        /// <summary>
        /// Returns true when the <see cref="StreamProvider"/> has been started.
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Returns true when the <see cref="StreamProvider"/> has been paused.
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// The underlying <see cref="TorrentManager"/> used to download the data.
        /// It is safe to attach to events, retrieve state and also change any of
        /// the settings associated with this TorrentManager. You should never
        /// call StartAsync, StopAsync, PauseAsync or similar life-cycle methods,
        /// nor should you attempt to register this with a <see cref="ClientEngine"/>.
        /// </summary>
        public TorrentManager Manager { get; }

        /// <summary>
        /// Creates a StreamProvider for the given <see cref="Torrent"/> so that files
        /// contained within the torrent can be accessed as they are downloading.
        /// </summary>
        /// <param name="engine">The engine used to host the download.</param>
        /// <param name="saveDirectory">The directory where the torrents data will be saved</param>
        /// <param name="torrent">The torrent to download</param>
        public StreamProvider (ClientEngine engine, string saveDirectory, Torrent torrent)
        {
            Engine = engine;
            Manager = new TorrentManager (torrent, saveDirectory);
            Manager.ChangePicker (Picker = new StreamingPiecePicker (new StandardPicker ()));
        }

        /// <summary>
        /// Creates a StreamProvider for the given <see cref="MagnetLink"/> so that files
        /// contained within the torrent can be accessed as they are downloading.
        /// </summary>
        /// <param name="engine">The engine used to host the download.</param>
        /// <param name="saveDirectory">The directory where the torrents data will be saved</param>
        /// <param name="magnetLink">The MagnetLink to download</param>
        /// <param name="metadataSaveDirectory">The directory where the metadata will be saved. The filename will be constucted using the InfoHash of the MagnetLink.</param>
        public StreamProvider (ClientEngine engine, string saveDirectory, MagnetLink magnetLink, string metadataSaveDirectory)
        {
            Engine = engine;
            Manager = new TorrentManager (magnetLink, saveDirectory, new TorrentSettings (), metadataSaveDirectory);
            Manager.ChangePicker (Picker = new StreamingPiecePicker (new StandardPicker ()));
        }

        /// <summary>
        /// Registers <see cref="Manager"/> with the <see cref="ClientEngine"/>
        /// and calls <see cref="TorrentManager.StartAsync()"/>.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync ()
        {
            if (Active)
                throw new InvalidOperationException ("The StreamProvider has already been started.");

            if (Manager.Engine != null)
                throw new InvalidOperationException ("The TorrentManager has already been registered with the ClientEngine. This should not occur.");

            if (Engine.Contains (Manager.InfoHash)) {
                throw new InvalidOperationException (
                    "This Torrent/MagnetLink is already being downloaded by the ClientEngine. You must choose to either " +
                    "stream the torrent using StreamProvider or to download it normally with the ClientEngine.");
            }

            await Engine.Register (Manager);
            await Manager.StartAsync ();
            Active = true;
        }

        /// <summary>
        /// Calls <see cref="TorrentManager.PauseAsync()"/> to pause Hashing, Seeding or Downloading.
        /// </summary>
        /// <returns></returns>
        public async Task PauseAsync ()
        {
            if (!Active)
                throw new InvalidOperationException ("The StreamProvider can only be Paused if it is Active.");
            if (Paused)
                throw new InvalidOperationException ("The StreamProvider cannot be Paused again as it is already paused.");

            await Manager.PauseAsync ();
            Paused = true;
        }

        /// <summary>
        /// Calls <see cref="TorrentManager.StartAsync()"/> to resume Hashing, Seeding or Downloading.
        /// </summary>
        /// <returns></returns>
        public async Task ResumeAsync ()
        {
            if (!Paused)
                throw new InvalidOperationException ("The StreamProvider cannot be resumed as it is not currently paused.");

            await Manager.StartAsync ();
            Paused = false;
        }

        /// <summary>
        /// Calls <see cref="TorrentManager.StopAsync()"/> on <see cref="Manager"/> and unregisters
        /// it from the <see cref="ClientEngine"/>. This will dispose the stream returned by the
        /// most recent invocation of <see cref="CreateHttpStreamAsync(TorrentFile)"/> or
        /// <see cref="CreateStreamAsync(TorrentFile)"/>.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync ()
        {
            if (!Active)
                throw new InvalidOperationException ("The StreamProvider can only be stopped if it is Active");

            if (Manager.State == TorrentState.Stopped) {
                throw new InvalidOperationException (
                    "The TorrentManager associated with this StreamProvider has already been stopped. " +
                    "It is an error to directly call StopAsync, PauseAsync or StartAsync on the TorrentManager.");
            }
            await Manager.StopAsync ();
            await Engine.Unregister (Manager);
            ActiveStream.SafeDispose ();
            Active = false;
        }

        /// <summary>
        /// Creates a <see cref="Stream"/> which can be used to access the given <see cref="TorrentFile"/>
        /// while it is downloading. This stream is seekable and readable. This stream must be disposed
        /// before another stream can be created.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Task<Stream> CreateStreamAsync (TorrentFile file)
        {
            if (file == null)
                throw new ArgumentNullException (nameof (file));
            if (!Manager.Torrent.Files.Contains (file))
                throw new ArgumentException ("The TorrentFile is not from this TorrentManager", nameof (file));
            if (!Active)
                throw new InvalidOperationException ("You must call StartAsync before creating a stream.");
            if (ActiveStream != null && !ActiveStream.Disposed)
                throw new InvalidOperationException ("You must Dispose the previous stream before creating a new one.");
            Picker.SeekToPosition (file, 0);
            ActiveStream = new LocalStream (Manager, file, Picker);
            return Task.FromResult<Stream> (ActiveStream);
        }

        /// <summary>
        /// Creates a <see cref="Stream"/> which can be used to access the given <see cref="TorrentFile"/>
        /// while it is downloading. This stream is seekable and readable. This stream must be disposed
        /// before another stream can be created.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<IUriStream> CreateHttpStreamAsync (TorrentFile file)
        {
            var stream = await CreateStreamAsync (file);
            var httpStreamer = new HttpStream (stream);
            return httpStreamer;
        }
    }
}