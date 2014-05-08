//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop, christiank, alanP
//
// Copyright 2004-2014 by OM International
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Resources;
using System.Threading;
using System.Reflection;
using System.IO;

using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Verification;
using Ict.Petra.Shared;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.CommonControls;

namespace Ict.Petra.Client.CommonForms
{
    /// <summary>todoComment</summary>
    public delegate void ActionEventHandler(object sender, ActionEventArgs e);

    /// <summary>
    /// this class provides some useful methods for a Petra form
    /// </summary>
    public class TFrmPetraUtils
    {
        /// <summary>
        /// This sets a limit on the cascading reference count when checking for GUI deletion of a row.
        /// </summary>
        private const int FMaxReferenceCountOnDelete = 1000;

        /// <summary>
        /// will set the help text for each control, when it gets the focus
        /// </summary>
        private TExtStatusBarHelp FStatusBar;

        /// <summary>
        /// This is a reference to the WinForm that contains this Petra object
        /// this object implements the IFrmPetra interface
        /// </summary>
        protected IFrmPetra FTheForm;

        /// <summary>
        /// ToolTip instance for the Form.
        /// </summary>
        protected System.Windows.Forms.ToolTip FtipForm;

        /// <summary>
        /// ToolTip instance which is used to show Data Validation messages.
        /// </summary>
        protected ToolTip FValidationToolTip;

        /// <summary>
        /// Dictionary that contains Controls on whose data Data Validation should be run.
        /// </summary>
        protected TValidationControlsDict FValidationControlsDict = new TValidationControlsDict();

        /// <summary>
        /// points to the same object as FTheForm, but already casted to a WinForm
        /// </summary>
        protected System.Windows.Forms.Form FWinForm;

        private Form FCallerForm;

        /// This helper class handles the extensions to TFrmPetraUtils that deal with opening and closing windows
        /// and loading and saving window sizes, positions and states
        private TFrmPetraWindowExtensions FWindowExtensions;

        /// Tells whether the Form is activated for the first time (after loading the Form) or not
        protected Boolean FFormActivatedForFirstTime;

        /// Set this to true to prevent the automatic hookup of change Events of all Controls on the Form
        protected Boolean FNoAutoHookupOfAllControls;

        /// This holds a reference to ALL controls on the screen  even if they are buried in GroupBoxes, Panels, or TabPages
        protected ArrayList FAllControls;

        /// This holds a reference to ALL controls on the screen that have child controls, even if they are buried in GroupBoxes, Panels, or TabPages
        protected ArrayList FControlsWithChildren;

        /// Used for keeping track of data verification errors
        protected TVerificationResultCollection FVerificationResultCollection;

        /// <summary>Whether the Form's Shown Event already occured. ATTENTION: See comment on
        /// <see cref="FormHasBeenShown" /> Property for important implementation details!</summary>
        protected Boolean FFormHasBeenShown = false;

        /// Used for keeping track of data verification errors
        public TVerificationResultCollection VerificationResultCollection
        {
            get
            {
                return FVerificationResultCollection;
            }
            set
            {
                FVerificationResultCollection = value;
            }
        }

        /// <summary>
        /// Whether the Form's Shown Event has already occured. ATTENTION: This Property's value is
        /// NOT AUTOMATICALLY SET once a Forms Shown Event has run!!! - A Form that wants to have
        /// that Property's value reflect the fact that a Form has been shown needs to
        /// hook up its .Shown Event to this Classes' <see cref="OnFormShown" /> Method!!!
        /// </summary>
        public Boolean FormHasBeenShown
        {
            get
            {
                return FFormHasBeenShown;
            }
        }

        /// <summary>
        /// Gets the maximum number of references to find before abandoning the count when deleting a record.
        /// </summary>
        public int MaxReferenceCountOnDelete
        {
            get
            {
                return FMaxReferenceCountOnDelete;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ACallerForm">the form that has opened this window; needed for focusing when this window is closed later</param>
        /// <param name="ATheForm"></param>
        /// <param name="AStatusBar"></param>
        public TFrmPetraUtils(Form ACallerForm, IFrmPetra ATheForm, TExtStatusBarHelp AStatusBar)
        {
            FFormActivatedForFirstTime = true;
            FVerificationResultCollection = new TVerificationResultCollection();

            FTheForm = ATheForm;
            FWinForm = (Form)ATheForm;
            FStatusBar = AStatusBar;
            FCallerForm = ACallerForm;

            if (ACallerForm != null)
            {
                TFormsList.GFormsList.NotifyWindowOpened(ACallerForm.Handle, FWinForm.Handle);
            }

            FWindowExtensions = new TFrmPetraWindowExtensions(FWinForm, FCallerForm);

            //
            // Initialise the Data Validation ToolTip
            //
            FValidationToolTip = new System.Windows.Forms.ToolTip();
            FValidationToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            FValidationToolTip.ToolTipTitle = Catalog.GetString("Incorrect Data");
            FValidationToolTip.UseAnimation = true;
            FValidationToolTip.UseFading = true;

            // WriteToStatusBar(Catalog.GetString("Ready."));
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TFrmPetra_Activated(System.Object sender, System.EventArgs e)
        {
            if (FFormActivatedForFirstTime == true)
            {
                // prevent this happening again
                FFormActivatedForFirstTime = false;

                // do any low level initialisation
                LocalRunOnceOnActivation();

                // virtual method, overriden in child forms
                RunOnceOnActivation();
            }
        }

        /// <summary>
        /// just call this function to clean up when closing the form
        /// </summary>
        public void Close()
        {
            // to prevent strange error message, that would stop the form from closing
            FFormActivatedForFirstTime = false;
        }

        /** used to allow subforms to initialise
         * Also, now that the form is shown we can set the splitter bar distances
         */
        public void LocalRunOnceOnActivation()
        {
            if (!FNoAutoHookupOfAllControls)
            {
                HookupAllControls();
            }

            // Are we saving/restoring the window position?  This option is stored in User Defaults.
            if (TUserDefaults.IsInitialised)
            {
                if (TUserDefaults.GetBooleanDefault(TUserDefaults.NamedDefaults.USERDEFAULT_SAVE_WINDOW_POS_AND_SIZE, true))
                {
                    // (Note: Nant tests do not have a caller so we need to allow for this possibility)
                    if ((FWinForm.Name == "TFrmMainWindowNew") || ((FCallerForm != null) && (FCallerForm.Name == "TFrmMainWindowNew")))
                    {
                        // Either we are loading the main window or we have been opened by the main window
                        // Now that the window has been activated we are ok to restore things like splitter distances
                        RestoreAdditionalWindowPositionProperties();
                    }
                }
            }
        }

        /// <summary>
        /// todoComment
        /// </summary>
        public void RunOnceOnActivation()
        {
            if (FTheForm != null)
            {
                FTheForm.RunOnceOnActivation();
            }
        }

        /** used to iterate through the controls on the form
         */
        public void EnumerateControls(Control c)
        {
            foreach (Control ctrl in c.Controls)
            {
                // exclude TPetraUserControl objects that should not be hooked up
                if ((ctrl is TPetraUserControl)
                    && (!((TPetraUserControl)ctrl).CanBeHookedUpForValueChangedEvent))
                {
                    continue;
                }

                // recurse into children;
                // but special case for UpDownBase/NumericUpDown, because we don't want the child controls of that
                if ((ctrl.HasChildren == true) && !(ctrl is UpDownBase) && !(ctrl is TClbVersatile))
                {
                    EnumerateControls(ctrl);

                    if ((ctrl is Panel)
                        || (ctrl is GroupBox)
                        || (ctrl is UserControl))
                    {
                        FControlsWithChildren.Add(ctrl);
                    }
                }
                else
                {
                    FAllControls.Add(ctrl);
                }
            }
        }

        /// <summary>
        /// todoComment
        /// </summary>
        public virtual void HookupAllControls()
        {
            Control IteratedControl;

            FAllControls = new ArrayList();
            FControlsWithChildren = new ArrayList();

            EnumerateControls(FWinForm); //this adds all controls on form to ArrayList

            // this is on an international version of Windows, so we want no bold fonts
            // because the letters are difficult to read
            bool changeFonts = TAppSettingsManager.ChangeFontForLocalisation();

            for (int Counter1 = 0; Counter1 < FAllControls.Count; Counter1++)
            {
                IteratedControl = (Control)FAllControls[Counter1];

                if (changeFonts)
                {
                    if (TAppSettingsManager.ReplaceFont(IteratedControl.Font))
                    {
                        // remove bold and replace with regular
                        IteratedControl.Font = new System.Drawing.Font(IteratedControl.Font.Name,
                            IteratedControl.Font.Size,
                            System.Drawing.FontStyle.Regular);
                    }
                }
            }

            // Hook up Global Application Help handler to to the Form itself
            FWinForm.HelpRequested += new HelpEventHandler(GlobalApplicationHelpEventHandler);

            // Hook up Global Application Help handler to all Controls that have child controls
            for (int Counter2 = 0; Counter2 < FControlsWithChildren.Count; Counter2++)
            {
                ((Control)FControlsWithChildren[Counter2]).HelpRequested += new HelpEventHandler(GlobalApplicationHelpEventHandler);
            }
        }

        /// <summary>
        /// Global application help handler. Every 'F1' key-press that happens in a context where it is hooked up
        /// gets passed to this handler.
        /// </summary>
        /// <param name="ASender">A Form or a Control.</param>
        /// <param name="AHelpEvent">Ignored.</param>
        void GlobalApplicationHelpEventHandler(object ASender, HelpEventArgs AHelpEvent)
        {
            Form HelpContextForm;
            Control HelpContextControl;

            if (ASender is Form)
            {
                HelpContextForm = (Form)ASender;
                HelpContextControl = null;
            }
            else
            {
                HelpContextForm = FWinForm;
                HelpContextControl = (Control)ASender;
            }

            try
            {
                if (!Ict.Common.HelpLauncher.ShowHelp(HelpContextForm, HelpContextControl))
                {
                    WriteToStatusBar(Catalog.GetString("Sorry, there is no help available for the context."));
                }
            }
            catch (EHelpLauncherException Exp)
            {
                MessageBox.Show(Exp.Message, "Error Launching Application Help", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="container"></param>
        public virtual void HookupAllInContainer(Control container)
        {
            // not implemented here
        }

        /// Set this to true to prevent the automatic hookup of change Events of all Controls on the Form
        public Boolean NoAutoHookupOfAllControls
        {
            set
            {
                FNoAutoHookupOfAllControls = value;
            }
        }

        #region Enable and Disable Action

        /// <summary>
        /// todoComment
        /// </summary>
        public event ActionEventHandler ActionEnablingEvent;

        private SortedList <string, bool>FActionStates = new SortedList <string, bool>();

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="AActionName"></param>
        /// <param name="enable"></param>
        public void EnableAction(string AActionName, bool enable)
        {
            // store action enabled
            if (FActionStates.ContainsKey(AActionName))
            {
                FActionStates[AActionName] = enable;
            }
            else
            {
                FActionStates.Add(AActionName, enable);
            }

            //trigger event
            ActionEnablingEvent(this, new ActionEventArgs(AActionName, enable));
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="AActionName"></param>
        /// <returns></returns>
        public bool IsEnabled(string AActionName)
        {
            if (FActionStates.ContainsKey(AActionName))
            {
                return FActionStates[AActionName];
            }

            return true;
        }

        /// <summary>todoComment</summary>
        public bool FormActivatedForFirstTime
        {
            get
            {
                return FFormActivatedForFirstTime;
            }
            set
            {
                FFormActivatedForFirstTime = value;
            }
        }

        /// <summary>
        /// ToolTip instance which is used to show Data Validation messages.
        /// </summary>
        public ToolTip ValidationToolTip
        {
            get
            {
                return FValidationToolTip;
            }
        }

        /// <summary>
        /// Sets the Validation ToolTip severity. This affects the icon and title of the Validation ToolTip.
        /// </summary>
        public TResultSeverity ValidationToolTipSeverity
        {
            set
            {
                switch (value)
                {
                    case TResultSeverity.Resv_Critical:
                        FValidationToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Error;
                        FValidationToolTip.ToolTipTitle = Catalog.GetString("Incorrect Data");

                        break;

                    case TResultSeverity.Resv_Noncritical:
                        FValidationToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
                        FValidationToolTip.ToolTipTitle = Catalog.GetString("Warning");

                        break;

                    case TResultSeverity.Resv_Info:
                        FValidationToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                        FValidationToolTip.ToolTipTitle = Catalog.GetString("Information");

                        break;

                    case TResultSeverity.Resv_Status:
                        FValidationToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.None;
                        FValidationToolTip.ToolTipTitle = Catalog.GetString("Note");

                        break;
                }
            }
        }

        /// <summary>
        /// Dictionary that contains Controls on whose data Data Validation should be run.
        /// </summary>
        public TValidationControlsDict ValidationControlsDict
        {
            get
            {
                return FValidationControlsDict;
            }
            set
            {
                FValidationControlsDict = value;
            }
        }

        /// useful for initialising actions, eg based on permissions
        virtual public void InitActionState()
        {
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Form_KeyDown(System.Object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (FWindowExtensions.AContainerControlWantsEscape(sender)
                    || (TUserDefaults.GetBooleanDefault(TUserDefaults.NamedDefaults.USERDEFAULT_ESC_CLOSES_SCREEN, true) == false)
                    || (this.FWinForm.Name == "TFrmMainWindowNew"))
                {
                    // Either the user default is NOT to use the ESC key to close screens
                    // or there is a control on the form that needs to handle the escape key,
                    // or we are the main window
                    //  so we do nothing and the keyDown message will get passed to the control - which will do something if it needs to
                }
                else
                {
                    // No control wants the escape key so we are free to handle the KeyDown message at the Form level
                    e.Handled = true;
                    ExecuteAction(eActionId.eClose);
                }
            }
            else if (e.KeyCode == Keys.F1)
            {
                e.Handled = true;
                ExecuteAction(eActionId.eHelp);
            }
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Mni_Click(System.Object sender, System.EventArgs e)
        {
            if (!(sender is ToolStripItem))
            {
                return;
            }

            ExecuteAction((TItemTag)((ToolStripItem)sender).Tag);
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="ATag"></param>
        public void ExecuteAction(TItemTag ATag)
        {
            ExecuteAction(ATag.Id);
        }

        private Assembly CommonDialogsAssembly = null;

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="id"></param>
        public void ExecuteAction(eActionId id)
        {
            if (CommonDialogsAssembly == null)
            {
                CommonDialogsAssembly = Assembly.LoadFrom(
                    TAppSettingsManager.ApplicationDirectory + Path.DirectorySeparatorChar + "Ict.Petra.Client.CommonDialogs.dll");
            }

            switch (id)
            {
                case eActionId.eClose:
                    this.Close();
                    FWinForm.Close();
                    break;

                case eActionId.eHelpDevelopmentTeam:
#if TODO
                    using (DevelopmentTeamDialog teamDialog = new DevelopmentTeamDialog())
                    {
                        teamDialog.ShowDialog();
                    }
#endif
                    break;

                case eActionId.eHelpAbout:
                    System.Type dialogType = CommonDialogsAssembly.GetType("Ict.Petra.Client.CommonDialogs.TFrmAboutDialog");

                    using (Form aboutDialog = (Form)Activator.CreateInstance(dialogType, new object[] { this.FWinForm }))
                    {
                        aboutDialog.ShowDialog();
                    }
                    break;

                case eActionId.eHelp:
                {
                    // TODO help action
                }
                break;
            }
        }

        /// <summary>
        /// add the form to the forms list and set its window position/size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TFrmPetra_Load(System.Object sender, System.EventArgs e)
        {
            TFormsList.GFormsList.Add(FWinForm);

            FWindowExtensions.TForm_Load();
        }

        /**
         * Event Handler that is invoked when the Form is about to close - no matter
         * how the closing was invoked (by calling Form.Close, a Close button, the
         * x Button of a Form, etc).
         *
         * @param sender Event sender
         * @param e EventArgs that allow cancelling of the closing
         *
         */
        public virtual void TFrmPetra_Closing(System.Object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CanClose() || !FTheForm.CanClose())
            {
                // MessageBox.Show('TFrmPetra.TFrmPetra_Closing: e.Cancel := true');
                e.Cancel = true;
            }
            else
            {
                // MessageBox.Show('TFrmPetra.TFrmPetra_Closing: GFormsList.Remove(Self as Form)');
                TFormsList.GFormsList.NotifyWindowClose(this.FWinForm.Handle);
                TFormsList.GFormsList.Remove(FWinForm);

                FWindowExtensions.TForm_Closing();
            }
        }

        /// <summary>
        /// Hook up this Event to a Forms' Shown Event to allow the <see cref="FormHasBeenShown" />
        /// Property to reflect that.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        public void OnFormShown(System.Object sender, System.EventArgs e)
        {
            FFormHasBeenShown = true;
        }

        #endregion

        #region Helper Functions

        /**
         * This function can be used to write to the StatusBar.
         *
         * TLogging can use it as a callback procedure, so it does not need to know
         * about the StatusBar itself
         *
         * @param s the text to be displayed in the StatusBar
         *
         */
        delegate void WriteCallback (String s);

        /// <summary>
        /// (Thread safe)
        /// </summary>
        /// <param name="s"></param>
        public void WriteToStatusBar(String s)
        {
            if (FStatusBar != null)
            {
                if (!FStatusBar.IsHandleCreated)
                {
                    return;
                }

                if (FStatusBar.InvokeRequired)
                {
                    FStatusBar.Invoke(new WriteCallback(WriteToStatusBar), new object[] { s });
                }
                else
                {
                    FStatusBar.ShowMessage(s);
                }
            }
        }

        /**
         * This function tells the caller whether the window can be closed.
         * It can be used to find out if something is still edited, for example.
         *
         * @return true if window can be closed
         *
         */
        virtual public bool CanClose()
        {
            return true;
        }

        /// <summary>
        /// Method that restores splitter bar positions.  If you have a tabbed control that hosts a split container
        /// you need to call this method when the tab page changes.
        /// </summary>
        public void RestoreAdditionalWindowPositionProperties()
        {
            FWindowExtensions.RestoreAdditionalWindowPositionProperties();
        }

        /// <summary>
        /// Loads the file that stores windows sizes/positions for the active user
        /// </summary>
        public void LoadWindowPositionsFromFile()
        {
            FWindowExtensions.LoadWindowPositionsFromFile();
        }

        #endregion

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="AControl"></param>
        /// <param name="AHelpText"></param>
        public void SetStatusBarText(Control AControl, string AHelpText)
        {
            if (FStatusBar != null)
            {
                FStatusBar.SetHelpText(AControl, AHelpText);
            }
        }

        const Int16 MAX_COMBOBOX_HISTORY = 30;

        /// <summary>
        /// add new value of combobox to the user defaults, or move existing value to the front;
        /// limits the number of values to MAX_COMBOBOX_HISTORY
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        public void AddComboBoxHistory(System.Object Sender, TAcceptNewEntryEventArgs e)
        {
            string keyName = "CmbHistory" + ((Control)Sender).Name;
            StringCollection values = StringHelper.StrSplit(TUserDefaults.GetStringDefault(keyName, ""), ",");

            values.Remove(e.ItemString);
            values.Insert(0, e.ItemString);

            while (values.Count > MAX_COMBOBOX_HISTORY)
            {
                values.RemoveAt(values.Count - 1);
            }

            TUserDefaults.SetDefault(keyName, StringHelper.StrMerge(values, ','));
        }

        /// <summary>
        /// load the history of a combobox for auto completion from the user defaults
        /// </summary>
        /// <param name="AComboBox"></param>
        public void LoadComboBoxHistory(TCmbAutoComplete AComboBox)
        {
            AComboBox.SetDataSourceStringList(TUserDefaults.GetStringDefault("CmbHistory" + AComboBox.Name, ""));
        }

        /// <summary>
        /// get the form that has opened this form
        /// </summary>
        /// <returns></returns>
        public Form GetCallerForm()
        {
            return FCallerForm;
        }

        /// <summary>
        /// get the current form
        /// </summary>
        /// <returns></returns>
        public Form GetForm()
        {
            return FWinForm;
        }

        /// <summary>
        /// Sets the tooltip for a Control.
        /// </summary>
        public void SetToolTip(Control AControl, string AToolTipText)
        {
            if (FtipForm == null)
            {
                FtipForm = new ToolTip();
            }

            FtipForm.SetToolTip(AControl, AToolTipText);
        }
    }

    /// <summary>todoComment</summary>
    public enum eActionId
    {
        /// <summary>todoComment</summary>
        eHelp,

        /// <summary>todoComment</summary>
        eAbout,

        /// <summary>todoComment</summary>
        eClose,

        /// <summary>todoComment</summary>
        eHelpDevelopmentTeam,

        /// <summary>todoComment</summary>
        eHelpAbout,

        /// <summary>todoComment</summary>
        eBugReport
    };

    /// <summary>
    /// this is a helper class to identify menu items and toolbar buttons
    /// </summary>
    public class TItemTag
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="AId">identifier for the type of menu item</param>
        public TItemTag(eActionId AId)
        {
            id = AId;
        }

        private eActionId id;

        /// <summary>todoComment</summary>
        public eActionId Id
        {
            get
            {
                return id;
            }
        }
    }

    /// <summary>todoComment</summary>
    public class PetraForm
    {
        /// <summary>todoComment</summary>
        public const Int32 AUTOSCALEBASESIZEWIDTHFOR96DPI = 6;
    }

    /// <summary>todoComment</summary>
    public interface IFrmPetra
    {
        /// <summary>todoComment</summary>
        void RunOnceOnActivation();

        /// <summary>todoComment</summary>
        bool CanClose();

        /// <summary>todoComment</summary>
        TFrmPetraUtils GetPetraUtilsObject();
    }
}