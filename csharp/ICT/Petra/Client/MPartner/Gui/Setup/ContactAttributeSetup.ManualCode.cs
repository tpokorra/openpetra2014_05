﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       alanp
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
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using SourceGrid;
using Ict.Petra.Shared;
using System.Resources;
using System.Collections.Specialized;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Shared.MPartner.Mailroom.Data;

namespace Ict.Petra.Client.MPartner.Gui.Setup
{
    public partial class TFrmContactAttributeSetup
    {
        // A local variable that saves the column ordinal for our additional column in the main data table
        private int NumDetailCodesColumnOrdinal = 0;

        private void InitializeManualCode()
        {
            // Initialise the user control variables
            ucoContactDetail.PetraUtilsObject = FPetraUtilsObject;

            // The auto-generator does not dock our user control correctly
            //grpExtraDetails.Dock = System.Windows.Forms.DockStyle.Bottom;

            // We need to capture the 'DataSaved' event, so we can save our Extra DataSet
            FPetraUtilsObject.DataSaved += new TDataSavedHandler(FPetraUtilsObject_DataSaved);

            // we also want to know if the number of rows in our user control changes
            ucoContactDetail.CountChanged += new CountChangedEventHandler(ucoContactDetail_CountChanged);

            // we capture the Leave event (This is more consistent than LostFocus. - it always occurs before validation,
            //  whereas LostFocus is before or after depending on mouse or keyboard)
            txtDetailContactAttributeCode.Leave += new EventHandler(txtDetailContactAttributeCode_Leave);
        }

        private void RunOnceOnActivationManual()
        {
            // Set up the correct filter for the bottom grid, based on our initial contact attribute
            if (FMainDS.PContactAttribute.Rows.Count > 0)
            {
                ucoContactDetail.SetContactAttribute(txtDetailContactAttributeCode.Text);
            }

            // Add an extra column to our main data set that contains the number of sub-details for a given code
            NumDetailCodesColumnOrdinal = FMainDS.PContactAttribute.Columns.Add("NumDetails", typeof(int)).Ordinal;

            for (int i = 0; i < FMainDS.PContactAttribute.Rows.Count; i++)
            {
                string code = FMainDS.PContactAttribute.Rows[i][FMainDS.PContactAttribute.ColumnContactAttributeCode.Ordinal].ToString();
                FMainDS.PContactAttribute.Rows[i][NumDetailCodesColumnOrdinal] = ucoContactDetail.NumberOfDetails(code);
            }

            // add a column to the grid and bind it to our new data set column
            grdDetails.AddTextColumn(Catalog.GetString("Number of Detail Codes"), FMainDS.PContactAttribute.Columns[NumDetailCodesColumnOrdinal]);

            SelectRowInGrid(1);
        }

        private void NewRowManual(ref PContactAttributeRow ARow)
        {
            string newCode = Catalog.GetString("NEWCODE");
            Int32 countNewCode = 1;

            if (FMainDS.PContactAttribute.Rows.Find(new object[] { newCode }) != null)
            {
                while (FMainDS.PContactAttribute.Rows.Find(new object[] { newCode + countNewCode.ToString() }) != null)
                {
                    countNewCode++;
                }

                newCode += countNewCode.ToString();
            }

            ARow.ContactAttributeCode = newCode;
        }

        private void NewRecord(Object sender, EventArgs e)
        {
            int nRowCount = grdDetails.Rows.Count;

            CreateNewPContactAttribute();

            // Did we actually create one??
            if (nRowCount == grdDetails.Rows.Count)
            {
                return;
            }

            // Create the required initial detail attribute.  This will automatically fire the event that updates our details count column
            ucoContactDetail.CreateFirstAttributeDetail(txtDetailContactAttributeCode.Text);
            txtDetailContactAttributeCode.Focus();
        }

        private bool PreDeleteManual(PContactAttributeRow ARowToDelete, ref string ADeletionQuestion)
        {
            ADeletionQuestion += String.Format(Catalog.GetString(
                    "{0}{0}If you choose 'Yes', all the detail attributes for this Contact Attribute will be deleted as well."), Environment.NewLine);
            return true;
        }

        private bool DeleteRowManual(PContactAttributeRow ARowToDelete, ref String ACompletionMessage)
        {
            ACompletionMessage = String.Empty;

            // Now we need to remove all the detail attributes associated with this contact attribute.
            // (If we can delete the current row, it must also be the case that we can delete all the detail attributes for this row)
            // Then we can delete the contact attribute itself...
            ucoContactDetail.DeleteAll();

            // Now delete this row
            ARowToDelete.Delete();

            return true;
        }

        private void ShowDetailsManual(PContactAttributeRow ARow)
        {
            if (ARow == null)
            {
                ucoContactDetail.Enabled = false;
            }
            else
            {
                ucoContactDetail.Enabled = true;

                // Pass the contact attribute to the user control - it will then update itself
                ucoContactDetail.SetContactAttribute(ARow.ContactAttributeCode);
            }
        }

        private void GetDetailDataFromControlsManual(PContactAttributeRow ARow)
        {
            // Tell the user control to get its data too
            ucoContactDetail.GetDetailsFromControls();
        }

        private void txtDetailContactAttributeCode_Leave(object sender, EventArgs e)
        {
            if (NumDetailCodesColumnOrdinal == 0)
            {
                return;                                                 // No problem if we have no details yet
            }

            string newCode = txtDetailContactAttributeCode.Text;

            if (newCode.CompareTo(ucoContactDetail.ContactAttribute) == 0)
            {
                return;                                                                 // same as before
            }

            // ooops!  The user has edited the attribute code and we have some detail codes that depended on it!
            // We have to update the detail codes provided the new code is good (and passed validation)
            if (newCode == String.Empty)
            {
                return;
            }

            // So it is safe to modify the detail attribute
            ucoContactDetail.ModifyAttributeCode(newCode);
        }

        private void FPetraUtilsObject_DataSaved(object Sender, TDataSavedEventArgs e)
        {
            // Save the changes in the user control
            if (e.Success)
            {
                FPetraUtilsObject.SetChangedFlag();
                ucoContactDetail.SaveChanges();
                FPetraUtilsObject.DisableSaveButton();
            }
        }

        private void ucoContactDetail_CountChanged(object Sender, CountEventArgs e)
        {
            // something has changed in our user control (add/delete rows)
            FPreviouslySelectedDetailRow[NumDetailCodesColumnOrdinal] = e.NewCount;
        }
    }
}