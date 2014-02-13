﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//       Tim Ingham
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
using System.Data;
using System.Windows.Forms;

using Ict.Common;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared.MFinance.Account.Data;
using GNU.Gettext;

namespace Ict.Petra.Client.MFinance.Gui.Setup
{
    public partial class TUC_AccountAnalysisAttributes
    {
        private Int32 FLedgerNumber;
        private string FAccountCode;

        /// <summary>
        /// use this ledger
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;
                FPetraUtilsObject.DataSavingStarted += new TDataSavingStartHandler(FPetraUtilsObject_DataSavingStarted);
            }
        }

        //
        // Before the dataset is saved, I'll go through and pull out anything that still says, "Unassigned".
        void FPetraUtilsObject_DataSavingStarted(object Sender, EventArgs e)
        {
            foreach (AAnalysisTypeRow Row in FMainDS.AAnalysisType.Rows)
            {
                if (Row.RowState != DataRowState.Deleted)
                {
                    if (Row.AnalysisTypeCode.StartsWith("Unassigned"))
                    {
                        Row.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Loads all available AnalTypeCodes into the Combo
        /// </summary>
        private void LoadCmbAnalType()
        {
            cmbDetailAnalTypeCode.Items.Clear();
            DataView MyView = new DataView(FMainDS.AAnalysisType);
            MyView.Sort = AAnalysisTypeTable.GetAnalysisTypeCodeDBName();

            foreach (DataRowView rv in MyView)
            {
                AAnalysisTypeRow Row = (AAnalysisTypeRow)rv.Row;
                cmbDetailAnalTypeCode.Items.Add(Row.AnalysisTypeCode);
            }
        }

        /// <summary>
        /// we are editing this account
        /// </summary>
        public string AccountCode
        {
            set
            {
                FAccountCode = value;
                FMainDS.AAnalysisAttribute.DefaultView.RowFilter = String.Format("{0}={1} and {2}='{3}'",
                    AAnalysisAttributeTable.GetLedgerNumberDBName(),
                    FLedgerNumber,
                    AAnalysisAttributeTable.GetAccountCodeDBName(),
                    FAccountCode);
                FMainDS.AAnalysisAttribute.DefaultView.Sort =
                    AAnalysisAttributeTable.GetLedgerNumberDBName() + ", " +
                    AAnalysisAttributeTable.GetAnalysisTypeCodeDBName() + ", " +
                    AAnalysisAttributeTable.GetAccountCodeDBName();


                LoadCmbAnalType();
                pnlDetails.Enabled = false;
                btnDelete.Enabled = (grdDetails.Rows.Count > 1);
            }
        }

        private void NewRow(System.Object sender, EventArgs e)
        {
            if (cmbDetailAnalTypeCode.Items.Count == 0)
            {
                MessageBox.Show(Catalog.GetString("Please create an analysis type first"), Catalog.GetString("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

//          GetDataFromControls();
            CreateNewAAnalysisAttribute();
            pnlDetails.Enabled = true;
            btnDelete.Enabled = true;

            SelectRowInGrid(grdDetails.Rows.Count);
        }

        private string NewUniqueAnalTypeCode()
        {
            string NewTypeCode = "Unassigned";
            int ItmIdx = -1;
            int FindAttempt = 0;

            do
            {
                ItmIdx = FMainDS.AAnalysisAttribute.DefaultView.Find(new object[] { FLedgerNumber, NewTypeCode, FAccountCode });

//                TLogging.Log("NewUniqueAnalTypeCode: Find (" + NewTypeCode + ") = " + ItmIdx);
                if (ItmIdx >= 0)
                {
                    FindAttempt++;
                    NewTypeCode = "Unassigned-" + FindAttempt.ToString();
                }
            } while (ItmIdx >= 0);

            return NewTypeCode;
        }

        private void ShowDetailsManual(AAnalysisAttributeRow ARow)
        {
            if (ARow != null)  // How can ARow ever be null!!
            {
                LoadCmbAnalType();
                cmbDetailAnalTypeCode.SelectedValueChanged -= OnDetailAnalysisTypeCodeChange;
                cmbDetailAnalTypeCode.Text = ARow.AnalysisTypeCode;
                cmbDetailAnalTypeCode.SelectedValueChanged += new System.EventHandler(OnDetailAnalysisTypeCodeChange);
            }
        }

        private void NewRowManual(ref AAnalysisAttributeRow ARow)
        {
            ARow.LedgerNumber = FLedgerNumber;
            ARow.Active = true;
            ARow.AccountCode = FAccountCode;

            //cmbDetailAnalTypeCode.SelectedIndex = 0; // I'm not convinced about this...

            ARow.AnalysisTypeCode = NewUniqueAnalTypeCode();
        }

        private bool PreDeleteManual(AAnalysisAttributeRow ARowToDelete, ref string ADeletionQuestion)
        {
            // I can't delete any Analysis Type code that's been used in transactions.
            if ((ARowToDelete != null) && (ARowToDelete.RowState != DataRowState.Deleted))
            {
                if (TRemote.MFinance.Setup.WebConnectors.CanDetachAnalysisType(ARowToDelete.LedgerNumber, ARowToDelete.AccountCode,
                        ARowToDelete.AnalysisTypeCode))
                {
                    ADeletionQuestion = String.Format(
                        Catalog.GetString("Confirm you want to Remove {0} from this account."),
                        ARowToDelete.AnalysisTypeCode);
                    return true;
                }
                else // The server reports that this can't be deleted because it's been using in transactions.
                {
                    MessageBox.Show(
                        String.Format(Catalog.GetString("Analysis type {0} cannot be deleted because it has been used in tranactions."),
                            ARowToDelete.AnalysisTypeCode),
                        Catalog.GetString("Delete Analysis Type"));
                }
            }

            return false;
        }

        private bool DeleteRowManual(AAnalysisAttributeRow ARowToDelete, ref string ACompletionMessage)
        {
            bool success = false;

            try
            {
                ARowToDelete.Delete();
                success = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error trying to delete current row:" + Environment.NewLine + Environment.NewLine + ex.Message);
            }

            return success;
        }

        private void PostDeleteManual(AAnalysisAttributeRow ARowToDelete,
            bool AAllowDeletion,
            bool ADeletionPerformed,
            string ACompletionMessage)
        {
            btnDelete.Enabled = (grdDetails.Rows.Count > 1);
        }

        private void OnDetailAnalysisTypeCodeChange(System.Object sender, EventArgs e)
        {
            if ((FPreviouslySelectedDetailRow != null) && (FPreviouslySelectedDetailRow.RowState != DataRowState.Deleted))
            {
                AvoidSelectingingDuplicateAnalysisType(FPreviouslySelectedDetailRow,
                    FPreviouslySelectedDetailRow.AnalysisTypeCode,
                    cmbDetailAnalTypeCode.Text);
            }
        }

        private void AvoidSelectingingDuplicateAnalysisType(AAnalysisAttributeRow ARow, String APreviousValue, String ARequestedValue)
        {
            string FilterString = String.Format("{0}={3} and {1}='{4}' and {2}='{5}'",
                AAnalysisAttributeTable.GetLedgerNumberDBName(),
                AAnalysisAttributeTable.GetAnalysisTypeCodeDBName(),
                AAnalysisAttributeTable.GetAccountCodeDBName(),
                FLedgerNumber,
                ARequestedValue,
                FAccountCode);
            DataView FilterView = new DataView(FMainDS.AAnalysisAttribute);

            FilterView.RowFilter = FilterString;
            Boolean CantUseName = (FilterView.Count > 0);

            ARow.BeginEdit();

            if (CantUseName)
            {
                ARow.AnalysisTypeCode = APreviousValue;
                cmbDetailAnalTypeCode.Text = APreviousValue;
            }
            else
            {
                ARow.AnalysisTypeCode = ARequestedValue;
            }

            ARow.EndEdit(); // Apply these changes now!
        }

        private void GetDetailDataFromControlsManual(AAnalysisAttributeRow ARow)
        {
            if (ARow != null) // Why would it ever be null!
            {
                // I need to check whether this row will break a DB constraint.

                // The row is being edited right now, (It's in a BeginEdit ... EndEdit bracket) so it doesn't show up in the DefaultView.
                // I need to call EndEdit, but I'll give this row a "safe" value first.

                String RequestedValue = cmbDetailAnalTypeCode.Text;
                String PreviousValue = ARow.AnalysisTypeCode;
                ARow.AnalysisTypeCode = "temp_edit";
                ARow.EndEdit();

                AvoidSelectingingDuplicateAnalysisType(ARow, PreviousValue, RequestedValue);

                ARow.BeginEdit();
            }
        }

        private int CurrentRowIndex()
        {
            int rowIndex = -1;

            SourceGrid.RangeRegion selectedRegion = grdDetails.Selection.GetSelectionRegion();

            if ((selectedRegion != null) && (selectedRegion.GetRowsIndex().Length > 0))
            {
                rowIndex = selectedRegion.GetRowsIndex()[0];
            }

            return rowIndex;
        }

        private void SelectByIndex(int rowIndex)
        {
            if (rowIndex >= grdDetails.Rows.Count)
            {
                rowIndex = grdDetails.Rows.Count - 1;
            }

            if ((rowIndex < 1) && (grdDetails.Rows.Count > 1))
            {
                rowIndex = 1;
            }

            if ((rowIndex >= 1) && (grdDetails.Rows.Count > 1))
            {
                grdDetails.Selection.SelectRow(rowIndex, true);
                FPreviouslySelectedDetailRow = GetSelectedDetailRow();
                ShowDetails(FPreviouslySelectedDetailRow);
            }
            else
            {
                FPreviouslySelectedDetailRow = null;
                chkDetailActive.Checked = false;
            }
        }
    }
}