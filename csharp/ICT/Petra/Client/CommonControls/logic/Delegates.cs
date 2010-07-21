//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       christiank
//
// Copyright 2004-2010 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using Ict.Petra.Shared.MPartner;

namespace Ict.Petra.Client.CommonControls.Logic
{
    /// <summary>Delegate for a call to open a Modal Partner Find screen</summary>
    public delegate bool TDelegateOpenPartnerFindScreen(String ARestrictToPartnerClass,
        out Int64 APartnerKey,
        out String AShortName,
        out TLocationPK ALocationPK,
        IntPtr AParentFormHandle);

    /// <summary>Delegate for a call to open a Modal Partner Find screen</summary>
    public delegate bool TDelegateOpenConferenceFindScreen(String AConferenceNamePattern,
        String ACampaignCodePattern,
        out Int64 AConferenceKey,
        out String AConferenceName,
        IntPtr AParentFormHandle);
}