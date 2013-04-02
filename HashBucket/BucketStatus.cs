using System;
using System.Collections.Generic;
using System.Threading;

namespace Theraot.Threading
{
    internal enum BucketStatus
    {
        Free = 0,
        GrowRequested = 1,
        Waiting = 2,
        Copy = 3,
        CopyCleanup = 4
    }
}
