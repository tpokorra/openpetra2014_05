//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       wolfgangb
//
// Copyright 2004-2011 by OM International
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
using System.Windows.Forms;
using Ict.Common;
using Ict.Common.Data;

using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared.MFinance.Gift.Data;

namespace Ict.Petra.Client.MFinance.Gui.Gift
{
    public partial class TFrmRecurringGiftBatch
    {
        private Int32 FLedgerNumber;

        /// <summary>
        /// use this ledger
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;
                ucoBatches.LoadBatches(FLedgerNumber);
            }
        }

        /// <summary>
        /// show the actual data of the database after server has changed data
        /// </summary>
        public void RefreshAll()
        {
            ucoBatches.RefreshAll();
        }

        private void InitializeManualCode()
        {
            tabGiftBatch.Selecting += new TabControlCancelEventHandler(TabSelectionChanging);
            this.tpgTransactions.Enabled = false;
        }

        /// <summary>
        /// Handles the click event for filter/find.
        /// </summary>
        /// <param name="sender">Pass this on to the user control.</param>
        /// <param name="e">Not evaluated.</param>
        public void mniFilterFind_Click(object sender, System.EventArgs e)
        {
            switch (tabGiftBatch.SelectedIndex)
            {
                case (int)eGiftTabs.Batches:
                    ucoBatches.MniFilterFind_Click(sender, e);
                    break;

                case (int)eGiftTabs.Transactions:
                    ucoTransactions.MniFilterFind_Click(sender, e);
                    break;
            }
        }

        private int standardTabIndex = 0;

        private void TFrmGiftBatch_Load(object sender, EventArgs e)
        {
            FPetraUtilsObject.TFrmPetra_Load(sender, e);

            tabGiftBatch.SelectedIndex = standardTabIndex;
            TabSelectionChanged(null, null);
        }

        private void RunOnceOnActivationManual()
        {
            ucoBatches.Focus();
            HookupAllInContainer(ucoBatches);
            HookupAllInContainer(ucoTransactions);
            this.Resize += new EventHandler(TFrmGiftBatch_Resize);
        }

        private bool FWindowIsMaximized = false;
        void TFrmGiftBatch_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                // set the flag that we are maximized
                FWindowIsMaximized = true;

                if (tabGiftBatch.SelectedTab == this.tpgBatches)
                {
                    ucoTransactions.AutoSizeGrid();
                    Console.WriteLine("Maximised - autosizing transactions");
                }
                else
                {
                    ucoBatches.AutoSizeGrid();
                    Console.WriteLine("Maximised - autosizing batches");
                }
            }
            else if (FWindowIsMaximized && (this.WindowState == FormWindowState.Normal))
            {
                // we have been maximized but now are normal.  In this case we need to re-autosize the cells because otherwise they are still 'stretched'.
                ucoBatches.AutoSizeGrid();
                ucoTransactions.AutoSizeGrid();
                FWindowIsMaximized = false;
                Console.WriteLine("Normal - autosizing both");
            }
        }

        /// <summary>
        /// activate the transactions tab and load the gift transactions of the batch
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ABatchNumber"></param>
        /// <returns>True if new transactions were actually loaded, False if transactions have already been loaded for the ledger/batch</returns>
        public bool LoadTransactions(Int32 ALedgerNumber, Int32 ABatchNumber)
        {
            try
            {
                //this.tpgTransactions.Enabled = true;
                FPetraUtilsObject.DisableDataChangedEvent();
                return this.ucoTransactions.LoadGifts(ALedgerNumber, ABatchNumber);
            }
            finally
            {
                FPetraUtilsObject.EnableDataChangedEvent();
            }
        }

        /// <summary>
        /// this should be called when all data is reloaded after posting
        /// </summary>
        public void ClearCurrentSelections()
        {
            if (this.ucoBatches != null)
            {
                this.ucoBatches.ClearCurrentSelection();
            }

            if (this.ucoTransactions != null)
            {
                this.ucoTransactions.ClearCurrentSelection();
            }
        }

        /// enable the transaction tab page
        public void EnableTransactionsTab(bool AEnable = true)
        {
            this.tpgTransactions.Enabled = AEnable;
        }

        /// enable the transaction tab page
        public void DisableTransactionsTab()
        {
            this.tpgTransactions.Enabled = false;
        }

        /// <summary>
        /// directly access the batches control
        /// </summary>
        public TUC_RecurringGiftBatches GetBatchControl()
        {
            return ucoBatches;
        }

        /// <summary>
        /// directly access the transactions control
        /// </summary>
        public TUC_RecurringGiftTransactions GetTransactionsControl()
        {
            return ucoTransactions;
        }

        /// this window contains 2 tabs
        public enum eGiftTabs
        {
            /// list of batches
            Batches,

            /// list of transactions
            Transactions
        };

        void TabSelectionChanging(object sender, TabControlCancelEventArgs e)
        {
            FPetraUtilsObject.VerificationResultCollection.Clear();

            if (!SaveChanges())
            {
                e.Cancel = true;

                FPetraUtilsObject.VerificationResultCollection.FocusOnFirstErrorControlRequested = true;
            }
        }

        private void SelectTabManual(int ASelectedTabIndex)
        {
            if (ASelectedTabIndex == (int)eGiftTabs.Batches)
            {
                SelectTab(eGiftTabs.Batches);
            }
            else
            {
                SelectTab(eGiftTabs.Transactions);
            }
        }

        /// <summary>
        /// Switch to the given tab
        /// </summary>
        /// <param name="ATab"></param>
        public void SelectTab(eGiftTabs ATab)
        {
            if (ATab == eGiftTabs.Batches)
            {
                this.tabGiftBatch.SelectedTab = this.tpgBatches;
                this.tpgTransactions.Enabled = (ucoBatches.GetSelectedDetailRow() != null);
                this.ucoBatches.FocusGrid();
            }
            else if (ATab == eGiftTabs.Transactions)
            {
                if (this.tpgTransactions.Enabled)
                {
                    // Note!! This call may result in this (SelectTab) method being called again (but no new transactions will be loaded the second time)
                    // But we need this to be set before calling ucoTransactions.AutoSizeGrid() because that only works once the page is actually loaded.
                    this.tabGiftBatch.SelectedTab = this.tpgTransactions;
                    ARecurringGiftBatchRow SelectedRow = ucoBatches.GetSelectedDetailRow();

                    if (SelectedRow != null)
                    {
                        this.Cursor = Cursors.WaitCursor;

                        if (LoadTransactions(SelectedRow.LedgerNumber, SelectedRow.BatchNumber))
                        {
                            // We will only call this on the first time through (if we are called twice the second time will not actually load new transactions)
                            ucoTransactions.AutoSizeGrid();
                        }

                        this.Cursor = Cursors.Default;
                    }

                    this.ucoTransactions.FocusGrid();
                }
            }
        }
    }
}