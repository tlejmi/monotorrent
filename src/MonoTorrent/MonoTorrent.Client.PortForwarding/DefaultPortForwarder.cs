//
// DefaultPortForwarder.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;

namespace MonoTorrent.Client.PortForwarding
{
    public class DefaultPortForwarder : IPortForwarder
    {
        public bool Active => NatUtility.IsSearching;

        HashSet<INatDevice> Devices { get; }

        List<(ushort internalPort, ushort externalPort)> Requests;

        public DefaultPortForwarder ()
        {
            Devices = new HashSet<INatDevice> ();
            Requests = new List<(ushort internalPort, ushort externalPort)> ();

            NatUtility.DeviceFound += async (o, e) => {
                await ClientEngine.MainLoop;
                Devices.Add (e.Device);
                var reqs = Requests.Select (r => e.Device.CreatePortMapAsync (new Mapping (Protocol.Tcp, r.internalPort, r.externalPort)));
                await Task.WhenAll (reqs);
            };

            NatUtility.DeviceLost += async (o, e) => {
                await ClientEngine.MainLoop;
                Devices.Remove (e.Device);
            };
        }

        public Task AddPortForwardAsync (ushort port, CancellationToken token)
            => AddPortForwardAsync (port, port, token);

        public async Task AddPortForwardAsync (ushort internalPort, ushort externalPort, CancellationToken token)
        {
            await ClientEngine.MainLoop;
            Requests.Add ((internalPort, externalPort));
            var tasks = Devices.Select (d => d.CreatePortMapAsync (new Mapping (Protocol.Tcp, internalPort, externalPort)));
            await Task.WhenAll (tasks);
        }

        public Task RemovePortForwardAsync (ushort port, CancellationToken token)
            => RemovePortForwardAsync (port, port, token);

        public async Task RemovePortForwardAsync (ushort internalPort, ushort externalPort, CancellationToken token)
        {
            await ClientEngine.MainLoop;
            if (Requests.Remove ((internalPort, externalPort))) {
                var tasks = Devices.Select (d => d.DeletePortMapAsync (new Mapping (Protocol.Tcp, internalPort, externalPort)));
                await Task.WhenAll (tasks);
            }
        }

        public async Task StartAsync ()
        {
            if (!Active) {
                await MainLoop.SwitchToThreadpool ();
                NatUtility.StartDiscovery (NatProtocol.Pmp, NatProtocol.Upnp);
            }
        }

        public Task StopAsync (CancellationToken token)
            => StopAsync (true, token);

        public async Task StopAsync (bool removeExisting, CancellationToken token)
        {
            if (removeExisting) {
                foreach ((var internalPort, var externalPort) in Requests.ToArray ()) {
                    try {
                        await RemovePortForwardAsync (internalPort, externalPort, token);
                    } catch {

                    }
                }
            }
            NatUtility.StopDiscovery ();
        }
    }
}
