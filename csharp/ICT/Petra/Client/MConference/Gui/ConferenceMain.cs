//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       peters
//
// Copyright 2004-2012 by OM International
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
using System.Windows.Forms;
using System.Runtime.Serialization;
using System;
using Ict.Common;
using Ict.Petra.Client.MConference.Gui.Setup;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared.MConference.Data;

namespace Ict.Petra.Client.MConference.Gui
{
    /// <summary>
    /// Elements
    /// </summary>
    public class TConferenceMain
    {
        /// PartnerKey for selected conference to be set from outside
        public static Int64 FPartnerKey;

        private static string ERRORMESSAGE = String.Format("A conference must be selected before the setup screens can be used.");

        /// <summary>
        /// opens Early Late Registration screen for pre selected conference
        /// </summary>
        public static void EarlyLateRegistrationsForSelectedConference(Form AParentForm)
        {
            DateTime StartDate = TRemote.MConference.Conference.WebConnectors.GetStartDate(FPartnerKey);
            DateTime EndDate = TRemote.MConference.Conference.WebConnectors.GetEndDate(FPartnerKey);

            // if one or both dates are missing for the selected conference then this screen will not open
            if ((StartDate == DateTime.MinValue) || (EndDate == DateTime.MinValue))
            {
                MessageBox.Show("The selected conference is missing a start and/or an end date. " +
                    "Go to the Conference Master Settings screen to assign a date before accessing this setup screen",
                    String.Format("Early and Late Registrations"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            if (FPartnerKey > 0)
            {
                TSearchCriteria[] Search = new TSearchCriteria[1];
                Search[0] = new TSearchCriteria(PcEarlyLateTable.GetConferenceKeyDBName(), FPartnerKey);

                TFrmEarlyLateRegistrationSetup.FPartnerKey = FPartnerKey;
                TFrmEarlyLateRegistrationSetup.FStartDate = StartDate;
                TFrmEarlyLateRegistrationSetup.FEndDate = EndDate;
                TFrmEarlyLateRegistrationSetup frm = new TFrmEarlyLateRegistrationSetup(AParentForm, Search);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Early and Late Registrations"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// opens Standard Cost screen for pre selected conference
        /// </summary>
        public static void StandardCostsForSelectedConference(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TSearchCriteria[] Search = new TSearchCriteria[1];
                Search[0] = new TSearchCriteria(PcConferenceCostTable.GetConferenceKeyDBName(), FPartnerKey);

                TFrmConferenceStandardCostSetup.FPartnerKey = FPartnerKey;
                TFrmConferenceStandardCostSetup frm = new TFrmConferenceStandardCostSetup(AParentForm, Search);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Conference Standard Costs"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// opens Child Discount screen for pre selected conference
        /// </summary>
        public static void ChildDiscountsForSelectedConference(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TSearchCriteria[] Search = new TSearchCriteria[2];
                Search[0] = new TSearchCriteria(PcDiscountTable.GetConferenceKeyDBName(), FPartnerKey);
                Search[1] = new TSearchCriteria(PcDiscountTable.GetDiscountCriteriaCodeDBName(), "CHILD");

                TFrmChildDiscountSetup.FPartnerKey = FPartnerKey;
                TFrmChildDiscountSetup frm = new TFrmChildDiscountSetup(AParentForm, Search);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Child Discounts"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// opens Outreach Supplement screen for pre selected conference
        /// </summary>
        public static void OutreachSupplementsForSelectedConference(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TSearchCriteria[] Search = new TSearchCriteria[1];
                Search[0] = new TSearchCriteria(PcSupplementTable.GetConferenceKeyDBName(), FPartnerKey);

                TFrmOutreachSupplementSetup.FPartnerKey = FPartnerKey;
                TFrmOutreachSupplementSetup frm = new TFrmOutreachSupplementSetup(AParentForm, Search);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Outreach Supplements"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// opens Master Settings screen for pre selected conference
        /// </summary>
        public static void MasterSettingsForSelectedConference(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TFrmConferenceMasterSettings.FPartnerKey = FPartnerKey;
                TFrmConferenceMasterSettings frm = new TFrmConferenceMasterSettings(AParentForm);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Master Settings"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// opens Event Find Screen screen and uses Conference Find Form (not displayed) to create a new conference
        /// </summary>
        public static void NewConference(Form AParentForm)
        {
            TFrmConferenceFindForm frm = new TFrmConferenceFindForm(AParentForm);

            frm.NewConference(new object(), new EventArgs());
        }

        /// <summary>
        /// opens Attendee List screen for pre selected conference
        /// </summary>
        public static void AttendeeListForSelectedConference(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TFrmAttendeeList.FPartnerKey = FPartnerKey;
                TFrmAttendeeList frm = new TFrmAttendeeList(AParentForm);

                frm.Show();
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Attendee List"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// refreshes the attendees for pre selected conference
        /// </summary>
        public static void RefreshAttendees(Form AParentForm)
        {
            if (FPartnerKey > 0)
            {
                TRemote.MConference.Conference.WebConnectors.RefreshAttendees(FPartnerKey);
            }
            else
            {
                MessageBox.Show(ERRORMESSAGE, String.Format("Refresh Attendees"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}