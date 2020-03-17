//
// IPortForwarder.cs
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


using System.Threading;
using System.Threading.Tasks;

namespace MonoTorrent.Client.PortForwarding
{
    public interface IPortForwarder
    {
        /// <summary>
        /// Creates a port forwarding mapping on the router for the specified port..
        /// </summary>
        /// <param name="port">The value to use for the external and internal port number.</param>
        /// <param name="token">The cancellation token used to abort the request.</param>
        /// <returns></returns>
        Task AddPortForwardAsync (ushort port, CancellationToken token);

        /// <summary>
        /// Creates a port forwarding mapping on the router to map the specified external port to the specified internal
        /// port.
        /// </summary>
        /// <param name="internalPort">The port number MonoTorrent is listening on.</param>
        /// <param name="externalPort">The external port number other clients will connect to.</param>
        /// <param name="token">Thecancellation  token used to abort the request.</param>
        /// <returns></returns>
        Task AddPortForwardAsync (ushort externalPort, ushort internalPort, CancellationToken token);

        /// <summary>
        /// Removes a port forwarding mapping from the router.
        /// </summary>
        /// <param name="port">The value to use for the external and internal port number.</param>
        /// <param name="token">The cancellation token used to abort the request.</param>
        /// <returns></returns>
        Task RemovePortForwardAsync (ushort port, CancellationToken token);

        /// <summary>
        /// Removes a port forwarding mapping from the router.
        /// </summary>
        /// <param name="internalPort">The port number MonoTorrent is listening on.</param>
        /// <param name="externalPort">The external port number other clients will connect to.</param>
        /// <param name="token">Thecancellation  token used to abort the request.</param>
        /// <returns></returns>
        Task RemovePortForwardAsync (ushort externalPort, ushort internalPort, CancellationToken token);

        /// <summary>
        /// Begins searching for any compatible port forwarding devices. Refreshes any forwarded ports automatically
        /// before the mapping expires.
        /// </summary>
        /// <returns></returns>
        Task StartAsync ();

        /// <summary>
        /// Removes any port map requests and stops searching for compatible port forwarding devices. Cancels any pending
        /// ForwardPort requests.
        /// </summary>
        /// <returns></returns>
        Task StopAsync (CancellationToken token);

        /// <summary>
        /// Removes any port map requests and stops searching for compatible port forwarding devices. Cancels any pending
        /// ForwardPort requests.
        /// </summary>
        /// <returns></returns>
        Task StopAsync (bool removeExistingMappings, CancellationToken token);
    }
}
