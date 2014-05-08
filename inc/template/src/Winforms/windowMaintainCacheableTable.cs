// auto generated with nant generateWinforms from {#XAMLSRCFILE} and template windowMaintainCachableTable
//
// DO NOT edit manually, DO NOT edit with the designer
//
{#GPLFILEHEADER}

#region Usings
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.Data;
using SourceGrid;
using Ict.Petra.Shared;
using System.Resources;
using System.Collections.Generic;
using System.Collections.Specialized;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Data;
using Ict.Common.Data.Exceptions;
using Ict.Common.Exceptions;
using Ict.Common.Remoting.Client;
using Ict.Common.Remoting.Shared;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.App.Gui;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Client.CommonControls;
{#IFDEF SHAREDVALIDATIONNAMESPACEMODULE}
using {#SHAREDVALIDATIONNAMESPACEMODULE};
{#ENDIF SHAREDVALIDATIONNAMESPACEMODULE}
{#USINGNAMESPACES}
#endregion

namespace {#NAMESPACE}
{

  /// auto generated: {#FORMTITLE}
  public partial class {#CLASSNAME}: System.Windows.Forms.Form, {#INTERFACENAME}
  {
#region Declarations
    private {#UTILOBJECTCLASS} FPetraUtilsObject;
{#FILTERVAR}
{#IFDEF SHOWDETAILS}       
    private DataColumn FPrimaryKeyColumn = null;
    private Control FPrimaryKeyControl = null;
    private Label FPrimaryKeyLabel = null;
    private string FDefaultDuplicateRecordHint = String.Empty;
{#ENDIF SHOWDETAILS}
{#IFDEF DATASETTYPE}
    private {#DATASETTYPE} FMainDS;
{#ENDIF DATASETTYPE}
{#IFNDEF DATASETTYPE}
    {#INLINETYPEDDATASET}
{#ENDIFN DATASETTYPE}
{#IFDEF FILTERANDFIND}
    {#FILTERANDFINDDECLARATIONS}
{#ENDIF FILTERANDFIND}
#endregion

#region Constructor and Initial Setup
    /// constructor
    public {#CLASSNAME}(Form AParentForm) : base()
    {
      Control[] FoundCheckBoxes;  
      
      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();
      #region CATALOGI18N

      // this code has been inserted by GenerateI18N, all changes in this region will be overwritten by GenerateI18N
      {#CATALOGI18N}
      #endregion
      
      {#ASSIGNFONTATTRIBUTES}
      
      FPetraUtilsObject = new {#UTILOBJECTCLASS}(AParentForm, this, stbMain);
{#IFDEF DATASETTYPE}
      FMainDS = new {#DATASETTYPE}();
{#ENDIF DATASETTYPE}
{#IFNDEF DATASETTYPE}
      FMainDS = new TLocalMainTDS();
{#ENDIFN DATASETTYPE}
      {#INITUSERCONTROLS}
      
      /*
       * Automatically disable 'Deletable' CheckBox (it must not get changed by the user because records where the 
       * 'Deletable' flag is true are system records that must not be deleted)
       */
      FoundCheckBoxes = this.Controls.Find("chkDetailDeletable", true);
      
      if (FoundCheckBoxes.Length > 0) 
      {
          FoundCheckBoxes[0].Enabled = false;
      }
      
      {#LOADDATAONCONSTRUCTORRUN}

{#IFDEF MASTERTABLE OR DETAILTABLE}
      BuildValidationControlsDict();
{#ENDIF MASTERTABLE OR DETAILTABLE}
{#IFDEF SHOWDETAILS}       
      SetPrimaryKeyControl();
{#ENDIF SHOWDETAILS}
{#IFDEF BUTTONPANEL}
      FinishButtonPanelSetup();
{#ENDIF BUTTONPANEL}
{#IFDEF FILTERANDFIND}
      SetupFilterAndFindControls();
{#ENDIF FILTERANDFIND}
      SelectRowInGrid(1);
    }

    /// <summary>Loads the data for the screen and finishes the setting up of the screen.</summary>
    /// <returns>void</returns>
    private void LoadDataAndFinishScreenSetup()
    {      
      Type DataTableType;
      
      // Load Data
      DataTable CacheDT = TDataCache.{#CACHEABLETABLERETRIEVEMETHOD}({#CACHEABLETABLE}, {#CACHEABLETABLESPECIFICFILTERLOAD}, out DataTableType);
      FMainDS.{#DETAILTABLE}.Merge(CacheDT);    
      
      FPetraUtilsObject.ActionEnablingEvent += ActionEnabledEvent;
      {#INITMANUALCODE}
{#IFDEF SAVEDETAILS}
      grdDetails.Enter += new EventHandler(grdDetails_Enter);
      grdDetails.Selection.FocusRowLeaving += new SourceGrid.RowCancelEventHandler(grdDetails_FocusRowLeaving);
      grdDetails.Selection.SelectionChanged += new RangeRegionChangedEventHandler(grdDetails_RowSelected);
      {#GRIDMULTISELECTION}
{#ENDIF SAVEDETAILS}
      
      DataView myDataView = FMainDS.{#DETAILTABLE}.DefaultView;
{#IFDEF GRIDSORT}
      myDataView.Sort = "{#GRIDSORT}";
{#ENDIF GRIDSORT}
{#IFDEF GRIDFILTER}
            myDataView.RowFilter = {#GRIDFILTER};
{#ENDIF GRIDFILTER}
      myDataView.AllowNew = false;
      grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(myDataView);

      {#INITACTIONSTATE}            
      {#DISPLAYFILTERINFORMTITLE}
{#IFDEF BUTTONPANEL}
      UpdateRecordNumberDisplay();
{#ENDIF BUTTONPANEL}
    }
#endregion

#region Show Method overrides

    /// <summary>
    /// Override of Form.Show(IWin32Window owner) Method. Caters for singleton Forms.
    /// </summary>
    /// <param name="owner">Any object that implements <see cref="IWin32Window" /> and represents the top-level window that will own this Form. </param>    
    public new void Show(IWin32Window owner)
    {
        Form OpenScreen = TFormsList.GFormsList[this.GetType().FullName];
        bool OpenSelf = true;

        if ((OpenScreen != null)
            && (OpenScreen.Modal != true))            
        {
            if (TFormsList.GSingletonForms.Contains(this.GetType().Name)) 
            {
//                MessageBox.Show("Activating singleton screen of Type '" + this.GetType().FullName + "'.");
                                   
                OpenSelf = false;
                this.Visible = false;   // needed as this.Close() would otherwise bring this Form to the foreground and OpenScreen.BringToFront() would not help...
                this.Close();
                
                OpenScreen.BringToFront();
            }            
        }
        
        if (OpenSelf) 
        {
            if (owner != null) 
            {
                base.Show(owner);    
            }
            else
            {
                base.Show();
            }            
        }        
    }

    /// <summary>
    /// Override of Form.Show() Method. Caters for singleton Forms.
    /// </summary>        
    public new void Show()
    {
        this.Show(null);
    }

#endregion

#region Event Handlers
    {#EVENTHANDLERSIMPLEMENTATION}
    
    private void TFrmPetra_Closed(object sender, EventArgs e)
    {

    }
#endregion

#region CreateNewRecord
    /// <summary>
    /// This automatically generated method creates a new record of {#DETAILTABLE}, highlights it in the grid
    /// and displays it on the edit screen.  We create the table locally, no dataset
    /// </summary>
    /// <returns>True if the existing Details data was validated successfully and the new row was added.</returns>
    public bool CreateNew{#DETAILTABLE}()
    {
        if(ValidateAllData(true, true))
        {
            {#DETAILTABLE}Row NewRow = FMainDS.{#DETAILTABLE}.NewRowTyped();
            {#INITNEWROWMANUAL}
            FMainDS.{#DETAILTABLE}.Rows.Add(NewRow);
            
            FPetraUtilsObject.SetChangedFlag();
{#IFDEF FILTERANDFIND}

            if (!SelectDetailRowByDataTableIndex(FMainDS.{#DETAILTABLE}.Rows.Count - 1))
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

                    SelectDetailRowByDataTableIndex(FMainDS.{#DETAILTABLE}.Rows.Count - 1);
                }
            }
{#ENDIF FILTERANDFIND}
{#IFNDEF FILTERANDFIND}
            SelectDetailRowByDataTableIndex(FMainDS.{#DETAILTABLE}.Rows.Count - 1);
{#ENDIFN FILTERANDFIND}
            
            Control[] pnl = this.Controls.Find("pnlDetails", true);
            if (pnl.Length > 0)
            {
                // Build up a list of controls on this panel that is sorted in true nested tab order
                SortedList<string, Control> controlsSortedByTabOrder = new SortedList<string, Control>();
                GetSortedControlList(ref controlsSortedByTabOrder, pnl[0], String.Empty);

                if (controlsSortedByTabOrder.Count > 0)
                {
                    foreach (Control c in controlsSortedByTabOrder.Values)
                    {
                        if (c is TextBox && c.Name.Contains("Desc"))
                        {
                            // Set the default text for the first TextBox whose name contains 'Desc'
                            c.Text = MCommonResourcestrings.StrPleaseEnterDescription;
                            ValidateAllData(true, false);
                            break;
                        }
                    }

                    // Focus the first control in our sorted list
                    controlsSortedByTabOrder.Values[0].Focus();
                }
            }
    
{#IFDEF BUTTONPANEL}
            UpdateRecordNumberDisplay();
{#ENDIF BUTTONPANEL}

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a list of relevant controls, sorted correctly by multi-level TabIndex in exactly the same way that the system uses to determine screen tab order.
    /// This includes nested controls so the tab order is, say, 100, 110, 120.30, 120.40, 130
    /// The method calls itself recursively for each contained panel
    /// </summary>
    /// <param name="ASortedControlList">Pass in the SortedList that will be the final result</param>
    /// <param name="AContainerControl">The contianer control (a Panel) that is to be searched for controls and sub-Panels</param>
    /// <param name="APrefix">A TabIndex prefix - use an empty string for the initial call</param>
    private void GetSortedControlList(ref SortedList<string, Control> ASortedControlList, Control AContainerControl, string APrefix)
    {
        foreach (Control control in AContainerControl.Controls)
        {
            if (control is Panel)
            {
                // Recursive call with an extended prefix
                GetSortedControlList(ref ASortedControlList, control, String.Format("{0}{1}.", APrefix, control.TabIndex.ToString("0000")));
            }
            else if (control is TextBox || control is ComboBox || control is TCmbAutoPopulated)
            {
                ASortedControlList.Add(String.Format("{0}{1}", APrefix, control.TabIndex.ToString("0000")), control);
            }
        }
    }
#endregion

#region Grid Row Selection and Discovery
    /// <summary>
    /// Selects the specified grid row and shows the details for the row in the details panel.
    /// The call still works even if the grid is empty (in which case no row is highlighted).
    /// Grid rows holding data are numbered 1..DataRowCount.
    /// If the specified grid row is less than 1, the first row is highlighted.
    /// If the specified grid row is greater than DataRowCount, the last row is highlighted.
    /// The details panel is disabled if the grid is empty or in Detail Protect Mode
    ///    otherwise the details are shown for the row that has been highlighted.
    /// </summary>
    /// <param name="ARowIndex">The row index to select.  Data rows start at 1</param>
    private void SelectRowInGrid(int ARowIndex)
    {
        grdDetails.SelectRowInGrid(ARowIndex, true);
    }

    /// <summary>
    /// Selects a grid row based its index in the data table (often the last, newest, row).
    /// The details panel is automatically updated to show the new details.
    /// If the grid is not displaying the specified data row, the first row will be selected, if it exists.
    /// </summary>
    /// <param name="ARowNumberInTable">Table row number (0-based)</param>
    /// <returns>True if the record is displayed in the grid, False otherwise</returns>
    private bool SelectDetailRowByDataTableIndex(Int32 ARowNumberInTable)
    {
        Int32 RowNumberGrid = -1;
        for (int Counter = 0; Counter < grdDetails.DataSource.Count; Counter++)
        {
            bool found = true;
            foreach (DataColumn myColumn in FMainDS.{#DETAILTABLE}.PrimaryKey)
            {
                string value1 = FMainDS.{#DETAILTABLE}.Rows[ARowNumberInTable][myColumn].ToString();
                string value2 = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).DataView[Counter][myColumn.Ordinal].ToString();
                if (value1 != value2)
                {
                    found = false;
                }
            }
            if (found)
            {
                RowNumberGrid = Counter + 1;
                break;
            }
        }

        SelectRowInGrid(RowNumberGrid);

        return RowNumberGrid >= 0;
    }

    /// <summary>
    /// Finds the grid row in the data table
    /// </summary>
    /// <returns>The data table row index for the data in the current grid row, or -1 if the row was not found</returns>
    private int GetDetailGridRowDataTableIndex()
    {
        Int32 RowNumberInData = -1;
        
        if (FPrevRowChangedRow > 0 && FPreviouslySelectedDetailRow != null)
        {
            int dataRowIndex = 0;
            
            foreach ({#DETAILTABLETYPE}Row myRow in FMainDS.{#DETAILTABLETYPE}.Rows)
            {
                bool found = true;
                foreach (DataColumn myColumn in FMainDS.{#DETAILTABLETYPE}.PrimaryKey)
                {
                    if (myRow.RowState != DataRowState.Deleted)
                    {
                        string value1 = myRow[myColumn].ToString();
                        string value2 = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).DataView[FPrevRowChangedRow - 1][myColumn.Ordinal].ToString();
                        if (value1 != value2)
                        {
                            found = false;
                        }
                    }
                    else
                    {
                        found = false;
                    }
                }
                
                if (found)
                {
                    RowNumberInData = dataRowIndex;
                    break;
                }
                
                dataRowIndex++;
            }
        }
        
        return RowNumberInData;
    }
    
{#IFDEF SHOWDETAILS OR GENERATEGETSELECTEDDETAILROW}

    /// <summary>
    /// Gets the selected Data Row as a {#DETAILTABLETYPE} record from the grid
    /// </summary>
    /// <returns>The selected row - or null if no row is selected</returns>
    public {#DETAILTABLETYPE}Row GetSelectedDetailRow()
    {
        return FPreviouslySelectedDetailRow;
    }

    /// <summary>
    /// Gets the selected Data Row index in the grid.  The first data row is 1.
    /// </summary>
    /// <returns>The selected row - or -1 if no row is selected</returns>
    public Int32 GetSelectedRowIndex()
    {
        return FPrevRowChangedRow;
    }
{#ENDIF SHOWDETAILS OR GENERATEGETSELECTEDDETAILROW}
#endregion

#region Show and Undo Data
    private void SetPrimaryKeyReadOnly(bool AReadOnly)
    {
        {#PRIMARYKEYCONTROLSREADONLY}
    }

{#IFDEF SHOWDATA}
    private void ShowData({#MASTERTABLE}Row ARow)
    {
        {#SHOWDATA}
    }
{#ENDIF SHOWDATA}
{#IFDEF UNDODATA}

    private void UndoData(DataRow ARow, Control AControl)
    {
        {#UNDODATA}
    }
{#ENDIF UNDODATA}
#endregion

#region Data Validation
    /// <summary>
    /// Performs data validation.
    /// </summary>
    /// <param name="ARecordChangeVerification">Set to true if the data validation happens when the user is changing 
    /// to another record, otherwise set it to false.</param>
    /// <param name="AProcessAnyDataValidationErrors">Set to true if data validation errors should be shown to the
    /// user, otherwise set it to false.</param>
    /// <returns>True if data validation succeeded or if there is no current row, otherwise false.</returns>    
    private bool ValidateAllData(bool ARecordChangeVerification, bool AProcessAnyDataValidationErrors)
    {
        bool ReturnValue = false;
        // Record a new Data Validation Run. (All TVerificationResults/TScreenVerificationResults that are created during this 'run' are associated with this 'run' through that.)
        FPetraUtilsObject.VerificationResultCollection.RecordNewDataValidationRun();

{#IFNDEF SHOWDETAILS}
{#IFDEF MASTERTABLE}
// :WMCT:GetDataFromControls
        GetDataFromControls(FMainDS.{#MASTERTABLE}[0]);
        ValidateData(FMainDS.{#MASTERTABLE}[0]);
{#IFDEF VALIDATEDATAMANUAL}
// :WMCT:ValidateDataManual
        ValidateDataManual(FMainDS.{#MASTERTABLE}[0]);
{#ENDIF VALIDATEDATAMANUAL}
{#ENDIF MASTERTABLE}
{#IFDEF PERFORMUSERCONTROLVALIDATION}

        // Perform validation in UserControls too
// :WMCT:ucValidation
        {#USERCONTROLVALIDATION}

{#ENDIF PERFORMUSERCONTROLVALIDATION}

        if (AProcessAnyDataValidationErrors)
        {
            ReturnValue = TDataValidation.ProcessAnyDataValidationErrors(ARecordChangeVerification, FPetraUtilsObject.VerificationResultCollection,
                this.GetType(), null);
        }
{#ENDIFN SHOWDETAILS}
{#IFDEF SHOWDETAILS}       
        if (FPreviouslySelectedDetailRow != null)
        {
            bool bGotConstraintException = false;
            int prevRowBeforeValidation = FPrevRowChangedRow;
// :WMCT:GetDetailsFromControls
            try
            {
                GetDetailsFromControls(FPreviouslySelectedDetailRow);
                ValidateDataDetails(FPreviouslySelectedDetailRow);
{#IFDEF VALIDATEDATADETAILSMANUAL}
// :WMCT:ValidateDataDetailsManual
                ValidateDataDetailsManual(FPreviouslySelectedDetailRow);
{#ENDIF VALIDATEDATADETAILSMANUAL}
            }
            catch (ConstraintException)
            {
                bGotConstraintException = true;
            }

            // Duplicate record validation
            if (FPrimaryKeyColumn == null)
            {
                // If controls have been named according to the column names, it should be impossible to get a constraint exception 
                //    without us knowing which is the 'prime' primary key column and control
                // But this is our ultimate fallback position.  This creates an exception message that simply lists all the primary key fields in a friendly format
                FPetraUtilsObject.VerificationResultCollection.AddOrRemove(
                    bGotConstraintException ? new TScreenVerificationResult(this, null,
                    String.Format(MCommonResourcestrings.StrDuplicateRecordNotAllowed, FDefaultDuplicateRecordHint),
                    CommonErrorCodes.ERR_DUPLICATE_RECORD, null, TResultSeverity.Resv_Critical) : null, null);
            }
            else
            {
                TControlExtensions.ValidateNonDuplicateRecord(this, bGotConstraintException, FPetraUtilsObject.VerificationResultCollection, 
                            FPrimaryKeyColumn, FPrimaryKeyControl, FMainDS.{#DETAILTABLE}.PrimaryKey);
            }

            // Validation might have moved the row, so we need to locate it again
            // If it has moved we will call the special grid-sorting method SelectRowAfterSort to highlight the new row.
            // This will give rise to a Selection_SelectionChanged event with a new ActivePosition but the grdDetails.Sorting property will be True
            // Furthermore with Filter/Find we need to be sure that we have not lost the row altogether by using a different row filter
            //  and we also need to protect against recursively call SelectRowAfterSort
            int newRowAfterValidation = grdDetails.DataSourceRowToIndex2(FPreviouslySelectedDetailRow, prevRowBeforeValidation - 1) + 1;
            if (newRowAfterValidation == 0)
            {
{#IFDEF FILTERANDFIND}
                // The row is no longer in the view - probably because the new data values are being filtered out
                if ((FCurrentActiveFilter != FFilterPanelControls.BaseFilter) || !FFilterPanelControls.BaseFilterShowsAllRecords)
                {
                    // Remember the control with the focus
                    Control c = FPetraUtilsObject.GetFocusedControl(pnlDetails);

                    // Clear the filters without selecting a new row
                    FClearingDiscretionaryFilters = true;
                    FFilterPanelControls.ClearAllDiscretionaryFilters();

                    if (FucoFilterAndFind.ShowApplyFilterButton != TUcoFilterAndFind.FilterContext.None)
                    {
                        ApplyFilter();
                    }

                    FClearingDiscretionaryFilters = false;

                    // Now find the row again - this time we should succeed.  If not some other piece of code will have to do the selection
                    newRowAfterValidation = grdDetails.DataSourceRowToIndex2(FPreviouslySelectedDetailRow, prevRowBeforeValidation - 1) + 1;

                    if (newRowAfterValidation > 0)
                    {
                        // Select the row
                        grdDetails.SelectRowAfterSort(newRowAfterValidation);
                    
                        // Put the focus back again on the editable control
                        if (c != null)
                        {
                            c.Focus();
                        }
                        
                        // Tell the user what we have done
                        MessageBox.Show(MCommonResourcestrings.StrEditedRecordIsFiltered, MCommonResourcestrings.StrEditRecordTitle, 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
{#ENDIF FILTERANDFIND}
{#IFNDEF FILTERANDFIND}
                // There is no Filter/Find on this screen so we should never take this code path
{#ENDIFN FILTERANDFIND}
            }
            else
            {
                if ((newRowAfterValidation != prevRowBeforeValidation) && !grdDetails.Sorting)
                {
                    grdDetails.SelectRowAfterSort(newRowAfterValidation);
                    //Console.WriteLine("{0}:    Validation: validated row moved to {1}.", DateTime.Now.Millisecond, newRowAfterValidation);
                }
            }
{#IFDEF PERFORMUSERCONTROLVALIDATION}

            // Perform validation in UserControls too
// :WMCT:ucValidation
            {#USERCONTROLVALIDATION}
{#ENDIF PERFORMUSERCONTROLVALIDATION}

            if (AProcessAnyDataValidationErrors)
            {
                ReturnValue = TDataValidation.ProcessAnyDataValidationErrors(ARecordChangeVerification, FPetraUtilsObject.VerificationResultCollection,
                    this.GetType(), null);
            }
        }
        else
        {
            ReturnValue = true;
        }
{#ENDIF SHOWDETAILS}

        if(ReturnValue)
        {
            // Remove a possibly shown Validation ToolTip as the data validation succeeded
            FPetraUtilsObject.ValidationToolTip.RemoveAll();
        }

        return ReturnValue;
    }
#endregion
{#IFDEF SHOWDETAILS}

#region Show Details, Grid Events and Deletion

    #region ShowDetails
    /// <summary>
    /// Use this override to Show the Details for a specified row in the grid.
    /// This override is safe to use in  manual code because it will update the FPreviouslySelectedDetailRow and FPrevRowChangedRow internal variables.
    /// </summary>
    /// <param name="ARowNumberInGrid">The grid row.  Valid rows start at 1.</param>
    private void ShowDetails(Int32 ARowNumberInGrid)
    {
        FPreviouslySelectedDetailRow = null;
        int GridRowCount = grdDetails.Rows.Count;

        if (ARowNumberInGrid >= GridRowCount)
        {
            ARowNumberInGrid = GridRowCount - 1;
        }

        if ((ARowNumberInGrid < 1) && (GridRowCount > 1))
        {
            ARowNumberInGrid = 1;
        }

        if (ARowNumberInGrid > 0)
        {
            DataRowView rowView = (DataRowView)grdDetails.Rows.IndexToDataSourceRow(ARowNumberInGrid);

            if (rowView != null)
            {
                FPreviouslySelectedDetailRow = ({#DETAILTABLE}Row)(rowView.Row);
            }

            FPrevRowChangedRow = ARowNumberInGrid;
        }
        else
        {
            FPrevRowChangedRow = -1;
        }

        ShowDetails(FPreviouslySelectedDetailRow);
    }

    /// <summary>
    /// This overload shows the details for the currently selected row.
    /// The method still works when the grid is empty and no row can be selected.
    /// This method is safe to use in manual code although it should be rarely necessary because the standard 
    /// code should always be showing the correct details.
    /// The Details panel is disabled when the grid is empty, or when in Detail Protected Mode
    /// The variable FPreviouslySelectedDetailRow is set by this call.
    /// </summary>
    private void ShowDetails()
    {
        ShowDetails(FPrevRowChangedRow);
    }

    /// <summary>
    /// This overload shows the details for the specified row, which can be null.
    /// The Details panel is disabled when the row is Null, or when in Detail Protected Mode
    /// IMPORTANT: Do not call this method from manual code because the internal variables will no longer match.
    /// </summary>
    /// <param name="ARow">The row for which details will be shown</param>
    private void ShowDetails({#DETAILTABLE}Row ARow)
    {
        FPetraUtilsObject.DisableDataChangedEvent();

        if (ARow == null)
        {
            pnlDetails.Enabled = false;
            {#CLEARDETAILS}
        }
        else
        {
            pnlDetails.Enabled = !FPetraUtilsObject.DetailProtectedMode;
            {#SHOWDETAILS}
        }
        
        {#ENABLEDELETEBUTTON}FPetraUtilsObject.EnableDataChangedEvent();
    }
{#CANDELETESELECTION}
    #endregion

{#IFDEF SAVEDETAILS}
    
    #region Grid events
    /// <summary>
    /// A reference to the Typed Data Row object from the grid whose Details are currently displayed.
    /// It is automatically updated when you call ShowDetails()
    /// You can use this variable in your manual code to access individual details, but you should take care
    ///   if you need to assign a different row object to it.  Try, if possible, to use 
    ///   SelectRowInGrid(N, true)
    /// or
    ///   ShowDetails(NewRow)
    /// so that the reference to the row object is updated automatically.
    /// </summary>
    private {#DETAILTABLE}Row FPreviouslySelectedDetailRow = null;
    
    /// <summary>
    /// This variable may become obsolete in future.  It used to hold the most recent row passed as the parameter to the FocusedRowChanged event.
    /// However we no longer use this event.  If you want to know the current row index use GetSelectedRowIndex() instead.
    /// </summary>
    private int FPrevRowChangedRow = -1;

    /// <summary>
    /// Fired when the user tabs to, or clicks in, the grid
    /// </summary>
    private void grdDetails_Enter(object sender, EventArgs e)
    {
        if (FPetraUtilsObject.VerificationResultCollection.Count > 0)
        {
            // No need to show the cell if there are no errors.  This allows the user to have scrolled the view-port away from the selected row and keep it there.
            grdDetails.ShowCell(FPrevRowChangedRow);
        }
    }

    /// <summary>
    /// This is the main event handler for changes in the grid selection
    /// </summary>
    private void grdDetails_RowSelected(object sender, RangeRegionChangedEventArgs e)
    {
        int gridRow = grdDetails.Selection.ActivePosition.Row;
        if (grdDetails.Sorting)
        {
            // No need to ShowDetails - just update our (obsolete) variable
            FPrevRowChangedRow = gridRow;
        }
        else
        {
            if ((gridRow == FPrevRowChangedRow) && grdDetails.IsMouseDown)
            {
                // This deals with the special case where the user edits a detail and then clicks on the same row in the grid
                // When this happens we do not get a control validated event for the control that we leave
                // We just want to treat this the same as tabbing round the controls - show any validation tooltips but do not stop the user leaving the row.
                ValidateAllData(true, false);
            }
            else
            {
                // This deals with all other cases - including the special case where, on a sorted grid, the row may be the same but contains different details
                //  because this event fires from SelectRowInGrid, even when the selected row hasn't changed but the data has.
                ShowDetails(gridRow);
                //Console.WriteLine("{0}: RowSelected: ShowDetails() for row {1}", DateTime.Now.Millisecond, gridRow);
            }
        }
    }

    /// <summary>
    /// FocusedRowLeaving is called when the user (or code) requests a change to the selected row.
    /// </summary>
    private void grdDetails_FocusRowLeaving(object sender, SourceGrid.RowCancelEventArgs e)
    {        
        //Console.WriteLine("{0}: FocusRowLeaving: from {1} to {2}", DateTime.Now.Millisecond, e.Row, e.ProposedRow);
        if (!ValidateAllData(true, true))
        {
            e.Cancel = true;
        }
    }
    #endregion
{#ENDIF SAVEDETAILS}

    #region Deletion
{#DELETERECORD}
    /// <summary>
    /// Standard method to delete the Data Row whose Details are currently displayed.
    /// There is full support for multi-row deletion.
    /// Optional manual code can be included to take action prior, during or after each deletion.
    /// When the row(s) have been deleted the highlighted row index stays the same unless the deleted row was the last one.
    /// The Details for the newly highlighted row are automatically displayed - or not, if the grid has now become empty.
    /// </summary>
    private void Delete{#DETAILTABLE}()
    {
        string CompletionMessage = String.Empty;
        
        if ((FPreviouslySelectedDetailRow == null) || (FPrevRowChangedRow == -1))
        {
            return;
        }

        DataRowView[] HighlightedRows = grdDetails.SelectedDataRowsAsDataRowView;

        if (HighlightedRows.Length == 1)
        {
{#IFDEF DELETEREFERENCECOUNT}
            TVerificationResultCollection VerificationResults = null;

            if (TVerificationHelper.IsNullOrOnlyNonCritical(FPetraUtilsObject.VerificationResultCollection))
            {
                int RefCountLimit = FPetraUtilsObject.MaxReferenceCountOnDelete;
                {#DELETEREFERENCECOUNT}
            }

            if ((VerificationResults != null)
                && (VerificationResults.Count > 0))
            {
                TCascadingReferenceCountHandler countHandler = new TCascadingReferenceCountHandler();
                TFrmExtendedMessageBox.TResult result = countHandler.HandleReferences(FPetraUtilsObject, VerificationResults, true);
                if (result == TFrmExtendedMessageBox.TResult.embrYes)
                {
                    // repeat the count but with no limit to the number of references
                    int RefCountLimit = 0;
                    {#DELETEREFERENCECOUNT}

                    countHandler.HandleReferences(FPetraUtilsObject, VerificationResults, false);
                }

                return;
            }
{#ENDIF DELETEREFERENCECOUNT}

            string DeletionQuestion = MCommonResourcestrings.StrDefaultDeletionQuestion;
            if ((FPrimaryKeyControl != null) && (FPrimaryKeyLabel != null))
            {
                DeletionQuestion += String.Format("{0}{0}({1} {2})",
                    Environment.NewLine,
                    FPrimaryKeyLabel.Text.Replace("&", ""),
                    TControlExtensions.GetDisplayTextForControl(FPrimaryKeyControl));
            }

            bool AllowDeletion = true;
            bool DeletionPerformed = false;

            {#PREDELETEMANUAL}
            if(AllowDeletion)
            {
                if ((MessageBox.Show(DeletionQuestion,
                         MCommonResourcestrings.StrConfirmDeleteTitle,
                         MessageBoxButtons.YesNo,
                         MessageBoxIcon.Question,
                         MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes))
                {
{#IFDEF DELETEROWMANUAL}
                    try
                    {
                        {#DELETEROWMANUAL}
{#ENDIF DELETEROWMANUAL}
{#IFNDEF DELETEROWMANUAL}               
                    try
                    {
                        FPreviouslySelectedDetailRow.Delete();
                        DeletionPerformed = true;
{#ENDIFN DELETEROWMANUAL}               
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format(MCommonResourcestrings.StrErrorWhileDeleting,
                            Environment.NewLine, ex.Message),
                            MCommonResourcestrings.StrGenericError,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
            
                    if (DeletionPerformed)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                    }
            
                    // Select and display the details of the nearest row to the one previously selected
                    SelectRowInGrid(FPrevRowChangedRow);
                    // Clear any errors left over from  the deleted row
                    FPetraUtilsObject.VerificationResultCollection.Clear();
{#IFDEF BUTTONPANEL}
                    UpdateRecordNumberDisplay();
{#ENDIF BUTTONPANEL}
                }
            }

{#IFDEF POSTDELETEMANUAL}
            {#POSTDELETEMANUAL}
{#ENDIF POSTDELETEMANUAL}
{#IFNDEF POSTDELETEMANUAL}
            if(DeletionPerformed && CompletionMessage.Length > 0)
            {
                MessageBox.Show(CompletionMessage, MCommonResourcestrings.StrDeletionCompletedTitle);
            }
{#ENDIFN POSTDELETEMANUAL}
        }
        else
        {
            string DeletionQuestion = String.Format(MCommonResourcestrings.StrMultiRowDeletionQuestion, HighlightedRows.Length, Environment.NewLine);
            DeletionQuestion += MCommonResourcestrings.StrMultiRowDeletionCheck;
            if (MessageBox.Show(DeletionQuestion,
                    MCommonResourcestrings.StrConfirmDeleteTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.Yes)
            {
                int recordsDeleted = 0;
                int recordsUndeletable = 0;
                int recordsDeleteDisallowed = 0;
                List<TMultiDeleteResult> listConflicts = new List<TMultiDeleteResult>();
                List<TMultiDeleteResult> listExceptions = new List<TMultiDeleteResult>();

                this.Cursor = Cursors.WaitCursor;

                foreach (DataRowView drv in HighlightedRows)
                {
                    {#DETAILTABLE}Row rowToDelete = ({#DETAILTABLE}Row)(drv.Row);
                    string rowDetails = MakePKValuesString(rowToDelete);

                    {#MULTIDELETEDELETABLE}

{#IFDEF DELETEREFERENCECOUNT}
                    TVerificationResultCollection VerificationResults = null;
                    int RefCountLimit = FPetraUtilsObject.MaxReferenceCountOnDelete;
                    {#DELETEREFERENCECOUNT}

                    if ((VerificationResults != null) && (VerificationResults.Count > 0))
                    {
                        TMultiDeleteResult result = new TMultiDeleteResult(rowDetails,
                            Messages.BuildMessageFromVerificationResult(String.Empty, VerificationResults));
                        listConflicts.Add(result);
                        continue;
                    }
{#ENDIF DELETEREFERENCECOUNT}

                    bool AllowDeletion = true;
                    bool DeletionPerformed = false;

                    {#PREMULTIDELETEMANUAL}
{#IFDEF DELETEROWMANUAL}
                    if (!AllowDeletion)
                    {
                        recordsDeleteDisallowed++;
                    }

                    try
                    {
                        {#DELETEMULTIROWMANUAL}
                    }
                    catch (Exception ex)
                    {
                        TMultiDeleteResult result = new TMultiDeleteResult(rowDetails, ex.Message);
                        listExceptions.Add(result);
                    }
{#ENDIF DELETEROWMANUAL}
{#IFNDEF DELETEROWMANUAL}               

                    if (AllowDeletion)
                    {
                        try
                        {
                            rowToDelete.Delete();
                            DeletionPerformed = true;
                        }
                        catch (Exception ex)
                        {
                            TMultiDeleteResult result = new TMultiDeleteResult(rowDetails, ex.Message);
                            listExceptions.Add(result);
                        }
                    }
                    else
                    {
                        recordsDeleteDisallowed++;
                    }
{#ENDIFN DELETEROWMANUAL}               

                    if (DeletionPerformed)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                        recordsDeleted++;
                    }

                    {#POSTMULTIDELETEMANUAL}
                }

                this.Cursor = Cursors.Default;
                SelectRowInGrid(FPrevRowChangedRow);
{#IFDEF BUTTONPANEL}
                UpdateRecordNumberDisplay();
{#ENDIF BUTTONPANEL}

                if (recordsDeleted > 0 && CompletionMessage.Length > 0)
                {
                    MessageBox.Show(CompletionMessage, MCommonResourcestrings.StrDeletionCompletedTitle);
                }

                //  Show the results of the multi-deletion
                string results = null;
                
                if (recordsDeleted > 0)
                {
                    results = String.Format(
                            Catalog.GetPluralString(MCommonResourcestrings.StrRecordSuccessfullyDeleted, MCommonResourcestrings.StrRecordsSuccessfullyDeleted, recordsDeleted),
                            recordsDeleted);
                }
                else
                {
                    results = MCommonResourcestrings.StrNoRecordsWereDeleted;
                }
                
                if (recordsUndeletable > 0)
                {
                    results += String.Format(
                        Catalog.GetPluralString(MCommonResourcestrings.StrRowNotDeletedBecauseNonDeletable,
                                                MCommonResourcestrings.StrRowsNotDeletedBecauseNonDeletable,
                                                recordsUndeletable),
                        Environment.NewLine,
                        recordsUndeletable);
                }

                if (recordsDeleteDisallowed > 0)
                {
                    results += String.Format(
                        Catalog.GetPluralString(MCommonResourcestrings.StrRowNotDeletedBecauseDeleteNotAllowed,
                                                MCommonResourcestrings.StrRowsNotDeletedBecauseDeleteNotAllowed,
                                                recordsDeleteDisallowed),
                        Environment.NewLine,
                        recordsDeleteDisallowed);
                }

                bool showCancel = false;
                
                if (listConflicts.Count > 0)
                {
                    showCancel = true;
                    results += String.Format(
                        Catalog.GetPluralString(MCommonResourcestrings.StrRowNotDeletedBecauseReferencedElsewhere,
                                                MCommonResourcestrings.StrRowsNotDeletedBecauseReferencedElsewhere,
                                                listConflicts.Count),
                        Environment.NewLine,
                        listConflicts.Count);
                }
                
                if (listExceptions.Count > 0)
                {
                    showCancel = true;
                    results += String.Format(
                        Catalog.GetPluralString(MCommonResourcestrings.StrRowNotDeletedDueToUnexpectedException,
                                                MCommonResourcestrings.StrRowNotDeletedDueToUnexpectedException,
                                                listExceptions.Count),
                        Environment.NewLine,
                        listExceptions.Count);
                }
                
                if (showCancel)
                {
                    results += String.Format(MCommonResourcestrings.StrClickToReviewDeletionOrCancel, Environment.NewLine);

                    if (MessageBox.Show(results,
                            MCommonResourcestrings.StrDeleteActionSummaryTitle,
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                    {
                        ReviewMultiDeleteResults(listConflicts, MCommonResourcestrings.StrRowsReferencedByOtherTables);
                        ReviewMultiDeleteResults(listExceptions, MCommonResourcestrings.StrExceptions);
                    }
                }
                else
                {
                    MessageBox.Show(results,
                        MCommonResourcestrings.StrDeleteActionSummaryTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }
    }

    private string MakePKValuesString({#DETAILTABLE}Row ARow)
    {
        string ReturnValue = String.Empty;
        object[] items = DataUtilities.GetPKValuesFromDataRow(ARow);

        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0)
            {
                ReturnValue += ", ";
            }

            ReturnValue += items[i].ToString();
        }

        return ReturnValue;
    }

    private class TMultiDeleteResult
    {
        private string FRecordID;
        private string FResult;

        public TMultiDeleteResult(string ARecordID, string AResult)
        {
            FRecordID = ARecordID;
            FResult = AResult;
        }

        public string RecordID
        {
            get
            {
                return FRecordID;
            }
        }

        public string Result
        {
            get
            {
                return FResult;
            }
        }

    }

    private void ReviewMultiDeleteResults(List<TMultiDeleteResult> AList, string ATitle)
    {
        int allItemsCount = AList.Count;
        int item = 0;

        foreach (TMultiDeleteResult result in AList)
        {
            item++;
            string s1 = result.RecordID;
            string s2 = result.Result;

            string details = String.Format(MCommonResourcestrings.StrItemXofYRecordColon,
                ATitle, item, allItemsCount, Environment.NewLine, s1, s2);

            if (item < allItemsCount)
            {
                details += String.Format(MCommonResourcestrings.StrViewNextDetailOrCancel, Environment.NewLine);
                if (MessageBox.Show(details, MCommonResourcestrings.StrMoreDetailsAboutRowsNotDeleted, MessageBoxButtons.OKCancel)
                    == System.Windows.Forms.DialogResult.Cancel)
                {
                    break;
                }
            }
            else
            {
                MessageBox.Show(details, MCommonResourcestrings.StrMoreDetailsAboutRowsNotDeleted, MessageBoxButtons.OK);
            }
        }
    }
#endregion
#endregion
{#ENDIF SHOWDETAILS}

#region Get and handle Data
{#IFDEF MASTERTABLE}
    /// This method may throw an exception at ARow.EndEdit()
    private void GetDataFromControls({#MASTERTABLETYPE}Row ARow, Control AControl=null)
    {
{#IFDEF SAVEDATA}
        if (ARow == null) return;

        object[] beforeEdit = ARow.ItemArray;
        ARow.BeginEdit();
        {#SAVEDATA}
        {#SAVEDATAEXTRA}
        if (Ict.Common.Data.DataUtilities.HaveDataRowsIdenticalValues(beforeEdit, ARow.ItemArray))
        {
            ARow.CancelEdit();
        }
        else
        {
            ARow.EndEdit();
        }
{#ENDIF SAVEDATA}
    }
{#ENDIF MASTERTABLE}
{#IFNDEF MASTERTABLE}

    private void GetDataFromControls(Control AControl=null)
    {
{#IFDEF SAVEDATA}
        {#SAVEDATA}
        {#SAVEDATAEXTRA}
{#ENDIF SAVEDATA}
    }
{#ENDIFN MASTERTABLE}    
{#IFDEF SAVEDETAILS}
    /// <summary>
    /// Do not call this method in your manual code.
    /// This is a method that is private to the generated code and is part of the Validation process.
    /// If you need to update the controls data into the Data Row object, you must use ValidateAllData and be prepared 
    ///   to handle the consequences of a failed validation.
    /// </summary>
    /// <param name="ARow">Do not use</param>
    /// <param name="AIsNewRow">Do not use</param>
    /// <param name="AControl">Do not use</param>
    private void GetDetailsFromControls({#DETAILTABLE}Row ARow, bool AIsNewRow = false, Control AControl=null)
    {
        if (ARow != null && !grdDetails.Sorting)
        {
            if (AIsNewRow)
            {
                {#SAVEDETAILS}
                {#SAVEDETAILSEXTRA}
            }
            else
            {
                object[] beforeEdit = ARow.ItemArray;
                ARow.BeginEdit();
                {#SAVEDETAILS}
                {#SAVEDETAILSEXTRA}
                if (Ict.Common.Data.DataUtilities.HaveDataRowsIdenticalValues(beforeEdit, ARow.ItemArray))
                {
                    ARow.CancelEdit();
                }
                else
                {
                    ARow.EndEdit();
                }
            }
        }
    }
{#IFDEF GENERATECONTROLUPDATEDATAHANDLER}

    private void ControlUpdateDataHandler(object sender, EventArgs e)
    {
        // This method should not normally be associated with a control that requires validation (because no validation takes place)
        // GetDetailsFromControls can return an exception if the control is associated with a primary key, so we use a try/catch just in case
        try
        {
            GetDetailsFromControls(FPreviouslySelectedDetailRow, false, (Control)sender);
        }
        catch (ConstraintException)
        {
        }
    }
{#ENDIF GENERATECONTROLUPDATEDATAHANDLER}
{#ENDIF SAVEDETAILS}
#endregion
{#IFDEF BUTTONPANEL}

#region Button Panel
    ///<summary>
    /// Finish the set up of the Button Panel.
    /// </summary>
    private void FinishButtonPanelSetup()
    {
        // Further set up certain Controls Properties that can't be set directly in the WinForms Generator...
        lblRecordCounter.AutoSize = true;
        lblRecordCounter.Padding = new Padding(4, 3, 0, 0);
        lblRecordCounter.ForeColor = System.Drawing.Color.SlateGray;

        pnlButtonsRecordCounter.AutoSize = true;
        
        UpdateRecordNumberDisplay();
    }
    
    private void UpdateRecordNumberDisplay()
    {
        int RecordCount;
        
        if (grdDetails.DataSource != null) 
        {
            RecordCount = ((DevAge.ComponentModel.BoundDataView)grdDetails.DataSource).Count;
            lblRecordCounter.Text = String.Format(
                Catalog.GetPluralString(MCommonResourcestrings.StrSingularRecordCount, MCommonResourcestrings.StrPluralRecordCount, RecordCount, true),
                RecordCount);
        }                
    }
#endregion
{#ENDIF BUTTONPANEL}
{#IFDEF FILTERANDFIND}

#region Filter and Find
    {#FILTERANDFINDMETHODS}
#endregion
{#ENDIF FILTERANDFIND}    

#region Implement interface functions

    /// auto generated
    public void RunOnceOnActivation()
    {
        {#RUNONCEONACTIVATIONMANUAL}
        {#RUNONCEINTERFACEIMPLEMENTATION}
        {#USERCONTROLSRUNONCEONACTIVATION}
        {#SETINITIALFOCUS}
    }

    /// <summary>
    /// Adds event handlers for the appropiate onChange event to call a central procedure
    /// </summary>
    public void HookupAllControls()
    {
        {#HOOKUPINTERFACEIMPLEMENTATION}
    }

    /// auto generated
    public void HookupAllInContainer(Control container)
    {
        FPetraUtilsObject.HookupAllInContainer(container);
    }

    /// auto generated
    public bool CanClose()
    {
        return FPetraUtilsObject.CanClose();
    }

    /// auto generated
    public TFrmPetraUtils GetPetraUtilsObject()
    {
        return (TFrmPetraUtils)FPetraUtilsObject;
    }

    /// auto generated
    public void FileSave(object sender, EventArgs e)
    {
        SaveChanges();
    }

    /// <summary>
    /// save the changes on the screen
    /// </summary>
    /// <returns></returns>
    public bool SaveChanges()
    {
        bool ReturnValue = false;

        FPetraUtilsObject.OnDataSavingStart(this, new System.EventArgs());

        // Clear any validation errors so that the following call to ValidateAllData starts with a 'clean slate'.
        FPetraUtilsObject.VerificationResultCollection.Clear();

        if (ValidateAllData(false, true))
        {       
            foreach (DataRow InspectDR in FMainDS.{#DETAILTABLE}.Rows)
            {
                InspectDR.EndEdit();
            }

            if (FPetraUtilsObject.HasChanges)
            {
                FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataInProgress);
                this.Cursor = Cursors.WaitCursor;

                TSubmitChangesResult SubmissionResult;
                TVerificationResultCollection VerificationResult;

                Ict.Common.Data.TTypedDataTable SubmitDT = FMainDS.{#DETAILTABLE}.GetChangesTyped();

                if (SubmitDT == null)
                {
                    // There is nothing to be saved.
                    // Update UI
                    FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataNothingToSave);
                    this.Cursor = Cursors.Default;

                    // We don't have unsaved changes anymore
                    FPetraUtilsObject.DisableSaveButton();
                    
                    return true;
                }
                
                // Submit changes to the PETRAServer
                try
                {
                    SubmissionResult = TDataCache.{#CACHEABLETABLESAVEMETHOD}({#CACHEABLETABLE}, ref SubmitDT{#CACHEABLETABLESPECIFICFILTERSAVE}, out VerificationResult);
                }
                catch (ESecurityDBTableAccessDeniedException Exp)
                {
                    FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataException);
                    this.Cursor = Cursors.Default;

                    TMessages.MsgSecurityException(Exp, this.GetType());
                    
                    ReturnValue = false;
                    FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                    return ReturnValue;
                }
                catch (EDBConcurrencyException Exp)
                {
                    FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataException);
                    this.Cursor = Cursors.Default;

                    TMessages.MsgDBConcurrencyException(Exp, this.GetType());
                    
                    ReturnValue = false;
                    FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                    return ReturnValue;
                }
                catch (Exception)
                {
                    FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataException);
                    this.Cursor = Cursors.Default;
                    
                    FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));                    
                    throw;
                }

                switch (SubmissionResult)
                {
                    case TSubmitChangesResult.scrOK:
                        // Call AcceptChanges to get rid now of any deleted columns before we Merge with the result from the Server
                        FMainDS.{#DETAILTABLE}.AcceptChanges();

                        // Merge back with data from the Server (eg. for getting Sequence values)
                        SubmitDT.AcceptChanges();
                        FMainDS.{#DETAILTABLE}.Merge(SubmitDT, false);

                        // need to accept the new modification ID
                        FMainDS.{#DETAILTABLE}.AcceptChanges();

                        // Update UI
                        FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataSuccessful);
                        this.Cursor = Cursors.Default;

                        // We don't have unsaved changes anymore
                        FPetraUtilsObject.DisableSaveButton();

                        SetPrimaryKeyReadOnly(true);

                        ReturnValue = true;
                        FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));

                        if((VerificationResult != null)
                            && (VerificationResult.HasCriticalOrNonCriticalErrors))
                        {
                            TDataValidation.ProcessAnyDataValidationErrors(false, VerificationResult,
                                this.GetType(), null);
                        }

                        break;

                    case TSubmitChangesResult.scrError:
                        this.Cursor = Cursors.Default;
                        FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataErrorOccured);

                        TDataValidation.ProcessAnyDataValidationErrors(false, VerificationResult,
                            this.GetType(), null);

                        FPetraUtilsObject.SubmitChangesContinue = false;
                        
                        ReturnValue = false;
                        FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                        break;

                    case TSubmitChangesResult.scrNothingToBeSaved:
                        this.Cursor = Cursors.Default;
                        FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataNothingToSave);

                        // We don't have unsaved changes anymore
                        FPetraUtilsObject.DisableSaveButton();
                        
                        ReturnValue = true;
                        FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
                        break;

                    case TSubmitChangesResult.scrInfoNeeded:

                        // TODO scrInfoNeeded
                        this.Cursor = Cursors.Default;
                        break;
                }
            }
            else
            {
                // Update UI
                FPetraUtilsObject.WriteToStatusBar(MCommonResourcestrings.StrSavingDataNothingToSave);
                this.Cursor = Cursors.Default;
                FPetraUtilsObject.DisableSaveButton();

                // We don't have unsaved changes anymore
                FPetraUtilsObject.HasChanges = false;

                ReturnValue = true;
                FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
            }                
        }
        else
        {
            // validation failed
            ReturnValue = false;
            FPetraUtilsObject.OnDataSaved(this, new TDataSavedEventArgs(ReturnValue));
        }


        return ReturnValue;
    }

#endregion

#region Action Handling

    /// auto generated
    public void ActionEnabledEvent(object sender, ActionEventArgs e)
    {
        {#ACTIONENABLING}
        {#ACTIONENABLINGDISABLEMISSINGFUNCS}
    }

    {#ACTIONHANDLERS}

#endregion

#region Keyboard handler

    /// Our main keyboard handler
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
{#IFDEF FILTERANDFIND}
        {#PROCESSCMDKEYCTRLF}
        {#PROCESSCMDKEYCTRLR}
{#ENDIF FILTERANDFIND}
        {#PROCESSCMDKEY}    
        {#PROCESSCMDKEYMANUAL}    

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void FocusFirstEditableControl()
    {
        {#FOCUSFIRSTEDITABLEDETAILSPANELCONTROL}
    }

#endregion

#region Data Validation Control Handlers
    
    private void ControlValidatedHandler(object sender, EventArgs e)
    {
        TScreenVerificationResult SingleVerificationResult;
        
        ValidateAllData(true, false);
        
        FPetraUtilsObject.ValidationToolTip.RemoveAll();
        
        if (FPetraUtilsObject.VerificationResultCollection.Count > 0) 
        {
            for (int Counter = 0; Counter < FPetraUtilsObject.VerificationResultCollection.Count; Counter++) 
            {
                SingleVerificationResult = (TScreenVerificationResult)FPetraUtilsObject.VerificationResultCollection[Counter];
                
                if (SingleVerificationResult.ResultControl == sender) 
                {
                    if (FPetraUtilsObject.VerificationResultCollection.FocusOnFirstErrorControlRequested)
                    {
                        SingleVerificationResult.ResultControl.Focus();
                        FPetraUtilsObject.VerificationResultCollection.FocusOnFirstErrorControlRequested = false;
                    }

{#IFDEF UNDODATA}
                    if(SingleVerificationResult.ControlValueUndoRequested)
                    {
                        UndoData(SingleVerificationResult.ResultColumn.Table.Rows[0], SingleVerificationResult.ResultControl);
                        SingleVerificationResult.OverrideResultText(SingleVerificationResult.ResultText + Environment.NewLine + Environment.NewLine + 
                            Catalog.GetString("--> The value you entered has been changed back to what it was before! <--"));
                    }

{#ENDIF UNDODATA}
                    if (!SingleVerificationResult.SuppressValidationToolTip) 
                    {
                        FPetraUtilsObject.ValidationToolTipSeverity = SingleVerificationResult.ResultSeverity;

                        if (SingleVerificationResult.ResultTextCaption != String.Empty) 
                        {
                            FPetraUtilsObject.ValidationToolTip.ToolTipTitle += ":  " + SingleVerificationResult.ResultTextCaption;    
                        }

                        FPetraUtilsObject.ValidationToolTip.Show(SingleVerificationResult.ResultText, (Control)sender, 
                            ((Control)sender).Width / 2, ((Control)sender).Height);
                    }
                }
            }
        }
{#IFDEF FILTERANDFIND}
        else if (FFailedValidation_CtrlChangeEventArgsInfo != null)
        {
            // The validation is all ok...  But we do have an outstanding filter update that we did not show due to previous invalid data
            //  So we can call that now and update the display.
            FucoFilterAndFind_ArgumentCtrlValueChanged(FFailedValidation_CtrlChangeEventArgsInfo.Sender,
                (TUcoFilterAndFind.TContextEventExtControlValueArgs)FFailedValidation_CtrlChangeEventArgsInfo.EventArgs);
            
            // Reset our cached change event
            FFailedValidation_CtrlChangeEventArgsInfo = null;
        }
{#ENDIF FILTERANDFIND}    
    }
{#IFDEF MASTERTABLE}
    private void ValidateData({#MASTERTABLE}Row ARow)
    {
        TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

        {#MASTERTABLE}Validation.Validate(this, ARow, ref VerificationResultCollection,
            FPetraUtilsObject.ValidationControlsDict);
    }
{#ENDIF MASTERTABLE}
{#IFDEF DETAILTABLE}
    private void ValidateDataDetails({#DETAILTABLE}Row ARow)
    {
        TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

        {#DETAILTABLE}Validation.Validate(this, ARow, ref VerificationResultCollection,
            FPetraUtilsObject.ValidationControlsDict);
    }
{#ENDIF DETAILTABLE}
{#IFDEF MASTERTABLE OR DETAILTABLE}

    private void BuildValidationControlsDict()
    {
{#IFDEF ADDCONTROLTOVALIDATIONCONTROLSDICT}
        {#ADDCONTROLTOVALIDATIONCONTROLSDICT}
{#ENDIF ADDCONTROLTOVALIDATIONCONTROLSDICT}
    }
{#ENDIF MASTERTABLE OR DETAILTABLE}    
{#IFDEF SHOWDETAILS}       

    private void SetPrimaryKeyControl()
    {
        // Make a default hint string from all the primary keys
        // and initialise the 'prime' primary key control on pnlDetails.
        // This is the last control in the tab order that matches a key
        int lastTabIndex = -1;
        DataRow row = (new {#DETAILTABLE}Table()).NewRow();
        for (int i = 0; i < row.Table.PrimaryKey.Length; i++)
        {
            DataColumn column = row.Table.PrimaryKey[i];
            if (FDefaultDuplicateRecordHint.Length > 0) FDefaultDuplicateRecordHint += ", ";
            FDefaultDuplicateRecordHint += TControlExtensions.DataColumnNameToFriendlyName(column, true);
            
            Label label;
            Control control;
            if (TControlExtensions.GetControlsForPrimaryKey(column, this, out label, out control))
            {
                if (control.TabIndex > lastTabIndex)
                {
                    FPrimaryKeyColumn = column;
                    FPrimaryKeyControl = control;
                    FPrimaryKeyLabel = label;
                    lastTabIndex = control.TabIndex;
                }
            }
        }
    }
{#ENDIF SHOWDETAILS}

#endregion
  }
}

{#INCLUDE copyvalues.cs}
{#INCLUDE validationcontrolsdict.cs}
{#INCLUDE inline_typed_dataset.cs}
{#INCLUDE findandfilter.cs}

{##SNIPDELETEREFERENCECOUNT}
this.Cursor = Cursors.WaitCursor;
TRemote.{#CONNECTORNAMESPACE}.ReferenceCount.WebConnectors.GetCacheableRecordReferenceCount(
    "{#CACHEABLETABLENAME}",
    DataUtilities.GetPKValuesFromDataRow(FPreviouslySelectedDetailRow),
    RefCountLimit,
    out VerificationResults);
this.Cursor = Cursors.Default;

{##SNIPMULTIDELETEDELETABLE}
if (!rowToDelete.{#DELETEABLEFLAG})
{
    recordsUndeletable++;
    continue;
}

{##SNIPCANDELETESELECTION}

    /// <summary>
    /// Returns true if all the selected rows can be deleted.
    /// </summary>
    private bool CanDeleteSelection()
    {
        // This table has a {#DELETEABLEFLAG} column
        DataRowView[] selectedRows = grdDetails.SelectedDataRowsAsDataRowView;

        foreach (DataRowView drv in selectedRows)
        {
            if ((({#DETAILTABLE}Row)drv.Row).{#DELETEABLEFLAG})
            {
                return true;
            }
        }

        return false;
    }
