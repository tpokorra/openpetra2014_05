//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop, christophert
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
using Ict.Common.Controls;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.CommonControls;
using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MFinance.Validation;

namespace Ict.Petra.Client.MFinance.Gui.Gift
{
    public partial class TUC_GiftTransactions
    {
        /// <summary>
        /// The current Ledger number
        /// </summary>
        public Int32 FLedgerNumber = -1;

        /// <summary>
        /// The current Batch number
        /// </summary>
        public Int32 FBatchNumber = -1;

        private string FBatchStatus = string.Empty;
        private bool FBatchUnposted = false;
        private string FBatchMethodOfPayment = string.Empty;
        private decimal FExchangeRateToBase = 1.0m;
        private Int64 FLastDonor = -1;
        private bool FActiveOnly = true;
        private AGiftBatchRow FBatchRow = null;
        private bool FGLEffectivePeriodChanged = false;
        private bool FGiftSelectedForDeletion = false;
        AGiftRow FGift = null;
        string FFilterAllDetailsOfGift = string.Empty;
        DataView FGiftDetailView = null;
        private bool FSuppressListChanged = false;

        private string FMotivationGroup = string.Empty;
        private string FMotivationDetail = string.Empty;
//        private string FKeyMinistry = string.Empty;

        private bool FShowingDetails = false;
        private bool FInEditMode = false;

        private Boolean ViewMode
        {
            get
            {
                return ((TFrmGiftBatch)ParentForm).ViewMode;
            }
        }

        private void InitialiseControls()
        {
            //Fix to length of field
            txtDetailReference.MaxLength = 20;

            //Fix a layering issue
            txtField.SendToBack();

            //Changing this will stop taborder issues
            sptTransactions.TabStop = false;

            txtDetailRecipientKey.PartnerClass = "WORKER,UNIT,FAMILY";

            //Set initial width of this textbox
            cmbMinistry.ComboBoxWidth = 250;
            cmbMinistry.AttachedLabel.Visible = false;

            //Setup hidden text boxes used to speed up reading transactions
            SetupComboTextBoxOverlayControls();
        }

        private void SetupComboTextBoxOverlayControls()
        {
            txtKeyMinistry.TabStop = false;
            txtKeyMinistry.BorderStyle = BorderStyle.None;
            txtKeyMinistry.Top = cmbMinistry.Top + 3;
            txtKeyMinistry.Left += 3;
            txtKeyMinistry.Width = cmbMinistry.ComboBoxWidth - 21;

            txtKeyMinistry.Click += new EventHandler(SetFocusToKeyMinistryCombo);
            txtKeyMinistry.KeyDown += new KeyEventHandler(OverlayTextBox_KeyDown);
            txtKeyMinistry.KeyPress += new KeyPressEventHandler(OverlayTextBox_KeyPress);

            pnlDetails.Enter += new EventHandler(BeginEditMode);
            pnlDetails.Leave += new EventHandler(EndEditMode);

            cmbMinistry.Enter += new EventHandler(SetKeyMinistryTextBoxInvisible);
            cmbDetailMotivationGroupCode.Enter += new EventHandler(SetKeyMinistryTextBoxInvisible);
            cmbDetailMotivationDetailCode.Enter += new EventHandler(SetKeyMinistryTextBoxInvisible);

            SetTextBoxOverlayOnKeyMinistryCombo();
        }

        /// <summary>
        /// load the gifts into the grid
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <param name="ABatchStatus"></param>
        /// <returns>True if new gift transactions were loaded, false if transactions had been loaded already.</returns>
        public bool LoadGifts(Int32 ALedgerNumber,
            Int32 ABatchNumber,
            string ABatchStatus = MFinanceConstants.BATCH_UNPOSTED)
        {
            //Reset Batch method of payment variable
            FBatchMethodOfPayment = ((TFrmGiftBatch)ParentForm).GetBatchControl().MethodOfPaymentCode;

            bool firstLoad = (FLedgerNumber == -1);

            if (firstLoad)
            {
                InitialiseControls();
            }

            //Check if the same batch is selected, so no need to apply filter
            if ((FLedgerNumber == ALedgerNumber) && (FBatchNumber == ABatchNumber) && (FBatchStatus == ABatchStatus))
            {
                //Same as previously selected
                if ((ABatchStatus == MFinanceConstants.BATCH_UNPOSTED) && (GetSelectedRowIndex() > 0))
                {
                    if (FGLEffectivePeriodChanged)
                    {
                        FGLEffectivePeriodChanged = false;
                        GetSelectedDetailRow().DateEntered = FBatchRow.GlEffectiveDate;
                        dtpDateEntered.Date = FBatchRow.GlEffectiveDate;
                    }

                    GetDetailsFromControls(GetSelectedDetailRow());
                }

                UpdateControlsProtection();

                if ((ABatchStatus == MFinanceConstants.BATCH_UNPOSTED) && (FExchangeRateToBase != GetBatchRow().ExchangeRateToBase))
                {
                    UpdateBaseAmount(false);
                }

                SetTextBoxOverlayOnKeyMinistryCombo();

                return false;
            }

            grdDetails.SuspendLayout();
            FSuppressListChanged = true;

            //Read key fields
            FLedgerNumber = ALedgerNumber;
            FBatchNumber = ABatchNumber;
            FBatchRow = GetBatchRow();

            //Process Batch status
            FBatchStatus = ABatchStatus;
            FBatchUnposted = (FBatchStatus == MFinanceConstants.BATCH_UNPOSTED);

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
            }

            // This sets the incomplete filter but does check the panel enabled state
            ShowData();

            // This sets the main part of the filter but excluding the additional items set by the user GUI
            // It gets the right sort order
            SetGiftDetailDefaultView();

            // only load from server if there are no transactions loaded yet for this batch
            // otherwise we would overwrite transactions that have already been modified
            if (FMainDS.AGiftDetail.DefaultView.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(ALedgerNumber, ABatchNumber));

                ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors();
            }

            //Check if need to update batch period in each gift
            ((TFrmGiftBatch)ParentForm).GetBatchControl().UpdateBatchPeriod();

            // Now we set the full filter
            ApplyFilter();
            UpdateRecordNumberDisplay();
            SetRecordNumberDisplayProperties();
            SelectRowInGrid(1);

            UpdateTotals();
            UpdateControlsProtection();

            FSuppressListChanged = false;
            grdDetails.ResumeLayout();

            return true;
        }

        private void UpdateCostCentreCodeForAllRecipients()
        {
            string FailedUpdates = string.Empty;

            if (!FBatchUnposted)
            {
                return;
            }

            //0 for the last two arguments means for all transactions in the batch
            TRemote.MFinance.Gift.WebConnectors.UpdateCostCentreCodeForRecipients(ref FMainDS, out FailedUpdates, 0, 0);

            ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors();
        }

        private void FindCostCentreCodeForRecipient(AGiftDetailRow ARow, bool AShowError = false)
        {
            string CurrentCostCentreCode = ARow.CostCentreCode;
            string NewCostCentreCode = string.Empty;

            string MotivationGroup = ARow.MotivationGroupCode;
            string MotivationDetail = ARow.MotivationDetailCode;

            Int64 PartnerKey = ARow.RecipientKey;
            Int64 RecipientLedgerNumber = ARow.RecipientLedgerNumber;
            Int64 LedgerPartnerKey = FMainDS.ALedger[0].PartnerKey;

            bool KeyMinIsActive = false;
            bool KeyMinExists = TRemote.MFinance.Gift.WebConnectors.KeyMinistryExists(PartnerKey, out KeyMinIsActive);

            string ValidLedgerNumberCostCentreCode;

            //bool ValidLedgerNumberExists = TRemote.MFinance.Gift.WebConnectors.ValidLedgerNumberExistsForRecipient(FLedgerNumber,
            //    PartnerKey,
            //    out ValidLedgerNumberCostCentreCode);

            string errMsg = string.Empty;

            if (TRemote.MFinance.Gift.WebConnectors.ValidLedgerNumberExistsForRecipient(FLedgerNumber, PartnerKey,
                    out ValidLedgerNumberCostCentreCode)
                || TRemote.MFinance.Gift.WebConnectors.ValidLedgerNumberExistsForRecipient(FLedgerNumber, RecipientLedgerNumber,
                    out ValidLedgerNumberCostCentreCode))
            {
                NewCostCentreCode = ValidLedgerNumberCostCentreCode;
            }
            else if ((RecipientLedgerNumber != LedgerPartnerKey) && ((MotivationGroup == "GIFT") || KeyMinExists))
            {
                errMsg = String.Format(
                    "Error in extracting Cost Centre Code for Recipient: {0} in Ledger: {1}.{2}{2}(Recipient Ledger Number: {3}, Ledger Partner Key: {4})",
                    PartnerKey,
                    FLedgerNumber,
                    Environment.NewLine,
                    RecipientLedgerNumber,
                    LedgerPartnerKey);

                if (AShowError)
                {
                    MessageBox.Show(errMsg,
                        "Cost Centre Code Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                }
                else
                {
                    TLogging.Log("Cost Centre Code Error: " + errMsg);
                }
            }
            else
            {
                AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                    new object[] { FLedgerNumber, MotivationGroup, MotivationDetail });

                if (motivationDetail != null)
                {
                    NewCostCentreCode = motivationDetail.CostCentreCode.ToString();
                }
                else
                {
                    errMsg = String.Format(
                        "Error in extracting Cost Centre Code for Motivation Group: {0} and Motivation Detail: {1} in Ledger: {2}.",
                        MotivationGroup,
                        MotivationDetail,
                        FLedgerNumber);

                    if (AShowError)
                    {
                        MessageBox.Show(errMsg,
                            "Cost Centre Code Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        TLogging.Log("Cost Centre Code Error: " + errMsg);
                    }
                }
            }

            if (CurrentCostCentreCode != NewCostCentreCode)
            {
                ARow.CostCentreCode = NewCostCentreCode;
            }
        }

        bool FinRecipientKeyChanging = false;

        private void RecipientKeyChanged(Int64 APartnerKey,
            String APartnerShortName,
            bool AValidSelection)
        {
            if (FinRecipientKeyChanging || FPetraUtilsObject.SuppressChangeDetection || FShowingDetails)
            {
                return;
            }

            FinRecipientKeyChanging = true;

            GiftBatchTDSAGiftDetailRow giftDetailRow = GetGiftDetailRow(FPreviouslySelectedDetailRow.GiftTransactionNumber,
                FPreviouslySelectedDetailRow.DetailNumber);
            giftDetailRow.RecipientDescription = APartnerShortName;

            try
            {
                FPetraUtilsObject.SuppressChangeDetection = true;

                //Set RecipientLedgerNumber
                if (APartnerKey > 0)
                {
                    FPreviouslySelectedDetailRow.RecipientLedgerNumber = TRemote.MFinance.Gift.WebConnectors.GetRecipientLedgerNumber(APartnerKey);
                }
                else
                {
                    FPreviouslySelectedDetailRow.RecipientLedgerNumber = 0;
                }

                if (TRemote.MFinance.Gift.WebConnectors.GetMotivationGroupAndDetail(
                        APartnerKey, ref FMotivationGroup, ref FMotivationDetail))
                {
                    if (FMotivationDetail.Equals(MFinanceConstants.GROUP_DETAIL_KEY_MIN))
                    {
                        cmbDetailMotivationDetailCode.SetSelectedString(MFinanceConstants.GROUP_DETAIL_KEY_MIN);
                    }
                }

                if (!FInKeyMinistryChanging)
                {
                    GetRecipientData(APartnerKey);
                }

                if (APartnerKey == 0)
                {
                    RetrieveMotivationDetailCostCentreCode();
                }
                else
                {
                    RetrieveRecipientCostCentreCode(APartnerKey);
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
            else if (FShowingDetails)
            {
                return;
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

                    foreach (GiftBatchTDSAGiftDetailRow giftDetail in FMainDS.AGiftDetail.Rows)
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

            if (txt.Name.Contains("One"))
            {
                if (txtValue == String.Empty)
                {
                    cmbDetailCommentOneType.SelectedIndex = -1;
                }
                else if (cmbDetailCommentOneType.SelectedIndex == -1)
                {
                    cmbDetailCommentOneType.SetSelectedString("Both");
                }
            }
            else if (txt.Name.Contains("Two"))
            {
                if (txtValue == String.Empty)
                {
                    cmbDetailCommentTwoType.SelectedIndex = -1;
                }
                else if (cmbDetailCommentTwoType.SelectedIndex == -1)
                {
                    cmbDetailCommentTwoType.SetSelectedString("Both");
                }
            }
            else if (txt.Name.Contains("Three"))
            {
                if (txtValue == String.Empty)
                {
                    cmbDetailCommentThreeType.SelectedIndex = -1;
                }
                else if (cmbDetailCommentThreeType.SelectedIndex == -1)
                {
                    cmbDetailCommentThreeType.SetSelectedString("Both");
                }
            }
        }

        private void SetTextBoxOverlayOnKeyMinistryCombo()
        {
            ResetMotivationDetailCodeFilter();

            txtKeyMinistry.Visible = true;
            txtKeyMinistry.BringToFront();
        }

        private void OverlayTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void OverlayTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void SetFocusToKeyMinistryCombo(object sender, EventArgs e)
        {
            cmbMinistry.Focus();
        }

        private void SetKeyMinistryTextBoxInvisible(object sender, EventArgs e)
        {
            if (txtKeyMinistry.Visible)
            {
                ApplyMotivationDetailCodeFilter();

                PopulateKeyMinistry();

                //hide the overlay box during editing
                txtKeyMinistry.Visible = false;
            }
        }

        private void BeginEditMode(object sender, EventArgs e)
        {
            FInEditMode = true;

            if (txtKeyMinistry.Visible)
            {
                SetKeyMinistryTextBoxInvisible(null, null);
            }
        }

        private void EndEditMode(object sender, EventArgs e)
        {
            FInEditMode = false;

            if (!txtKeyMinistry.Visible)
            {
                SetTextBoxOverlayOnKeyMinistryCombo();
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
            if (FInKeyMinistryChanging || FinRecipientKeyChanging || FPetraUtilsObject.SuppressChangeDetection || txtKeyMinistry.Visible)
            {
                return;
            }

            FInKeyMinistryChanging = true;

            try
            {
                if (cmbMinistry.Count == 0)
                {
                    cmbMinistry.SelectedIndex = -1;
                    txtKeyMinistry.Text = string.Empty;
                }
                else
                {
                    txtKeyMinistry.Text = cmbMinistry.GetSelectedDescription();
                }
            }
            finally
            {
                FInKeyMinistryChanging = false;
            }
        }

        private void FilterMotivationDetail(object sender, EventArgs e)
        {
            if (!FBatchUnposted || FPetraUtilsObject.SuppressChangeDetection || !FInEditMode || txtKeyMinistry.Visible)
            {
                return;
            }

            FMotivationGroup = cmbDetailMotivationGroupCode.GetSelectedString();
            FMotivationDetail = string.Empty;

            ApplyMotivationDetailCodeFilter();
        }

        private void ApplyMotivationDetailCodeFilter()
        {
            //FMotivationbDetail will change by next process
            string motivationDetail = FMotivationDetail;

            ResetMotivationDetailCodeFilter();
            TFinanceControls.ChangeFilterMotivationDetailList(ref cmbDetailMotivationDetailCode, FMotivationGroup);
            FMotivationDetail = motivationDetail;

            if (FMotivationDetail.Length > 0)
            {
                cmbDetailMotivationDetailCode.SetSelectedString(FMotivationDetail);
                cmbDetailMotivationDetailCode.Text = FMotivationDetail;
            }
            else if (cmbDetailMotivationDetailCode.Count > 0)
            {
                cmbDetailMotivationDetailCode.SelectedIndex = 0;
                //FMotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();
                //Force refresh of label
                MotivationDetailChanged(null, null);
            }

            RetrieveMotivationDetailAccountCode();

            if ((txtDetailRecipientKey.Text == string.Empty) || (Convert.ToInt64(txtDetailRecipientKey.Text) == 0))
            {
                txtDetailRecipientKey.Text = String.Format("{0:0000000000}", 0);
                RetrieveMotivationDetailCostCentreCode();
            }
        }

        private void ResetMotivationDetailCodeFilter()
        {
            FMotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();

            if (FActiveOnly)
            {
                cmbDetailMotivationDetailCode.Filter = AMotivationDetailTable.GetMotivationStatusDBName() + " = true";
            }
            else
            {
                cmbDetailMotivationDetailCode.Filter = string.Empty;
            }

            cmbDetailMotivationDetailCode.SetSelectedString(FMotivationDetail);
            cmbDetailMotivationDetailCode.RefreshLabel();
        }

        /// <summary>
        /// To be called on the display of a new record
        /// </summary>
        private void RetrieveMotivationDetailAccountCode()
        {
            string AcctCode = string.Empty;

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, FMotivationGroup, FMotivationDetail });

            if (motivationDetail != null)
            {
                AcctCode = motivationDetail.AccountCode.ToString();
            }

            txtDetailAccountCode.Text = AcctCode;
        }

        private void RetrieveMotivationDetailCostCentreCode()
        {
            string CostCentreCode = string.Empty;

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, FMotivationGroup, FMotivationDetail });

            if (motivationDetail != null)
            {
                CostCentreCode = motivationDetail.CostCentreCode.ToString();
            }

            txtDetailCostCentreCode.Text = CostCentreCode;
        }

        private void RetrieveRecipientCostCentreCode(Int64 APartnerKey)
        {
            string FailedUpdates = string.Empty;

            if (FInKeyMinistryChanging || (FPreviouslySelectedDetailRow == null))
            {
                return;
            }

            TRemote.MFinance.Gift.WebConnectors.UpdateCostCentreCodeForRecipients(ref FMainDS,
                out FailedUpdates,
                FPreviouslySelectedDetailRow.GiftTransactionNumber,
                FPreviouslySelectedDetailRow.DetailNumber);

            ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors();

            //FindCostCentreCodeForRecipient(FPreviouslySelectedDetailRow, true);

            txtDetailCostCentreCode.Text = FPreviouslySelectedDetailRow.CostCentreCode;
        }

        private void MotivationDetailChanged(object sender, EventArgs e)
        {
            if (!FBatchUnposted || !FInEditMode || txtKeyMinistry.Visible)
            {
                return;
            }

            FMotivationDetail = cmbDetailMotivationDetailCode.GetSelectedString();

            AMotivationDetailRow motivationDetail = (AMotivationDetailRow)FMainDS.AMotivationDetail.Rows.Find(
                new object[] { FLedgerNumber, FMotivationGroup, FMotivationDetail });

            cmbDetailMotivationDetailCode.RefreshLabel();

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
                PopulateKeyMinistry(PartnerKey);
            }
        }

        private void PopulateKeyMinistry(long APartnerKey = 0)
        {
            cmbMinistry.Clear();

            if (APartnerKey == 0)
            {
                APartnerKey = Convert.ToInt64(txtDetailRecipientKey.Text);

                if (APartnerKey == 0)
                {
                    return;
                }
            }

            GetRecipientData(APartnerKey);

            //long FieldNumber = Convert.ToInt64(txtField.Text);
            //txtDetailCostCentreCode.Text = TRemote.MFinance.Gift.WebConnectors.IdentifyPartnerCostCentre(FLedgerNumber, FieldNumber);
        }

        private void GetRecipientData(long APartnerKey = 0)
        {
            if (APartnerKey == 0)
            {
                APartnerKey = Convert.ToInt64(txtDetailRecipientKey.Text);
            }

            TFinanceControls.GetRecipientData(ref cmbMinistry, ref txtField, APartnerKey, true);
        }

        private void GiftDetailAmountChanged(object sender, EventArgs e)
        {
            TTxtNumericTextBox txn = (TTxtNumericTextBox)sender;

            if (txn.NumberValueDecimal == null)
            {
                return;
            }

            if ((FPreviouslySelectedDetailRow != null) && (GetBatchRow().BatchStatus == MFinanceConstants.BATCH_UNPOSTED))
            {
                UpdateBaseAmount(true);
            }

            UpdateTotals();
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
                if ((FLedgerNumber != -1) && (grdDetails.Rows.Count == 1) && (FBatchRow != null))
                {
                    ((TFrmGiftBatch) this.ParentForm).GetBatchControl().UpdateBatchTotal(0, FBatchRow.BatchNumber);
                }
            }
            else
            {
                GiftNumber = FPreviouslySelectedDetailRow.GiftTransactionNumber;

                foreach (AGiftDetailRow gdr in FMainDS.AGiftDetail.Rows)
                {
                    if (gdr.RowState != DataRowState.Deleted)
                    {
                        if ((gdr.BatchNumber == FBatchNumber) && (gdr.LedgerNumber == FLedgerNumber))
                        {
                            if (gdr.GiftTransactionNumber == GiftNumber)
                            {
                                if (FPreviouslySelectedDetailRow.DetailNumber == gdr.DetailNumber)
                                {
                                    sum += Convert.ToDecimal(txtDetailGiftTransactionAmount.NumberValueDecimal);
                                    sumBatch += Convert.ToDecimal(txtDetailGiftTransactionAmount.NumberValueDecimal);
                                }
                                else
                                {
                                    sum += gdr.GiftTransactionAmount;
                                    sumBatch += gdr.GiftTransactionAmount;
                                }
                            }
                            else
                            {
                                sumBatch += gdr.GiftTransactionAmount;
                            }
                        }
                    }
                }

//christiank: when you have a moFBatchRow.BatchStatus == MFinanceConstants.BATCH_UNPOSTED &&
                txtGiftTotal.NumberValueDecimal = sum;
                txtGiftTotal.CurrencyCode = txtDetailGiftTransactionAmount.CurrencyCode;
                txtGiftTotal.ReadOnly = true;
                //this is here because at the moment the generator does not generate this
                txtBatchTotal.NumberValueDecimal = sumBatch;
                //Now we look at the batch and update the batch data
                ((TFrmGiftBatch) this.ParentForm).GetBatchControl().UpdateBatchTotal(sumBatch, FBatchRow.BatchNumber);
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
        private AGiftBatchRow GetBatchRow()
        {
            return ((TFrmGiftBatch)ParentForm).GetBatchControl().GetCurrentBatchRow();
        }

        /// <summary>
        /// get the details of the current gift
        /// </summary>
        /// <returns></returns>
        private AGiftRow GetGiftRow(Int32 AGiftTransactionNumber)
        {
            return (AGiftRow)FMainDS.AGift.Rows.Find(new object[] { FLedgerNumber, FBatchNumber, AGiftTransactionNumber });
        }

        /// <summary>
        /// get the details of the current gift
        /// </summary>
        /// <returns></returns>
        private GiftBatchTDSAGiftDetailRow GetGiftDetailRow(Int32 AGiftTransactionNumber, Int32 AGiftDetailNumber)
        {
            return (GiftBatchTDSAGiftDetailRow)FMainDS.AGiftDetail.Rows.Find(new object[] { FLedgerNumber, FBatchNumber, AGiftTransactionNumber,
                                                                                            AGiftDetailNumber });
        }

        private bool PreDeleteManual(GiftBatchTDSAGiftDetailRow ARowToDelete, ref string ADeletionQuestion)
        {
            bool allowDeletion = true;

            FGift = GetGiftRow(FPreviouslySelectedDetailRow.GiftTransactionNumber);
            FFilterAllDetailsOfGift = String.Format("{0}={1} and {2}={3}",
                AGiftDetailTable.GetBatchNumberDBName(),
                FPreviouslySelectedDetailRow.BatchNumber,
                AGiftDetailTable.GetGiftTransactionNumberDBName(),
                FPreviouslySelectedDetailRow.GiftTransactionNumber);

            FGiftDetailView = new DataView(FMainDS.AGiftDetail);
            FGiftDetailView.RowFilter = FFilterAllDetailsOfGift;
            FGiftDetailView.Sort = AGiftDetailTable.GetDetailNumberDBName() + " ASC";
            String formattedDetailAmount = StringHelper.FormatUsingCurrencyCode(ARowToDelete.GiftTransactionAmount, GetBatchRow().CurrencyCode);

            if (FGiftDetailView.Count == 1)
            {
                ADeletionQuestion = String.Format(Catalog.GetString("Are you sure you want to delete gift no. {1} from Gift Batch no. {2}?" +
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
                    String.Format(Catalog.GetString("Are you sure you want to delete detail line: {0} from gift no. {1} in Gift Batch no. {2}?" +
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
                    String.Format(Catalog.GetString("Gift gift no. {1} in Batch no. {2} has no detail rows in the Gift Detail table!"),
                        ARowToDelete.DetailNumber,
                        ARowToDelete.GiftTransactionNumber,
                        ARowToDelete.BatchNumber);
                allowDeletion = false;
            }

            return allowDeletion;
        }

        private void DeleteAllGifts(System.Object sender, EventArgs e)
        {
            string completionMessage = string.Empty;

            if ((FPreviouslySelectedDetailRow == null) || (FBatchRow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            if ((FPreviouslySelectedDetailRow.RowState == DataRowState.Added)
                ||
                (MessageBox.Show(String.Format(Catalog.GetString(
                             "You have chosen to delete all gifts from batch ({0}).\n\nDo you really want to delete all?"),
                         FBatchNumber),
                     Catalog.GetString("Confirm Delete All"),
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes))
            {
                try
                {
                    //Normally need to set the message parameters before the delete is performed if requiring any of the row values
                    completionMessage = String.Format(Catalog.GetString("All gifts and details cancelled successfully."),
                        FPreviouslySelectedDetailRow.BatchNumber);

                    //Load all journals for current Batch
                    //clear any transactions currently being editied in the Transaction Tab
                    ClearCurrentSelection();

                    //Clear gifts and details etc for current Batch
                    FMainDS.AGiftDetail.Clear();
                    FMainDS.AGift.Clear();

                    //Load tables afresh
                    FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(FLedgerNumber, FBatchNumber));

                    ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors(false);

                    //Delete gift details
                    for (int i = FMainDS.AGiftDetail.Count - 1; i >= 0; i--)
                    {
                        FMainDS.AGiftDetail[i].Delete();
                    }

                    //Delete gifts
                    for (int i = FMainDS.AGift.Count - 1; i >= 0; i--)
                    {
                        FMainDS.AGift[i].Delete();
                    }

                    FBatchRow.BatchTotal = 0;

                    // Be sure to set the last gift number in the parent table before saving all the changes
                    SetBatchLastGiftNumber();

                    FPetraUtilsObject.HasChanges = true;

                    // save first, then post
                    if (!((TFrmGiftBatch)ParentForm).SaveChanges())
                    {
                        SelectRowInGrid(1);

                        // saving failed, therefore do not try to cancel
                        MessageBox.Show(Catalog.GetString("The emptied batch failed to save!"));
                    }
                    else
                    {
                        MessageBox.Show(completionMessage,
                            "All Gifts Deleted.",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    completionMessage = ex.Message;
                    MessageBox.Show(ex.Message,
                        "Deletion Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    //Return FMainDS to original state
                    FMainDS.RejectChanges();
                }
            }

            if (grdDetails.Rows.Count < 2)
            {
                ShowDetails(null);
                UpdateControlsProtection();
            }

            UpdateRecordNumberDisplay();
        }

        private bool DeleteRowManual(GiftBatchTDSAGiftDetailRow ARowToDelete, ref string ACompletionMessage)
        {
            bool deletionSuccessful = false;
            string originatingDetailRef = string.Empty;

            ACompletionMessage = string.Empty;

            if (ARowToDelete == null)
            {
                return deletionSuccessful;
            }

            if ((ARowToDelete.RowState != DataRowState.Added) && !((TFrmGiftBatch) this.ParentForm).SaveChanges())
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
                if (ARowToDelete.ModifiedDetailKey != null)
                {
                    originatingDetailRef = ARowToDelete.ModifiedDetailKey;
                }

                //If deleting a detail row as opposed to a gift header
                if (FGiftDetailView.Count > 1)
                {
                    ARowToDelete.Delete();

                    FGiftSelectedForDeletion = false;

                    foreach (DataRowView rv in FGiftDetailView)
                    {
                        GiftBatchTDSAGiftDetailRow row = (GiftBatchTDSAGiftDetailRow)rv.Row;

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
                        AGiftDetailTable.GetBatchNumberDBName(),
                        currentBatchNumber);

                    DataView giftDetailView = new DataView(FMainDS.AGiftDetail);
                    giftDetailView.RowFilter = filterAllGiftDetailsOfBatch;

                    foreach (DataRowView rv in giftDetailView)
                    {
                        GiftBatchTDSAGiftDetailRow row = (GiftBatchTDSAGiftDetailRow)rv.Row;

                        if (row.GiftTransactionNumber > giftToDeleteTransNo)
                        {
                            row.GiftTransactionNumber--;
                        }
                    }

                    filterAllGiftsOfBatch = String.Format("{0}={1}",
                        AGiftTable.GetBatchNumberDBName(),
                        currentBatchNumber);

                    DataView giftView = new DataView(FMainDS.AGift);
                    giftView.RowFilter = filterAllGiftsOfBatch;
                    giftView.Sort = AGiftTable.GetGiftTransactionNumberDBName();

                    AGiftRow giftRowToReceive = null;
                    AGiftRow giftRowToCopyDown = null;

                    AGiftRow giftRowCurrent = null;

                    int currentGiftTransNo = 0;

                    foreach (DataRowView gv in giftView)
                    {
                        giftRowCurrent = (AGiftRow)gv.Row;

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
                            giftRowCurrent.GiftStatus = MFinanceConstants.MARKED_FOR_DELETION;
                        }

                        //Will always be previous row
                        giftRowToReceive = giftRowCurrent;
                    }

                    FPreviouslySelectedDetailRow = null;

                    FPetraUtilsObject.SetChangedFlag();

                    FGiftSelectedForDeletion = true;
                }

                //Check if deleting a reversed gift detail
                if (originatingDetailRef.StartsWith("|"))
                {
                    bool ok = TRemote.MFinance.Gift.WebConnectors.ReversedGiftReset(FLedgerNumber, originatingDetailRef);

                    if (!ok)
                    {
                        throw new Exception("Error in trying to reset Modified Detail field of the originating gift detail.");
                    }
                }

                //Try to save changes
                if (((TFrmGiftBatch) this.ParentForm).SaveChanges())
                {
                    //Reload from server
                    FMainDS.AGiftDetail.Clear();
                    FMainDS.AGift.Clear();

                    FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(FLedgerNumber, FBatchNumber));

                    ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors(false);
                }
                else
                {
                    throw new Exception("Unable to save after deleting a gift!");
                }

                ACompletionMessage = Catalog.GetString("Gift row deleted successfully!");

                deletionSuccessful = true;
            }
            catch (Exception ex)
            {
                ACompletionMessage = ex.Message;
                MessageBox.Show(ex.Message,
                    "Gift Deletion Error",
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

        private void PostDeleteManual(GiftBatchTDSAGiftDetailRow ARowToDelete,
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

                ((TFrmGiftBatch) this.ParentForm).SaveChanges();

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
            DataView dv = new DataView(FMainDS.AGift);

            dv.RowFilter = String.Format("{0}={1}",
                AGiftTable.GetBatchNumberDBName(),
                FBatchNumber);

            dv.Sort = String.Format("{0} DESC",
                AGiftTable.GetGiftTransactionNumberDBName());

            dv.RowStateFilter = DataViewRowState.CurrentRows;

            if (dv.Count > 0)
            {
                AGiftRow transRow = (AGiftRow)dv[0].Row;
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
                    AGiftDetailTable.GetBatchNumberDBName(),
                    FBatchNumber);
                FFilterPanelControls.SetBaseFilter(rowFilter, true);
                FMainDS.AGiftDetail.DefaultView.RowFilter = rowFilter;
                FCurrentActiveFilter = rowFilter;
                // We don't apply the filter yet!

                FMainDS.AGiftDetail.DefaultView.Sort = string.Format("{0} DESC, {1}",
                    AGiftDetailTable.GetGiftTransactionNumberDBName(),
                    AGiftDetailTable.GetDetailNumberDBName());
            }
        }

        private void ClearControls()
        {
            try
            {
                FPetraUtilsObject.SuppressChangeDetection = true;

                txtDetailDonorKey.Text = string.Empty;
                txtDetailReference.Clear();
                dtpDateEntered.Clear();
                txtGiftTotal.NumberValueDecimal = 0;
                txtDetailGiftTransactionAmount.NumberValueDecimal = 0;
                txtDetailRecipientKey.Text = string.Empty;
                txtField.Text = string.Empty;
                txtDetailAccountCode.Clear();
                txtDetailGiftCommentOne.Clear();
                txtDetailGiftCommentTwo.Clear();
                txtDetailGiftCommentThree.Clear();
                cmbDetailMethodOfPaymentCode.SelectedIndex = -1;
                cmbDetailReceiptLetterCode.SelectedIndex = -1;
                cmbDetailMotivationGroupCode.SelectedIndex = -1;
                cmbDetailMotivationDetailCode.SelectedIndex = -1;
                cmbDetailCommentOneType.SelectedIndex = -1;
                cmbDetailCommentTwoType.SelectedIndex = -1;
                cmbDetailCommentThreeType.SelectedIndex = -1;
                cmbDetailMailingCode.SelectedIndex = -1;
                cmbDetailMethodOfGivingCode.SelectedIndex = -1;
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
            AGiftDetailRow GiftDetailRow = NewGift(); // returns AGiftDetailRow

            if (GiftDetailRow != null)
            {
                FPetraUtilsObject.SetChangedFlag();

                if (!SelectDetailRowByDataTableIndex(FMainDS.AGiftDetail.Rows.Count - 1))
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

                        SelectDetailRowByDataTableIndex(FMainDS.AGiftDetail.Rows.Count - 1);
                    }
                }

                btnDeleteAll.Enabled = btnDelete.Enabled && (FFilterPanelControls.BaseFilter == FCurrentActiveFilter);
            }

            UpdateRecordNumberDisplay();
        }

        /// <summary>
        /// make sure the correct transaction number is assigned and the batch.lastTransactionNumber is updated
        /// </summary>
        private AGiftDetailRow NewGift()
        {
            GiftBatchTDSAGiftDetailRow newRow = null;

            if (ValidateAllData(true, true))
            {
                AGiftRow giftRow = FMainDS.AGift.NewRowTyped(true);

                giftRow.DateEntered = FBatchRow.GlEffectiveDate;
                giftRow.LedgerNumber = FBatchRow.LedgerNumber;
                giftRow.BatchNumber = FBatchRow.BatchNumber;
                giftRow.GiftTransactionNumber = FBatchRow.LastGiftNumber + 1;
                giftRow.MethodOfPaymentCode = FBatchRow.MethodOfPaymentCode;
                FBatchRow.LastGiftNumber++;
                giftRow.LastDetailNumber = 1;

                FMainDS.AGift.Rows.Add(giftRow);

                newRow = FMainDS.AGiftDetail.NewRowTyped(true);

                newRow.LedgerNumber = FBatchRow.LedgerNumber;
                newRow.BatchNumber = FBatchRow.BatchNumber;
                newRow.GiftTransactionNumber = giftRow.GiftTransactionNumber;
                newRow.DetailNumber = 1;
                newRow.DateEntered = giftRow.DateEntered;
                newRow.DonorKey = 0;
                cmbDetailMotivationGroupCode.SelectedIndex = 0;
                newRow.MotivationGroupCode = cmbDetailMotivationGroupCode.GetSelectedString();
                newRow.MotivationDetailCode = cmbDetailMotivationDetailCode.GetSelectedString();
                //RetrieveMotivationDetailCostCentreCode();
                //newRow.CostCentreCode = txtDetailCostCentreCode.Text;

                FMainDS.AGiftDetail.Rows.Add(newRow);
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
            AGiftDetailRow giftDetailRow = NewGiftDetail((GiftBatchTDSAGiftDetailRow)FPreviouslySelectedDetailRow);

            if (giftDetailRow != null)
            {
                FPetraUtilsObject.SetChangedFlag();

                if (!SelectDetailRowByDataTableIndex(FMainDS.AGiftDetail.Rows.Count - 1))
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

                        SelectDetailRowByDataTableIndex(FMainDS.AGiftDetail.Rows.Count - 1);
                    }
                }

                btnDeleteAll.Enabled = btnDelete.Enabled && (FFilterPanelControls.BaseFilter == FCurrentActiveFilter);

                RetrieveMotivationDetailAccountCode();
                txtDetailRecipientKey.Focus();
            }

            UpdateRecordNumberDisplay();
        }

        /// <summary>
        /// add another gift detail to an existing gift
        /// </summary>
        private AGiftDetailRow NewGiftDetail(GiftBatchTDSAGiftDetailRow ACurrentRow)
        {
            GiftBatchTDSAGiftDetailRow newRow = null;

            if (ValidateAllData(true, true))
            {
                if (ACurrentRow == null)
                {
                    return NewGift();
                }

                // find gift row
                AGiftRow giftRow = GetGiftRow(ACurrentRow.GiftTransactionNumber);

                giftRow.LastDetailNumber++;

                newRow = FMainDS.AGiftDetail.NewRowTyped(true);

                newRow.LedgerNumber = giftRow.LedgerNumber;
                newRow.BatchNumber = giftRow.BatchNumber;
                newRow.GiftTransactionNumber = giftRow.GiftTransactionNumber;
                newRow.DetailNumber = giftRow.LastDetailNumber;
                newRow.MethodOfPaymentCode = giftRow.MethodOfPaymentCode;
                newRow.MethodOfGivingCode = giftRow.MethodOfGivingCode;
                newRow.DonorKey = ACurrentRow.DonorKey;
                newRow.DonorName = ACurrentRow.DonorName;
                newRow.DateEntered = giftRow.DateEntered;
                cmbDetailMotivationGroupCode.SelectedIndex = 0;
                newRow.MotivationGroupCode = cmbDetailMotivationGroupCode.GetSelectedString();
                newRow.MotivationDetailCode = cmbDetailMotivationDetailCode.GetSelectedString();
                //RetrieveMotivationDetailCostCentreCode();
                //newRow.CostCentreCode = txtDetailCostCentreCode.Text;

                FMainDS.AGiftDetail.Rows.Add(newRow);
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
                txtDetailGiftTransactionAmount.CurrencyCode = FBatchRow.CurrencyCode;
                txtBatchStatus.Text = FBatchStatus;
            }

            if (grdDetails.Rows.Count == 1)
            {
                txtBatchTotal.NumberValueDecimal = 0;
                ClearControls();
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

        private void ShowDetailsManual(GiftBatchTDSAGiftDetailRow ARow)
        {
            if (!txtKeyMinistry.Visible)
            {
                SetTextBoxOverlayOnKeyMinistryCombo();
            }

            if (ARow == null)
            {
                return;
            }

            this.Cursor = Cursors.WaitCursor;

            FShowingDetails = true;
            FInEditMode = false;

            //Record current values for motivation
            FMotivationGroup = ARow.MotivationGroupCode;
            FMotivationDetail = ARow.MotivationDetailCode;
//            FKeyMinistry = ARow.RecipientKeyMinistry;

            if (ARow.IsRecipientKeyMinistryNull())
            {
                txtKeyMinistry.Text = string.Empty;
            }
            else
            {
                txtKeyMinistry.Text = ARow.RecipientKeyMinistry;
            }

            //Show gift table values
            AGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);
            ShowDetailsForGift(giftRow);

            if (ARow.IsDonorKeyNull())
            {
                txtDetailDonorKey.Text = "0";
            }
            else
            {
                txtDetailDonorKey.Text = ARow.DonorKey.ToString();
            }

            if (ARow.IsCostCentreCodeNull())
            {
                txtDetailCostCentreCode.Text = string.Empty;
            }
            else
            {
                txtDetailCostCentreCode.Text = ARow.CostCentreCode;
            }

            if (ARow.IsAccountCodeNull())
            {
                txtDetailAccountCode.Text = string.Empty;
            }
            else
            {
                txtDetailAccountCode.Text = ARow.AccountCode;
            }

            if (ARow.IsRecipientKeyNull())
            {
                txtDetailRecipientKey.Text = String.Format("{0:0000000000}", 0);
            }
            else
            {
                txtDetailRecipientKey.Text = String.Format("{0:0000000000}", ARow.RecipientKey);
            }

            if (ARow.IsRecipientFieldNull())
            {
                txtField.Text = string.Empty;
            }
            else
            {
                txtField.Text = ARow.RecipientField.ToString();
            }

            UpdateControlsProtection(ARow);

            FShowingDetails = false;

            this.Cursor = Cursors.Default;
        }

        private void ShowDetailsForGift(AGiftRow ACurrentGiftRow)
        {
            //Set GiftRow controls
            dtpDateEntered.Date = ACurrentGiftRow.DateEntered;

//            if (cmbMinistry.Count == 0)
//            {
//                cmbMinistry.SelectedIndex = -1;
//                cmbMinistry.Text = string.Empty;
//            }
//
            txtDetailDonorKey.Text = ACurrentGiftRow.DonorKey.ToString();

            if (ACurrentGiftRow.IsMethodOfGivingCodeNull())
            {
                cmbDetailMethodOfGivingCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailMethodOfGivingCode.SetSelectedString(ACurrentGiftRow.MethodOfGivingCode);
            }

            if (ACurrentGiftRow.IsMethodOfPaymentCodeNull())
            {
                cmbDetailMethodOfPaymentCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailMethodOfPaymentCode.SetSelectedString(ACurrentGiftRow.MethodOfPaymentCode);
            }

            if (ACurrentGiftRow.IsReferenceNull())
            {
                txtDetailReference.Text = String.Empty;
            }
            else
            {
                txtDetailReference.Text = ACurrentGiftRow.Reference;
            }

            if (ACurrentGiftRow.IsReceiptLetterCodeNull())
            {
                cmbDetailReceiptLetterCode.SelectedIndex = -1;
            }
            else
            {
                cmbDetailReceiptLetterCode.SetSelectedString(ACurrentGiftRow.ReceiptLetterCode);
            }
        }

        /// <summary>
        /// set the currency symbols for the currency field from outside
        /// </summary>
        public void UpdateCurrencySymbols(String ACurrencyCode)
        {
            txtDetailGiftTransactionAmount.CurrencyCode = ACurrencyCode;
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

            if (!((TFrmGiftBatch) this.ParentForm).GetBatchControl().FBatchLoaded)
            {
                return;
            }

            FBatchRow = GetBatchRow();

            if (FBatchRow == null)
            {
                FBatchRow = ((TFrmGiftBatch) this.ParentForm).GetBatchControl().GetSelectedDetailRow();
            }

            FBatchMethodOfPayment = ((TFrmGiftBatch) this.ParentForm).GetBatchControl().FSelectedBatchMethodOfPayment;

            ledgerNumber = FBatchRow.LedgerNumber;
            batchNumber = FBatchRow.BatchNumber;

            if (FMainDS.AGift.Rows.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(ledgerNumber, batchNumber));

                ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors(false);
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
            foreach (AGiftRow giftRow in FMainDS.AGift.Rows)
            {
                if (giftRow.BatchNumber.Equals(batchNumber) && giftRow.LedgerNumber.Equals(ledgerNumber)
                    && (giftRow.MethodOfPaymentCode != FBatchMethodOfPayment))
                {
                    giftRow.MethodOfPaymentCode = FBatchMethodOfPayment;
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

        private void UpdateControlsProtection(AGiftDetailRow ARow, AGiftRow AGift = null)
        {
            bool firstIsEnabled = (ARow != null) && (ARow.DetailNumber == 1) && !ViewMode;

            dtpDateEntered.Enabled = firstIsEnabled;
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
                PnlDetailsProtected = (ViewMode
                                       || !FBatchUnposted);
            }
            else
            {
                PnlDetailsProtected = (ViewMode
                                       || !FBatchUnposted
                                       || (ARow.GiftTransactionAmount < 0 && GetGiftRow(ARow.GiftTransactionNumber).ReceiptNumber != 0)
                                       );    // taken from old petra
            }

            pnlDetails.Enabled = !(PnlDetailsProtected);

            btnDelete.Enabled = ((grdDetails.Rows.Count > 1) && !PnlDetailsProtected);
            btnDeleteAll.Enabled = btnDelete.Enabled && (FFilterPanelControls.BaseFilter == FCurrentActiveFilter);
            btnNewDetail.Enabled = !PnlDetailsProtected;
            btnNewGift.Enabled = !PnlDetailsProtected;
        }

        private Boolean BatchHasMethodOfPayment()
        {
            String batchMop = GetMethodOfPaymentFromBatch();

            return batchMop != null && batchMop.Length > 0;
        }

        private String GetMethodOfPaymentFromBatch()
        {
            if (FBatchMethodOfPayment == string.Empty)
            {
                FBatchMethodOfPayment = ((TFrmGiftBatch)ParentForm).GetBatchControl().MethodOfPaymentCode;
            }

            return FBatchMethodOfPayment;
        }

        private void GetDetailDataFromControlsManual(GiftBatchTDSAGiftDetailRow ARow)
        {
            if (txtDetailCostCentreCode.Text.Length == 0)
            {
                ARow.SetCostCentreCodeNull();
            }
            else
            {
                ARow.CostCentreCode = txtDetailCostCentreCode.Text;
            }

            if (txtDetailAccountCode.Text.Length == 0)
            {
                ARow.SetAccountCodeNull();
            }
            else
            {
                ARow.AccountCode = txtDetailAccountCode.Text;
            }

            if (txtDetailRecipientKey.LabelText.Length == 0)
            {
                ARow.SetRecipientDescriptionNull();
            }
            else
            {
                ARow.RecipientDescription = txtDetailRecipientKey.LabelText;
            }

            if (txtField.Text.Length == 0)
            {
                ARow.SetRecipientFieldNull();
            }
            else
            {
                ARow.RecipientField = Convert.ToInt64(txtField.Text);
            }

            if (txtKeyMinistry.Text.Length == 0)
            {
                ARow.SetRecipientKeyMinistryNull();
            }
            else
            {
                ARow.RecipientKeyMinistry = txtKeyMinistry.Text;
            }

            //Handle gift table fields for first detail only
            if (ARow.DetailNumber == 1)
            {
                AGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);

                giftRow.DonorKey = Convert.ToInt64(txtDetailDonorKey.Text);
                giftRow.DateEntered = (dtpDateEntered.Date.HasValue ? dtpDateEntered.Date.Value : FBatchRow.GlEffectiveDate);

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

        private void ValidateDataDetailsManual(AGiftDetailRow ARow)
        {
            if ((ARow == null) || (GetBatchRow() == null) || (GetBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

            TSharedFinanceValidation_Gift.ValidateGiftDetailManual(this, ARow, ref VerificationResultCollection,
                FValidationControlsDict);

            //It is necessary to validate the unbound control for date entered. This requires us to pass the control.
            AGiftRow giftRow = GetGiftRow(ARow.GiftTransactionNumber);

            TSharedFinanceValidation_Gift.ValidateGiftManual(this,
                giftRow,
                FBatchRow.BatchYear,
                FBatchRow.BatchPeriod,
                dtpDateEntered,
                ref VerificationResultCollection,
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

        /// <summary>
        /// Refresh the dataset for this form
        /// </summary>
        public void RefreshAll()
        {
            if ((FMainDS != null) && (FMainDS.AGiftDetail != null))
            {
                FMainDS.AGiftDetail.Rows.Clear();
            }

            FBatchRow = GetBatchRow();

            if (FBatchRow != null)
            {
                LoadGifts(FBatchRow.LedgerNumber, FBatchRow.BatchNumber, FBatchRow.BatchStatus);
            }
        }

        private void ReverseGift(System.Object sender, System.EventArgs e)
        {
            ShowRevertAdjustForm("Reverse Gift");
        }

        /// <summary>
        /// show the form for the gift reversal/adjustment
        /// </summary>
        /// <param name="AFunctionName">Which function shall be called on the server</param>
        public void ShowRevertAdjustForm(String AFunctionName)
        {
            bool reverseWholeBatch = (AFunctionName == "Reverse Gift Batch");

            AGiftBatchRow giftBatch = ((TFrmGiftBatch)ParentForm).GetBatchControl().GetSelectedDetailRow();

            if (giftBatch == null)
            {
                MessageBox.Show(Catalog.GetString("Please select a Gift Batch to Reverse."));
                return;
            }

            if (!giftBatch.BatchStatus.Equals(MFinanceConstants.BATCH_POSTED))
            {
                MessageBox.Show(Catalog.GetString("This function is only possible when the selected batch is already posted."));
                return;
            }

            if (FPetraUtilsObject.HasChanges)
            {
                MessageBox.Show(Catalog.GetString("Please save first and than try again!"));
                return;
            }

            if (FPreviouslySelectedDetailRow == null)
            {
                MessageBox.Show(Catalog.GetString("Please select a Gift to Reverse."));
                return;
            }

            if (reverseWholeBatch && (FBatchNumber != giftBatch.BatchNumber))
            {
                LoadGifts(giftBatch.LedgerNumber, giftBatch.BatchNumber, MFinanceConstants.BATCH_POSTED);
            }

            TFrmGiftRevertAdjust revertForm = new TFrmGiftRevertAdjust(FPetraUtilsObject.GetForm());

            try
            {
                ParentForm.ShowInTaskbar = false;
                revertForm.LedgerNumber = FLedgerNumber;
                revertForm.Text = AFunctionName;

                revertForm.AddParam("Function", AFunctionName.Replace(" ", string.Empty));

                if (reverseWholeBatch)
                {
                    revertForm.GiftMainDS = FMainDS;
                }

//                revertForm.GiftBatchRow = giftBatch;   // TODO Decide whether to remove altogether

                revertForm.GiftDetailRow = FPreviouslySelectedDetailRow;

                if (revertForm.ShowDialog() == DialogResult.OK)
                {
                    ((TFrmGiftBatch)ParentForm).RefreshAll();
                }
            }
            finally
            {
                revertForm.Dispose();
                ParentForm.ShowInTaskbar = true;
            }
        }

        /// <summary>
        /// Reverse the whole gift batch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReverseGiftBatch(System.Object sender, System.EventArgs e)
        {
            ShowRevertAdjustForm("Reverse Gift Batch");
        }

        private void ReverseGiftDetail(System.Object sender, System.EventArgs e)
        {
            ShowRevertAdjustForm("Reverse Gift Detail");
        }

        private void AdjustGift(System.Object sender, System.EventArgs e)
        {
            ShowRevertAdjustForm("Adjust Gift");
        }

        /// <summary>
        /// update the transaction DateEntered from outside
        /// </summary>
        /// <param name="ABatchRow"></param>
        public void UpdateDateEntered(AGiftBatchRow ABatchRow)
        {
            Int32 ledgerNumber;
            Int32 batchNumber;
            DateTime batchEffectiveDate;

            if (ABatchRow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED)
            {
                return;
            }

            ledgerNumber = ABatchRow.LedgerNumber;
            batchNumber = ABatchRow.BatchNumber;
            batchEffectiveDate = ABatchRow.GlEffectiveDate;

            if (FMainDS.AGift.Rows.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(ledgerNumber, batchNumber));

                ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors(false);
            }
            else if ((FLedgerNumber == ledgerNumber) || (FBatchNumber == batchNumber))
            {
                FGLEffectivePeriodChanged = true;
                //Rows already active in transaction tab. Need to set current row ac code below will not update selected row
                GetSelectedDetailRow().DateEntered = batchEffectiveDate;
            }

            //Update all transactions
            foreach (AGiftRow giftRow in FMainDS.AGift.Rows)
            {
                if (giftRow.BatchNumber.Equals(batchNumber) && giftRow.LedgerNumber.Equals(ledgerNumber))
                {
                    giftRow.DateEntered = batchEffectiveDate;
                }
            }

            if (FGLEffectivePeriodChanged)
            {
                ShowDetails();
            }
        }

        /// <summary>
        /// update the transaction base amount calculation from outside
        /// </summary>
        public void UpdateBaseAmount(bool AUpdateCurrentRowOnly)
        {
            Int32 ledgerNumber;
            Int32 batchNumber;

            if ((((TFrmGiftBatch)ParentForm).GetBatchControl().GetCurrentBatchRow() == null) || (FLedgerNumber == -1)
                || (GetBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            if (AUpdateCurrentRowOnly)
            {
                //This code runs when the gift amount is updated
                if (FExchangeRateToBase != GetBatchRow().ExchangeRateToBase)
                {
                    FExchangeRateToBase = GetBatchRow().ExchangeRateToBase;
                }

                FPreviouslySelectedDetailRow.GiftAmount = (decimal)txtDetailGiftTransactionAmount.NumberValueDecimal * FExchangeRateToBase;

                return;
            }

            if (!((TFrmGiftBatch) this.ParentForm).GetBatchControl().FBatchLoaded)
            {
                return;
            }

            FBatchRow = GetBatchRow();

            if (FBatchRow == null)
            {
                FBatchRow = ((TFrmGiftBatch) this.ParentForm).GetBatchControl().GetSelectedDetailRow();
            }

            if (FExchangeRateToBase != FBatchRow.ExchangeRateToBase)
            {
                FExchangeRateToBase = FBatchRow.ExchangeRateToBase;
            }
            else
            {
                return;
            }

            ledgerNumber = FBatchRow.LedgerNumber;
            batchNumber = FBatchRow.BatchNumber;

            if (FMainDS.AGift.Rows.Count == 0)
            {
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadTransactions(ledgerNumber, batchNumber));
                ((TFrmGiftBatch)ParentForm).ProcessRecipientCostCentreCodeUpdateErrors(false);
            }
            else if ((FLedgerNumber == ledgerNumber) || (FBatchNumber == batchNumber))
            {
                //Rows already active in transaction tab. Need to set current row ac code below will not update selected row
                if (FPreviouslySelectedDetailRow != null)
                {
                    FPreviouslySelectedDetailRow.GiftAmount = FPreviouslySelectedDetailRow.GiftTransactionAmount * FExchangeRateToBase;
                }
            }

            //Update all transactions
            foreach (AGiftDetailRow gdr in FMainDS.AGiftDetail.Rows)
            {
                gdr.GiftAmount = gdr.GiftTransactionAmount * FExchangeRateToBase;
            }
        }

        private void GiftDateChanged(object sender, EventArgs e)
        {
            if ((FPetraUtilsObject == null) || FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null))
            {
                return;
            }

            try
            {
                DateTime dateValue;

                string aDate = dtpDateEntered.Date.ToString();

                if (!DateTime.TryParse(aDate, out dateValue))
                {
                    dtpDateEntered.Date = FBatchRow.GlEffectiveDate;
                }
            }
            catch
            {
                //Do nothing
            }
        }

        /// Select a special gift detail number from outside
        public void SelectGiftDetailNumber(Int32 AGiftNumber, Int32 AGiftDetailNumber)
        {
            DataView myView = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).DataView;

            for (int counter = 0; (counter < myView.Count); counter++)
            {
                int myViewGiftNumber = (int)myView[counter][2];
                int myViewGiftDetailNumber = (int)(int)myView[counter][3];

                if ((myViewGiftNumber == AGiftNumber) && (myViewGiftDetailNumber == AGiftDetailNumber))
                {
                    SelectRowInGrid(counter + 1);
                    break;
                }
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

            btnDeleteAll.Enabled = btnDelete.Enabled && (FFilterPanelControls.BaseFilter == FCurrentActiveFilter);
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