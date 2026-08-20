// 
// Copyright © 2010-2025, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//
// Make Me Admin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// Make Me Admin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Make Me Admin. If not, see <http://www.gnu.org/licenses/>.
//

namespace LsaLogonSessions
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains information about the group security identifiers (SIDs) in an access token.
    /// </summary>
    internal struct TOKEN_GROUPS
    {
        /// <summary>
        /// The number of groups in the access token.
        /// </summary>
        public uint GroupCount;

        // NOTE: The native TOKEN_GROUPS layout is GroupCount followed by an
        // inline array of SID_AND_ATTRIBUTES structures. The array is NOT
        // declared here: a ByValArray marshaling attribute without SizeConst
        // makes Marshal.PtrToStructure/Marshal.SizeOf throw an ArgumentException
        // on every call. LogonSessions.GetGroupMemberships walks the array
        // manually from the buffer, so the field is unnecessary.
    }
}
