// <copyright file="StatusListing.ascx.cs" company="Engage Software">
// Engage: Employment
// Copyright (c) 2004-2013
// by Engage Software ( http://www.engagesoftware.com )
// </copyright>
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

namespace Engage.Dnn.Employment.Admin
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using DotNetNuke.Services.Exceptions;
    using DotNetNuke.Services.Localization;

    using Engage.Annotations;

    /// <summary>An editable listing of the user statuses for this portal</summary>
    public partial class StatusListing : ModuleBase
    {
        /// <summary>The maximum length of a user status' name</summary>
        private const int StatusMaxLength = 255;

        /// <summary>Gets a regular expression which will not match text that is longer than the <see cref="StatusMaxLength"/>.</summary>
        /// <value>A regular expression <see cref="string"/>.</value>
        protected static string MaxLengthValidationExpression
        {
            get { return Utility.GetMaxLengthValidationExpression(StatusMaxLength); }
        }

        /// <summary>Gets the validation text to show when a status' name is too long.</summary>
        /// <value>The validation text.</value>
        protected string MaxLengthValidationText
        {
            get { return string.Format(CultureInfo.CurrentCulture, this.Localize("StatusMaxLength"), StatusMaxLength); }
        }

        /// <summary>Raises the <see cref="Control.Init"/> event.</summary>
        /// <param name="e">An <see cref="EventArgs"/> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            if (!PermissionController.CanManageApplications(this))
            {
                this.DenyAccess();
                return;
            }

            this.Load += this.Page_Load;
            this.AddButton.Click += this.AddButton_Click;
            this.BackButton.Click += this.BackButton_Click;
            this.CancelNewButton.Click += this.CancelNewButton_Click;
            this.SaveNewButton.Click += this.SaveNewButton_Click;
            this.StatusesGridView.RowCancelingEdit += this.StatusesGridView_RowCancelingEdit;
            this.StatusesGridView.RowCommand += this.StatusesGridView_RowCommand;
            this.StatusesGridView.RowDataBound += this.StatusesGridView_RowDataBound;
            this.StatusesGridView.RowDeleting += this.StatusesGridView_RowDeleting;
            this.StatusesGridView.RowEditing += this.StatusesGridView_RowEditing;

            base.OnInit(e);
        }

        /// <summary>Gets the status ID from the given row.</summary>
        /// <param name="row">The row control.</param>
        /// <returns>An <see cref="int" />, or <c>null</c>.</returns>
        private static int? GetStatusId(Control row)
        {
            var statusIdHiddenField = (HiddenField)row.FindControl("StatusIdHiddenField");

            int statusId;
            if (statusIdHiddenField != null && int.TryParse(statusIdHiddenField.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out statusId))
            {
                return statusId;
            }

            return null;
        }

        /// <summary>Binds the <see cref="StatusesGridView"/> to the list of user statuses.</summary>
        private void BindData()
        {
            var statuses = UserStatus.LoadStatuses(this.PortalId).ToArray();
            this.StatusesGridView.DataSource = statuses;
            this.StatusesGridView.DataBind();

            this.NewPanel.CssClass = statuses.Length % 2 == 0
                                         ? this.StatusesGridView.RowStyle.CssClass
                                         : this.StatusesGridView.AlternatingRowStyle.CssClass;

            this.rowNewHeader.Visible = !statuses.Any();
        }

        /// <summary>Gets the status ID of the row with the given index.</summary>
        /// <param name="rowIndex">Index of the row for which to find the status ID.</param>
        /// <returns>An <see cref="int"/>, or <c>null</c>.</returns>
        private int? GetStatusId(int rowIndex)
        {
            if (this.StatusesGridView == null || this.StatusesGridView.Rows.Count <= rowIndex)
            {
                return null;
            }

            return GetStatusId(this.StatusesGridView.Rows[rowIndex]);
        }

        /// <summary>Gets the name of the status in the row with the given index.</summary>
        /// <param name="rowIndex">Index of the row for which to find the name of the status.</param>
        /// <returns>The name of the status, or <c>null</c>.</returns>
        [CanBeNull]
        private string GetStatusName(int rowIndex)
        {
            if (this.StatusesGridView == null || this.StatusesGridView.Rows.Count <= rowIndex)
            {
                return null;
            }

            var row = this.StatusesGridView.Rows[rowIndex];
            var statusTextBox = (TextBox)row.FindControl("StatusTextBox");
            return statusTextBox.Text;
        }

        /// <summary>Hides and clears the panel for adding a new status.</summary>
        private void HideAndClearNewStatusPanel()
        {
            this.NewPanel.Visible = false;
            this.txtNewStatus.Text = string.Empty;
        }

        /// <summary>Determines whether the <paramref name="newStatusName"/> is unique.</summary>
        /// <param name="statusId">The ID of the status being renamed, or <c>null</c> for a new status.</param>
        /// <param name="newStatusName">New name of the status.</param>
        /// <returns><c>true</c> if the new status name is unique; otherwise, <c>false</c>.</returns>
        private bool IsStatusNameUnique(int? statusId, string newStatusName)
        {
            var newStatusId = UserStatus.GetStatusId(newStatusName, this.PortalId);
            return !newStatusId.HasValue || (statusId.HasValue && newStatusId.Value == statusId.Value);
        }

        /// <summary>Sets up the validator for a new status's name.</summary>
        private void SetupStatusLengthValidation()
        {
            this.regexNewUserStatus.ValidationExpression = MaxLengthValidationExpression;
            this.regexNewUserStatus.ErrorMessage = this.MaxLengthValidationText;
        }

        /// <summary>Handles the <see cref="Button.Click"/> event of the <see cref="AddButton"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void AddButton_Click(object sender, EventArgs e)
        {
            this.NewPanel.Visible = true;
            this.txtNewStatus.Focus();
        }

        /// <summary>Handles the <see cref="LinkButton.Click"/> event of the <see cref="BackButton"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void BackButton_Click(object sender, EventArgs e)
        {
            this.Response.Redirect(this.EditUrl(ControlKey.ManageApplications.ToString()));
        }

        /// <summary>Handles the <see cref="Button.Click"/> event of the <see cref="CancelNewButton"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CancelNewButton_Click(object sender, EventArgs e)
        {
            this.HideAndClearNewStatusPanel();
        }

        /// <summary>Handles the <see cref="Button.Click"/> event of the <see cref="SaveNewButton"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SaveNewButton_Click(object sender, EventArgs e)
        {
            if (!this.Page.IsValid)
            {
                return;
            }

            if (!this.IsStatusNameUnique(null, this.txtNewStatus.Text))
            {
                this.cvDuplicateUserStatus.IsValid = false;
                return;
            }
            
            UserStatus.InsertStatus(this.txtNewStatus.Text, this.PortalId);
            this.HideAndClearNewStatusPanel();
            this.BindData();
        }

        /// <summary>Handles the <see cref="GridView.RowCancelingEdit"/> event of the <see cref="StatusesGridView"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewCancelEditEventArgs"/> instance containing the event data.</param>
        private void StatusesGridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            this.StatusesGridView.EditIndex = -1;
            this.BindData();
        }

        /// <summary>Handles the <see cref="GridView.RowCommand"/> event of the <see cref="StatusesGridView"/> control. </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewCommandEventArgs"/> instance containing the event data.</param>
        private void StatusesGridView_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!string.Equals("Save", e.CommandName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!this.Page.IsValid)
            {
                return;
            }

            int rowIndex;
            if (!int.TryParse(e.CommandArgument.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rowIndex))
            {
                return;
            }

            var statusId = this.GetStatusId(rowIndex);
            if (!statusId.HasValue)
            {
                return;
            }

            var newStatusName = this.GetStatusName(rowIndex);
            if (!this.IsStatusNameUnique(statusId, newStatusName))
            {
                this.cvDuplicateUserStatus.IsValid = false;
                return;
            }
            
            UserStatus.UpdateStatus(statusId.Value, newStatusName);
            this.StatusesGridView.EditIndex = -1;
            this.BindData();
        }

        /// <summary>Handles the <see cref="GridView.RowDataBound"/> event of the <see cref="StatusesGridView"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void StatusesGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var row = e.Row;
            if (row == null)
            {
                return;
            }

            var deleteButton = (Button)row.FindControl("DeleteButton");
            if (deleteButton == null)
            {
                return;
            }

            var statusId = GetStatusId(row);
            if (statusId.HasValue && UserStatus.IsStatusUsed(statusId.Value))
            {
                deleteButton.Enabled = false;
                return;
            }
            
            deleteButton.Attributes["data-confirm-click"] = this.Localize("DeleteConfirm");
            Dnn.Utility.RequestEmbeddedScript(this.Page, "confirmClick.js");
        }

        /// <summary>Handles the <see cref="GridView.RowDeleting"/> event of the <see cref="StatusesGridView"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewDeleteEventArgs"/> instance containing the event data.</param>
        private void StatusesGridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            var statusId = GetStatusId(e.RowIndex);
            if (!statusId.HasValue)
            {
                return;
            }

            UserStatus.DeleteStatus(statusId.Value);
            this.BindData();
        }

        /// <summary>Handles the <see cref="GridView.RowEditing"/> event of the <see cref="StatusesGridView"/> control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewEditEventArgs"/> instance containing the event data.</param>
        private void StatusesGridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            this.StatusesGridView.EditIndex = e.NewEditIndex;
            this.HideAndClearNewStatusPanel();
            this.BindData();
        }

        /// <summary>Handles the <see cref="Control.Load"/> event of this control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (this.IsPostBack)
                {
                    return;
                }

                Localization.LocalizeGridView(ref this.StatusesGridView, this.LocalResourceFile);
                this.SetupStatusLengthValidation();
                this.BindData();
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }
    }
}