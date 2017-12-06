﻿// <copyright>uns   
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Security;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using System.Globalization;
using Humanizer;
using Rock.Web.UI.Controls;
using System.Web.UI.WebControls;
using Rock.Web;
using System.Data.Entity;

namespace RockWeb.Blocks.Crm
{
    [DisplayName( "Ncoa Results" )]
    [Category( "CRM" )]
    [Description( "Display the Ncoa History Record" )]

    [IntegerField( "Result Count", "Number of result to display per page (default 20).", false, 20 )]
    public partial class NcoaResults : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gfNcoaFilter.ClearFilterClick += gfNcoaFilter_ClearFilterClick;
            gfNcoaFilter.ApplyFilterClick += gfNcoaFilter_ApplyFilterClick;
            gfNcoaFilter.DisplayFilterValue += gfNcoaFilter_DisplayFilterValue;

            this.BlockUpdated += NcoaResults_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upNcoaResults );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindFilter();
                ShowView();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the NcoaResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void NcoaResults_BlockUpdated( object sender, EventArgs e )
        {
            BindFilter();
            ShowView();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfNcoaFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfNcoaFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfNcoaFilter.DeleteUserPreferences();
            BindFilter();
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfNcoaFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfNcoaFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfNcoaFilter.SaveUserPreference( "Processed", ddlProcessed.SelectedValue );
            gfNcoaFilter.SaveUserPreference( "Move Date", sdpMoveDate.DelimitedValues );
            gfNcoaFilter.SaveUserPreference( "NCOA Processed Date", sdpProcessedDate.DelimitedValues );
            gfNcoaFilter.SaveUserPreference( "Move Type", ddlMoveType.SelectedValue );
            gfNcoaFilter.SaveUserPreference( "Address Status", ddlAddressStatus.SelectedValue );
            gfNcoaFilter.SaveUserPreference( "Address Invalid Reason", ddlInvalidReason.SelectedValue );
            gfNcoaFilter.SaveUserPreference( "Last Name", tbLastName.Text );
            gfNcoaFilter.SaveUserPreference( "Move Distance", nbMoveDistance.Text );

            ShowView();
        }

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfNcoaFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs"/> instance containing the event data.</param>
        protected void gfNcoaFilter_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Move Date":
                case "NCOA Processed Date":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }

                case "Processed":
                    {
                        var processed = e.Value.ConvertToEnumOrNull<Processed>();
                        if ( processed.HasValue )
                        {
                            e.Value = processed.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
                case "Move Type":
                    {
                        var moveType = e.Value.ConvertToEnumOrNull<MoveType>();
                        if ( moveType.HasValue )
                        {
                            e.Value = moveType.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
                case "Address Status":
                    {
                        var addressStatus = e.Value.ConvertToEnumOrNull<AddressStatus>();
                        if ( addressStatus.HasValue )
                        {
                            e.Value = addressStatus.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
                case "Invalid Reason":
                    {
                        var invalidReason = e.Value.ConvertToEnumOrNull<AddressInvalidReason>();
                        if ( invalidReason.HasValue )
                        {
                            e.Value = invalidReason.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptNcoaResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptNcoaResults_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var ncoaRow = e.Item.DataItem as NcoaRow;
            var familyMembers = e.Item.FindControl( "lMembers" ) as Literal;

            DescriptionList mmembers = new DescriptionList();
            bool individual = false;
            if ( ncoaRow.Individual != null )
            {
                individual = true;
                mmembers.Add( "Individual", ncoaRow.Individual.FullName );
            }
            if ( ncoaRow.FamilyMembers != null && ncoaRow.FamilyMembers.Count > 0 )
            {
                if ( !individual )
                {
                    mmembers.Add( "Family Members", ncoaRow.FamilyMembers.Select( a => a.FullName ).ToList().AsDelimited( "<Br/>" ) );
                }
                else
                {
                    mmembers.Add( "Other Family Members", ncoaRow.FamilyMembers.Select( a => a.FullName ).ToList().AsDelimited( "<Br/>" ) );
                }
            }
            familyMembers.Text = mmembers.Html;

        }

        /// <summary>
        /// Handles the ItemCommand event of the rptNcoaResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptNcoaResults_ItemCommand( object Sender, RepeaterCommandEventArgs e )
        {
            var ncoaHistoryId = e.CommandArgument.ToString().AsIntegerOrNull();
            if ( e.CommandName == "MarkAddressAsPrevious" )
            {
                
            }
            if ( e.CommandName == "MarkProcessed" )
            {

            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            ddlProcessed.BindToEnum<Processed>( true );
            int? processedId = gfNcoaFilter.GetUserPreference( "Processed" ).AsIntegerOrNull();
            if ( processedId.HasValue )
            {
                ddlProcessed.SetValue( processedId.Value.ToString() );
            }



            sdpMoveDate.DelimitedValues = gfNcoaFilter.GetUserPreference( "Move Date" );

            sdpProcessedDate.DelimitedValues = gfNcoaFilter.GetUserPreference( "NCOA Processed Date" );

            ddlMoveType.BindToEnum<MoveType>( true );
            int? moveTypeId = gfNcoaFilter.GetUserPreference( "Move Type" ).AsIntegerOrNull();
            if ( moveTypeId.HasValue )
            {
                ddlMoveType.SetValue( moveTypeId.Value.ToString() );
            }

            ddlAddressStatus.BindToEnum<AddressStatus>( true );
            int? addressStatusId = gfNcoaFilter.GetUserPreference( "Address Status" ).AsIntegerOrNull();
            if ( addressStatusId.HasValue )
            {
                ddlAddressStatus.SetValue( addressStatusId.Value.ToString() );
            }

            ddlInvalidReason.BindToEnum<AddressInvalidReason>( true );
            int? addressInvalidReasonId = gfNcoaFilter.GetUserPreference( "Address Invalid Reason" ).AsIntegerOrNull();
            if ( addressInvalidReasonId.HasValue )
            {
                ddlInvalidReason.SetValue( addressInvalidReasonId.Value.ToString() );
            }

            string lastNameFilter = gfNcoaFilter.GetUserPreference( "Last Name" );
            tbLastName.Text = !string.IsNullOrWhiteSpace( lastNameFilter ) ? lastNameFilter : string.Empty;

            string moveDistanceFilter = gfNcoaFilter.GetUserPreference( "Move Distance" );
            nbMoveDistance.Text = moveDistanceFilter.ToString();

        }

        /// <summary>
        /// Shows the view.
        /// </summary>
        protected void ShowView()
        {
            var rockContext = new RockContext();

            int resultCount = Int32.Parse( GetAttributeValue( "ResultCount" ) );
            int pageNumber = 0;

            if ( !String.IsNullOrEmpty( PageParameter( "page" ) ) )
            {
                pageNumber = Int32.Parse( PageParameter( "page" ) );
            }

            var skipCount = pageNumber * resultCount;

            var query = new NcoaHistoryService( rockContext ).Queryable();

            var processed = gfNcoaFilter.GetUserPreference( "Processed" ).ConvertToEnumOrNull<Processed>();
            if ( processed.HasValue )
            {
                query = query.Where( i => i.Processed == processed );
            }
            else
            {
                query = query.Where( i => i.Processed == Processed.NotProcessed || i.Processed == Processed.ManualUpdateRequired );
            }

            var moveDateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( gfNcoaFilter.GetUserPreference( "Move Date" ) );
            if ( moveDateRange.Start.HasValue )
            {
                query = query.Where( e => e.MoveDate.HasValue && e.MoveDate.Value >= moveDateRange.Start.Value );
            }
            if ( moveDateRange.End.HasValue )
            {
                query = query.Where( e => e.MoveDate.HasValue && e.MoveDate.Value < moveDateRange.End.Value );
            }

            var ncoaDateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( gfNcoaFilter.GetUserPreference( "NCOA Processed Date" ) );
            if ( ncoaDateRange.Start.HasValue )
            {
                query = query.Where( e => e.NcoaRunDateTime >= ncoaDateRange.Start.Value );
            }
            if ( ncoaDateRange.End.HasValue )
            {
                query = query.Where( e => e.NcoaRunDateTime < ncoaDateRange.End.Value );
            }

            var moveType = gfNcoaFilter.GetUserPreference( "Move type" ).ConvertToEnumOrNull<MoveType>();
            if ( moveType.HasValue )
            {
                query = query.Where( i => i.MoveType == moveType );
            }

            var addressStatus = gfNcoaFilter.GetUserPreference( "Address Status" ).ConvertToEnumOrNull<AddressStatus>();
            if ( addressStatus.HasValue )
            {
                query = query.Where( i => i.AddressStatus == addressStatus );
            }

            var addressInvalidReason = gfNcoaFilter.GetUserPreference( "Address Invalid Reason" ).ConvertToEnumOrNull<AddressInvalidReason>();
            if ( addressInvalidReason.HasValue )
            {
                query = query.Where( i => i.AddressInvalidReason == addressInvalidReason );
            }

            decimal? moveDistance = gfNcoaFilter.GetUserPreference( "Move Distance" ).AsDecimalOrNull();
            if ( moveDistance.HasValue )
            {
                query = query.Where( i => i.MoveDistance == moveDistance.Value );
            }

            string lastName = gfNcoaFilter.GetUserPreference( "Last Name" );
            if ( !string.IsNullOrWhiteSpace( lastName ) )
            {
                var personAliasQuery = new PersonAliasService( rockContext )
                    .Queryable()
                    .Where( p =>
                        p.Person != null &&
                        p.Person.LastName.Contains( lastName ) )
                    .Select( p => p.Id );
                query = query.Where( i => personAliasQuery.Contains( i.PersonAliasId ) );
            }

            var filteredRecords = query.ToList();

            #region Grouping rows

            var ncoaRows = filteredRecords
                       .Where( a => a.MoveType != MoveType.Individual )
                       .GroupBy( a => new { a.FamilyId, a.MoveType, a.MoveDate } )
                       .Select( a => new NcoaRow
                       {
                           Id = a.Select( b => b.Id ).Max(),
                           FamilyMemberPersonAliasIds = a.Select( b => b.PersonAliasId ).ToList()
                       } ).ToList();

            var ncoaIndividualRows = filteredRecords
                        .Where( a => a.MoveType == MoveType.Individual )
                       .Select( a => new NcoaRow
                       {
                           Id = a.Id,
                           IndividualPersonAliasId = a.PersonAliasId
                       } ).ToList();

            ncoaRows.AddRange( ncoaIndividualRows );

            #endregion

            var pagedNcoaRows = ncoaRows.OrderBy( a => a.Id ).Skip( skipCount ).Take( resultCount + 1 );
            var familyMemberPersonAliasIds = pagedNcoaRows.SelectMany( r => r.FamilyMemberPersonAliasIds ).ToList();
            var individualPersonAliasIds = pagedNcoaRows.Select( r => r.IndividualPersonAliasId ).ToList();

            var people = new PersonAliasService( rockContext )
                .Queryable().AsNoTracking()
                .Where( p =>
                    familyMemberPersonAliasIds.Contains( p.Id ) ||
                    individualPersonAliasIds.Contains( p.Id ) )
                .Select( p => new
                {
                    PersonAliasId = p.Id,
                    Person = p.Person
                } )
                .ToList();

            foreach ( var ncoaRow in pagedNcoaRows )
            {
                ncoaRow.FamilyMembers = people
                    .Where( p => ncoaRow.FamilyMemberPersonAliasIds.Contains( p.PersonAliasId ) )
                    .Select( p => p.Person )
                    .ToList();

                ncoaRow.Individual = people
                    .Where( p => p.PersonAliasId == ncoaRow.IndividualPersonAliasId )
                    .Select( p => p.Person )
                    .FirstOrDefault();

                var ncoaHistoryRecord = filteredRecords.Single( a => a.Id == ncoaRow.Id );

                ncoaRow.OriginalAddress = FormattedAddress( ncoaHistoryRecord.OriginalStreet1, ncoaHistoryRecord.OriginalStreet2,
                                         ncoaHistoryRecord.OriginalCity, ncoaHistoryRecord.OriginalState, ncoaHistoryRecord.OriginalPostalCode )
                                         .ConvertCrLfToHtmlBr();
                ncoaRow.Status = ncoaHistoryRecord.Processed == Processed.Complete ? "Processed" : "Not Processed";
                ncoaRow.StatusCssClass = ncoaHistoryRecord.Processed == Processed.Complete ? "label-success" : "label-warning";
                ncoaRow.ShowButton = false;

                var family = new GroupService( rockContext ).Get( ncoaHistoryRecord.FamilyId );
                var person = ncoaRow.Individual ?? ncoaRow.FamilyMembers.First();
                if ( family == null )
                {
                    family = person.GetFamily( rockContext );
                }

                var personService = new PersonService( rockContext );

                ncoaRow.FamilyName = family.Name;
                ncoaRow.HeadOftheHousehold = personService.GetHeadOfHousehold( person, family );

                if ( ncoaHistoryRecord.MoveType != MoveType.Individual )
                {
                    ncoaRow.FamilyMembers = personService.GetFamilyMembers( family, person.Id, true ).Select( a => a.Person ).ToList();
                }
                else
                {
                    ncoaRow.FamilyMembers = personService.GetFamilyMembers( family, person.Id, false ).Select( a => a.Person ).ToList();
                }

                if ( ncoaHistoryRecord.AddressStatus == AddressStatus.Invalid )
                {
                    ncoaRow.TagLine = "Invalid Address";
                    ncoaRow.TagLineCssClass = "label-warning";

                    if ( ncoaHistoryRecord.Processed != Processed.Complete )
                    {
                        ncoaRow.CommandName = "MarkAddressAsPrevious";
                        ncoaRow.CommandText = "Mark Address As Previous";
                        ncoaRow.ShowButton = true;
                    }
                }

                if ( ncoaHistoryRecord.NcoaType == NcoaType.Month48Move )
                {
                    ncoaRow.TagLine = "48 Month Move";
                    ncoaRow.TagLineCssClass = "label-info";

                    if ( ncoaHistoryRecord.Processed != Processed.Complete )
                    {
                        ncoaRow.CommandName = "MarkAddressAsPrevious";
                        ncoaRow.CommandText = "Mark Address As Previous";
                        ncoaRow.ShowButton = true;
                    }
                }

                if ( ncoaHistoryRecord.NcoaType == NcoaType.Move )
                {
                    ncoaRow.TagLine = ncoaHistoryRecord.MoveType.ConvertToString();
                    ncoaRow.TagLineCssClass = "label-success";
                    ncoaRow.MoveDate = ncoaHistoryRecord.MoveDate;
                    ncoaRow.MoveDistance = ncoaHistoryRecord.MoveDistance;
                    ncoaRow.NewAddress = FormattedAddress( ncoaHistoryRecord.UpdatedStreet1, ncoaHistoryRecord.UpdatedStreet2,
                                           ncoaHistoryRecord.UpdatedCity, ncoaHistoryRecord.UpdatedState, ncoaHistoryRecord.UpdatedPostalCode )
                                           .ConvertCrLfToHtmlBr();
                    if ( ncoaHistoryRecord.Processed != Processed.Complete )
                    {
                        ncoaRow.CommandText = "Mark Processed";
                        ncoaRow.CommandName = "MarkProcessed";
                        ncoaRow.ShowButton = true;
                    }
                }

            }

            rptNcoaResults.DataSource = pagedNcoaRows.Take( resultCount );
            rptNcoaResults.DataBind();


            if ( pagedNcoaRows.Count() > resultCount )
            {
                hlNext.Visible = hlNext.Enabled = true;
                Dictionary<string, string> queryStringNext = new Dictionary<string, string>();
                queryStringNext.Add( "page", ( pageNumber + 1 ).ToString() );
                var pageReferenceNext = new Rock.Web.PageReference( CurrentPageReference.PageId, CurrentPageReference.RouteId, queryStringNext );
                hlNext.NavigateUrl = pageReferenceNext.BuildUrl();
            }
            else
            {
                hlNext.Visible = hlNext.Enabled = false;
            }

            // build prev button
            if ( pageNumber == 0 )
            {
                hlPrev.Visible = hlPrev.Enabled = false;
            }
            else
            {
                hlPrev.Visible = hlPrev.Enabled = true;
                Dictionary<string, string> queryStringPrev = new Dictionary<string, string>();
                queryStringPrev.Add( "page", ( pageNumber - 1 ).ToString() );
                var pageReferencePrev = new Rock.Web.PageReference( CurrentPageReference.PageId, CurrentPageReference.RouteId, queryStringPrev );
                hlPrev.NavigateUrl = pageReferencePrev.BuildUrl();
            }

        }

        private string FormattedAddress( string street1, string street2, string city, string state, string postalCode )
        {
            if ( string.IsNullOrWhiteSpace( street1 ) &&
            string.IsNullOrWhiteSpace( street2 ) &&
            string.IsNullOrWhiteSpace( city ) )
            {
                return string.Empty;
            }

            string result = string.Format( "{0} {1} {2}, {3} {4}",
              street1, street2, city, state, postalCode ).ReplaceWhileExists( "  ", " " );

            // Remove blank lines
            while ( result.Contains( Environment.NewLine + Environment.NewLine ) )
            {
                result = result.Replace( Environment.NewLine + Environment.NewLine, Environment.NewLine );
            }
            while ( result.Contains( "\x0A\x0A" ) )
            {
                result = result.Replace( "\x0A\x0A", "\x0A" );
            }

            if ( string.IsNullOrWhiteSpace( result.Replace( ",", string.Empty ) ) )
            {
                return string.Empty;
            }

            return result;
        }

        #endregion

        #region nested classes

        public class NcoaRow
        {
            public NcoaRow()
            {
                FamilyMembers = new List<Person>();
            }

            public int Id { get; set; }

            public string TagLine { get; set; }

            public string TagLineCssClass { get; set; }

            public DateTime? MoveDate { get; set; }

            public string OriginalAddress { get; set; }

            public string NewAddress { get; set; }

            public decimal? MoveDistance { get; set; }

            public List<int> FamilyMemberPersonAliasIds { get; set; }

            public List<Person> FamilyMembers { get; set; }

            public int IndividualPersonAliasId { get; set; }

            public Person Individual { get; set; }

            public Person HeadOftheHousehold { get; set; }

            public string Status { get; set; }

            public string StatusCssClass { get; set; }

            public string CommandName { get; set; }

            public string CommandText { get; set; }

            public string FamilyName { get; set; }

            public bool ShowButton { get; set; }
        }

        #endregion


    }
}