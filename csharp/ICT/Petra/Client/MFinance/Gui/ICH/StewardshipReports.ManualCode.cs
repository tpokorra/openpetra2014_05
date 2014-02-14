﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       christophert
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
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Common.Remoting.Shared;
using Ict.Common.Remoting.Client;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Client.CommonControls;
using Ict.Petra.Client.MFinance.Logic;

using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.Interfaces.MFinance;


namespace Ict.Petra.Client.MFinance.Gui.ICH
{
    /// <summary>
    /// Enums holding the possible reporting period selection modes
    /// </summary>
    public enum TICHReportingPeriodSelectionModeEnum
    {
        /// <summary>
        /// ICH Statement reporting period selection mode
        /// </summary>
        rpsmICHStatement,
        /// <summary>
        /// ICH Stewardship Calculation reporting period selection mode
        /// </summary>
        rpsmICHStewardshipCalc
    }


    /// manual methods for the generated window
    public partial class TFrmStewardshipReports : System.Windows.Forms.Form
    {
        /// <summary>
        /// Field to store the reporting period selection mode
        /// </summary>
        public TICHReportingPeriodSelectionModeEnum FReportingPeriodSelectionMode = TICHReportingPeriodSelectionModeEnum.rpsmICHStewardshipCalc;
        /// <summary>
        /// Field to store the relevant Ledger number
        /// </summary>
        public Int32 FLedgerNumber = 0;

        /// <summary>
        /// Gets or sets the ICH reporting period selection mode
        /// </summary>
        public TICHReportingPeriodSelectionModeEnum ReportingPeriodSelectionMode
        {
            get
            {
                return FReportingPeriodSelectionMode;
            }

            set
            {
                FReportingPeriodSelectionMode = value;

                if (FReportingPeriodSelectionMode == TICHReportingPeriodSelectionModeEnum.rpsmICHStatement)
                {
                    chkEmailHOSAReport.Enabled = true;
                }
                else
                {
                    chkEmailHOSAReport.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Write-only Ledger number property
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;

                ALedgerRow Ledger =
                    ((ALedgerTable)TDataCache.TMFinance.GetCacheableFinanceTable(TCacheableFinanceTablesEnum.LedgerDetails, FLedgerNumber))[0];

                TFinanceControls.InitialiseAvailableFinancialYearsListHOSA(
                    ref cmbYearEnding,
                    FLedgerNumber);

                //Resize and move label
                cmbYearEnding.ComboBoxWidth += 18;
                cmbYearEnding.AttachedLabel.Left += 18;
            }
        }

        private void EnableHOSAFileOptions(object sender, EventArgs e)
        {
            bool IsEnabled = chkHOSAFile.Checked;

            txtHOSAPrefix.Enabled = IsEnabled;
            chkEmailHOSAFile.Enabled = IsEnabled;
        }

        private void EnableStewardshipFileOptions(object sender, EventArgs e)
        {
            bool IsEnabled = (chkStewardshipFile.Checked || chkEmailStewardshipFileAndReport.Checked);

            chkStewardshipFile.Enabled = !chkEmailStewardshipFileAndReport.Checked;

            if ((chkStewardshipFile.Checked && chkEmailStewardshipFileAndReport.Checked))
            {
                chkStewardshipFile.Checked = false;
            }

            txtBrowseStewardshipFile.Enabled = IsEnabled;
            btnBrowse.Enabled = IsEnabled;
        }

        private void RefreshReportPeriodList(object sender, EventArgs e)
        {
            if (cmbYearEnding.SelectedIndex > -1)
            {
                TFinanceControls.InitialiseAvailableFinancialPeriodsList(
                    ref cmbReportPeriod,
                    FLedgerNumber,
                    cmbYearEnding.GetSelectedInt32(),
                    0,
                    false);
            }
        }

        private void RefreshICHStewardshipNumberList(object sender, EventArgs e)
        {
            if ((cmbReportPeriod.SelectedIndex > -1) && (cmbYearEnding.SelectedIndex > -1))
            {
                DateTime YearEnding;
                DateTime YearStart;

                if (DateTime.TryParse(cmbYearEnding.GetSelectedDescription(), out YearEnding))
                {
                    YearStart = TRemote.MFinance.GL.WebConnectors.DecrementYear(YearEnding).AddDays(1);

                    TFinanceControls.InitialiseICHStewardshipList(ref cmbICHNumber, FLedgerNumber,
                        cmbReportPeriod.GetSelectedInt32(),
                        YearStart.ToShortDateString(),
                        YearEnding.ToShortDateString());
                }
                else
                {
                    TFinanceControls.InitialiseICHStewardshipList(ref cmbICHNumber, FLedgerNumber,
                        cmbReportPeriod.GetSelectedInt32(),
                        null,
                        null);
                }

                cmbICHNumber.SelectedIndex = 0;
            }
        }

        private void GenerateHOSAFiles(Object Sender, EventArgs e)
        {
            int Currency = (this.rbtBase.Checked ? 0 : 1); //0 = base 1 = intl
            string FileName = string.Empty;
            string CostCentreCode = string.Empty;
            bool HOSASuccess = false;

            bool DoGenerateHOSAReports = chkHOSAReport.Checked;
            bool DoEmailHOSAReports = chkEmailHOSAReport.Checked;
            bool DoGenerateHOSAFiles = chkHOSAFile.Checked;
            bool DoEmailHOSAFiles = chkEmailHOSAFile.Checked;
            string HOSAFilePrefix = txtHOSAPrefix.Text;

            TVerificationResultCollection VerificationResults;

            string msg = string.Empty;
            string SuccessfullCostCentres = string.Empty;
            string FailedCostCentres = string.Empty;

            int SelectedReportPeriod = cmbReportPeriod.GetSelectedInt32();
            int SelectedICHNumber = cmbICHNumber.GetSelectedInt32();

            if (!ValidReportPeriod())
            {
                return;
            }

            bool HOSAFilePrefixExists = (HOSAFilePrefix.Length > 0);

            int IndexOfInvalidFilenameCharacter = 0;

            if (HOSAFilePrefixExists)
            {
                IndexOfInvalidFilenameCharacter = HOSAFilePrefix.IndexOfAny(Path.GetInvalidFileNameChars());
            }

            if (!HOSAFilePrefixExists)
            {
                HOSAFilePrefix = Catalog.GetString("HOSAFilesExportFor");
            }
            else if (IndexOfInvalidFilenameCharacter >= 0)
            {
                msg = String.Format("The HOSA File Prefix: '{0}', contains characters not valid in a filename: {1}{2}{2}Please remove and retry.",
                    HOSAFilePrefix,
                    String.Join(", ", Path.GetInvalidFileNameChars()),
                    Environment.NewLine);

                MessageBox.Show(msg, "Generate HOSA Reports and Files", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                txtHOSAPrefix.Focus();
                txtHOSAPrefix.Select(IndexOfInvalidFilenameCharacter, 1);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                DataTable ICHNumbers = TDataCache.TMFinance.GetCacheableFinanceTable(TCacheableFinanceTablesEnum.ICHStewardshipList, FLedgerNumber);

                //Filter for current period
                ICHNumbers.DefaultView.RowFilter = String.Format("{0}={1} And {2}={3}",
                    AIchStewardshipTable.GetPeriodNumberDBName(),
                    SelectedReportPeriod,
                    AIchStewardshipTable.GetIchNumberDBName(),
                    SelectedICHNumber);

                ICHNumbers.DefaultView.Sort = AIchStewardshipTable.GetCostCentreCodeDBName();

                foreach (DataRowView tmpRow in ICHNumbers.DefaultView)
                {
                    AIchStewardshipRow ichRow = (AIchStewardshipRow)tmpRow.Row;

                    CostCentreCode = (string)ichRow[AIchStewardshipTable.ColumnCostCentreCodeId];

                    if (DoGenerateHOSAReports)
                    {
                        //TODO code to generate the HOSA reports
                    }
                    else if (DoGenerateHOSAFiles)
                    {
                        FileName = TClientSettings.PathTemp + Path.DirectorySeparatorChar + HOSAFilePrefix + CostCentreCode + ".csv";

                        HOSASuccess = TRemote.MFinance.ICH.WebConnectors.GenerateHOSAFiles(FLedgerNumber, cmbReportPeriod.GetSelectedInt32(),
                            cmbICHNumber.GetSelectedInt32(), CostCentreCode, Currency, FileName, out VerificationResults);
                    }

                    if (HOSASuccess)
                    {
                        if (SuccessfullCostCentres.Length == 0)
                        {
                            SuccessfullCostCentres = CostCentreCode;
                        }
                        else
                        {
                            SuccessfullCostCentres += ", " + CostCentreCode;
                        }
                    }
                    else
                    {
                        if (FailedCostCentres.Length == 0)
                        {
                            FailedCostCentres = CostCentreCode;
                        }
                        else
                        {
                            FailedCostCentres += ", " + CostCentreCode;
                        }
                    }
                }

                Cursor = Cursors.Default;

                if (SuccessfullCostCentres.Length > 0)
                {
                    msg = String.Format(Catalog.GetString("HOSA file generated successfully for Cost Centre(s):{0}{0}{1}{0}{0}"),
                        Environment.NewLine,
                        SuccessfullCostCentres);
                }

                if (FailedCostCentres.Length > 0)
                {
                    msg += String.Format(Catalog.GetString("HOSA generation FAILED for Cost Centre(s):{0}{0}{1}"),
                        Environment.NewLine,
                        FailedCostCentres);
                }

                if (msg.Length == 0)
                {
                    msg = String.Format(Catalog.GetString("No Cost Centres to process in Ledger {0} for report period: {1} and ICH No.: {2}."),
                        FLedgerNumber,
                        SelectedReportPeriod,
                        SelectedICHNumber);
                }

                MessageBox.Show(msg, Catalog.GetString("Generate HOSA Files"));

                btnCancel.Text = "Close";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private bool ValidReportPeriod()
        {
            if (cmbReportPeriod.SelectedIndex > -1)
            {
                return true;
            }
            else if ((cmbReportPeriod.SelectedIndex == -1) && (cmbReportPeriod.Count > 0))
            {
                MessageBox.Show(Catalog.GetString("Please select a valid reporting period first."));
                cmbReportPeriod.Focus();
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}