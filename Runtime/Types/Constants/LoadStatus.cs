namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Types
    {
        namespace Constants
        {
            /// <summary>
            ///   The load status of a server and its scopes.
            /// </summary>
            public enum LoadStatus
            {
                /// <summary>
                ///   The server is either just instantiated or has disposed
                ///   everything successfully. This means that no scope and no
                ///   game components (but primarily: scopes) are loaded in the
                ///   game server. Any connection already established or just
                ///   arriving will go to some sort of limbo while the server
                ///   is in this state.
                /// </summary>
                Empty,
                /// <summary>
                ///   The server is loading everything. Some scopes deemed as
                ///   "default" will try to be loaded in this status, and any
                ///   failure to succeed will abort the process, log the error,
                ///   move to <see cref="LoadError"/> status, and immediately
                ///   starting an unload process.
                /// </summary>
                Loading,
                /// <summary>
                ///   This is a quite brief status. It is reached on any error
                ///   occurring in the <see cref="Loading"/> status. After
                ///   arriving, The server will be told to close.
                /// </summary>
                LoadError,
                /// <summary>
                ///   This is the ideal status. It is reached when the load of
                ///   all of the "default" scopes is successful, if any. If no
                ///   "default" scopes are specified, this status is reached
                ///   immediately. When this status is reached, all of the
                ///   connections with some sort of "pending dispatch" will go
                ///   to the appropriate scope, if any (for some connections,
                ///   it might not be the case, depending on the per-game logic,
                ///   e.g. if they did login or provide any sort of help to the
                ///   server so it knows where to send the connection). The
                ///   server may be told to close any time. In this status,
                ///   everything is ready to be closed and dispatched.
                /// </summary>
                Ready,
                /// <summary>
                ///   The server is currently unloading its contents. Unloading
                ///   involves releasing any per-scope resources (e.g. storage
                ///   of its contents), if any, then unregistering and removing
                ///   the scope, and finally destroying it. Any exception will
                ///   be caught and logged, but the unregistering and destruction
                ///   will always occur. This, for *each* scope. The next status
                ///   after this one is <see cref="Empty"/>.
                /// </summary>
                Unloading
            }
        }
    }
}
