//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
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
//using System.Collections;
//using System.Collections.Specialized;

using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MFinance.Validation;

namespace Ict.Petra.Client.MFinance.Gui.Gift
{
    public partial class TUC_RecurringGiftTransactions
    {
        private Int32 FLedgerNumber = -1;
        private Int32 FBatchNumber = -1;
        private string FBatchMethodOfPayment = string.Empty;
        private Int64 FLastDonor = -1;
        private bool FActiveOnly = true;
        private ARecurringGiftBatchRow FBatchRow = null;
        private bool FGiftSelectedForDeletion = false;
        private bool FSuppressListChanged = false;

        private ARecurringGiftRow FGift = null;
        private string FFilterAllDetailsOfGift = string.Empty;
        private DataView FGiftDetailView = null;


        private void InitialiseControls()
        {
            txtDetailReference.MaxLength = 20;

            txtField.SendToBack();

            txtDetailRecipientKey.PartnerClass = "WORKER,UNIT,FAMILY";
        }

        /// <summary>
        /// load the gifts into the grid
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <returns>True if new gift transactions were loaded, false if transactions had been loaded already.</returns>
        public bool LoadGifts(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            Console.WriteLine("LoadGifts");
            DateTime dtStart = DateTime.Now;

            bool firstLoad = (FLedgerNumber == -1);

            if (firstLoad)
            {
                InitialiseControls();
            }

            //Enable buttons accordingly
            btnDelete.Enabled = !FPetraUtilsObject.DetailProtectedMode;
            btnNewDetail.Enabled = !FPetraUtilsObject.DetailProtectedMode;
            btnNewGift.Enabled = !FPetraUtilsObject.DetailProtectedMode;

            //Check if the same batch is selected, so no need to apply filter
            if ((FLedgerNumber == ALedgerNumber) && (FBatchNumber == ABatchNumber) && (FPreviouslySelectedDetailRow != null))
            {
                //Same as previously selected
                if (GetSelectedRowIndex() > 0)
                {
                    GetDetailsFromControls(GetSelectedDetailRow());
                    grdDetails.Focus();
                }

                UpdateControlsProtection();

                Console.WriteLine("LoadGifts - Quick exit  {0} ms", (DateTime.Now - dtStart).TotalMilliseconds);
                return false;
            }

            grdDetails.SuspendLayout();
            FSuppressListChanged = true;

            FLedgerNumber = ALedgerNumber;
            FBatchNumber = ABatchNumber;
            FBatchRow = GetBatchRow();

            //Apply new filter
            FPreviouslySelectedDetailRow = null;
            grdDetails.DataSource = null;

            // if this form is readonly, then we need all codes, because old codes might have been used
            if (firstLoad || (FActiveOnly != this.Enabled))
            {
                FActiveOnly = this.Enabled;

                TFinanceControls.InitialiseMotivationGroupList(ref cmbDetailMotivationGroupCode, FLedgerNumber, FActiveOnly);
                TFinanceControls.InitialiseMotivationDetailList(ref cmbDetailMotivationDetailCode, FLedgerNumber, FActiveOnly);
                TFinanceControls.InitialiseMethodOfGivingCodeList(ref cmbDetailMethodOfGivingCode, FActiveOnly);
                TFinanceControls.InitialiseMethodOfPaymentCodeList(ref cmbDetailMethodOfPaymentCode, FActiveOnly);
                TFinanceControls.InitialisePMailingList(ref cmbDetailMailingCode, FActiveOnly);
                //TFinanceControls.InitialiseKeyMinList(ref cmbMinistry, (Int64)0);

                //TODO            TFinanceControls.InitialiseAccountList(ref cmbDetailAccountCode, FLedgerNumber, true, false, ActiveOnly, false);
                //TODO            TFinanceControls.InitialiseCostCentreList(ref cmbDetailCostCentreCode, FLedgerNumber, true, false, ActiveOnly, false);
            }

            // This sets the incomplete filter but does check the panel enabled state
            ShowData();

            // This sets the main part of the filter but excluding the additional items set by the user GUI
            // It gets the right sort order
            SetGiftDetailDefaultView();

            // only load from server if there are no transactions loaded yet for this batch
            // otherwise we would overwrite transactions that have already been modified
            if (FMainDS.ARecurringGiftDetail.DefaultView.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadRecurringTransactions(ALedgerNumber, ABatchNumber));
            }

            // Now we set the full filter
            ApplyFilter();
            UpdateRecordNumberDisplay();
            SetRecordNumberDisplayProperties();

            SelectRowInGrid(1);

            UpdateTotals();
            UpdateControlsProtection();

            FSuppressListChanged = false;
            grdDetails.ResumeLayout();
            Console.WriteLine("LoadGifts completed  {0}", ((DateTime.Now - dtStart).TotalMilliseconds));

            return true;
        }

        bool FinRecipientKeyChanging = false;

        private void RecipientKeyChanged(Int64 APartnerKey,
            String APartnerShortName,
            bool AValidSelection)
        {
            String strMotivationGroup;
            String strMotivationDetail;

            if (FinRecipientKeyChanging | FPetraUtilsObject.SuppressChangeDetection)
            {
                return;
            }

            FinRecipientKeyChanging = true;

            GiftBatchTDSARecurringGiftDetailRow giftDetailRow = GetGiftDetailRow(FPreviouslySelectedDetailRow.GiftTransactionNumber,
                FPreviouslySelectedDetailRow.DetailNumber);
            giftDetailRow.RecipientDescription = APartnerShortName;

            try
            {
                FPetraUtilsObject.SuppressChangeDetection = true;

                strMotivationGroup = cmbDetailMotivationGroupCode.GetSelectedString();
                strMotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();

                if (TRemote.MFinance.Gift.WebConnectors.GetMotivationGroupAndDetail(
                        APartnerKey, ref strMotivationGroup, ref strMotivationDetail))
                {
                    if (strMotivationDetail.Equals(MFinanceConstants.GROUP_DETAIL_KEY_MIN))
                    {
                        cmbDetailMotivationDetailCode.SetSelectedString(MFinanceConstants.GROUP_DETAIL_KEY_MIN);
                    }
                }

                if (!FInKeyMinistryChanging)
                {
                    //...this does not work as expected, because the timer fires valuechanged event after this value is reset
                    TFinanceControls.GetRecipientData(ref cmbMinistry, ref txtField, APartnerKey);

                    long FieldNumber = Convert.ToInt64(txtField.Text);

                    txtDetailCostCentreCode.Text = TRemote.MFinance.Gift.WebConnectors.IdentifyPartnerCostCentre(FLedgerNumber, FieldNumber);
                }

                if (APartnerKey == 0)
                {
                    RetrieveMotivationDetailCostCentreCode();
                }
            }
            finally
            {
                FinRecipientKeyChanging = false;
                FPetraUtilsObject.SuppressChangeDetection = false;
            }
        }

        private void DonorKeyChanged(Int64 APartnerKey,
            String APartnerShortName,
            bool AValidSelection)
        {
            // At the moment this event is thrown twice
            // We want to deal only on manual entered changes, not on selections changes
            if (FPetraUtilsObject.SuppressChangeDetection)
            {
                FLastDonor = APartnerKey;
            }
            else
            {
                if (APartnerKey != FLastDonor)
                {
                    PPartnerTable PartnerDT = TRemote.MFinance.Gift.WebConnectors.LoadPartnerData(APartnerKey);

                    if (PartnerDT.Rows.Count > 0)
                    {
                        PPartnerRow pr = PartnerDT[0];
                        chkDetailConfidentialGiftFlag.Checked = pr.AnonymousDonor;
                    }

                    FLastDonor = APartnerKey;

                    foreach (GiftBatchTDSARecurringGiftDetailRow giftDetail in FMainDS.ARecurringGiftDetail.Rows)
                    {
                        if (giftDetail.BatchNumber.Equals(FBatchNumber)
                            && giftDetail.GiftTransactionNumber.Equals(FPreviouslySelectedDetailRow.GiftTransactionNumber))
                        {
                            giftDetail.DonorKey = APartnerKey;
                            giftDetail.DonorName = APartnerShortName;
                        }
                    }
                }
            }
        }

        private void DetailCommentChanged(object sender, EventArgs e)
        {
            if (FPreviouslySelectedDetailRow == null)
            {
                return;
            }

            TextBox txt = (TextBox)sender;

            string txtValue = txt.Text;

            if (txtValue == String.Empty)
            {
                if (txt.Name.Contains("One"))
                {
                    if (cmbDetailCommentOneType.SelectedIndex >= 0)
                    {
                        cmbDetailCommentOneType.SelectedIndex = -1;
                    }
                }
                else if (txt.Name.Contains("Two"))
                {
                    if (cmbDetailCommentTwoType.SelectedIndex >= 0)
                    {
                        cmbDetailCommentTwoType.SelectedIndex = -1;
                    }
                }
                else if (txt.Name.Contains("Three"))
                {
                    if (cmbDetailCommentThreeType.SelectedIndex >= 0)
                    {
                        cmbDetailCommentThreeType.SelectedIndex = -1;
                    }
                }
            }
        }

        private void DetailCommentTypeChanged(object sender, EventArgs e)
        {
            //TODO This code is called from the OnLeave event because the underlying
            //    combo control does not detect a value changed when the user tabs to
            //    and clears out the contents. AWAITING FIX to remove this code

            if (FPreviouslySelectedDetailRow == null)
            {
                return;
            }

            TCmbAutoComplete cmb = (TCmbAutoComplete)sender;

            string cmbValue = cmb.GetSelectedString();

            if (cmbValue == String.Empty)
            {
                if (cmb.Name.Contains("One"))
                {
                    if (cmbValue != FPreviouslySelectedDetailRow.CommentOneType)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                    }
                }
                else if (cmb.Name.Contains("Two"))
                {
                    if (cmbValue != FPreviouslySelectedDetailRow.CommentTwoType)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                    }
                }
                else if (cmb.Name.Contains("Three"))
                {
                    if (cmbValue != FPreviouslySelectedDetailRow.CommentThreeType)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                    }
                }
            }
        }

        bool FInKeyMinistryChanging = false;
        private void KeyMinistryChanged(object sender, EventArgs e)
        {
            if (FInKeyMinistryChanging || FinRecipientKeyChanging || FPetraUtilsObject.SuppressChangeDetection)
            {
                return;
            }

            FInKeyMinistryChanging = true;

            try
            {
                if (cmbMinistry.Count == 0)
                {
                    cmbMinistry.SelectedIndex = -1;
                }
                else
                {
                    Int64 rcp = cmbMinistry.GetSelectedInt64();

                    txtDetailRecipientKey.Text = String.Format("{0:0000000000}", rcp);
                }
            }
            finally
            {
                FInKeyMinistryChanging = false;
            }
        }

        private void FilterMotivationDetail(object sender, EventArgs e)
        {
            TFinanceControls.ChangeFilterMotivationDetailList(ref cmbDetailMotivationDetailCode, cmbDetailMotivationGroupCode.GetSelectedString());

            if ((cmbDetailMotivationDetailCode.Count > 0) && (cmbDetailMotivationDetailCode.Text.Trim() == string.Empty))
            {
                cmbDetailMotivationDetailCode.SelectedIndex = 0;
            }

            RetrieveMotivationDetailAccountCode();

            if ((txtDetailRecipientKey.Text == string.Empty) || (Convert.ToInt64(txtDetailRecipientKey.Text) == 0))
            {
                txtDetailRecipientKey.Text = String.Format("{0:0000000000}", 0);
                RetrieveMotivationDetailCostCentreCode();
            }
        }

        /// <summary>
        /// Called on TextChanged event for combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotivationGroupCodeChanged(object sender, EventArgs e)
        {
            if (cmbDetailMotivationGroupCode.Text.Trim() == string.Empty)
            {
                cmbDetailMotivationGroupCode.SelectedIndex = -1;
                cmbDetailMotivationDetailCode.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Called on TextChanged event for combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MotivationDetailCodeChanged(object sender, EventArgs e)
        {
            if (cmbDetailMotivationDetailCode.Text.Trim() == string.Empty)
            {
                txtDetailAccountCode.Text = string.Empty;
            }
        }

        /// <summary>
        /// To be called on the display of a new record
        /// </summary>
        private void RetrieveMotivationDetailAccountCode()
        {
            string MotivationGroup = cmbDetailMotivationGroupCode.GetSelectedString();
            string MotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();
            string AcctCode = string.Empty;

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, MotivationGroup, MotivationDetail });

            if (motivationDetail != null)
            {
                AcctCode = motivationDetail.AccountCode.ToString();
            }

            txtDetailAccountCode.Text = AcctCode;
        }

        private void RetrieveMotivationDetailCostCentreCode()
        {
            string MotivationGroup = cmbDetailMotivationGroupCode.GetSelectedString();
            string MotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();
            string CostCentreCode = string.Empty;

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, MotivationGroup, MotivationDetail });

            if (motivationDetail != null)
            {
                CostCentreCode = motivationDetail.CostCentreCode.ToString();
            }

            txtDetailCostCentreCode.Text = CostCentreCode;
        }

        private void MotivationDetailChanged(object sender, EventArgs e)
        {
            string MotivationGroup = cmbDetailMotivationGroupCode.GetSelectedString();
            string MotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, MotivationGroup, MotivationDetail });

            if (motivationDetail != null)
            {
                RetrieveMotivationDetailAccountCode();
            }

            long PartnerKey = 0;
            Int64.TryParse(txtDetailRecipientKey.Text, out PartnerKey);

            if (PartnerKey == 0)
            {
                RetrieveMotivationDetailCostCentreCode();
            }
            else
            {
                TFinanceControls.GetRecipientData(ref cmbMinistry, ref txtField, PartnerKey);

                long FieldNumber = Convert.ToInt64(txtField.Text);

                txtDetailCostCentreCode.Text = TRemote.MFinance.Gift.WebConnectors.IdentifyPartnerCostCentre(FLedgerNumber, FieldNumber);
            }
        }

        private void GiftDetailAmountChanged(object sender, EventArgs e)
        {
            TTxtNumericTextBox txn = (TTxtNumericTextBox)sender;

            if (txn.NumberValueDecimal == null)
            {
                return;
            }
            else
            {
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            Decimal sum = 0;
            Decimal sumBatch = 0;
            Int32 GiftNumber = 0;
            bool disableSaveButton = false;

            if (FPetraUtilsObject == null)
            {
                return;
            }

            //Sometimes a change in this unbound textbox causes a data changed condition
            disableSaveButton = !FPetraUtilsObject.HasChanges;

            if (FPreviouslySelectedDetailRow == null)
            {
                txtGiftTotal.NumberValueDecimal = 0;
                txtBatchTotal.NumberValueDecimal = 0;

                //If all details have been deleted
                if ((FLedgerNumber != -1) && (grdDetails.Rows.Count == 1))
                {
                    if ((FBatchRow != null) && (FBatchRow.BatchTotal != 0))
                    {
                        FBatchRow.BeginEdit();
                        FBatchRow.BatchTotal = 0;
                        FBatchRow.EndEdit();
                    }
                }
            }
            else
            {
                GiftNumber = FPreviouslySelectedDetailRow.GiftTransactionNumber;

                foreach (ARecurringGiftDetailRow gdr in FMainDS.ARecurringGiftDetail.Rows)
                {
                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        if ((gdr.BatchNumber == FBatchNumber) && (gdr.LedgerNumber == FLedgerNumber))
                        {
                            if (gdr.GiftTransactionNumber == GiftNumber)
                            {
                                if (FPreviouslySelectedDetailRow.DetailNumber == gdr.DetailNumber)
                                {
                                    sum += Convert.ToDecimal(txtDetailGiftAmount.NumberValueDecimal);
                                    sumBatch += Convert.ToDecimal(txtDetailGiftAmount.NumberValueDecimal);
                                }
                                else
                                {
                                    sum += gdr.GiftAmount;
                                    sumBatch += gdr.GiftAmount;
                                }
                            }
                            else
                            {
                                sumBatch += gdr.GiftAmount;
                            }
                        }
                    }
                }

                txtGiftTotal.NumberValueDecimal = sum;
                txtGiftTotal.CurrencyCode = txtDetailGiftAmount.CurrencyCode;
                txtGiftTotal.ReadOnly = true;
                //this is here because at the moment the generator does not generate this
                txtBatchTotal.NumberValueDecimal = sumBatch;
                //Now we look at the batch and update the batch data
                FBatchRow.BatchTotal = sumBatch;
            }

            if (disableSaveButton && FPetraUtilsObject.HasChanges)
            {
                FPetraUtilsObject.DisableSaveButton();
            }
        }

        /// reset the control
        public void ClearCurrentSelection()
        {
            this.FPreviouslySelectedDetailRow = null;
        }

        /// <summary>
        /// This Method is needed for UserControls who get dynamicly loaded on TabPages.
        /// </summary>
        public void AdjustAfterResizing()
        {
            // TODO Adjustment of SourceGrid's column widhts needs to be done like in Petra 2.3 ('SetupDataGridVisualAppearance' Methods)
        }

        /// <summary>
        /// get the details of the current batch
        /// </summary>
        /// <returns></returns>
        private ARecurringGiftBatchRow GetBatchRow()
        {
            return (ARecurringGiftBatchRow)FMainDS.ARecurringGiftBatch.Rows.Find(new object[] { FLedgerNumber, FBatchNumber });
        }

        /// <summary>
        /// get the details of the current gift
        /// </summary>
        /// <returns></returns>
        private ARecurringGiftRow GetGiftRow(Int32 ARecurringGiftTransactionNumber)
        {
            return (ARecurringGiftRow)FMainDS.ARecurringGift.Rows.Find(new object[] { FLedgerNumber, FBatchNumber, ARecurringGiftTransactionNumber });
        }

        /// <summary>
        /// get the details of the current gift
        /// </summary>
        /// <returns></returns>
        private GiftBatchTDSARecurringGiftDetailRow GetGiftDetailRow(Int32 AGiftTransactionNumber, Int32 AGiftDetailNumber)
        {
            return (GiftBatchTDSARecurringGiftDetailRow)FMainDS.ARecurringGiftDetail.Rows.Find(new object[] { FLedgerNumber, FBatchNumber,
                                                                                                              AGiftTransactionNumber,
                                                                                                              AGiftDetailNumber });
        }

        private bool PreDeleteManual(GiftBatchTDSARecurringGiftDetailRow ARowToDelete, ref string ADeletionQuestion)
        {
            bool allowDeletion = true;

            FGift = GetGiftRow(FPreviouslySelectedDetailRow.GiftTransactionNumber);
            FFilterAllDetailsOfGift = String.Format("{0}={1} and {2}={3}",
                ARecurringGiftDetailTable.GetBatchNumberDBName(),
                FPreviouslySelectedDetailRow.BatchNumber,
                ARecurringGiftDetailTable.GetGiftTransactionNumberDBName(),
                FPreviouslySelectedDetailRow.GiftTransactionNumber);

            FGiftDetailView = new DataView(FMainDS.ARecurringGiftDetail);
            FGiftDetailView.RowFilter = FFilterAllDetailsOfGift;
            FGiftDetailView.Sort = ARecurringGiftDetailTable.GetDetailNumberDBName() + " ASC";
            String formattedDetailAmount = StringHelper.FormatUsingCurrencyCode(ARowToDelete.GiftAmount, GetBatchRow().CurrencyCode);

            if (FGiftDetailView.Count == 1)
            {
                ADeletionQuestion = String.Format(Catalog.GetString("Are you sure you want to delete transaction {1} from Gift Batch no. {2}?" +
                        "\n\r\n\r" + "     From:  {3}" +
                        "\n\r" + "         To:  {4}" +
                        "\n\r" + "Amount:  {5}"),
                    ARowToDelete.DetailNumber,
                    ARowToDelete.GiftTransactionNumber,
                    ARowToDelete.BatchNumber,
                    ARowToDelete.DonorName,
                    ARowToDelete.RecipientDescription,
                    formattedDetailAmount);
            }
            else if (FGiftDetailView.Count > 1)
            {
                ADeletionQuestion =
                    String.Format(Catalog.GetString("Are you sure you want to delete detail {0} from transaction {1} in Gift Batch no. {2}?" +
                            "\n\r\n\r" + "     From:  {3}" +
                            "\n\r" + "         To:  {4}" +
                            "\n\r" + "Amount:  {5}"),
                        ARowToDelete.DetailNumber,
                        ARowToDelete.GiftTransactionNumber,
                        ARowToDelete.BatchNumber,
                        ARowToDelete.DonorName,
                        ARowToDelete.RecipientDescription,
                        formattedDetailAmount);
            }
            else //this should never happen
            {
                ADeletionQuestion =
                    String.Format(Catalog.GetString("Gift transaction {1} in Gift Batch no. {2} has no detail rows in the Gift Detail table!"),
                        ARowToDelete.DetailNumber,
                        ARowToDelete.GiftTransactionNumber,
                        ARowToDelete.BatchNumber);
                allowDeletion = false;
            }

            return allowDeletion;
        }

        private bool DeleteRowManual(GiftBatchTDSARecurringGiftDetailRow ARowToDelete, ref string ACompletionMessage)
        {
            bool deletionSuccessful = false;

            ACompletionMessage = string.Empty;

            if (ARowToDelete == null)
            {
                return deletionSuccessful;
            }

            if ((ARowToDelete.RowState != DataRowState.Added) && !((TFrmRecurringGiftBatch) this.ParentForm).SaveChanges())
            {
                MessageBox.Show("Error in trying to save prior to deleting current gift detail!");
                return deletionSuccessful;
            }

            //Backup the Dataset for reversion purposes
            GiftBatchTDS FTempDS = (GiftBatchTDS)FMainDS.Copy();

            int selectedDetailNumber = ARowToDelete.DetailNumber;
            int giftToDeleteTransNo = 0;
            int currentBatchNumber = 0;
            string filterAllGiftsOfBatch = String.Empty;
            string filterAllGiftDetailsOfBatch = String.Empty;

            try
            {
                //If deleting a detail row as opposed to a gift header
                if (FGiftDetailView.Count > 1)
                {
                    ARowToDelete.Delete();

                    FGiftSelectedForDeletion = false;

                    foreach (DataRowView rv in FGiftDetailView)
                    {
                        GiftBatchTDSARecurringGiftDetailRow row = (GiftBatchTDSARecurringGiftDetailRow)rv.Row;

                        if (row.DetailNumber > selectedDetailNumber)
                        {
                            row.DetailNumber--;
                        }
                    }

                    FGift.LastDetailNumber--;

                    FPetraUtilsObject.SetChangedFlag();
                }
                else
                {
                    ARowToDelete.Delete();

                    giftToDeleteTransNo = FGift.GiftTransactionNumber;
                    currentBatchNumber = FGift.BatchNumber;

                    filterAllGiftDetailsOfBatch = String.Format("{0}={1}",
                        ARecurringGiftDetailTable.GetBatchNumberDBName(),
                        currentBatchNumber);

                    DataView giftDetailView = new DataView(FMainDS.ARecurringGiftDetail);
                    giftDetailView.RowFilter = filterAllGiftDetailsOfBatch;

                    foreach (DataRowView rv in giftDetailView)
                    {
                        GiftBatchTDSARecurringGiftDetailRow row = (GiftBatchTDSARecurringGiftDetailRow)rv.Row;

                        if (row.GiftTransactionNumber > giftToDeleteTransNo)
                        {
                            row.GiftTransactionNumber--;
                        }
                    }

                    filterAllGiftsOfBatch = String.Format("{0}={1}",
                        ARecurringGiftTable.GetBatchNumberDBName(),
                        currentBatchNumber);

                    DataView giftView = new DataView(FMainDS.ARecurringGift);
                    giftView.RowFilter = filterAllGiftsOfBatch;
                    giftView.Sort = ARecurringGiftTable.GetGiftTransactionNumberDBName();

                    ARecurringGiftRow giftRowToReceive = null;
                    ARecurringGiftRow giftRowToCopyDown = null;

                    ARecurringGiftRow giftRowCurrent = null;

                    int currentGiftTransNo = 0;

                    foreach (DataRowView gv in giftView)
                    {
                        giftRowCurrent = (ARecurringGiftRow)gv.Row;

                        currentGiftTransNo = giftRowCurrent.GiftTransactionNumber;

                        if (currentGiftTransNo > giftToDeleteTransNo)
                        {
                            giftRowToCopyDown = giftRowCurrent;

                            //Copy column values down
                            for (int j = 3; j < giftRowToCopyDown.Table.Columns.Count; j++)
                            {
                                //Update all columns except the pk fields that remain the same
                                if (!giftRowToCopyDown.Table.Columns[j].ColumnName.EndsWith("_text"))
                                {
                                    giftRowToReceive[j] = giftRowToCopyDown[j];
                                }
                            }
                        }

                        if (currentGiftTransNo == giftView.Count)
                        {
                            //Mark last record for deletion
                            giftRowCurrent.ChargeStatus = MFinanceConstants.MARKED_FOR_DELETION;
                        }

                        //Will always be previous row
                        giftRowToReceive = giftRowCurrent;
                    }

                    FPreviouslySelectedDetailRow = null;

                    FPetraUtilsObject.SetChangedFlag();

                    FGiftSelectedForDeletion = true;
                }

                //Try to save changes
                if (((TFrmRecurringGiftBatch) this.ParentForm).SaveChanges())
                {
                    //Reload from server
                    FMainDS.ARecurringGiftDetail.Clear();
                    FMainDS.ARecurringGift.Clear();

                    FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadRecurringTransactions(FLedgerNumber, FBatchNumber));
                }
                else
                {
                    throw new Exception("Unable to save after deleting a recurring gift!");
                }

                ACompletionMessage = Catalog.GetString("Recurring gift row deleted successfully!");

                deletionSuccessful = true;
            }
            catch (Exception ex)
            {
                ACompletionMessage = ex.Message;
                MessageBox.Show(ex.Message,
                    "Recurring Gift Deletion Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                //Revert to previous state
                FMainDS = (GiftBatchTDS)FTempDS.Copy();
            }
            finally
            {
                SetGiftDetailDefaultView();
                ApplyFilter();
            }

            UpdateRecordNumberDisplay();

            return deletionSuccessful;
        }

        private void PostDeleteManual(GiftBatchTDSARecurringGiftDetailRow ARowToDelete,
            bool AAllowDeletion,
            bool ADeletionPerformed,
            string ACompletionMessage)
        {
            if (ADeletionPerformed)
            {
                if (FGiftSelectedForDeletion)
                {
                    FGiftSelectedForDeletion = false;

                    SetBatchLastGiftNumber();

                    UpdateControlsProtection();

                    if (!pnlDetails.Enabled)
                    {
                        ClearControls();
                    }
                }

                UpdateTotals();

                ((TFrmRecurringGiftBatch) this.ParentForm).SaveChanges();

                //message to user
                MessageBox.Show(ACompletionMessage,
                    "Deletion Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!AAllowDeletion && (ACompletionMessage.Length > 0))
            {
                //message to user
                MessageBox.Show(ACompletionMessage,
                    "Deletion not allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else if (!ADeletionPerformed && (ACompletionMessage.Length > 0))
            {
                //message to user
                MessageBox.Show(ACompletionMessage,
                    "Deletion failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetBatchLastGiftNumber()
        {
            DataView dv = new DataView(FMainDS.ARecurringGift);

            dv.RowFilter = String.Format("{0}={1}",
                ARecurringGiftTable.GetBatchNumberDBName(),
                FBatchNumber);

            dv.Sort = String.Format("{0} DESC",
                ARecurringGiftTable.GetGiftTransactionNumberDBName());

            dv.RowStateFilter = DataViewRowState.CurrentRows;

            if (dv.Count > 0)
            {
                ARecurringGiftRow transRow = (ARecurringGiftRow)dv[0].Row;
                FBatchRow.LastGiftNumber = transRow.GiftTransactionNumber;
            }
            else
            {
                FBatchRow.LastGiftNumber = 0;
            }
        }

        private void SetGiftDetailDefaultView()
        {
            if (FBatchNumber != -1)
            {
                string rowFilter = String.Format("{0}={1}",
                    ARecurringGiftDetailTable.GetBatchNumberDBName(),
                    FBatchNumber);
                FFilterPanelControls.SetBaseFilter(rowFilter, true);
                FMainDS.ARecurringGiftDetail.DefaultView.RowFilter = rowFilter;
                FCurrentActiveFilter = rowFilter;
                // We don't apply the filter yet!

                FMainDS.ARecurringGiftDetail.DefaultView.Sort = string.Format("{0}, {1}",
                    ARecurringGiftDetailTable.GetGiftTransactionNumberDBName(),
                    ARecurringGiftDetailTable.GetDetailNumberDBName());
            }
        }

        private void ClearControls()
        {
            try
            {
                FPetraUtilsObject.SuppressChangeDetection = true;

                txtBatchTotal.NumberValueDecimal = 0;
                txtDetailDonorKey.Text = string.Empty;
                chkDetailActive.Checked = false;
                txtDetailReference.Clear();
                txtGiftTotal.NumberValueDecimal = 0;
                txtDetailGiftAmount.NumberValueDecimal = 0;
                dtpStartDonations.Clear();
                dtpEndDonations.Clear();
                txtDetailRecipientKey.Text = string.Empty;
                txtField.Text = string.Empty;
                txtDetailAccountCode.Clear();
                txtDetailGiftCommentOne.Clear();
                txtDetailGiftCommentTwo.Clear();
                txtDetailGiftCommentThree.Clear();
                cmbDetailReceiptLetterCode.SelectedIndex = -1;
                cmbDetailMotivationGroupCode.SelectedIndex = -1;
                cmbDetailMotivationDetailCode.SelectedIndex = -1;
                cmbDetailCommentOneType.SelectedIndex = -1;
                cmbDetailCommentTwoType.SelectedIndex = -1;
                cmbDetailCommentThreeType.SelectedIndex = -1;
                cmbDetailMailingCode.SelectedIndex = -1;
                cmbDetailMethodOfGivingCode.SelectedIndex = -1;
                cmbDetailMethodOfPaymentCode.SelectedIndex = -1;
                cmbMinistry.SelectedIndex = -1;
                txtDetailCostCentreCode.Text = string.Empty;
            }
            finally
            {
                FPetraUtilsObject.SuppressChangeDetection = false;
            }
        }

        /// <summary>
        /// add a new gift
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGift(System.Object sender, EventArgs e)
        {
            // this is coded manually, to use the correct gift record

            // we create the table locally, no dataset
            ARecurringGiftDetailRow RecurringGiftDetailRow = NewGift(); // returns ARecurringGiftDetailRow

            if (RecurringGiftDetailRow != null)
            {
                FPetraUtilsObject.SetChangedFlag();

                if (!SelectDetailRowByDataTableIndex(FMainDS.ARecurringGiftDetail.Rows.Count - 1))
                {
                    if (FCurrentActiveFilter != FFilterPanelControls.BaseFilter)
                    {
                        MessageBox.Show(
                            MCommonResourcestrings.StrNewRecordIsFiltered,
                            MCommonResourcestrings.StrAddNewRecordTitle,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        FFilterPanelControls.ClearAllDiscretionaryFilters();

                        if (FucoFilterAndFind.ShowApplyFilterButton != TUcoFilterAndFind.FilterContext.None)
                        {
                            ApplyFilter();
                        }

                        SelectDetailRowByDataTableIndex(FMainDS.ARecurringGiftDetail.Rows.Count - 1);
                    }
                }
            }

            UpdateRecordNumberDisplay();
        }

        /// <summary>
        /// make sure the correct transaction number is assigned and the batch.lastTransactionNumber is updated
        /// </summary>
        private ARecurringGiftDetailRow NewGift()
        {
            GiftBatchTDSARecurringGiftDetailRow newRow = null;

            if (ValidateAllData(true, true))
            {
                ARecurringGiftRow giftRow = FMainDS.ARecurringGift.NewRowTyped(true);

                giftRow.Active = true;

                giftRow.LedgerNumber = FBatchRow.LedgerNumber;
                giftRow.BatchNumber = FBatchRow.BatchNumber;
                giftRow.GiftTransactionNumber = FBatchRow.LastGiftNumber + 1;
                FBatchRow.LastGiftNumber++;
                giftRow.LastDetailNumber = 1;

                FMainDS.ARecurringGift.Rows.Add(giftRow);

                newRow = FMainDS.ARecurringGiftDetail.NewRowTyped(true);

                newRow.LedgerNumber = FBatchRow.LedgerNumber;
                newRow.BatchNumber = FBatchRow.BatchNumber;
                newRow.GiftTransactionNumber = giftRow.GiftTransactionNumber;
                newRow.DetailNumber = 1;
                newRow.DonorKey = 0;
                cmbDetailMotivationGroupCode.SelectedIndex = 0;
                newRow.MotivationGroupCode = cmbDetailMotivationGroupCode.GetSelectedString();
                newRow.MotivationDetailCode = cmbDetailMotivationDetailCode.GetSelectedString();
                RetrieveMotivationDetailCostCentreCode();

                FMainDS.ARecurringGiftDetail.Rows.Add(newRow);
            }

            return newRow;
        }

        /// <summary>
        /// add a new gift detail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGiftDetail(System.Object sender, EventArgs e)
        {
            //If grid is empty call NewGift() instead
            if (grdDetails.Rows.Count == 1)
            {
                NewGift(sender, e);
                return;
            }

            // this is coded manually, to use the correct gift record
            // we create the table locally, no dataset
            ARecurringGiftDetailRow recurringGiftDetailRow = NewGiftDetail((GiftBatchTDSARecurringGiftDetailRow)FPreviouslySelectedDetailRow);

            if (recurringGiftDetailRow != null)
            {
                FPetraUtilsObject.SetChangedFlag();

                if (!SelectDetailRowByDataTableIndex(FMainDS.ARecurringGiftDetail.Rows.Count - 1))
                {
                    if (FCurrentActiveFilter != FFilterPanelControls.BaseFilter)
                    {
                        MessageBox.Show(
                            MCommonResourcestrings.StrNewRecordIsFiltered,
                            MCommonResourcestrings.StrAddNewRecordTitle,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        FFilterPanelControls.ClearAllDiscretionaryFilters();

                        if (FucoFilterAndFind.ShowApplyFilterButton != TUcoFilterAndFind.FilterContext.None)
                        {
                            ApplyFilter();
                        }

                        SelectDetailRowByDataTableIndex(FMainDS.ARecurringGiftDetail.Rows.Count - 1);
                    }
                }

                RetrieveMotivationDetailAccountCode();
                dtpStartDonations.Focus();
            }

            UpdateRecordNumberDisplay();
        }

        /// <summary>
        /// add another gift detail to an existing gift
        /// </summary>
        private ARecurringGiftDetailRow NewGiftDetail(GiftBatchTDSARecurringGiftDetailRow ACurrentRow)
        {
            GiftBatchTDSARecurringGiftDetailRow newRow = null;

            if (ValidateAllData(true, true))
            {
                if (ACurrentRow == null)
                {
                    return NewGift();
                }

                // find gift row
                ARecurringGiftRow giftRow = GetGiftRow(ACurrentRow.GiftTransactionNumber);

                giftRow.LastDetailNumber++;

                newRow = FMainDS.ARecurringGiftDetail.NewRowTyped(true);

                newRow.LedgerNumber = giftRow.LedgerNumber;
                newRow.BatchNumber = giftRow.BatchNumber;
                newRow.GiftTransactionNumber = giftRow.GiftTransactionNumber;
                newRow.DetailNumber = giftRow.LastDetailNumber;
                newRow.DonorKey = ACurrentRow.DonorKey;
                newRow.DonorName = ACurrentRow.DonorName;
                newRow.DateEntered = ACurrentRow.DateEntered;
                cmbDetailMotivationGroupCode.SelectedIndex = 0;
                newRow.MotivationGroupCode = cmbDetailMotivationGroupCode.GetSelectedString();
                newRow.MotivationDetailCode = cmbDetailMotivationDetailCode.GetSelectedString();
                RetrieveMotivationDetailCostCentreCode();

                FMainDS.ARecurringGiftDetail.Rows.Add(newRow);
            }

            return newRow;
        }

        /// <summary>
        /// show ledger and batch number
        /// </summary>
        private void ShowDataManual()
        {
            txtLedgerNumber.Text = TFinanceControls.GetLedgerNumberAndName(FLedgerNumber);
            txtBatchNumber.Text = FBatchNumber.ToString();

            if (FBatchRow != null)
            {
                txtDetailGiftAmount.CurrencyCode = FBatchRow.CurrencyCode;
            }

            if (grdDetails.Rows.Count == 1)
            {
                txtBatchTotal.NumberValueDecimal = 0;
                ClearControls();
            }
            else
            {
                ARecurringGiftDetailRow ARow = (ARecurringGiftDetailRow)FMainDS.ARecurringGiftDetail.Rows[0];
                cmbDetailMotivationGroupCode.SetSelectedString(ARow.MotivationGroupCode);
                cmbDetailMotivationDetailCode.SetSelectedString(ARow.MotivationDetailCode);
                UpdateControlsProtection(ARow);
            }

            if ((Convert.ToInt64(txtDetailRecipientKey.Text) == 0) && (cmbDetailMotivationGroupCode.SelectedIndex == -1))
            {
                txtDetailCostCentreCode.Text = string.Empty;
            }

            FPetraUtilsObject.SetStatusBarText(cmbDetailMethodOfGivingCode, Catalog.GetString("Enter method of giving"));
            FPetraUtilsObject.SetStatusBarText(cmbDetailMethodOfPaymentCode, Catalog.GetString("Enter the method of payment"));
            FPetraUtilsObject.SetStatusBarText(txtDetailReference, Catalog.GetString("Enter a reference code."));
            FPetraUtilsObject.SetStatusBarText(cmbDetailReceiptLetterCode, Catalog.GetString("Select the receipt letter code"));
        }

        private void ShowDetailsManual(ARecurringGiftDetailRow ARow)
        {
            txtLedgerNumber.Text = TFinanceControls.GetLedgerNumberAndName(FLedgerNumber);
            txtBatchNumber.Text = FBatchNumber.ToString();

            if (ARow == null)
            {
                return;
            }

            // show cost centre
            MotivationDetailChanged(null, null);

            TFinanceControls.GetRecipientData(ref cmbMinistry, ref txtField, ARow.RecipientKey);

            ARecurringGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);


            cmbDetailMethodOfPaymentCode.SetSelectedString(giftRow.MethodOfPaymentCode);

            if (((GiftBatchTDSARecurringGiftDetailRow)ARow).IsDonorKeyNull())
            {
                txtDetailDonorKey.Text = "0";
            }
            else
            {
                txtDetailDonorKey.Text = ((GiftBatchTDSARecurringGiftDetailRow)ARow).DonorKey.ToString();
            }

            if ((Convert.ToInt64(txtDetailRecipientKey.Text) == 0) && (cmbDetailMotivationGroupCode.SelectedIndex == -1))
            {
                txtDetailCostCentreCode.Text = string.Empty;
            }

            UpdateControlsProtection(ARow);

            ShowDetailsForGift(ARow);
        }

        private void ShowDetailsForGift(ARecurringGiftDetailRow ARow)
        {
            // this is a special case - normally these lines would be produced by the generator
            ARecurringGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);

            if (giftRow.IsActiveNull())
            {
                chkDetailActive.Checked = false;
            }
            else
            {
                chkDetailActive.Checked = giftRow.Active;
            }

            if (Convert.ToInt64(txtDetailRecipientKey.Text) == 0)
            {
                txtDetailCostCentreCode.Text = string.Empty;
            }

            if (cmbMinistry.Count == 0)
            {
                cmbMinistry.SelectedIndex = -1;
                cmbMinistry.Text = string.Empty;
            }

            if (giftRow.IsMethodOfGivingCodeNull())
            {
                cmbDetailMethodOfGivingCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailMethodOfGivingCode.SetSelectedString(giftRow.MethodOfGivingCode);
            }

            if (giftRow.IsMethodOfPaymentCodeNull())
            {
                cmbDetailMethodOfPaymentCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailMethodOfPaymentCode.SetSelectedString(giftRow.MethodOfPaymentCode);
            }

            if (giftRow.IsReferenceNull())
            {
                txtDetailReference.Text = String.Empty;
            }
            else
            {
                txtDetailReference.Text = giftRow.Reference;
            }

            if (giftRow.IsReceiptLetterCodeNull())
            {
                cmbDetailReceiptLetterCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailReceiptLetterCode.SetSelectedString(giftRow.ReceiptLetterCode);
            }
        }

        /// <summary>
        /// set the currency symbols for the currency field from outside
        /// </summary>
        public void UpdateCurrencySymbols(String ACurrencyCode)
        {
            txtDetailGiftAmount.CurrencyCode = ACurrencyCode;
            txtGiftTotal.CurrencyCode = ACurrencyCode;
            txtBatchTotal.CurrencyCode = ACurrencyCode;
            txtHashTotal.CurrencyCode = ACurrencyCode;
        }

        /// <summary>
        /// update the transaction method payment from outside
        /// </summary>
        public void UpdateMethodOfPayment(bool ACalledLocally)
        {
            Int32 ledgerNumber;
            Int32 batchNumber;

            if (ACalledLocally)
            {
                cmbDetailMethodOfPaymentCode.SetSelectedString(FBatchMethodOfPayment);
                return;
            }

            if (!((TFrmRecurringGiftBatch) this.ParentForm).GetBatchControl().FBatchLoaded)
            {
                return;
            }

            FBatchRow = GetBatchRow();

            if (FBatchRow == null)
            {
                FBatchRow = ((TFrmRecurringGiftBatch) this.ParentForm).GetBatchControl().GetSelectedDetailRow();
            }

            FBatchMethodOfPayment = ((TFrmRecurringGiftBatch) this.ParentForm).GetBatchControl().FSelectedBatchMethodOfPayment;

            ledgerNumber = FBatchRow.LedgerNumber;
            batchNumber = FBatchRow.BatchNumber;

            if (FMainDS.ARecurringGift.Rows.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadRecurringTransactions(ledgerNumber, batchNumber));
            }
            else if ((FLedgerNumber == ledgerNumber) || (FBatchNumber == batchNumber))
            {
                //Rows already active in transaction tab. Need to set current row ac code below will not update selected row
                if (FPreviouslySelectedDetailRow != null)
                {
                    FPreviouslySelectedDetailRow.MethodOfPaymentCode = FBatchMethodOfPayment;
                    cmbDetailMethodOfPaymentCode.SetSelectedString(FBatchMethodOfPayment);
                }
            }

            //Update all transactions
            foreach (ARecurringGiftRow recurringGiftRow in FMainDS.ARecurringGift.Rows)
            {
                if (recurringGiftRow.BatchNumber.Equals(batchNumber) && recurringGiftRow.LedgerNumber.Equals(ledgerNumber)
                    && (recurringGiftRow.MethodOfPaymentCode != FBatchMethodOfPayment))
                {
                    recurringGiftRow.MethodOfPaymentCode = FBatchMethodOfPayment;
                }
            }
        }

        /// <summary>
        /// set the Hash Total symbols for the currency field from outside
        /// </summary>
        public void UpdateHashTotal(Decimal AHashTotal)
        {
            txtHashTotal.NumberValueDecimal = AHashTotal;
        }

        /// <summary>
        /// set the correct protection from outside
        /// </summary>
        public void UpdateControlsProtection()
        {
            UpdateControlsProtection(FPreviouslySelectedDetailRow);
        }

        private void UpdateControlsProtection(ARecurringGiftDetailRow ARow)
        {
            bool firstIsEnabled = (ARow != null) && (ARow.DetailNumber == 1);

            chkDetailActive.Enabled = firstIsEnabled;
            txtDetailDonorKey.Enabled = firstIsEnabled;
            cmbDetailMethodOfGivingCode.Enabled = firstIsEnabled;

            cmbDetailMethodOfPaymentCode.Enabled = firstIsEnabled && !BatchHasMethodOfPayment();
            txtDetailReference.Enabled = firstIsEnabled;
            cmbDetailReceiptLetterCode.Enabled = firstIsEnabled;

            if (FBatchRow == null)
            {
                FBatchRow = GetBatchRow();
            }

            if (ARow == null)
            {
                PnlDetailsProtected = true;
            }
            else
            {
                PnlDetailsProtected = (ARow != null && ARow.GiftAmount < 0);    // taken from old petra
            }

            pnlDetails.Enabled = !(PnlDetailsProtected);
        }

        private Boolean BatchHasMethodOfPayment()
        {
            String batchMop = GetMethodOfPaymentFromBatch();

            return batchMop != null && batchMop.Length > 0;
        }

        private String GetMethodOfPaymentFromBatch()
        {
            return ((TFrmRecurringGiftBatch)ParentForm).GetBatchControl().MethodOfPaymentCode;
        }

        private void GetDetailDataFromControlsManual(ARecurringGiftDetailRow ARow)
        {
            if (ARow.DetailNumber != 1)
            {
                return;
            }

            ARecurringGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);

            if (giftRow != null)
            {
                giftRow.DonorKey = Convert.ToInt64(txtDetailDonorKey.Text);
                giftRow.Active = chkDetailActive.Checked;

                GiftBatchTDSARecurringGiftDetailRow giftDetailRow = GetGiftDetailRow(ARow.GiftTransactionNumber, ARow.DetailNumber);
                giftDetailRow.RecipientKey = Convert.ToInt64(txtDetailRecipientKey.Text);
                giftDetailRow.RecipientDescription = txtDetailRecipientKey.LabelText;

                if (cmbDetailMethodOfGivingCode.SelectedIndex == -1)
                {
                    giftRow.SetMethodOfGivingCodeNull();
                }
                else
                {
                    giftRow.MethodOfGivingCode = cmbDetailMethodOfGivingCode.GetSelectedString();
                }

                if (cmbDetailMethodOfPaymentCode.SelectedIndex == -1)
                {
                    giftRow.SetMethodOfPaymentCodeNull();
                }
                else
                {
                    giftRow.MethodOfPaymentCode = cmbDetailMethodOfPaymentCode.GetSelectedString();
                }

                if (txtDetailReference.Text.Length == 0)
                {
                    giftRow.SetReferenceNull();
                }
                else
                {
                    giftRow.Reference = txtDetailReference.Text;
                }

                if (cmbDetailReceiptLetterCode.SelectedIndex == -1)
                {
                    giftRow.SetReceiptLetterCodeNull();
                }
                else
                {
                    giftRow.ReceiptLetterCode = cmbDetailReceiptLetterCode.GetSelectedString();
                }
            }
        }

        private void ValidateDataDetailsManual(ARecurringGiftDetailRow ARow)
        {
            if ((ARow == null) || (GetBatchRow() == null))
            {
                return;
            }

            if ((txtDetailGiftAmount.NumberValueDecimal == null) || !txtDetailGiftAmount.NumberValueDecimal.HasValue)
            {
                txtDetailGiftAmount.NumberValueDecimal = 0;
                ARow.GiftAmount = 0;
            }

            TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

            TSharedFinanceValidation_Gift.ValidateRecurringGiftDetailManual(this, ARow, ref VerificationResultCollection,
                FValidationControlsDict);
        }

        /// <summary>
        /// Focus on grid
        /// </summary>
        public void FocusGrid()
        {
            if ((grdDetails != null) && grdDetails.Enabled && grdDetails.TabStop)
            {
                grdDetails.Focus();
            }
        }

        private void RunOnceOnParentActivationManual()
        {
            grdDetails.DataSource.ListChanged += new System.ComponentModel.ListChangedEventHandler(DataSource_ListChanged);
        }

        private void DataSource_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if (grdDetails.CanFocus && !FSuppressListChanged && (grdDetails.Rows.Count > 1))
            {
                AutoSizeGrid();
            }
        }

        /// <summary>
        /// AutoSize the grid columns (call this after the window has been restored to normal size after being maximized)
        /// </summary>
        public void AutoSizeGrid()
        {
            //TODO: Using this manual code until we can do something better
            //      Autosizing all the columns is very time consuming when there are many rows
            foreach (SourceGrid.DataGridColumn column in grdDetails.Columns)
            {
                column.Width = 100;
                column.AutoSizeMode = SourceGrid.AutoSizeMode.EnableStretch;
            }

            grdDetails.Columns[0].Width = 60;
            grdDetails.Columns[1].Width = 60;
            grdDetails.Columns[2].AutoSizeMode = SourceGrid.AutoSizeMode.Default;
            grdDetails.Columns[3].Width = 50;
            grdDetails.Columns[4].Width = 25;
            grdDetails.Columns[6].AutoSizeMode = SourceGrid.AutoSizeMode.Default;

            grdDetails.AutoStretchColumnsToFitWidth = true;
            grdDetails.Rows.AutoSizeMode = SourceGrid.AutoSizeMode.None;
            grdDetails.AutoSizeCells();
            grdDetails.ShowCell(FPrevRowChangedRow);

            Console.WriteLine("Done AutoSizeGrid() on {0} rows", grdDetails.Rows.Count);
        }
    }
}