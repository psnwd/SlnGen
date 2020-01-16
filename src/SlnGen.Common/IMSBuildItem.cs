// Copyright (c) Jeff Kluge.
//
// Licensed under the MIT license.

namespace SlnGen.Common
{
    public interface IMSBuildItem
    {
        string ItemSpec { get; }

        string GetMetadata(string name);
    }
}