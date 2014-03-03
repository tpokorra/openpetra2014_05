//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//		 cthomas
//
// Copyright 2004-2013 by OM International
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Xml;
using System.IO;
using System.Text;
using System.Globalization;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.IO;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Common.Data;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MCommon.Data.Access;
using Ict.Petra.Server.MFinance.Common;
using Ict.Petra.Server.MFinance.GL.Data.Access;
using Ict.Petra.Server.App.Core.Security;
using Ict.Petra.Server.MFinance.GL.WebConnectors;

namespace Ict.Petra.Server.MFinance.Budget.WebConnectors
{
    /// <summary>
    /// maintain the budget
    /// </summary>
    public class TBudgetMaintainWebConnector
    {
        /// <summary>
        /// load budgets
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static BudgetTDS LoadBudget(Int32 ALedgerNumber)
        {
            BudgetTDS MainDS = new BudgetTDS();

            //TODO: need to filter on Year
            ABudgetAccess.LoadViaALedger(MainDS, ALedgerNumber, null);
            ABudgetRevisionAccess.LoadViaALedger(MainDS, ALedgerNumber, null);
            //TODO: need to filter on ABudgetPeriod using LoadViaBudget or LoadViaUniqueKey
            ABudgetPeriodAccess.LoadAll(MainDS, null);
            ALedgerAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, null);

//            ABudgetPeriodTable BudgetPeriodTable = new ABudgetPeriodTable();
//            ABudgetPeriodRow TemplateRow = (ABudgetPeriodRow)BudgetPeriodTable.NewRow(false);
//
//            TemplateRow.BudgetSequence;
//            ABudgetPeriodAccess.LoadViaABudgetTemplate(MainDS, TemplateRow, null);


            // Accept row changes here so that the Client gets 'unmodified' rows
            MainDS.AcceptChanges();

            // Remove all Tables that were not filled with data before remoting them.
            MainDS.RemoveEmptyTables();

            return MainDS;
        }

        /// <summary>
        /// save modified budgets
        /// </summary>
        /// <param name="AInspectDS"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static TSubmitChangesResult SaveBudget(ref BudgetTDS AInspectDS)
        {
            if (AInspectDS != null)
            {
                BudgetTDSAccess.SubmitChanges(AInspectDS);
                
                return TSubmitChangesResult.scrOK;
            }

            return TSubmitChangesResult.scrError;
        }

        /// <summary>
        /// Imports budgets
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACurrentBudgetYear"></param>
        /// <param name="ACSVFileName"></param>
        /// <param name="AFdlgSeparator"></param>
        /// <param name="AImportDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <returns>Total number of records imported and number of which updated as the fractional part</returns>
        [RequireModulePermission("FINANCE-3")]
        public static decimal ImportBudgets(Int32 ALedgerNumber,
            Int32 ACurrentBudgetYear,
            string ACSVFileName,
            string[] AFdlgSeparator,
            ref BudgetTDS AImportDS,
            out TVerificationResultCollection AVerificationResult)
        {
            AVerificationResult = null;

            if (AImportDS != null)
            {
                decimal retVal = ImportBudgetFromCSV(ALedgerNumber,
                    ACurrentBudgetYear,
                    ACSVFileName,
                    AFdlgSeparator,
                    ref AImportDS,
                    ref AVerificationResult);
                return retVal;
            }

            return 0;
        }

        private static CultureInfo FCultureInfoNumberFormat;

        /// <summary>
        /// Import the budget from a CSV file
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACurrentBudgetYear"></param>
        /// <param name="ACSVFileName"></param>
        /// <param name="AFdlgSeparator"></param>
        /// <param name="AImportDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <returns>Total number of records imported and number of which updated as the fractional part</returns>
        private static decimal ImportBudgetFromCSV(Int32 ALedgerNumber,
            Int32 ACurrentBudgetYear,
            string ACSVFileName,
            string[] AFdlgSeparator,
            ref BudgetTDS AImportDS,
            ref TVerificationResultCollection AVerificationResult)
        {
            StreamReader DataFile = new StreamReader(ACSVFileName, System.Text.Encoding.Default);

            string Separator = AFdlgSeparator[0];
            string DateFormat = AFdlgSeparator[1];
            string NumberFormat = AFdlgSeparator[2];

            FCultureInfoNumberFormat = new CultureInfo(NumberFormat.Equals("American") ? "en-US" : "de-DE");
            CultureInfo MyCultureInfoDate = new CultureInfo("en-GB");
            MyCultureInfoDate.DateTimeFormat.ShortDatePattern = DateFormat;

            // To store the From and To currencies
            // Use an array to store these to make for easy
            //   inverting of the two currencies when calculating
            //   the inverse value.

            //string currentBudgetVal = string.Empty;
            //string mess = string.Empty;
            string CostCentre = string.Empty;
            string Account = string.Empty;
            string budgetType = string.Empty;
            string budgetYearString = string.Empty;
            int budgetYear = 0;

            Int32 numPeriods = TAccountingPeriodsWebConnector.GetNumberOfPeriods(ALedgerNumber);

            decimal[] BudgetPeriods = new decimal[numPeriods];
            int YearForBudgetRevision = 0;
            int BdgRevision = 0;  //not currently implementing versioning so always zero

            decimal rowNumber = 0;
            decimal duplicateRowNumber = 0;

            while (!DataFile.EndOfStream)
            {
                decimal totalBudgetRowAmount = 0;

                try
                {
                    string Line = DataFile.ReadLine();

                    CostCentre = StringHelper.GetNextCSV(ref Line, Separator, false).ToString();

                    if (CostCentre == "Cost Centre")
                    {
                        //Read the next line
                        Line = DataFile.ReadLine();
                        CostCentre = StringHelper.GetNextCSV(ref Line, Separator, false).ToString();
                    }

                    //Increment row number
                    rowNumber++;

                    //Convert separator to a char
                    // char Sep = Separator[0];
                    //Turn current line into string array of column values
                    // string[] CsvColumns = Line.Split(Sep);

                    //int NumCols = CsvColumns.Length;

                    //If number of columns is not 4 then import csv file is wrongly formed.
//                if (NumCols != 24)
//                {
//                    AVerificationResult. MessageBox.Show(Catalog.GetString("Failed to import the CSV budget file:\r\n\r\n" +
//                            "   " + ADataFilename + "\r\n\r\n" +
//                            "It contains " + NumCols.ToString() + " columns. " +
//                            ), AImportMode + " Exchange Rates Import Error");
//                    return;
//                }

                    //Read the values for the current line
                    Account = StringHelper.GetNextCSV(ref Line, Separator, false).ToString();
                    budgetType = StringHelper.GetNextCSV(ref Line, Separator, false).ToString().ToUpper();
                    budgetType = budgetType.Replace(" ", ""); //Ad hoc will become ADHOC

                    //Allow for variations on Inf.Base and Inf.N
                    if (budgetType.Contains("INF"))
                    {
                        if (budgetType.Contains("BASE"))
                        {
                            if (budgetType != MFinanceConstants.BUDGET_INFLATE_BASE)
                            {
                                budgetType = MFinanceConstants.BUDGET_INFLATE_BASE;
                            }
                        }
                        else if (budgetType != MFinanceConstants.BUDGET_INFLATE_N)
                        {
                            budgetType = MFinanceConstants.BUDGET_INFLATE_N;
                        }
                    }

                    if ((budgetType != MFinanceConstants.BUDGET_ADHOC)
                        && (budgetType != MFinanceConstants.BUDGET_SAME)
                        && (budgetType != MFinanceConstants.BUDGET_INFLATE_N)
                        && (budgetType != MFinanceConstants.BUDGET_SPLIT)
                        && (budgetType != MFinanceConstants.BUDGET_INFLATE_BASE)
                        )
                    {
                        throw new InvalidOperationException("Budget Type: " + budgetType + " in row: " + rowNumber.ToString() + " does not exist.");
                    }

                    //Calculate the budget Year
                    budgetYearString = StringHelper.GetNextCSV(ref Line, Separator, false);

                    YearForBudgetRevision = BudgetRevisionYearNumber(ALedgerNumber, budgetYearString);

                    //Add budget revision record if there's not one already.
                    if (AImportDS.ABudgetRevision.Rows.Find(new object[] { ALedgerNumber, YearForBudgetRevision, BdgRevision }) == null)
                    {
                        ABudgetRevisionRow BudgetRevisionRow = (ABudgetRevisionRow)AImportDS.ABudgetRevision.NewRowTyped();
                        BudgetRevisionRow.LedgerNumber = ALedgerNumber;
                        BudgetRevisionRow.Year = YearForBudgetRevision;
                        BudgetRevisionRow.Revision = BdgRevision;
                        BudgetRevisionRow.Description = "Budget Import from: " + ACSVFileName;
                        AImportDS.ABudgetRevision.Rows.Add(BudgetRevisionRow);
                    }

                    //Read the budgetperiod values to check if valid according to type
                    Array.Clear(BudgetPeriods, 0, numPeriods);

                    bool successfulBudgetRowProcessing = ProcessBudgetTypeImportDetails(ref Line, Separator, budgetType, ref BudgetPeriods);

                    for (int i = 0; i < numPeriods; i++)
                    {
                        totalBudgetRowAmount += BudgetPeriods[i];
                    }

                    if (!successfulBudgetRowProcessing)
                    {
                        throw new InvalidOperationException(String.Format(
                                "The budget in row {0} for Ledger: {1}, Year: {2}, Cost Centre: {3} and Account: {4}, does not have values consistent with Budget Type: {5}.",
                                rowNumber,
                                ALedgerNumber,
                                budgetYear,
                                CostCentre,
                                Account,
                                budgetType));
                    }

                    BudgetTDS MainDS = new BudgetTDS();

                    ABudgetAccess.LoadByUniqueKey(MainDS, ALedgerNumber, YearForBudgetRevision, BdgRevision, CostCentre, Account, null);
                    //TODO: need to filter on ABudgetPeriod using LoadViaBudget or LoadViaUniqueKey

                    //Check to see if the budget combination already exists:
                    if (MainDS.ABudget.Count > 0)
                    {
                        ABudgetRow BR2 = (ABudgetRow)MainDS.ABudget.Rows[0];

                        int BTSeq = BR2.BudgetSequence;

                        ABudgetRow BdgTRow = (ABudgetRow)AImportDS.ABudget.Rows.Find(new object[] { BTSeq });

                        if (BdgTRow != null)
                        {
                            duplicateRowNumber++;

                            BdgTRow.BeginEdit();
                            //Edit the new budget row
                            BdgTRow.BudgetTypeCode = budgetType;
                            BdgTRow.EndEdit();

                            ABudgetPeriodRow BPRow = null;

                            for (int i = 0; i < numPeriods; i++)
                            {
                                BPRow = (ABudgetPeriodRow)AImportDS.ABudgetPeriod.Rows.Find(new object[] { BTSeq, i + 1 });

                                if (BPRow != null)
                                {
                                    BPRow.BeginEdit();
                                    BPRow.BudgetBase = BudgetPeriods[i];
                                    BPRow.EndEdit();
                                }

                                BPRow = null;
                            }
                        }
                    }
                    else
                    {
                        //Add the new budget row
                        ABudgetRow BudgetRow = (ABudgetRow)AImportDS.ABudget.NewRowTyped();
                        int newSequence = -1 * (AImportDS.ABudget.Rows.Count + 1);

                        BudgetRow.BudgetSequence = newSequence;
                        BudgetRow.LedgerNumber = ALedgerNumber;
                        BudgetRow.Year = YearForBudgetRevision;
                        BudgetRow.Revision = BdgRevision;
                        BudgetRow.CostCentreCode = CostCentre;
                        BudgetRow.AccountCode = Account;
                        BudgetRow.BudgetTypeCode = budgetType;
                        AImportDS.ABudget.Rows.Add(BudgetRow);

                        //Add the budget periods
                        for (int i = 0; i < numPeriods; i++)
                        {
                            ABudgetPeriodRow BudgetPeriodRow = (ABudgetPeriodRow)AImportDS.ABudgetPeriod.NewRowTyped();
                            BudgetPeriodRow.BudgetSequence = newSequence;
                            BudgetPeriodRow.PeriodNumber = i + 1;
                            BudgetPeriodRow.BudgetBase = BudgetPeriods[i];
                            AImportDS.ABudgetPeriod.Rows.Add(BudgetPeriodRow);
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            DataFile.Close();

            if (duplicateRowNumber > 0)
            {
                //fractional part is the number of updates divided by 10000
                if (duplicateRowNumber < 10000)
                {
                    rowNumber += (duplicateRowNumber / 10000);
                }
            }

            return rowNumber;
        }

        private static int BudgetRevisionYearNumber(int ALedgerNumber, string ABudgetYearName)
        {
            int budgetYear = 0;

            ALedgerTable LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, null);
            ALedgerRow ledgerRow = (ALedgerRow)LedgerTable.Rows[0];

            AAccountingPeriodTable accPeriodTable = AAccountingPeriodAccess.LoadByPrimaryKey(ALedgerNumber, 1, null);
            AAccountingPeriodRow accPeriodRow = (AAccountingPeriodRow)accPeriodTable.Rows[0];

            if (ABudgetYearName.ToUpper() == "THIS")
            {
                budgetYear = accPeriodRow.PeriodStartDate.Year;
            }
            else
            {
                budgetYear = accPeriodRow.PeriodStartDate.Year + 1;
            }

            DateTime CurrentYearEnd = TAccountingPeriodsWebConnector.GetPeriodEndDate(ALedgerNumber,
                ledgerRow.CurrentFinancialYear,
                0,
                ledgerRow.NumberOfAccountingPeriods);

            return budgetYear - CurrentYearEnd.Year + ledgerRow.CurrentFinancialYear;
        }

        private static string BudgetRevisionYearName(int ALedgerNumber, int ABudgetRevisionYear)
        {
            int budgetYear = 0;

            ALedgerTable LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, null);
            ALedgerRow ledgerRow = (ALedgerRow)LedgerTable.Rows[0];

            AAccountingPeriodTable accPeriodTable = AAccountingPeriodAccess.LoadByPrimaryKey(ALedgerNumber, 1, null);
            AAccountingPeriodRow accPeriodRow = (AAccountingPeriodRow)accPeriodTable.Rows[0];

            DateTime CurrentYearEnd = TAccountingPeriodsWebConnector.GetPeriodEndDate(ALedgerNumber,
                ledgerRow.CurrentFinancialYear,
                0,
                ledgerRow.NumberOfAccountingPeriods);

            budgetYear = ABudgetRevisionYear + CurrentYearEnd.Year - ledgerRow.CurrentFinancialYear;

            if (budgetYear == accPeriodRow.PeriodStartDate.Year)
            {
                return "This";
            }
            else
            {
                return "Next";
            }
        }

        private static bool ProcessBudgetTypeImportDetails(ref string Line, string Separator, string ABudgetType, ref decimal[] ABudgetPeriods)
        {
            bool retVal = false;
            int numPeriods = ABudgetPeriods.Length;

            try
            {
                switch (ABudgetType)
                {
                    case MFinanceConstants.BUDGET_SAME:

                        string periodAmountString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal periodAmount = Convert.ToDecimal(periodAmountString, FCultureInfoNumberFormat);

                        for (int i = 0; i < numPeriods; i++)
                        {
                            ABudgetPeriods[i] = periodAmount;
                        }

                        break;

                    case MFinanceConstants.BUDGET_SPLIT:

                        string totalAmountString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal totalAmount = Convert.ToDecimal(totalAmountString, FCultureInfoNumberFormat);
                        decimal perPeriodAmount = Math.Truncate(totalAmount / numPeriods);
                        decimal lastPeriodAmount = totalAmount - perPeriodAmount * (numPeriods - 1);

                        //Write to Budget rows
                        for (int i = 0; i < numPeriods; i++)
                        {
                            if (i < (numPeriods - 1))
                            {
                                ABudgetPeriods[i] = perPeriodAmount;
                            }
                            else
                            {
                                ABudgetPeriods[i] = lastPeriodAmount;
                            }
                        }

                        break;

                    case MFinanceConstants.BUDGET_INFLATE_BASE:

                        string period1AmountString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal period1Amount = Convert.ToDecimal(period1AmountString, FCultureInfoNumberFormat);
                        string periodNPercentString = string.Empty;

                        ABudgetPeriods[0] = period1Amount;

                        for (int i = 1; i < numPeriods; i++)
                        {
                            periodNPercentString = StringHelper.GetNextCSV(ref Line, Separator, false);
                            ABudgetPeriods[i] = ABudgetPeriods[i - 1] * (1 + (Convert.ToDecimal(periodNPercentString, FCultureInfoNumberFormat) / 100));
                        }

                        break;

                    case MFinanceConstants.BUDGET_INFLATE_N:

                        string periodStartAmountString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal periodStartAmount = Convert.ToDecimal(periodStartAmountString, FCultureInfoNumberFormat);

                        string inflateAfterPeriodString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal inflateAfterPeriod = Convert.ToDecimal(inflateAfterPeriodString, FCultureInfoNumberFormat);

                        string inflationRateString = StringHelper.GetNextCSV(ref Line, Separator, false);
                        decimal inflationRate = Convert.ToDecimal(inflationRateString, FCultureInfoNumberFormat);

                        decimal subsequentPeriodsAmount = periodStartAmount * (1 + inflationRate);

                        //Control the inflate after period number
                        if (inflateAfterPeriod < 0)
                        {
                            inflateAfterPeriod = 0;
                        }
                        else if (inflateAfterPeriod >= numPeriods)
                        {
                            inflateAfterPeriod = (numPeriods - 1);
                        }

                        //Write the period values
                        for (int i = 0; i < numPeriods; i++)
                        {
                            if (i <= (inflateAfterPeriod - 1))
                            {
                                ABudgetPeriods[i] = periodStartAmount;
                            }
                            else
                            {
                                ABudgetPeriods[i] = subsequentPeriodsAmount;
                            }
                        }

                        break;

                    default:                              //MFinanceConstants.BUDGET_ADHOC:

                        string periodNAmount = string.Empty;

                        for (int i = 0; i < numPeriods; i++)
                        {
                            periodNAmount = StringHelper.GetNextCSV(ref Line, Separator, false);
                            ABudgetPeriods[i] = Convert.ToDecimal(periodNAmount, FCultureInfoNumberFormat);
                        }

                        break;
                }

                retVal = true;
            }
            catch
            {
                //Do nothing
            }

            return retVal;
        }

        /// <summary>
        /// GetGLMSequence
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AAccountCode"></param>
        /// <param name="ACostCentreCode"></param>
        /// <param name="AYear"></param>
        /// <returns>GLM Sequence no</returns>
        [RequireModulePermission("FINANCE-3")]
        public static int GetGLMSequenceForBudget(int ALedgerNumber, string AAccountCode, string ACostCentreCode, int AYear)
        {
            int retVal;

            AGeneralLedgerMasterTable GeneralLedgerMasterTable = AGeneralLedgerMasterAccess.LoadByUniqueKey(ALedgerNumber,
                AYear,
                AAccountCode,
                ACostCentreCode,
                null);

            if (GeneralLedgerMasterTable.Count > 0)
            {
                retVal = (int)GeneralLedgerMasterTable.Rows[0].ItemArray[0];
            }
            else
            {
                retVal = -1;
            }

            return retVal;
        }

        /// <summary>
        /// ------------------------------------------------------------------------------
        /// Description: GetActual retrieves the actuals value of the given period, no matter if it is in a forwarding period.
        ///  GetActual is similar to GetBudget. The main difference is, that forwarding periods are saved in the current year.
        ///  You still need the sequence_next_year, because this_year can be older than current_financial_year of the ledger.
        ///  So you need to give number_accounting_periods and current_financial_year of the ledger.
        ///  You also need to give the number of the year from which you want the data.
        ///  Currency_select is either "B" for base or "I" for international currency or "T" for transaction currency
        ///  You want e.g. the actual data of period 13 in year 2, the current financial year is 3.
        ///  The call would look like: GetActual(sequence_year_2, sequence_year_3, 13, 12, 3, 2, false, "B");
        ///  That means, the function has to return the difference between year 3 period 1 and the start balance of year 3.
        /// ------------------------------------------------------------------------------
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AGLMSeqThisYear"></param>
        /// <param name="AGLMSeqNextYear"></param>
        /// <param name="APeriodNumber"></param>
        /// <param name="ANumberAccountingPeriods"></param>
        /// <param name="ACurrentFinancialYear"></param>
        /// <param name="AThisYear"></param>
        /// <param name="AYTD"></param>
        /// <param name="ACurrencySelect"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static decimal GetActual(int ALedgerNumber,
            int AGLMSeqThisYear,
            int AGLMSeqNextYear,
            int APeriodNumber,
            int ANumberAccountingPeriods,
            int ACurrentFinancialYear,
            int AThisYear,
            bool AYTD,
            string ACurrencySelect)
        {
            decimal retVal = 0;

            retVal = GetActualInternal(ALedgerNumber,
                AGLMSeqThisYear,
                AGLMSeqNextYear,
                APeriodNumber,
                ANumberAccountingPeriods,
                ACurrentFinancialYear,
                AThisYear,
                AYTD,
                false,
                ACurrencySelect);

            return retVal;
        }

        /// <summary>
        /// Get the actual amount
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AGLMSeqThisYear"></param>
        /// <param name="AGLMSeqNextYear"></param>
        /// <param name="APeriodNumber"></param>
        /// <param name="ANumberAccountingPeriods"></param>
        /// <param name="ACurrentFinancialYear"></param>
        /// <param name="AThisYear"></param>
        /// <param name="AYTD"></param>
        /// <param name="ABalSheetForwardPeriods"></param>
        /// <param name="ACurrencySelect"></param>
        /// <returns></returns>
        private static decimal GetActualInternal(int ALedgerNumber,
            int AGLMSeqThisYear,
            int AGLMSeqNextYear,
            int APeriodNumber,
            int ANumberAccountingPeriods,
            int ACurrentFinancialYear,
            int AThisYear,
            bool AYTD,
            bool ABalSheetForwardPeriods,
            string ACurrencySelect)
        {
            decimal retVal = 0;

            decimal currencyAmount = 0;
            bool incExpAccountFwdPeriod = false;

            //DEFINE BUFFER a_glm_period FOR a_general_ledger_master_period.
            //DEFINE BUFFER a_glm FOR a_general_ledger_master.
            //DEFINE BUFFER buf_account FOR a_account.

            if (AGLMSeqThisYear == -1)
            {
                return retVal;
            }

            bool newTransaction = false;
            TDBTransaction dBTransaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(IsolationLevel.ReadCommitted, out newTransaction);

            AGeneralLedgerMasterTable generalLedgerMasterTable = null;
            AGeneralLedgerMasterRow generalLedgerMasterRow = null;

            AGeneralLedgerMasterPeriodTable generalLedgerMasterPeriodTable = null;
            AGeneralLedgerMasterPeriodRow generalLedgerMasterPeriodRow = null;

            AAccountTable AccountTable = null;
            AAccountRow AccountRow = null;

            try
            {
                if (APeriodNumber == 0)             /* start balance */
                {
                    generalLedgerMasterTable = AGeneralLedgerMasterAccess.LoadByPrimaryKey(AGLMSeqThisYear, dBTransaction);
                    generalLedgerMasterRow = (AGeneralLedgerMasterRow)generalLedgerMasterTable.Rows[0];

                    switch (ACurrencySelect)
                    {
                        case MFinanceConstants.CURRENCY_BASE:
                            currencyAmount = generalLedgerMasterRow.StartBalanceBase;
                            break;

                        case MFinanceConstants.CURRENCY_INTERNATIONAL:
                            currencyAmount = generalLedgerMasterRow.StartBalanceIntl;
                            break;

                        default:
                            currencyAmount = generalLedgerMasterRow.StartBalanceForeign;
                            break;
                    }
                }
                else if (APeriodNumber > ANumberAccountingPeriods)             /* forwarding periods only exist for the current financial year */
                {
                    if (ACurrentFinancialYear == AThisYear)
                    {
                        generalLedgerMasterPeriodTable = AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(AGLMSeqThisYear,
                            APeriodNumber,
                            dBTransaction);
                        generalLedgerMasterPeriodRow = (AGeneralLedgerMasterPeriodRow)generalLedgerMasterPeriodTable.Rows[0];
                    }
                    else
                    {
                        generalLedgerMasterPeriodTable =
                            AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(AGLMSeqNextYear,
                                (APeriodNumber - ANumberAccountingPeriods),
                                dBTransaction);
                        generalLedgerMasterPeriodRow = (AGeneralLedgerMasterPeriodRow)generalLedgerMasterPeriodTable.Rows[0];
                    }
                }
                else             /* normal period */
                {
                    generalLedgerMasterPeriodTable = AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(AGLMSeqThisYear, APeriodNumber, dBTransaction);
                    generalLedgerMasterPeriodRow = (AGeneralLedgerMasterPeriodRow)generalLedgerMasterPeriodTable.Rows[0];
                }

                if (generalLedgerMasterPeriodRow != null)
                {
                    switch (ACurrencySelect)
                    {
                        case MFinanceConstants.CURRENCY_BASE:
                            currencyAmount = generalLedgerMasterPeriodRow.ActualBase;
                            break;

                        case MFinanceConstants.CURRENCY_INTERNATIONAL:
                            currencyAmount = generalLedgerMasterPeriodRow.ActualIntl;
                            break;

                        default:
                            currencyAmount = generalLedgerMasterPeriodRow.ActualForeign;
                            break;
                    }
                }

                if ((APeriodNumber > ANumberAccountingPeriods) && (ACurrentFinancialYear == AThisYear))
                {
                    generalLedgerMasterTable = AGeneralLedgerMasterAccess.LoadByPrimaryKey(AGLMSeqThisYear, dBTransaction);
                    generalLedgerMasterRow = (AGeneralLedgerMasterRow)generalLedgerMasterTable.Rows[0];

                    AccountTable = AAccountAccess.LoadByPrimaryKey(ALedgerNumber, generalLedgerMasterRow.AccountCode, dBTransaction);
                    AccountRow = (AAccountRow)AccountTable.Rows[0];

                    if ((AccountRow.AccountCode.ToUpper() == MFinanceConstants.ACCOUNT_TYPE_INCOME.ToUpper())
                        || (AccountRow.AccountCode.ToUpper() == MFinanceConstants.ACCOUNT_TYPE_EXPENSE.ToUpper())
                        && !ABalSheetForwardPeriods)
                    {
                        incExpAccountFwdPeriod = true;
                        currencyAmount -= GetActualInternal(ALedgerNumber,
                            AGLMSeqThisYear,
                            AGLMSeqNextYear,
                            ANumberAccountingPeriods,
                            ANumberAccountingPeriods,
                            ACurrentFinancialYear,
                            AThisYear,
                            true,
                            ABalSheetForwardPeriods,
                            ACurrencySelect);
                    }
                }

                if (!AYTD)
                {
                    if (!((APeriodNumber == (ANumberAccountingPeriods + 1)) && incExpAccountFwdPeriod)
                        && !((APeriodNumber == (ANumberAccountingPeriods + 1)) && (ACurrentFinancialYear > AThisYear)))
                    {
                        /* if it is an income expense acount, and we are in a forward period, nothing needs to be subtracted,
                         * because that was done in correcting the amount in the block above;
                         * if we are in a previous year, in a forward period, don't worry about subtracting.
                         */
                        currencyAmount -= GetActualInternal(ALedgerNumber,
                            AGLMSeqThisYear,
                            AGLMSeqNextYear,
                            (APeriodNumber - 1),
                            ANumberAccountingPeriods,
                            ACurrentFinancialYear,
                            AThisYear,
                            true,
                            ABalSheetForwardPeriods,
                            ACurrencySelect);
                    }
                }

                retVal = currencyAmount;
            }
            finally
            {
                if (newTransaction)
                {
                    DBAccess.GDBAccessObj.RollbackTransaction();
                }
            }

            return retVal;
        }

        /// <summary>
        /// Retrieves a budget value
        /// </summary>
        /// <param name="AGLMSeqThisYear"></param>
        /// <param name="AGLMSeqNextYear"></param>
        /// <param name="APeriodNumber"></param>
        /// <param name="ANumberAccountingPeriods"></param>
        /// <param name="AYTD"></param>
        /// <param name="ACurrencySelect"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-3")]
        public static decimal GetBudget(int AGLMSeqThisYear,
            int AGLMSeqNextYear,
            int APeriodNumber,
            int ANumberAccountingPeriods,
            bool AYTD,
            string ACurrencySelect)
        {
            decimal retVal = 0;

            if (APeriodNumber > ANumberAccountingPeriods)
            {
                retVal = CalculateBudget(AGLMSeqNextYear, 1, (APeriodNumber - ANumberAccountingPeriods), AYTD, ACurrencySelect);
            }
            else
            {
                retVal = CalculateBudget(AGLMSeqThisYear, 1, APeriodNumber, AYTD, ACurrencySelect);
            }

            return retVal;
        }

        /// <summary>
        ///Description: CalculateBudget is only used internally as a helper function for GetBudget.
        ///Returns the budget for the given period of time,
        ///  if ytd is set, this period is from start_period to end_period,
        ///  otherwise it is only the value of the end_period.
        ///  currency_select is either "B" for base or "I" for international currency
        /// </summary>
        /// <param name="AGLMSeq"></param>
        /// <param name="AStartPeriod"></param>
        /// <param name="AEndPeriod"></param>
        /// <param name="AYTD"></param>
        /// <param name="ACurrencySelect"></param>
        /// <returns></returns>
        private static decimal CalculateBudget(int AGLMSeq, int AStartPeriod, int AEndPeriod, bool AYTD, string ACurrencySelect)
        {
            decimal retVal = 0;

            decimal lv_currency_amount_n = 0;
            int lv_ytd_period_i;

            if (AGLMSeq == -1)
            {
                return retVal;
            }

            bool NewTransaction = false;
            TDBTransaction transaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(IsolationLevel.ReadCommitted, out NewTransaction);

            AGeneralLedgerMasterPeriodTable GeneralLedgerMasterPeriodTable = null;
            AGeneralLedgerMasterPeriodRow GeneralLedgerMasterPeriodRow = null;

            if (!AYTD)
            {
                AStartPeriod = AEndPeriod;
            }

            for (lv_ytd_period_i = AStartPeriod; lv_ytd_period_i <= AEndPeriod; lv_ytd_period_i++)
            {
                GeneralLedgerMasterPeriodTable = AGeneralLedgerMasterPeriodAccess.LoadByPrimaryKey(AGLMSeq, lv_ytd_period_i, transaction);
                GeneralLedgerMasterPeriodRow = (AGeneralLedgerMasterPeriodRow)GeneralLedgerMasterPeriodTable.Rows[0];

                if (GeneralLedgerMasterPeriodRow != null)
                {
                    if (ACurrencySelect == MFinanceConstants.CURRENCY_BASE)
                    {
                        lv_currency_amount_n += GeneralLedgerMasterPeriodRow.BudgetBase;
                    }
                    else if (ACurrencySelect == MFinanceConstants.CURRENCY_INTERNATIONAL)
                    {
                        lv_currency_amount_n += GeneralLedgerMasterPeriodRow.BudgetIntl;
                    }
                }
            }

            retVal = lv_currency_amount_n;

            if (NewTransaction)
            {
                DBAccess.GDBAccessObj.RollbackTransaction();
            }

            return retVal;
        }

        /// <summary>
        /// Exports budgets
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ACSVFileName"></param>
        /// <param name="AFdlgSeparator"></param>
        /// <param name="AFileContents"></param>
        /// <param name="AExportDS"></param>
        /// <param name="AVerificationResult"></param>
        /// <returns>Total number of records exported</returns>
        [RequireModulePermission("FINANCE-3")]
        public static int ExportBudgets(Int32 ALedgerNumber,
            string ACSVFileName,
            string[] AFdlgSeparator,
            ref string AFileContents,
            ref BudgetTDS AExportDS,
            out TVerificationResultCollection AVerificationResult)
        {
            AVerificationResult = null;

            if (AExportDS != null)
            {
                int retVal = ExportBudgetToCSV(ALedgerNumber,
                    ACSVFileName,
                    AFdlgSeparator,
                    ref AFileContents,
                    ref AExportDS,
                    ref AVerificationResult);
                return retVal;
            }

            return 0;
        }

        private static Int32 ExportBudgetToCSV(Int32 ALedgerNumber,
            string ACSVFileName,
            string[] AFdlgSeparator,
            ref string AFileContents,
            ref BudgetTDS AExportDS,
            ref TVerificationResultCollection AVerificationResult)
        {
            Int32 numBudgetsExported = 0;

            ALedgerRow lr = (ALedgerRow)AExportDS.ALedger.Rows[0];

            ABudgetPeriodTable budgetPeriod = (ABudgetPeriodTable)AExportDS.ABudgetPeriod;

            Int32 numPeriods = lr.NumberOfAccountingPeriods;

            char separator = AFdlgSeparator[0].Substring(0, 1).ToCharArray()[0];

            TLogging.Log("Writing file: " + ACSVFileName);

            StringBuilder sb = new StringBuilder();
            string budgetAmounts = string.Empty;

            foreach (ABudgetRow row in AExportDS.ABudget.Rows)
            {
                switch (row.BudgetTypeCode)
                {
                    case MFinanceConstants.BUDGET_SAME:
                        StringBudgetTypeSameAmounts(row.BudgetSequence, ref budgetPeriod, out budgetAmounts);

                        break;

                    case MFinanceConstants.BUDGET_SPLIT:
                        StringBudgetTypeSplitAmounts(row.BudgetSequence, numPeriods, ref budgetPeriod, separator, out budgetAmounts);

                        break;

                    case MFinanceConstants.BUDGET_INFLATE_BASE:
                        StringBudgetTypeInflateBaseAmounts(row.BudgetSequence, numPeriods, ref budgetPeriod, separator, out budgetAmounts);

                        break;

                    case MFinanceConstants.BUDGET_INFLATE_N:
                        StringBudgetTypeInflateNAmounts(row.BudgetSequence, numPeriods, ref budgetPeriod, separator, out budgetAmounts);

                        break;

                    default:                              //MFinanceConstants.BUDGET_ADHOC:
                        StringBudgetTypeAdhocAmounts(row.BudgetSequence, numPeriods, ref budgetPeriod, separator, out budgetAmounts);

                        break;
                }

                sb.Append(StringHelper.StrMerge(
                        new string[] {
                            Encase(row.CostCentreCode),
                            Encase(row.AccountCode),
                            Encase(row.BudgetTypeCode),
                            Encase(BudgetRevisionYearName(ALedgerNumber, row.Year))
                        }, separator));

                sb.Append(separator.ToString());
                sb.Append(budgetAmounts);
                sb.Append(Environment.NewLine);

                numBudgetsExported++;
            }

            AFileContents = sb.ToString();

            return numBudgetsExported;
        }

        private static string Encase(string AStringToEncase)
        {
            return "\"" + AStringToEncase + "\"";
        }

        private static void StringBudgetTypeSplitAmounts(int ABudgetSequence,
            int ANumPeriods,
            ref ABudgetPeriodTable ABudgetPeriod,
            char ASeparator,
            out String ASb)
        {
            ABudgetPeriodRow budgetPeriodRow;

            ASb = string.Empty;
            decimal perPeriodAmount = 0;
            decimal endPeriodAmount = 0;

            //Find periods 1-(total periods-1) amount
            budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence, 1 });

            if (budgetPeriodRow != null)
            {
                perPeriodAmount = budgetPeriodRow.BudgetBase;
                budgetPeriodRow = null;

                //Find period FNumberOfPeriods amount
                budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence,
                                                                                           ANumPeriods });

                if (budgetPeriodRow != null)
                {
                    endPeriodAmount = budgetPeriodRow.BudgetBase;
                }
            }

            //Calculate the total amount
            ASb += (perPeriodAmount * (ANumPeriods - 1) + endPeriodAmount).ToString();
        }

        private static void StringBudgetTypeAdhocAmounts(int ABudgetSequence,
            int ANumPeriods,
            ref ABudgetPeriodTable ABudgetPeriod,
            char ASeparator,
            out String ASb)
        {
            ABudgetPeriodRow budgetPeriodRow;

            ASb = string.Empty;

            for (int i = 1; i <= ANumPeriods; i++)
            {
                budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence, i });

                if (budgetPeriodRow != null)
                {
                    ASb += budgetPeriodRow.BudgetBase.ToString();

                    if (i < ANumPeriods)
                    {
                        ASb += ASeparator.ToString();
                    }
                }

                budgetPeriodRow = null;
            }
        }

        private static void StringBudgetTypeSameAmounts(int ABudgetSequence, ref ABudgetPeriodTable ABudgetPeriod, out String ASb)
        {
            ABudgetPeriodRow budgetPeriodRow;

            ASb = string.Empty;

            budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence, 1 });

            if (budgetPeriodRow != null)
            {
                ASb += budgetPeriodRow.BudgetBase.ToString();
            }
        }

        private static void StringBudgetTypeInflateBaseAmounts(int ABudgetSequence,
            int ANumPeriods,
            ref ABudgetPeriodTable ABudgetPeriod,
            char ASeparator,
            out String ASb)
        {
            ABudgetPeriodRow budgetPeriodRow;

            ASb = string.Empty;

            decimal priorPeriodAmount = 0;
            decimal currentPeriodAmount = 0;

            for (int i = 1; i <= ANumPeriods; i++)
            {
                budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence, i });

                if (budgetPeriodRow != null)
                {
                    currentPeriodAmount = budgetPeriodRow.BudgetBase;

                    if (i == 1)
                    {
                        ASb += currentPeriodAmount.ToString();
                    }
                    else
                    {
                        ASb += ((currentPeriodAmount - priorPeriodAmount) / priorPeriodAmount * 100).ToString();
                    }

                    if (i < ANumPeriods)
                    {
                        ASb += ASeparator.ToString();
                    }

                    priorPeriodAmount = currentPeriodAmount;
                }

                budgetPeriodRow = null;
            }
        }

        private static void StringBudgetTypeInflateNAmounts(int ABudgetSequence,
            int ANumPeriods,
            ref ABudgetPeriodTable ABudgetPeriod,
            char ASeparator,
            out String ASb)
        {
            ABudgetPeriodRow budgetPeriodRow;

            ASb = string.Empty;

            decimal firstPeriodAmount = 0;
            decimal currentPeriodAmount;

            for (int i = 1; i <= ANumPeriods; i++)
            {
                budgetPeriodRow = (ABudgetPeriodRow)ABudgetPeriod.Rows.Find(new object[] { ABudgetSequence, i });

                if (budgetPeriodRow != null)
                {
                    currentPeriodAmount = budgetPeriodRow.BudgetBase;

                    if (i == 1)
                    {
                        firstPeriodAmount = currentPeriodAmount;
                        ASb += currentPeriodAmount.ToString();
                        ASb += ASeparator.ToString();
                    }
                    else
                    {
                        if (currentPeriodAmount != firstPeriodAmount)
                        {
                            ASb += (i - 1).ToString();
                            ASb += ASeparator.ToString();
                            ASb += ((currentPeriodAmount - firstPeriodAmount) / firstPeriodAmount * 100).ToString();
                            break;
                        }
                        else if (i == ANumPeriods)     // and by implication CurrentPeriodAmount == FirstPeriodAmount
                        {
                            //This is an odd case that the user should never implement, but still needs to be covered.
                            //  It is equivalent to using BUDGET TYPE: SAME
                            ASb += "0";
                            ASb += ASeparator.ToString();
                            ASb += "0";
                        }
                    }
                }

                budgetPeriodRow = null;
            }
        }

        private static bool IsZero(decimal d)
        {
            return d == 0;
        }

        /// <summary>
        /// Validate Budget Type: Same
        /// </summary>
        /// <param name="APeriodValues"></param>
        /// <param name="ANumberOfPeriods"></param>
        /// <returns></returns>
        private static bool ValidateBudgetTypeSame(Int32 ANumberOfPeriods, decimal[] APeriodValues)
        {
            bool PeriodValuesOK = true;

            decimal Period1Amount = APeriodValues[0];

            for (int i = 1; i < ANumberOfPeriods; i++)
            {
                if (Period1Amount != APeriodValues[i])
                {
                    PeriodValuesOK = false;
                    break;
                }
            }

            return PeriodValuesOK;
        }

        /// <summary>
        /// Validate Budget Type: Split
        /// </summary>
        /// <param name="APeriodValues"></param>
        /// <param name="ANumberOfPeriods"></param>
        /// <returns></returns>
        private static bool ValidateBudgetTypeSplit(Int32 ANumberOfPeriods, decimal[] APeriodValues)
        {
            bool PeriodValuesOK = true;

            decimal Period1Amount = APeriodValues[0];

            for (int i = 1; i < (ANumberOfPeriods - 1); i++)
            {
                if (Period1Amount != APeriodValues[i])
                {
                    PeriodValuesOK = false;
                    break;
                }
            }

            if (PeriodValuesOK)
            {
                if ((APeriodValues[ANumberOfPeriods - 1] < Period1Amount)
                    || ((APeriodValues[ANumberOfPeriods - 1] - Period1Amount) >= ANumberOfPeriods))
                {
                    PeriodValuesOK = false;
                }
            }

            return PeriodValuesOK;
        }

        /// <summary>
        /// Validate Budget Type: Base
        /// </summary>
        /// <param name="APeriodValues"></param>
        /// <returns></returns>
        private static bool ValidateBudgetTypeInflateBase(decimal[] APeriodValues)
        {
            bool PeriodValuesOK = true;

            if (APeriodValues[0] == 0)
            {
                PeriodValuesOK = false;
            }

            return PeriodValuesOK;
        }

        /// <summary>
        /// Validate Budget Type: Inflate n
        /// </summary>
        /// <param name="APeriodValues"></param>
        /// <param name="ANumberOfPeriods"></param>
        /// <returns></returns>
        private static bool ValidateBudgetTypeInflateN(Int32 ANumberOfPeriods, decimal[] APeriodValues)
        {
            bool PeriodValuesOK = true;
            bool PeriodAmountHasChanged = false;

            decimal Period1Amount = APeriodValues[0];

            if (Period1Amount == 0)
            {
                PeriodValuesOK = false;
                return PeriodValuesOK;
            }

            for (int i = 1; i < ANumberOfPeriods; i++)
            {
                if (Period1Amount != APeriodValues[i])
                {
                    if (PeriodAmountHasChanged)
                    {
                        PeriodValuesOK = false;
                        break;
                    }
                    else
                    {
                        Period1Amount = APeriodValues[i];
                        PeriodAmountHasChanged = true;
                    }
                }
            }

            return PeriodValuesOK;
        }
    }
}